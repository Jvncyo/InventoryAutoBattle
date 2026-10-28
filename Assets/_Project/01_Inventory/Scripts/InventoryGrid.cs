using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using InventoryAutoBattle.Core;

namespace InventoryAutoBattle.Inventory
{
    /// <summary>
    /// 6x6 그리드 인벤토리의 데이터 및 UI 배치를 총괄하는 시스템 매니저.
    /// 그리드 충돌 판정, 배치, 자유 공간 계산 및 GameEvents 연동을 담당합니다.
    /// </summary>
    public class InventoryGrid : MonoBehaviour
    {
        public const int GridWidth = 6;
        public const int GridHeight = 6;
        public const int TotalCells = GridWidth * GridHeight;

        [Header("그리드 레이아웃 설정")]
        [SerializeField] private float cellSize = 70f;
        [SerializeField] private float cellSpacing = 6f;
        [SerializeField] private RectTransform slotsContainer;
        [SerializeField] private RectTransform itemsContainer;
        [SerializeField] private GameObject slotPrefab;

        public float CellSize => cellSize;
        public float CellSpacing => cellSpacing;

        private InventorySlot[,] slots = new InventorySlot[GridWidth, GridHeight];
        private List<InventoryItem> placedItems = new List<InventoryItem>();

        private void Awake()
        {
            if (slotsContainer == null)
                slotsContainer = transform as RectTransform;

            if (itemsContainer == null)
                itemsContainer = slotsContainer;

            InitializeGrid();
        }

        private void OnEnable()
        {
            GameEvents.OnTokenApplied += HandleTokenApplied;
        }

        private void OnDisable()
        {
            GameEvents.OnTokenApplied -= HandleTokenApplied;
        }

        private void Start()
        {
            UpdateFreeCellCount();
        }

        /// <summary>
        /// 6x6 슬롯 자동 생성 및 초기화
        /// </summary>
        public void InitializeGrid()
        {
            // 기존 생성된 슬롯이 있다면 수집 또는 새로 생성
            InventorySlot[] existingSlots = slotsContainer.GetComponentsInChildren<InventorySlot>();
            if (existingSlots != null && existingSlots.Length == TotalCells)
            {
                foreach (var slot in existingSlots)
                {
                    slots[slot.GridPosition.x, slot.GridPosition.y] = slot;
                }
                return;
            }

            // 슬롯 생성
            for (int y = 0; y < GridHeight; y++)
            {
                for (int x = 0; x < GridWidth; x++)
                {
                    GameObject slotObj;
                    if (slotPrefab != null)
                    {
                        slotObj = Instantiate(slotPrefab, slotsContainer);
                    }
                    else
                    {
                        slotObj = CreateDefaultSlotObject(x, y);
                    }

                    InventorySlot slot = slotObj.GetComponent<InventorySlot>();
                    if (slot == null)
                        slot = slotObj.AddComponent<InventorySlot>();

                    slot.Initialize(x, y);
                    slots[x, y] = slot;

                    // 슬롯 로컬 위치 배치 (좌상단 (0,0) 기준)
                    RectTransform rt = slotObj.GetComponent<RectTransform>();
                    rt.sizeDelta = new Vector2(cellSize, cellSize);
                    rt.anchorMin = new Vector2(0, 1);
                    rt.anchorMax = new Vector2(0, 1);
                    rt.pivot = new Vector2(0, 1);
                    rt.anchoredPosition = GetSlotAnchoredPosition(x, y);
                }
            }
        }

        private GameObject CreateDefaultSlotObject(int x, int y)
        {
            GameObject obj = new GameObject($"Slot_{x}_{y}", typeof(RectTransform), typeof(Image), typeof(InventorySlot));
            obj.transform.SetParent(slotsContainer, false);
            Image img = obj.GetComponent<Image>();
            img.color = new Color(0.18f, 0.2f, 0.24f, 0.9f);
            return obj;
        }

