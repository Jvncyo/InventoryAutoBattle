using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CombatManager : MonoBehaviour
{
    public static CombatManager Instance { get; private set; }

    [SerializeField] private ItemContainer playerInventory;
    [SerializeField] private PlayerCombatActor playerActor;
    [SerializeField] private EnemyCombatActor enemyActor;

    private bool _inCombat;

    public bool InCombat => _inCombat;
    public PlayerCombatActor PlayerActor => playerActor;
    public EnemyCombatActor EnemyActor => enemyActor;

    public void StopCombat()
    {
        _inCombat = false;
        StopAllCoroutines();
    }

    private void Awake()
    {
        Instance = this;
    }

    public void StartCombat(EnemyDataSO enemyData)
    {
        if (_inCombat || enemyData == null || playerInventory == null ||
            playerActor == null || enemyActor == null)
        {
            Debug.LogError("Cannot start combat: combat references or enemy data are missing.");
            return;
        }

        _inCombat = true;
        var waves = WaveManager.Instance;
        enemyActor.Initialize(
            enemyData,
            waves != null ? waves.CurrentEnemyHPMultiplier : 1f,
            waves != null ? waves.CurrentEnemyAttackMultiplier : 1f,
            waves != null ? waves.CurrentEnemySpeedMultiplier : 1f);

        var equipments = playerInventory.Items
            .Where(i => i.Data.ItemType == ItemType.Equipment)
            .ToList();

        foreach (var eq in equipments)
            eq.ResetCombatState();

        StartCoroutine(CombatTickRoutine(equipments));
        EventBus.Publish(new CombatStartedEvent());
    }

    private IEnumerator CombatTickRoutine(List<ItemInstance> equipments)
    {
        const float tickInterval = 0.1f;
        var wait = new WaitForSeconds(tickInterval);

        while (_inCombat)
        {
            yield return wait;
            if (!_inCombat || ResolveOutcome()) yield break;

            // 1. Process Equipment Cooldowns
            foreach (var eq in equipments)
            {
                eq.CooldownTimer -= tickInterval;
                if (eq.CooldownTimer <= 0f)
                {
                    ExecuteEquipmentTrigger(eq);
                    if (ResolveOutcome()) yield break;
                    eq.CooldownTimer = eq.Data.BaseCooldown * eq.CooldownMultiplier;
                }
            }

            // 2. Process Enemy Attack Tick
            enemyActor.Tick(tickInterval, playerActor);

            ResolveOutcome();
        }
    }

    public bool ResolveOutcome()
    {
        if (!_inCombat || playerActor == null || enemyActor == null) return false;
        if (!playerActor.IsDead && !enemyActor.IsDead) return false;
        _inCombat = false;
        EventBus.Publish(new CombatEndedEvent(!playerActor.IsDead));
        
        // 사망 애니메이션 트리거
        if (playerActor.IsDead)
            EventBus.Publish(new CombatActionEvent(playerActor.gameObject, CombatActionType.Die));
        if (enemyActor.IsDead)
            EventBus.Publish(new CombatActionEvent(enemyActor.gameObject, CombatActionType.Die));
        
        return true;
    }

    private void ExecuteEquipmentTrigger(ItemInstance eq)
    {
        float totalDamage = eq.Data.BaseDamage + eq.BonusDamage;
        if (totalDamage > 0f) enemyActor.TakeDamage(totalDamage);

        float totalShield = eq.Data.BaseShield + eq.BonusShield;
        if (totalShield > 0f) playerActor.AddShield(totalShield);

        if (eq.Data.BaseHeal > 0f) playerActor.Heal(eq.Data.BaseHeal);

        EventBus.Publish(new EquipmentTriggeredEvent(eq));
        
        // 장비 공격 애니메이션 트리거
        EventBus.Publish(new CombatActionEvent(playerActor.gameObject, CombatActionType.Attack));
    }
}