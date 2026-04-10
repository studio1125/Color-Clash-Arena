using System;
using UnityEngine;

[Serializable]
public class EffectData {

    [Header("Effect")]
    [SerializeField] private EffectType effectType;
    [SerializeField] private float effectMultiplier;
    [SerializeField] private Sprite effectIcon;

    public EffectType GetEffectType() => effectType;

    public float GetEffectMultiplier() => effectMultiplier;

    public void AddEffectMultiplier(float multiplier) => effectMultiplier += multiplier;

    public void RemoveEffectMultiplier(float multiplier) => effectMultiplier -= multiplier;

    public Sprite GetEffectIcon() => effectIcon;

}
