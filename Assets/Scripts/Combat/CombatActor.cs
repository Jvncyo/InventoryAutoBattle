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
    }

    public virtual void AddShield(float amount) => Shield += amount;
    public virtual void Heal(float amount) => CurrentHP = Mathf.Min(MaxHP, CurrentHP + amount);
}