        public Vector2 GetSlotAnchoredPosition(int x, int y)
        {
            float posX = x * (cellSize + cellSpacing);
            float posY = -y * (cellSize + cellSpacing);
            return new Vector2(posX, posY);
        }

        #region 충돌 검사 및 배치 로직
        /// <summary>
        /// 지정한 그리드 좌표에 해당 크기의 아이템 배치가 가능한지 검사
        /// </summary>
        public bool CanPlaceItem(InventoryItem item, Vector2Int startPos, Vector2Int size)
        {
            // 경계선 검사 (6x6 밖으로 벗어나는지)
            if (startPos.x < 0 || startPos.y < 0) return false;
            if (startPos.x + size.x > GridWidth || startPos.y + size.y > GridHeight) return false;

            // 이미 점유된 슬롯 충돌 검사
            for (int y = 0; y < size.y; y++)
            {
                for (int x = 0; x < size.x; x++)
                {
                    int checkX = startPos.x + x;
                    int checkY = startPos.y + y;
                    InventorySlot slot = slots[checkX, checkY];

                    if (slot.IsOccupied && slot.OccupyingItem != item)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// 월드 마우스 좌표를 인벤토리 그리드 좌표로 변환하여 배치 시도
        /// </summary>
        public bool TryPlaceItemAtWorldPos(InventoryItem item, Vector3 worldPos)
        {
            Vector2Int gridPos = WorldToGridPosition(item, worldPos);
            Vector2Int size = item.CurrentSize;

            if (CanPlaceItem(item, gridPos, size))
            {
                PlaceItemAt(item, gridPos);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 특정 그리드 좌표에 아이템 확정 배치
        /// </summary>
        public void PlaceItemAt(InventoryItem item, Vector2Int startPos)
        {
            Vector2Int size = item.CurrentSize;

            // 기존 점유 슬롯 해제
            TemporaryClearItem(item);

            // 새 슬롯 점유 등록
            for (int y = 0; y < size.y; y++)
            {
                for (int x = 0; x < size.x; x++)
                {
                    int targetX = startPos.x + x;
                    int targetY = startPos.y + y;
                    slots[targetX, targetY].IsOccupied = true;
                    slots[targetX, targetY].OccupyingItem = item;
                }
            }

            item.SetGrid(this);
            item.SetGridPosition(startPos);

            if (!placedItems.Contains(item))
            {
                placedItems.Add(item);
            }

            // 아이템 UI 위치와 크기 정렬
            RectTransform itemRt = item.GetComponent<RectTransform>();
            item.transform.SetParent(itemsContainer, true);
            itemRt.anchorMin = new Vector2(0, 1);
            itemRt.anchorMax = new Vector2(0, 1);
            itemRt.pivot = new Vector2(0, 1);

            float totalWidth = size.x * cellSize + (size.x - 1) * cellSpacing;
            float totalHeight = size.y * cellSize + (size.y - 1) * cellSpacing;
            itemRt.sizeDelta = new Vector2(totalWidth, totalHeight);
            itemRt.anchoredPosition = GetSlotAnchoredPosition(startPos.x, startPos.y);

            UpdateFreeCellCount();
        }

        /// <summary>
        /// 아이템 드래그 시작 시 해당 아이템이 점유하던 슬롯 비우기
        /// </summary>
        public void TemporaryClearItem(InventoryItem item)
        {
            for (int y = 0; y < GridHeight; y++)
            {
                for (int x = 0; x < GridWidth; x++)
                {
                    if (slots[x, y].OccupyingItem == item)
                    {
                        slots[x, y].IsOccupied = false;
                        slots[x, y].OccupyingItem = null;
                    }
                }
            }

            UpdateFreeCellCount();
        }

        /// <summary>
        /// 아이템 완전 제거
        /// </summary>
        public void RemoveItem(InventoryItem item)
        {
            TemporaryClearItem(item);
            placedItems.Remove(item);
            item.SetGridPosition(new Vector2Int(-1, -1));
            UpdateFreeCellCount();
        }

        /// <summary>
        /// 모든 배치된 아이템 제거 및 그리드 초기화
        /// </summary>
        public void ClearAllItems()
        {
            for (int i = placedItems.Count - 1; i >= 0; i--)
            {
                if (placedItems[i] != null)
                {
                    Destroy(placedItems[i].gameObject);
                }
            }
            placedItems.Clear();

            for (int y = 0; y < GridHeight; y++)
            {
                for (int x = 0; x < GridWidth; x++)
                {
                    slots[x, y].IsOccupied = false;
                    slots[x, y].OccupyingItem = null;
                    slots[x, y].SetHighlight(SlotHighlightState.Normal);
                }
            }

            UpdateFreeCellCount();
        }
        #endregion

        #region 좌표 변환 및 하이라이트
        /// <summary>
        /// 월드 좌표를 기준으로 아이템의 좌상단 그리드 인덱스를 산출
        /// </summary>
        public Vector2Int WorldToGridPosition(InventoryItem item, Vector3 worldPos)
        {
            if (slots == null || slots[0, 0] == null || slotsContainer == null)
            {
                return new Vector2Int(-1, -1);
            }

            // slotsContainer 로컬 좌표계로 변환하여 슬롯 (0, 0)과의 상대 거리를 계산
            Vector3 localPos = slotsContainer.InverseTransformPoint(worldPos);
            Vector3 originPos = slots[0, 0].transform.localPosition;

            float step = cellSize + cellSpacing;
            if (step <= 0.001f) step = 1f;

            float dx = localPos.x - originPos.x;
            float dy = originPos.y - localPos.y;

            int gridX = Mathf.RoundToInt(dx / step);
            int gridY = Mathf.RoundToInt(dy / step);

            return new Vector2Int(gridX, gridY);
        }

        /// <summary>
        /// 드래그 중인 아이템의 위치에 맞춰 그리드 슬롯 하이라이트(녹색/적색) 갱신
        /// </summary>
        public void UpdateDragHighlight(InventoryItem item, Vector3 worldPos)
        {
            ClearHighlights();

            Vector2Int gridPos = WorldToGridPosition(item, worldPos);
            Vector2Int size = item.CurrentSize;
            bool canPlace = CanPlaceItem(item, gridPos, size);

            SlotHighlightState state = canPlace ? SlotHighlightState.Valid : SlotHighlightState.Invalid;

            // 아이템이 걸치는 영역 하이라이트
            for (int y = 0; y < size.y; y++)
            {
                for (int x = 0; x < size.x; x++)
                {
                    int targetX = gridPos.x + x;
                    int targetY = gridPos.y + y;

                    if (targetX >= 0 && targetX < GridWidth && targetY >= 0 && targetY < GridHeight)
                    {
                        slots[targetX, targetY].SetHighlight(state);
                    }
                }
            }
        }

        public void ClearHighlights()
        {
            for (int y = 0; y < GridHeight; y++)
            {
                for (int x = 0; x < GridWidth; x++)
                {
                    slots[x, y].SetHighlight(SlotHighlightState.Normal);
                }
            }
        }
        #endregion

        #region 빈 공간 추적 및 이벤트
        /// <summary>
        /// 빈 셀 개수를 계산하고 GameEvents.OnFreeCellCountChanged 발행
        /// </summary>
        public int UpdateFreeCellCount()
        {
            int freeCount = 0;
            for (int y = 0; y < GridHeight; y++)
            {
                for (int x = 0; x < GridWidth; x++)
                {
                    if (!slots[x, y].IsOccupied)
                    {
                        freeCount++;
                    }
                }
            }

            GameEvents.OnFreeCellCountChanged?.Invoke(freeCount, TotalCells);
            return freeCount;
        }

        public int GetFreeCellCount()
        {
            return UpdateFreeCellCount();
        }

        private void HandleTokenApplied(TokenData token, object itemTarget)
        {
            if (itemTarget is InventoryItem targetItem)
            {
                targetItem.ApplyToken(token);
            }
        }
        #endregion
    }
}
