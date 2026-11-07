using UnityEngine;

public class EnemyCombatActor : CombatActor
{
    private EnemyDataSO _data;
    private float _attackTimer;
    private float _attackMultiplier = 1f;

    public void Initialize(
        EnemyDataSO data,
        float hpMultiplier = 1f,
        float attackMultiplier = 1f,
        float speedMultiplier = 1f)
    {
        _data = data;
        _attackMultiplier = attackMultiplier;
        MaxHP = data.MaxHP * hpMultiplier;
        CurrentHP = MaxHP;
        Shield = 0f;
        _attackTimer = data.AttackInterval / Mathf.Max(0.01f, speedMultiplier);
        
        // 전투 비주얼 리셋 (새 적 스폰 시 애니메이션 상태 초기화)
        ResetCombatVisuals();
    }

    public void Tick(float delta, PlayerCombatActor target)
    {
        _attackTimer -= delta;
        if (_attackTimer <= 0f)
        {
            float damage = _data.AttackPower * _attackMultiplier;
            
            // 적 공격 애니메이션 트리거
            EventBus.Publish(new CombatActionEvent(gameObject, CombatActionType.Attack));
            
            target.TakeDamage(damage);
            _attackTimer = _data.AttackInterval;
            EventBus.Publish(new EnemyAttackedEvent(damage));
        }
    }
}