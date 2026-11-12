using UnityEngine;

[CreateAssetMenu(fileName = "Effect_StatBuff", menuName = "Roguelike/Token Effects/Stat Buff")]
public class StatBuffEffectSO : TokenEffectSO
{
    public float DamageBonus = 0f;
    public float ShieldBonus = 0f;
    public float CooldownDeltaRate = 0f; // e.g. -0.1f = 10% reduction

    public bool CanApply(ItemInstance equipment, out string message)
    {
        if (equipment == null || equipment.Data == null || equipment.Data.ItemType != ItemType.Equipment)
        { message = "Drop on compatible equipment."; return false; }
        float next = Mathf.Max(0.2f, equipment.CooldownMultiplier + CooldownDeltaRate);
        if (DamageBonus == 0 && ShieldBonus == 0 && Mathf.Approximately(next, equipment.CooldownMultiplier))
        { message = "Maximum enhancement reached."; return false; }
        message = $"Damage {DamageBonus:+0.#;-0.#;0} / Shield {ShieldBonus:+0.#;-0.#;0} / Cooldown {equipment.Data.BaseCooldown * equipment.CooldownMultiplier:0.##} -> {equipment.Data.BaseCooldown * next:0.##}s";
        return true;
    }

    public override bool ApplyToEquipment(ItemInstance targetEquipment)
    {
        if (!CanApply(targetEquipment, out _)) return false;
        if (targetEquipment == null || targetEquipment.Data == null || targetEquipment.Data.ItemType != ItemType.Equipment)
            return false;

        float oldMultiplier = targetEquipment.CooldownMultiplier;
        targetEquipment.BonusDamage += DamageBonus;
        targetEquipment.BonusShield += ShieldBonus;
        targetEquipment.CooldownMultiplier = Mathf.Max(0.2f, targetEquipment.CooldownMultiplier + CooldownDeltaRate);
        if (CombatManager.Instance != null && CombatManager.Instance.InCombat && oldMultiplier > 0f)
            targetEquipment.CooldownTimer *= targetEquipment.CooldownMultiplier / oldMultiplier;
        return true;
    }
}