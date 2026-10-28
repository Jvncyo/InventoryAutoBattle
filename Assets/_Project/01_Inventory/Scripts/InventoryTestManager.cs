using UnityEngine;
using UnityEngine.UI;
using InventoryAutoBattle.Core;

namespace InventoryAutoBattle.Inventory
{
    /// <summary>
    /// Dev1_Inventory_Test 씬 전용 테스트 매니저.
    /// 그리드 동작, 회전, 충돌 검사, 빈 슬롯 수 추적, 토큰 충전 및 발동 이벤트를 화면에서 실시간 검증합니다.
    /// </summary>
    public class InventoryTestManager : MonoBehaviour
    {
        [Header("참조 컴포넌트")]
        [SerializeField] private InventoryGrid inventoryGrid;
        [SerializeField] private RectTransform itemStorageArea;
        [SerializeField] private GameObject itemPrefab;

        [Header("테스트용 ItemData ScriptableObjects")]
        [SerializeField] private ItemData testSword;
        [SerializeField] private ItemData testShield;
        [SerializeField] private ItemData testPotion;
        [SerializeField] private ItemData testGreatsword;

        [Header("UI 텍스트 참조")]
        [SerializeField] private Text freeCellText;
        [SerializeField] private Text logText;
        [SerializeField] private Text guideText;

        private void OnEnable()
        {
            GameEvents.OnFreeCellCountChanged += HandleFreeCellCountChanged;
            GameEvents.OnItemTriggered += HandleItemTriggered;
        }

        private void OnDisable()
        {
            GameEvents.OnFreeCellCountChanged -= HandleFreeCellCountChanged;
            GameEvents.OnItemTriggered -= HandleItemTriggered;
        }

        private void Start()
        {
            // 폴더에 SO 에셋이 인스펙터로 연결되지 않은 경우를 위한 폴백 데이터 생성
            EnsureTestData();

            if (guideText != null)
            {
                guideText.text = "<b>[조작 가이드]</b>\n" +
                                 "• <b>좌클릭 드래그</b>: 아이템 이동 및 그리드 배치\n" +
                                 "• <b>드래그 중 R키</b>: 아이템 90도 회전\n" +
                                 "• <b>우클릭</b>: [Mock] 가상 토큰 주입 -> 완충 시 효과 발동";
            }

            // 초기 테스트 아이템 스폰
            SpawnInitialTestItems();
        }

        private void EnsureTestData()
        {
            if (testSword == null)
            {
                testSword = ScriptableObject.CreateInstance<ItemData>();
                testSword.itemId = "item_sword";
                testSword.itemName = "초보자의 검";
                testSword.itemType = ItemType.Weapon;
                testSword.size = new Vector2Int(1, 2);
                testSword.requiredTokenType = TokenType.Attack;
                testSword.requiredTokenCount = 2;
                testSword.effectValue = 15f;
            }

            if (testShield == null)
            {
                testShield = ScriptableObject.CreateInstance<ItemData>();
                testShield.itemId = "item_shield";
                testShield.itemName = "나무 방패";
                testShield.itemType = ItemType.Shield;
                testShield.size = new Vector2Int(2, 2);
                testShield.requiredTokenType = TokenType.Defense;
                testShield.requiredTokenCount = 2;
                testShield.effectValue = 20f;
            }

            if (testPotion == null)
            {
                testPotion = ScriptableObject.CreateInstance<ItemData>();
                testPotion.itemId = "item_potion";
                testPotion.itemName = "체력 물약";
                testPotion.itemType = ItemType.Potion;
                testPotion.size = new Vector2Int(1, 1);
                testPotion.requiredTokenType = TokenType.Heal;
                testPotion.requiredTokenCount = 1;
                testPotion.effectValue = 30f;
            }

            if (testGreatsword == null)
            {
                testGreatsword = ScriptableObject.CreateInstance<ItemData>();
                testGreatsword.itemId = "item_greatsword";
                testGreatsword.itemName = "강철 대검";
                testGreatsword.itemType = ItemType.Greatsword;
                testGreatsword.size = new Vector2Int(2, 3);
                testGreatsword.requiredTokenType = TokenType.Attack;
                testGreatsword.requiredTokenCount = 3;
                testGreatsword.effectValue = 45f;
            }
        }

        private void SpawnInitialTestItems()
        {
            // 인벤토리 초기 배치 (검, 방패, 포션)
            SpawnAndPlaceItem(testSword, new Vector2Int(0, 0));
            SpawnAndPlaceItem(testShield, new Vector2Int(2, 0));
            SpawnAndPlaceItem(testPotion, new Vector2Int(0, 3));
        }

        private void SpawnAndPlaceItem(ItemData data, Vector2Int gridPos)
        {
            GameObject itemObj = CreateItemGameObject(data);
            InventoryItem item = itemObj.GetComponent<InventoryItem>();
            item.Initialize(data, inventoryGrid);

            if (inventoryGrid.CanPlaceItem(item, gridPos, item.CurrentSize))
            {
                inventoryGrid.PlaceItemAt(item, gridPos);
            }
        }

