using System;
using UnityEngine;

public class ItemInstance : MonoBehaviour
{
    public string InstanceId { get; private set; }
    public ItemDataSO Data { get; private set; }
    public ItemContainer CurrentContainer { get; set; }
    
    public int GridX { get; set; }
    public int GridY { get; set; }

    // Runtime Buff Modifiers (Target of Token Effects)
    public float BonusDamage { get; set; }
    public float BonusShield { get; set; }
    public float BonusDamageReduction { get; set; }
    public float CooldownMultiplier { get; set; } = 1.0f;
    public float CooldownTimer { get; set; }

    public void Initialize(ItemDataSO data)
    {
        InstanceId = Guid.NewGuid().ToString();
        Data = data;
        CooldownTimer = data.BaseCooldown;
    }

    public void ResetCombatState()
    {
        CooldownTimer = Data.BaseCooldown * CooldownMultiplier;
    }
}