using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CombatManager : MonoBehaviour
{
    public static CombatManager Instance { get; private set; }

    [SerializeField] private ItemContainer playerInventory;
    [SerializeField] private PlayerCombatActor playerActor;
    [SerializeField] private Transform enemyRoot;

    private EnemyCombatActor _enemyActor;
    private StageProgressionManager _stageManager;
    private bool _inCombat;
    private List<ItemInstance> _currentEquipments;

    public bool InCombat => _inCombat;
    public PlayerCombatActor PlayerActor => playerActor;
    public EnemyCombatActor EnemyActor => _enemyActor;

    private void Awake()
    {
        Instance = this;
        _stageManager = StageProgressionManager.Instance;

        // enemyRoot가 인스pector에 할당되지 않은 경우 씬에서 자동 탐색 ( Awake 순서 문제 대비 )
        if (enemyRoot == null)
        {
            var found = GameObject.Find("EnemyRoot");
            if (found != null) enemyRoot = found.transform;
        }

        // OnDamaged 트리거 구독
        EventBus.Subscribe<EquipmentDamagedTriggerEvent>(OnEquipmentDamagedTrigger);
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<EquipmentDamagedTriggerEvent>(OnEquipmentDamagedTrigger);
    }

    public void StartCombat(EnemyDataSO enemyData)
    {
        // StageProgressionManager 인스턴스를 안전하게 해결 (Awake 순서 문제 대비)
        var stageManager = StageProgressionManager.Instance
            ?? FindAnyObjectByType<StageProgressionManager>();
        if (stageManager != null) _stageManager = stageManager;

        // GameManager의 인베토리를 우선 사용, 없으면 시리얼라이즈된 필드 사용
        var inventory = GameManager.Instance != null ? GameManager.Instance.PlayerInventory : playerInventory;

        if (_inCombat || enemyData == null || inventory == null ||
            playerActor == null || enemyRoot == null)
        {
            Debug.LogError("Cannot start combat: combat references or enemy data are missing.");
            return;
        }

        _inCombat = true;

        // 적 스폰 (CombatEntityFactory 사용)
        int currentStage = stageManager != null ? stageManager.CurrentStage : 1;
        
        _enemyActor = CombatEntityFactory.SpawnEnemy(
            enemyData,
            currentStage,
            enemyRoot,
            hpMultiplier: 1f,
            attackMultiplier: 1f,
            speedMultiplier: 1f);

        if (_enemyActor == null)
        {
            Debug.LogError("[CombatManager] Failed to spawn enemy actor!");
            StopCombat();
            return;
        }

        // 적 스폰 완료 후 토큰 타겟 재할당 (EnemyHpBar용)
        var autoAssign = FindAnyObjectByType<AutoAssignCombatTokenTargets>();
        autoAssign?.AssignTargets();

        _currentEquipments = inventory.Items
            .Where(i => i.Data.ItemType == ItemType.Equipment)
            .ToList();

        foreach (var eq in _currentEquipments)
        {
            eq.ResetCombatState();
            // ShieldInitialOnly=true 장비는 전투 시작 시 1회 실드 부여
            if (eq.Data != null && eq.Data.ShieldInitialOnly && eq.Data.BaseShield > 0f)
            {
                float initShield = eq.Data.BaseShield + eq.BonusShield;
                if (initShield > 0f) playerActor?.AddShield(initShield);
            }
        }

        StartCoroutine(CombatTickRoutine(_currentEquipments));
        EventBus.Publish(new CombatStartedEvent());
    }

    private void OnEquipmentDamagedTrigger(EquipmentDamagedTriggerEvent evt)
    {
        if (!_inCombat || _currentEquipments == null) return;
        if (evt.Equipment == null) return;
        
        // 쿨다운이 끝난 장비만 트리거 발동
        if (evt.Equipment.CooldownTimer <= 0f)
        {
            ExecuteEquipmentTrigger(evt.Equipment);
            evt.Equipment.CooldownTimer = evt.Equipment.Data.BaseCooldown * evt.Equipment.CooldownMultiplier;
        }
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
                // Skip destroyed or null equipment
                if (eq == null || eq.Equals(null) || !eq || eq.gameObject == null) continue;
                
                eq.CooldownTimer -= tickInterval;
                if (eq.CooldownTimer <= 0f)
                {
                    ExecuteEquipmentTrigger(eq);
                    if (ResolveOutcome()) yield break;
                    eq.CooldownTimer = eq.Data.BaseCooldown * eq.CooldownMultiplier;
                }
            }

            // 2. Process Enemy Attack Tick
            if (_enemyActor != null)
                _enemyActor.Tick(tickInterval, playerActor);

            ResolveOutcome();
        }
    }

    public void StopCombat()
    {
        _inCombat = false;
        StopAllCoroutines();
        
        // 스폰된 적 오브젝트 정리
        if (_enemyActor != null)
        {
            Destroy(_enemyActor.gameObject);
            _enemyActor = null;
        }
    }

    public bool ResolveOutcome()
    {
        if (!_inCombat || playerActor == null || _enemyActor == null) return false;
        if (!playerActor.IsDead && !_enemyActor.IsDead) return false;

        // Cache references before publishing events (handlers may null out _enemyActor)
        var enemyActor = _enemyActor;
        var isPlayerDead = playerActor.IsDead;
        var isEnemyDead = enemyActor.IsDead;

        _inCombat = false;
        EventBus.Publish(new CombatEndedEvent(!isPlayerDead));

        // 사망 애니메이션 트리거 - 캐시된 참조 사용
        if (isPlayerDead && playerActor.gameObject != null)
            EventBus.Publish(new CombatActionEvent(playerActor.gameObject, CombatActionType.Die));
        if (isEnemyDead && enemyActor != null && enemyActor.gameObject != null)
            EventBus.Publish(new CombatActionEvent(enemyActor.gameObject, CombatActionType.Die));

        return true;
    }

    private void ExecuteEquipmentTrigger(ItemInstance eq)
    {
        float totalDamage = eq.Data.BaseDamage + eq.BonusDamage;
        if (totalDamage > 0f && _enemyActor != null) _enemyActor.TakeDamage(totalDamage);

        float totalShield = eq.Data.ShieldInitialOnly ? 0f : eq.Data.BaseShield + eq.BonusShield;
        if (totalShield > 0f) playerActor?.AddShield(totalShield);

        if (eq.Data.BaseHeal > 0f) playerActor?.Heal(eq.Data.BaseHeal);

        EventBus.Publish(new EquipmentTriggeredEvent(eq));
        
        // 장비 공격 애니메이션 트리거 - playerActor와 gameObject 모두 체크
        if (playerActor != null && playerActor.gameObject != null && playerActor.gameObject.activeInHierarchy)
            EventBus.Publish(new CombatActionEvent(playerActor.gameObject, CombatActionType.Attack));
    }
}