        public GameObject CreateItemGameObject(ItemData data)
        {
            GameObject itemObj;
            if (itemPrefab != null)
            {
                itemObj = Instantiate(itemPrefab);
            }
            else
            {
                // 프리팹이 없을 경우 기본 UI 오브젝트 생성
                itemObj = new GameObject($"Item_{data.itemName}", typeof(RectTransform), typeof(CanvasGroup), typeof(Image), typeof(InventoryItem));
                Image bg = itemObj.GetComponent<Image>();
                bg.color = GetColorByItemType(data.itemType);
                bg.raycastTarget = true;

                // 1. 이름 및 크기 텍스트
                GameObject txtObj = new GameObject("Text_Info", typeof(RectTransform), typeof(Text));
                txtObj.transform.SetParent(itemObj.transform, false);
                Text txt = txtObj.GetComponent<Text>();
                txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                txt.alignment = TextAnchor.MiddleCenter;
                txt.color = Color.white;
                txt.fontSize = 12;
                txt.text = $"{data.itemName}\n({data.size.x}x{data.size.y})";
                txt.raycastTarget = false; // 드래그 레이캐스트 방해 방지

                RectTransform txtRt = txtObj.GetComponent<RectTransform>();
                txtRt.anchorMin = new Vector2(0, 0.35f);
                txtRt.anchorMax = Vector2.one;
                txtRt.offsetMin = Vector2.zero;
                txtRt.offsetMax = Vector2.zero;

                // 2. 토큰 요구량 텍스트
                GameObject tokenTxtObj = new GameObject("Text_Token", typeof(RectTransform), typeof(Text));
                tokenTxtObj.transform.SetParent(itemObj.transform, false);
                Text tokenTxt = tokenTxtObj.GetComponent<Text>();
                tokenTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                tokenTxt.alignment = TextAnchor.MiddleCenter;
                tokenTxt.color = new Color(1f, 0.95f, 0.6f);
                tokenTxt.fontSize = 11;
                tokenTxt.raycastTarget = false; // 드래그 레이캐스트 방해 방지

                RectTransform tokenRt = tokenTxtObj.GetComponent<RectTransform>();
                tokenRt.anchorMin = Vector2.zero;
                tokenRt.anchorMax = new Vector2(1, 0.35f);
                tokenRt.offsetMin = Vector2.zero;
                tokenRt.offsetMax = Vector2.zero;
            }

            return itemObj;
        }

        private Color GetColorByItemType(ItemType type)
        {
            return type switch
            {
                ItemType.Weapon => new Color(0.75f, 0.25f, 0.25f, 0.95f),    // 적갈색
                ItemType.Shield => new Color(0.25f, 0.45f, 0.75f, 0.95f),    // 푸른색
                ItemType.Potion => new Color(0.25f, 0.75f, 0.35f, 0.95f),    // 초록색
                ItemType.Greatsword => new Color(0.85f, 0.5f, 0.1f, 0.95f),  // 주황색
                _ => new Color(0.5f, 0.5f, 0.5f, 0.9f)
            };
        }

        #region 버튼 연동 함수
        public void OnClickSpawnSword()
        {
            SpawnToStorage(testSword);
        }

        public void OnClickSpawnShield()
        {
            SpawnToStorage(testShield);
        }

        public void OnClickSpawnPotion()
        {
            SpawnToStorage(testPotion);
        }

        public void OnClickSpawnGreatsword()
        {
            SpawnToStorage(testGreatsword);
        }

        public void OnClickClearInventory()
        {
            inventoryGrid.ClearAllItems();
            AddLog("인벤토리 전체 비우기 완료");
        }

        private void SpawnToStorage(ItemData data)
        {
            Transform parent = itemStorageArea != null ? itemStorageArea : inventoryGrid.transform.parent;
            GameObject itemObj = CreateItemGameObject(data);
            itemObj.transform.SetParent(parent, false);

            InventoryItem item = itemObj.GetComponent<InventoryItem>();
            item.Initialize(data, inventoryGrid);

            // 보관함 위치 임의 배치
            RectTransform rt = itemObj.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(Random.Range(-50f, 50f), Random.Range(-50f, 50f));

            AddLog($"보관함에 '{data.itemName}' 생성됨. 드래그하여 인벤토리에 넣어보세요.");
        }
        #endregion

        #region 이벤트 수신 핸들러
        private void HandleFreeCellCountChanged(int freeCount, int totalCount)
        {
            if (freeCellText != null)
            {
                freeCellText.text = $"남은 빈 슬롯: <b>{freeCount}</b> / {totalCount}";
            }
        }

        private void HandleItemTriggered(ItemType type, float value)
        {
            string message = $"<color=yellow>[이벤트 발생] {type} 발동! 수치: {value}</color>";
            AddLog(message);
        }

        private void AddLog(string msg)
        {
            Debug.Log($"[Dev1_Test] {msg}");
            if (logText != null)
            {
                logText.text = $"[{System.DateTime.Now:HH:mm:ss}] {msg}\n" + logText.text;
                if (logText.text.Length > 500)
                {
                    logText.text = logText.text.Substring(0, 500);
                }
            }
        }
        #endregion
    }
}
