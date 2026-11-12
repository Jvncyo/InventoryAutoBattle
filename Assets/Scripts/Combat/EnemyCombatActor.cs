using UnityEngine;

public class EnemyCombatActor : CombatActor
{
    protected EnemyDataSO _data;
    protected float _attackTimer;
    protected float _attackMultiplier = 1f;
    protected float _currentCooldown;
    protected float _baseCooldown;
    protected int _currentStageIndex = 1;
    public string EnemyName { get; private set; }

    public void Initialize(
        EnemyDataSO data,
        int stageIndex,
        float hpMultiplier = 1f,
        float attackMultiplier = 1f,
        float speedMultiplier = 1f)
    {
        _data = data;
        _currentStageIndex = stageIndex;
        _attackMultiplier = attackMultiplier;
        EnemyName = data.EnemyName;

        // V5 스케일링 API 사용 (현재 스테이지 인덱스 사용)
        var stats = data.GetLegacyStatsForStage(_currentStageIndex);
        float hp = stats.hp;
        float damage = stats.damage;
        float cooldown = stats.cooldown;
        // Shield는 legacy API에 없으므로 0으로 초기화
        float shield = 0f;

        MaxHP = hp * hpMultiplier;
        CurrentHP = MaxHP;
        Shield = shield * hpMultiplier;
        _baseCooldown = cooldown / Mathf.Max(0.01f, speedMultiplier);
        _currentCooldown = _baseCooldown;
        _attackTimer = _currentCooldown;

        // 전투 비주얼 리셋 (새 적 스폰 시 애니메이션 상태 초기화)
        ResetCombatVisuals();
    }

    protected virtual void Update()
    {
        // 기본 구현: Tick은 외부에서 호출 (CombatManager)
    }

    public void Tick(float delta, PlayerCombatActor target)
    {
        _attackTimer -= delta;
        if (_attackTimer <= 0f)
        {
            // V5 스케일링: 현재 스테이지 인덱스 사용
            var stats = _data.GetLegacyStatsForStage(_currentStageIndex);
            float damage = stats.damage;
            float finalDamage = damage * _attackMultiplier;

            // 적 공격 애니메이션 트리거
            EventBus.Publish(new CombatActionEvent(gameObject, CombatActionType.Attack));

            target.TakeDamage(finalDamage);
            _attackTimer = _currentCooldown;
            EventBus.Publish(new EnemyAttackedEvent(finalDamage));
        }
    }

    /// <summary>
    /// 쿨다운을 강제로 덮어씁니다 (BossCombatActor 2페이즈 진입 시 사용).
    /// </summary>
    public virtual void SetCooldownOverride(float cooldown)
    {
        _baseCooldown = cooldown;
        _currentCooldown = cooldown;
        _attackTimer = Mathf.Min(_attackTimer, _currentCooldown);
    }
}