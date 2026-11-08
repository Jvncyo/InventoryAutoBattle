using UnityEngine;

public class EnemyCombatActor : CombatActor
{
    protected EnemyDataSO _data;
    protected float _attackTimer;
    protected float _attackMultiplier = 1f;
    protected float _currentCooldown;
    protected float _baseCooldown;

    public void Initialize(
        EnemyDataSO data,
        float hpMultiplier = 1f,
        float attackMultiplier = 1f,
        float speedMultiplier = 1f)
    {
        _data = data;
        _attackMultiplier = attackMultiplier;

        // V5 스케일링 API 사용 (Legacy 필드 호환 보장)
        var stats = data.GetLegacyStatsForStage(1);
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
            // V5 스케일링: AttackPower는 GetLegacyStatsForStage(1) 사용
            var stats = _data.GetLegacyStatsForStage(1);
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