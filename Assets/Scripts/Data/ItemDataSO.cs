using System.Collections.Generic;
using UnityEngine;

public enum ItemType { Equipment, Token }
public enum TokenType { None, Attack, Defense, Utility, Heal, DirectDamage, ImmediateDefense }
public enum TokenCategory { Enhancement, Combat }
public enum Rarity { Common, Rare, Epic, Legendary }
public enum TriggerType { OnCooldown, OnHit, OnDamaged }

/// <summary>
/// 장비 서브타입 - 방어구 타입별로 다른 부위 슬롯 및 효과 적용
/// </summary>
public enum EquipmentSubtype
{
    None,
    Helmet,
    Chest,
    Gloves,
    Boots,
    Weapon,
    Accessory
}

[System.Serializable]
public struct EquipmentTrigger
{
    public TriggerType Type;
    public float Value;
    public float Cooldown;
}

[CreateAssetMenu(fileName = "Item_", menuName = "Roguelike/Item Data")]
public class ItemDataSO : ScriptableObject
{
    [Header("Universal Properties")]
    public string Id;
    public string DisplayName;
    public ItemType ItemType;
    public Rarity Rarity;
    [Range(1, 4)] public int Width = 1;
    [Range(1, 4)] public int Height = 1;
    public Sprite Icon;

    [Header("Equipment Exclusive (ItemType == Equipment)")]
    /// <summary>장비 서브타입 (헬멧, 가슴, 장갑, 부츠, 무기, 액세서리)</summary>
    public EquipmentSubtype Subtype = EquipmentSubtype.None;
    
    /// <summary>커스텀 셰이프 오프셋 (기본 1x1이 아닌 경우, 로컬 그리드 좌표 리스트)</summary>
    public List<Vector2Int> CustomShapeOffsets = new();
    
    public float BaseCooldown = 3.0f;
    public float BaseDamage = 0f;
    public float BaseShield = 0f;
    public float BaseHeal = 0f;
    
    /// <summary>기본 데미지 감소율 (0~1, 아머 아이템용)</summary>
    [Range(0f, 1f)] public float BaseDamageReduction = 0f;
    
    public List<EquipmentTrigger> Triggers = new();
    public List<TokenType> AcceptableTokens = new();

    [Header("Token Exclusive (ItemType == Token)")]
    public TokenCategory TokenCategory = TokenCategory.Enhancement;
    public TokenType TokenType = TokenType.None;
    public TokenEffectSO TokenEffect;
}