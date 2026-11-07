using UnityEngine;

public abstract class CombatActor : MonoBehaviour
{
    public float MaxHP { get; protected set; }
    public float CurrentHP { get; protected set; }
    public float Shield { get; protected set; }
    public bool IsDead => CurrentHP <= 0f;

    public virtual void TakeDamage(float amount)
    {
        if (Shield > 0)
        {
            float shieldDmg = Mathf.Min(Shield, amount);
            Shield -= shieldDmg;
            amount -= shieldDmg;
        }

        CurrentHP = Mathf.Max(0, CurrentHP - amount);
        
        // 피격 애니메이션 트리거
        EventBus.Publish(new CombatActionEvent(gameObject, CombatActionType.Hit));
    }

    public virtual void AddShield(float amount) => Shield += amount;
    public virtual void Heal(float amount) => CurrentHP = Mathf.Min(MaxHP, CurrentHP + amount);

    /// <summary>
    /// 전투 비주얼(애니메이션 등)을 초기 상태로 리셋합니다.
    /// 새 웨이브 시작 시 또는 전투 재시작 시 호출됩니다.
    /// </summary>
    public virtual void ResetCombatVisuals()
    {
        var animator = GetComponentInChildren<CombatAnimatorController>();
        animator?.ResetToIdle();
    }
}