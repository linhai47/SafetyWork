using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EffectDatabase", menuName = "Game /Effect Database")]
public class EffectDatabaseSO : ScriptableObject
{
    public List<WeaponEffectSO> allEffects;

    public WeaponEffectSO GetRandomEffect()
    {
        if (allEffects == null || allEffects.Count == 0) return null;
        return allEffects[Random.Range(0, allEffects.Count)];
    }
}