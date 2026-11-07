using UnityEngine;

public class PlayerCombatActor : CombatActor
{
    [SerializeField] private float startingMaxHP = 100f;

    private void Awake()
    {
        Initialize(startingMaxHP, startingMaxHP);
    }

    public void Initialize(float maxHp, float currentHp)
    {
        MaxHP = maxHp;
        CurrentHP = currentHp;
        Shield = 0f;
    }

    /// <summary>
    /// Restore full HP for a new wave. Called when transitioning to Preparation.
    /// </summary>
    public void ResetForNewWave()
    {
        if (MaxHP <= 0f) MaxHP = 100f;
        CurrentHP = MaxHP;
        Shield = 0f;
    }
}