using UnityEngine;
using InventoryAutoBattle.Core;

namespace InventoryAutoBattle.Inventory
{
    /// <summary>
    /// 게임 시작 시 인벤토리에 기본 장비 아이템들(검, 방패, 물약)을 자동으로 생성하고 배치해주는 헬퍼 컴포넌트.
    /// </summary>
    public class InventoryStarter : MonoBehaviour
    {
        [SerializeField] private InventoryGrid inventoryGrid;
        [SerializeField] private GameObject itemPrefab;

        private void Start()
        {
            if (inventoryGrid == null)
                inventoryGrid = GetComponent<InventoryGrid>();

            if (inventoryGrid != null)
            {
                SpawnStarterItems();
            }
        }

        public void SpawnStarterItems()
        {
            // 1. 초보자의 검 (1x2, 공격 토큰 2개 요구, 공격력 25)
            ItemData sword = ScriptableObject.CreateInstance<ItemData>();
            sword.itemId = "starter_sword";
            sword.itemName = "초보자의 검";
            sword.itemType = ItemType.Weapon;
            sword.size = new Vector2Int(1, 2);
            sword.requiredTokenType = TokenType.Attack;
            sword.requiredTokenCount = 2;
            sword.effectValue = 25f;

            // 2. 나무 방패 (2x2, 방어 토큰 2개 요구, 보호막 20)
            ItemData shield = ScriptableObject.CreateInstance<ItemData>();
            shield.itemId = "starter_shield";
            shield.itemName = "나무 방패";
            shield.itemType = ItemType.Shield;
            shield.size = new Vector2Int(2, 2);
            shield.requiredTokenType = TokenType.Defense;
            shield.requiredTokenCount = 2;
            shield.effectValue = 20f;

            // 3. 체력 물약 (1x1, 회복 토큰 1개 요구, 회복량 25)
            ItemData potion = ScriptableObject.CreateInstance<ItemData>();
            potion.itemId = "starter_potion";
            potion.itemName = "체력 물약";
            potion.itemType = ItemType.Potion;
            potion.size = new Vector2Int(1, 1);
            potion.requiredTokenType = TokenType.Heal;
            potion.requiredTokenCount = 1;
            potion.effectValue = 25f;

            // 슬롯 (0, 0)에 검 배치
            CreateAndPlaceItem(sword, new Vector2Int(0, 0), new Color(0.85f, 0.3f, 0.3f));

            // 슬롯 (2, 0)에 방패 배치
            CreateAndPlaceItem(shield, new Vector2Int(2, 0), new Color(0.3f, 0.5f, 0.85f));

            // 슬롯 (4, 0)에 물약 배치
            CreateAndPlaceItem(potion, new Vector2Int(4, 0), new Color(0.3f, 0.85f, 0.4f));

            // 슬롯 (0, 3)에 보조 검 하나 더 배치
            ItemData dagger = ScriptableObject.CreateInstance<ItemData>();
            dagger.itemId = "starter_dagger";
            dagger.itemName = "단검";
            dagger.itemType = ItemType.Weapon;
            dagger.size = new Vector2Int(1, 1);
            dagger.requiredTokenType = TokenType.Attack;
            dagger.requiredTokenCount = 1;
            dagger.effectValue = 15f;
            CreateAndPlaceItem(dagger, new Vector2Int(0, 3), new Color(0.85f, 0.45f, 0.3f));
        }

        private void CreateAndPlaceItem(ItemData data, Vector2Int gridPos, Color color)
        {
            GameObject itemObj = new GameObject($"Item_{data.itemName}");
            RectTransform rt = itemObj.AddComponent<RectTransform>();
            UnityEngine.UI.Image img = itemObj.AddComponent<UnityEngine.UI.Image>();
            img.color = color;

            InventoryItem item = itemObj.AddComponent<InventoryItem>();
            item.Initialize(data, inventoryGrid);

            if (inventoryGrid.CanPlaceItem(item, gridPos, data.size))
            {
                inventoryGrid.PlaceItemAt(item, gridPos);
            }
            else
            {
                Debug.LogWarning($"[InventoryStarter] {data.itemName}을 ({gridPos.x}, {gridPos.y})에 배치할 수 없습니다.");
            }
        }
    }
}
