using System.Collections.Generic;
using UnityEngine;

public enum EnemyArchetype { Standard, Tank, Rusher, Disrupter }

[CreateAssetMenu(fileName = "Enemy_", menuName = "Roguelike/Enemy Data")]
public class EnemyDataSO : ScriptableObject
{
    public string Id;
    public string EnemyName;

    [Header("Legacy Stats (used when Base* fields are 0)")]
    [Min(0)] public float MaxHP = 100f;
    [Min(0)] public float AttackPower = 8f;
    [Min(0.01f)] public float AttackInterval = 2.0f;
    public Sprite Icon;

    [Header("V5 Archetype & Visual")]
    public EnemyArchetype Archetype = EnemyArchetype.Standard;
    public bool IsBoss;
    [Tooltip("런타임에 자식으로 인스턴스화되는 적 비주얼 프리팹 (SpriteRenderer + CombatAnimatorController). null이면 씬의 EnemyEntity 사용")]
    public GameObject visualPrefab;

    [Header("V5 Stage Scaling Base Stats (Stage 1 기준, 0이면 Legacy 필드 사용)")]
    [Min(0)] public float BaseMaxHP;
    [Min(0)] public float BaseDamage;
    [Min(0.01f)] public float BaseCooldown;
    [Min(0)] public float BaseShield;

    [Header("Boss / Special Triggers")]
    public List<EquipmentTrigger> SpecialTriggers = new();

    /// <summary>
    /// Stage 1 기준 실효 스탯. Base* 필드가 0이면 Legacy 필드(MaxHP/AttackPower/AttackInterval)를 사용합니다.
    /// </summary>
    public (float hp, float damage, float cooldown, float shield) GetLevel1Stats()
    {
        float hp = BaseMaxHP > 0f ? BaseMaxHP : MaxHP;
        float damage = BaseDamage > 0f ? BaseDamage : AttackPower;
        float cooldown = BaseCooldown > 0f ? BaseCooldown : AttackInterval;
        float shield = BaseShield;
        return (hp, damage, cooldown, shield);
    }

    /// <summary>
    /// 결정론적 무한 스케일링 공식 (V5 Spec 4.1).
    /// - HP: Base * (1 + 0.15*(stage-1))^1.12
    /// - Dmg: Base * (1 + 0.10*(stage-1))^1.08
    /// - CD: max(0.8s, Base * 0.97^(stage-1))
    /// - Shield: Base * (1 + 0.12*(stage-1))
    /// - 보스: HP x2.5, Dmg x1.3
    /// </summary>
    public (float hp, float damage, float cooldown, float shield) CalculateStatsForStage(int stage)
    {
        var (baseHp, baseDmg, baseCd, baseShield) = GetLevel1Stats();
        float stageFactor = Mathf.Max(0, stage - 1);

        float scaledHP = baseHp * Mathf.Pow(1.0f + 0.15f * stageFactor, 1.12f);
        float scaledDmg = baseDmg * Mathf.Pow(1.0f + 0.10f * stageFactor, 1.08f);
        float scaledCd = Mathf.Max(0.8f, baseCd * Mathf.Pow(0.97f, stageFactor));
        float scaledShield = baseShield > 0f ? baseShield * (1.0f + 0.12f * stageFactor) : 0f;

        // if (IsBoss)
        // {
        //     scaledHP *= 2.5f;
        //     scaledDmg *= 1.3f;
        // }

        return (scaledHP, scaledDmg, scaledCd, scaledShield);
    }

    /// <summary>
    /// Legacy 접근자: 스테이지 1 스탯을 반환. 기존 EnemyCombatActor 호환용.
    /// </summary>
    public (float hp, float damage, float cooldown) GetLegacyStatsForStage(int stage)
    {
        var (hp, damage, cooldown, _) = CalculateStatsForStage(stage);
        return (hp, damage, cooldown);
    }
}
