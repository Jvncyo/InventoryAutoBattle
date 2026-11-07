using UnityEngine;

public enum CombatTokenAction { Damage, Heal, Shield }

[CreateAssetMenu(fileName = "Effect_Combat", menuName = "Roguelike/Token Effects/Combat")]
public class CombatTokenEffectSO : TokenEffectSO
{
    public CombatTokenAction Action;
    [Min(0)] public float Amount = 10f;

    public override bool ApplyToEquipment(ItemInstance targetEquipment) => false;

    public bool CanApply(CombatActor actor, out string message)
    {
        if (actor == null || actor.IsDead || Amount <= 0 || float.IsNaN(Amount) || float.IsInfinity(Amount))
        { message = "No valid living target."; return false; }
        var combat = CombatManager.Instance;
        if (combat == null || !combat.InCombat || combat.PlayerActor == null || combat.EnemyActor == null
            || combat.PlayerActor.IsDead || combat.EnemyActor.IsDead)
        { message = "Use during an active battle."; return false; }
        if (Action == CombatTokenAction.Damage ? actor != combat.EnemyActor : actor != combat.PlayerActor)
        { message = "Wrong target."; return false; }
        if (Action == CombatTokenAction.Heal && actor.CurrentHP >= actor.MaxHP)
        { message = "HP is already full."; return false; }
        float value = Action == CombatTokenAction.Heal ? Mathf.Min(Amount, actor.MaxHP - actor.CurrentHP) : Amount;
        message = $"{Action} +{value:0.#}";
        return true;
    }

    public bool ApplyToActor(CombatActor actor)
    {
        if (!CanApply(actor, out _)) return false;
        switch (Action)
        {
            case CombatTokenAction.Damage: actor.TakeDamage(Amount); break;
            case CombatTokenAction.Heal: actor.Heal(Amount); break;
            case CombatTokenAction.Shield: actor.AddShield(Amount); break;
        }
        return true;
    }
}
