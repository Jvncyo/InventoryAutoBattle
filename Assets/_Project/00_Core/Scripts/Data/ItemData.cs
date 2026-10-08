using UnityEngine;

namespace InventoryAutoBattle.Core
{
    [CreateAssetMenu(fileName = "NewItemData", menuName = "InventoryAutoBattle/Item Data")]
    public class ItemData : ScriptableObject
    {
        [Header("기본 정보")]
        public string itemId;
        public string itemName;
        [TextArea] public string description;
        public ItemType itemType;
        public Sprite icon;

        [Header("그리드 점유 (너비 x 높이)")]
        public Vector2Int size = new Vector2Int(1, 1);

        [Header("요구 토큰 및 발동 효과")]
        public TokenType requiredTokenType = TokenType.Attack;
        public int requiredTokenCount = 1;
        public float effectValue = 10f; // 공격력, 방어율(%), 회복량 등
    }
}
