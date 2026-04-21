import asyncio
import json
from dataclasses import dataclass
from datetime import datetime, timezone
from typing import Any, Dict, Optional, Set

import websockets
from websockets.exceptions import ConnectionClosed


HOST = "0.0.0.0"
PORT = 8765
MAX_JUMP_COUNT = 2


@dataclass
class ClientSession:
    client_id: Optional[str] = None
    room_id: Optional[str] = None

    last_seq: int = -1
    accepted_state: str = "Grounded"
    accepted_grounded: bool = True
    accepted_jump_count: int = 0


class RelayServer:
    def __init__(self, host: str = HOST, port: int = PORT) -> None:
        self.host = host
        self.port = port
        self.sessions: Dict[Any, ClientSession] = {}
        self.rooms: Dict[str, Set[Any]] = {}
        self.tick: int = 0

    async def run(self) -> None:
        print("=" * 72)
        print(f"[SERVER] WebSocket 游戏服务启动: ws://{self.host}:{self.port}")
        print("[SERVER] 模式: 轻量规则状态机（路线 B）")
        print("=" * 72)

        async with websockets.serve(self.handle_client, self.host, self.port):
            await asyncio.Future()

    async def handle_client(self, websocket: Any) -> None:
        remote = websocket.remote_address
        self.sessions[websocket] = ClientSession()
        print(f"[CONNECT] 新连接: remote={remote} | 当前连接数={len(self.sessions)}")

        try:
            async for raw_message in websocket:
                await self.handle_message(websocket, raw_message)
        except ConnectionClosed as close_info:
            print(f"[CLOSED ] remote={remote} | code={close_info.code} | reason={close_info.reason}")
        finally:
            await self.cleanup_client(websocket, reason="disconnect")

    async def handle_message(self, websocket: Any, raw_message: str) -> None:
        try:
            data = json.loads(raw_message)
        except json.JSONDecodeError:
            await self.send_error(websocket, "无效 JSON：请发送合法的 JSON 字符串")
            return

        msg_type = str(data.get("type", "")).strip()
        if not msg_type:
            await self.send_error(websocket, "缺少字段 type")
            return

        if msg_type == "JOIN_ROOM":
            await self.handle_join_room(websocket, data)
            return
        if msg_type == "INPUT":
            await self.handle_input(websocket, data)
            return
        if msg_type == "CHAT":
            await self.handle_chat(websocket, data)
            return
        if msg_type == "LEAVE_ROOM":
            await self.handle_leave_room(websocket, data)
            return

        await self.send_error(websocket, f"未知消息类型: {msg_type}")

    async def handle_join_room(self, websocket: Any, data: Dict[str, Any]) -> None:
        client_id = str(data.get("clientId", "")).strip()
        room_id = str(data.get("roomId", "")).strip()

        if not client_id or not room_id:
            await self.send_error(websocket, "JOIN_ROOM 缺少 clientId 或 roomId")
            return

        session = self.sessions.get(websocket)
        if session is None:
            await self.send_error(websocket, "服务端未找到该连接的会话")
            return

        if session.room_id:
            self.remove_from_room(websocket, session.room_id)

        session.client_id = client_id
        session.room_id = room_id
        session.last_seq = -1
        session.accepted_state = "Grounded"
        session.accepted_grounded = True
        session.accepted_jump_count = 0

        self.rooms.setdefault(room_id, set()).add(websocket)

        ack = {
            "type": "SERVER_BROADCAST",
            "roomId": room_id,
            "fromClientId": "SERVER",
            "text": f"{client_id} 已加入房间 {room_id}",
            "timestamp": self.utc_now_iso(),
        }
        await self.send_json(websocket, ack)
        await self.send_snapshot(websocket, session, "")

    async def handle_input(self, websocket: Any, data: Dict[str, Any]) -> None:
        session = self.sessions.get(websocket)
        if session is None or not session.room_id or not session.client_id:
            await self.send_error(websocket, "请先 JOIN_ROOM，再发送 INPUT")
            return

        payload_raw = data.get("payload", "")
        if not payload_raw:
            await self.send_error(websocket, "INPUT 缺少 payload")
            return

        try:
            cmd = json.loads(payload_raw)
        except json.JSONDecodeError:
            await self.send_error(websocket, "INPUT payload 不是合法 JSON")
            return

        seq = int(cmd.get("seq", 0))
        jump_pressed = bool(cmd.get("jumpPressed", False))
        client_state = str(cmd.get("clientState", "Unknown"))
        client_grounded = bool(cmd.get("clientGrounded", False))
        client_jump_count = int(cmd.get("clientJumpCount", 0))

        session.last_seq = seq
        reject_reason = ""

        # 先根据客户端当前“已落地”摘要，重置服务器接受态
        if client_grounded:
            session.accepted_grounded = True
            session.accepted_jump_count = 0
            session.accepted_state = "Grounded"
        else:
            session.accepted_grounded = False
            if session.accepted_jump_count == 0 and client_jump_count > 0:
                session.accepted_jump_count = min(client_jump_count, MAX_JUMP_COUNT)
            session.accepted_state = client_state or "Airborne"

        # 规则裁决：跳跃 / 二段跳
        if jump_pressed:
            if session.accepted_grounded:
                session.accepted_grounded = False
                session.accepted_jump_count = 1
                session.accepted_state = "Jump"
            elif session.accepted_jump_count < MAX_JUMP_COUNT:
                session.accepted_jump_count += 1
                session.accepted_state = "Jump"
            else:
                reject_reason = "超过最大跳跃次数"

        self.tick += 1

        print(
            f"[INPUT] client={session.client_id} seq={seq} "
            f"clientState={client_state} grounded={client_grounded} "
            f"clientJumpCount={client_jump_count} jumpPressed={jump_pressed} -> "
            f"acceptedState={session.accepted_state} acceptedGrounded={session.accepted_grounded} "
            f"acceptedJumpCount={session.accepted_jump_count} reject={reject_reason}"
        )

        await self.send_snapshot(websocket, session, reject_reason)

    async def send_snapshot(self, websocket: Any, session: ClientSession, reject_reason: str) -> None:
        snapshot = {
            "tick": self.tick,
            "lastProcessedSeq": session.last_seq,
            "acceptedState": session.accepted_state,
            "acceptedGrounded": session.accepted_grounded,
            "acceptedJumpCount": session.accepted_jump_count,
            "rejectReason": reject_reason,
        }
        response = {
            "type": "SNAPSHOT",
            "roomId": session.room_id,
            "clientId": session.client_id,
            "payload": json.dumps(snapshot, ensure_ascii=False),
        }
        await self.send_json(websocket, response)

    async def handle_chat(self, websocket: Any, data: Dict[str, Any]) -> None:
        session = self.sessions.get(websocket)
        if session is None or not session.room_id or not session.client_id:
            await self.send_error(websocket, "请先 JOIN_ROOM，再发送 CHAT")
            return

        text = str(data.get("text", "")).strip()
        if not text:
            await self.send_error(websocket, "CHAT 缺少 text")
            return

        payload = {
            "type": "SERVER_BROADCAST",
            "roomId": session.room_id,
            "fromClientId": session.client_id,
            "text": text,
            "timestamp": self.utc_now_iso(),
        }

        for peer in self.rooms.get(session.room_id, set()):
            if peer is not websocket:
                await self.send_json(peer, payload)

    async def handle_leave_room(self, websocket: Any, data: Dict[str, Any]) -> None:
        session = self.sessions.get(websocket)
        if session is None or not session.room_id:
            await self.send_error(websocket, "当前连接尚未加入任何房间")
            return

        room_id = session.room_id
        self.remove_from_room(websocket, room_id)
        session.room_id = None

    async def cleanup_client(self, websocket: Any, reason: str) -> None:
        session = self.sessions.pop(websocket, None)
        if session is None:
            return
        if session.room_id:
            self.remove_from_room(websocket, session.room_id)

    def remove_from_room(self, websocket: Any, room_id: str) -> None:
        members = self.rooms.get(room_id)
        if not members:
            return
        members.discard(websocket)
        if not members:
            self.rooms.pop(room_id, None)

    async def send_error(self, websocket: Any, error_message: str) -> None:
        await self.send_json(websocket, {"type": "ERROR", "error": error_message})

    async def send_json(self, websocket: Any, payload: Dict[str, Any]) -> None:
        try:
            await websocket.send(json.dumps(payload, ensure_ascii=False))
        except ConnectionClosed:
            pass

    @staticmethod
    def utc_now_iso() -> str:
        return datetime.now(timezone.utc).isoformat()


def main() -> None:
    server = RelayServer()
    try:
        asyncio.run(server.run())
    except KeyboardInterrupt:
        print("\n[SERVER] 收到 Ctrl+C，服务已停止")


if __name__ == "__main__":
    main()