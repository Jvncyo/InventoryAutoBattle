using UnityEngine;

/// <summary>
/// 통합 보상 모달에서 표시되는 단일 선택지.
/// 타입에 따라 장비 획득(Equipment) 또는 장비 강화(Enhancement)를 나타낸다.
/// </summary>
public enum RewardOptionType { Equipment, Enhancement }

public class UnifiedRewardOption
{
    public RewardOptionType Type;
    public ItemDataSO EquipmentData;        // Type == Equipment: 새로 받을 장비 데이터
    public ItemInstance TargetEquipment;    // Type == Enhancement: 강화할 기존 장비 인스턴스
    public StatBuffEffectSO BuffEffect;     // Type == Enhancement: 적용할 버프 이펙트
    public string DisplayText;              // UI 표시용 텍스트
    public Sprite Icon;                     // 표시용 아이콘

    // 장비 획득 옵션 생성
    public static UnifiedRewardOption CreateEquipment(ItemDataSO data)
    {
        if (data == null) return null;
        var bonuses = new System.Collections.Generic.List<string>();
        if (data.BaseDamage != 0f) bonuses.Add($"공격력 {data.BaseDamage:+0.#;-0.#;0}");
        if (data.BaseShield != 0f) bonuses.Add($"실드 {data.BaseShield:+0.#;-0.#;0}");
        if (data.BaseHeal != 0f) bonuses.Add($"치유 {data.BaseHeal:+0.#;-0.#;0}");
        if (data.BaseDamageReduction > 0f) bonuses.Add($"데미지감소 {data.BaseDamageReduction * 100f:0.#}%");
        string statText = bonuses.Count > 0 ? string.Join(" / ", bonuses) : "기본 장비";

        return new UnifiedRewardOption
        {
            Type = RewardOptionType.Equipment,
            EquipmentData = data,
            DisplayText = $"[신규 획득] {data.DisplayName}\n{data.Width}x{data.Height}\n{statText}",
            Icon = data.Icon
        };
    }

    // 장비 강화 옵션 생성
    public static UnifiedRewardOption CreateEnhancement(ItemInstance target, StatBuffEffectSO buff)
    {
        if (target == null || buff == null) return null;
        var bonuses = new System.Collections.Generic.List<string>();
        if (buff.DamageBonus != 0f) bonuses.Add($"공격력 {buff.DamageBonus:+0.#;-0.#;0}");
        if (buff.ShieldBonus != 0f) bonuses.Add($"실드 {buff.ShieldBonus:+0.#;-0.#;0}");
        if (buff.CooldownDeltaRate != 0f) bonuses.Add($"쿨다운 {buff.CooldownDeltaRate * 100f:+0.#;-0.#;0}%");
        string buffText = bonuses.Count > 0 ? string.Join(" / ", bonuses) : "강화";

        return new UnifiedRewardOption
        {
            Type = RewardOptionType.Enhancement,
            TargetEquipment = target,
            BuffEffect = buff,
            DisplayText = $"[강화] {target.Data.DisplayName} → {buffText}",
            Icon = target.Data.Icon
        };
    }

    // 선택지 적용 실행
    public bool Apply()
    {
        if (Type == RewardOptionType.Equipment)
        {
            // 장비 획득은 인벤토리 배치 로직에서 처리 (여기선 true 반환만)
            return EquipmentData != null;
        }
        else // Enhancement
        {
            if (TargetEquipment == null || BuffEffect == null) return false;
            return BuffEffect.ApplyToEquipment(TargetEquipment);
        }
    }
}