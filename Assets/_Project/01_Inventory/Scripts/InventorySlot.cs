using UnityEngine;
using UnityEngine.UI;

namespace InventoryAutoBattle.Inventory
{
    public enum SlotHighlightState
    {
        Normal,
        Valid,   // 녹색 (배치 가능)
        Invalid  // 적색 (배치 불가)
    }

    /// <summary>
    /// 6x6 인벤토리 그리드의 단일 셀 UI 컴포넌트
    /// </summary>
    public class InventorySlot : MonoBehaviour
    {
        [Header("좌표 정보")]
        [SerializeField] private Vector2Int gridPosition;
        public Vector2Int GridPosition => gridPosition;

        [Header("컴포넌트 참조")]
        [SerializeField] private Image slotImage;
        [SerializeField] private Image highlightImage;

        [Header("하이라이트 색상 설정")]
        [SerializeField] private Color normalColor = new Color(0.2f, 0.22f, 0.25f, 0.85f);
        [SerializeField] private Color validColor = new Color(0.2f, 0.85f, 0.3f, 0.55f);
        [SerializeField] private Color invalidColor = new Color(0.9f, 0.2f, 0.2f, 0.65f);

        public bool IsOccupied { get; set; } = false;
        public InventoryItem OccupyingItem { get; set; } = null;

        private void Awake()
        {
            if (slotImage == null)
                slotImage = GetComponent<Image>();

            SetHighlight(SlotHighlightState.Normal);
        }

        public void Initialize(int x, int y)
        {
            gridPosition = new Vector2Int(x, y);
            gameObject.name = $"Slot_{x}_{y}";
            SetHighlight(SlotHighlightState.Normal);
        }

        public void SetHighlight(SlotHighlightState state)
        {
            if (highlightImage != null)
            {
                switch (state)
                {
                    case SlotHighlightState.Normal:
                        highlightImage.enabled = false;
                        break;
                    case SlotHighlightState.Valid:
                        highlightImage.enabled = true;
                        highlightImage.color = validColor;
                        break;
                    case SlotHighlightState.Invalid:
                        highlightImage.enabled = true;
                        highlightImage.color = invalidColor;
                        break;
                }
            }
            else if (slotImage != null)
            {
                switch (state)
                {
                    case SlotHighlightState.Normal:
                        slotImage.color = normalColor;
                        break;
                    case SlotHighlightState.Valid:
                        slotImage.color = validColor;
                        break;
                    case SlotHighlightState.Invalid:
                        slotImage.color = invalidColor;
                        break;
                }
            }
        }
    }
}
