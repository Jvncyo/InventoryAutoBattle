using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using InventoryAutoBattle.Core;
using System.Collections;

namespace InventoryAutoBattle.Inventory
{
    /// <summary>
    /// 인벤토리 상에 배치되는 개별 아이템 UI 컴포넌트.
    /// 마우스 드래그 앤 드롭, R키 회전, 토큰 충전 및 발동 로직을 담당합니다.
    /// </summary>
    [RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
    public class InventoryItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler, ITokenReceiver
    {
        [Header("데이터")]
        [SerializeField] private ItemData itemData;
        public ItemData Data => itemData;

        [Header("UI 컴포넌트")]
        [SerializeField] private Image iconImage;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Text itemNameText;
        [SerializeField] private Text tokenStatusText;

        [Header("그리드 상태")]
        [SerializeField] private Vector2Int currentGridPosition = new Vector2Int(-1, -1);
        [SerializeField] private bool isRotated = false;

        public Vector2Int CurrentGridPosition => currentGridPosition;
        public bool IsRotated => isRotated;

        /// <summary>
        /// 회전 여부가 반영된 현재 아이템의 셀 점유 크기 (너비 x 높이)
        /// </summary>
        public Vector2Int CurrentSize => isRotated 
            ? new Vector2Int(itemData.size.y, itemData.size.x) 
            : itemData.size;

        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private Canvas parentCanvas;
        private InventoryGrid currentGrid;

        private Vector2 originalAnchoredPosition;
        private Vector2 originalLocalPosition;
        private Transform originalParent;
        private bool isDragging = false;
        private int currentTokenCount = 0;

        public int CurrentTokenCount => currentTokenCount;

        private Canvas ParentCanvas
        {
            get
            {
                if (parentCanvas == null)
                {
                    parentCanvas = GetComponentInParent<Canvas>();
                }
                return parentCanvas;
            }
        }

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
            if (backgroundImage == null)
            {
                backgroundImage = GetComponent<Image>();
            }
            if (backgroundImage != null)
            {
                backgroundImage.raycastTarget = true;
            }
            parentCanvas = GetComponentInParent<Canvas>();
        }

        private void Start()
        {
            if (itemData != null)
            {
                UpdateVisuals();
            }
        }

        private void Update()
        {
            // 드래그 중일 때 R키를 누르면 아이템 90도 회전
            if (isDragging && CheckRotateKey())
            {
                RotateItem();
            }
        }

        public void Initialize(ItemData data, InventoryGrid grid)
        {
            itemData = data;
            currentGrid = grid;
            currentTokenCount = 0;
            isRotated = false;

            if (backgroundImage == null)
                backgroundImage = GetComponent<Image>();

            if (itemNameText == null)
            {
                Text[] texts = GetComponentsInChildren<Text>(true);
                if (texts.Length > 0) itemNameText = texts[0];
                if (texts.Length > 1) tokenStatusText = texts[1];
            }

            UpdateVisuals();
        }

        public void SetGrid(InventoryGrid grid)
        {
            currentGrid = grid;
        }

        public void SetGridPosition(Vector2Int pos)
        {
            currentGridPosition = pos;
        }

        public void UpdateVisuals()
        {
            if (itemData == null) return;

            if (iconImage != null)
            {
                iconImage.sprite = itemData.icon;
                iconImage.enabled = itemData.icon != null;
            }

            if (itemNameText != null)
            {
                itemNameText.text = itemData.itemName;
            }

            UpdateTokenText();

            // 셀 크기에 맞춰 RectTransform 크기 설정
            if (currentGrid != null)
            {
                float cellSize = currentGrid.CellSize;
                float spacing = currentGrid.CellSpacing;
                Vector2Int size = CurrentSize;

                float width = size.x * cellSize + (size.x - 1) * spacing;
                float height = size.y * cellSize + (size.y - 1) * spacing;
                rectTransform.sizeDelta = new Vector2(width, height);
            }
        }

        private void UpdateTokenText()
        {
            if (tokenStatusText == null || itemData == null) return;

            string tokenIcon = itemData.requiredTokenType switch
            {
                TokenType.Attack => "⚔️",
                TokenType.Defense => "🛡️",
                TokenType.Heal => "❤️",
                _ => "●"
            };

            tokenStatusText.text = $"{tokenIcon} {currentTokenCount}/{itemData.requiredTokenCount}";
        }

        /// <summary>
        /// R키 회전: 가로/세로 크기를 뒤바꾸고 UI 회전 및 크기 갱신
        /// </summary>
        public void RotateItem()
        {
            isRotated = !isRotated;
            UpdateVisuals();

            // 드래그 중이라면 실시간 그리드 유효성 하이라이트 다시 계산
            if (isDragging && currentGrid != null)
            {
                currentGrid.UpdateDragHighlight(this, rectTransform.position);
            }
        }

        #region 드래그 앤 드롭 구현
        public void OnBeginDrag(PointerEventData eventData)
        {
            isDragging = true;
            originalLocalPosition = rectTransform.localPosition;
            originalAnchoredPosition = rectTransform.anchoredPosition;
            originalParent = transform.parent;

            // 드래그 중인 아이템이 다른 UI 위에 보이도록 캔버스 최상위로 올림
            Canvas canvas = ParentCanvas;
            if (canvas != null)
            {
                transform.SetParent(canvas.transform, true);
                transform.SetAsLastSibling();
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0.75f;
                canvasGroup.blocksRaycasts = false;
            }

            if (currentGrid != null && currentGridPosition.x >= 0)
            {
                currentGrid.TemporaryClearItem(this);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            Canvas canvas = ParentCanvas;
            float scaleFactor = canvas != null ? canvas.scaleFactor : 1f;
            if (scaleFactor <= 0.001f) scaleFactor = 1f;

            // 마우스 이동량(delta)만큼 정확하게 UI 이동
            rectTransform.anchoredPosition += eventData.delta / scaleFactor;

            if (currentGrid != null)
            {
                currentGrid.UpdateDragHighlight(this, rectTransform.position);
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            isDragging = false;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1.0f;
                canvasGroup.blocksRaycasts = true;
            }

            if (currentGrid != null)
            {
                currentGrid.ClearHighlights();
                bool placed = currentGrid.TryPlaceItemAtWorldPos(this, rectTransform.position);

                if (!placed)
                {
                    // 배치 실패 시 원래 위치 또는 이전 슬롯으로 복귀
                    ReturnToOriginalPosition();
                }
            }
            else
            {
                ReturnToOriginalPosition();
            }
        }

        private void ReturnToOriginalPosition()
        {
            if (originalParent != null)
            {
                transform.SetParent(originalParent, false);
                rectTransform.anchoredPosition = originalAnchoredPosition;
            }
            else
            {
                rectTransform.localPosition = originalLocalPosition;
            }

            if (currentGrid != null && currentGridPosition.x >= 0)
            {
                currentGrid.PlaceItemAt(this, currentGridPosition);
            }
        }
        #endregion

        #region 토큰 주입 및 발동 로직
        public bool CanReceiveToken(TokenData token)
        {
            if (token == null || itemData == null) return false;
            return token.tokenType == itemData.requiredTokenType;
        }

        public bool OnReceiveToken(TokenData token)
        {
            return ApplyToken(token);
        }

        /// <summary>
        /// 토큰을 아이템에 주입하여 요구치 충족 시 효과 발동
        /// </summary>
        public bool ApplyToken(TokenData token)
        {
            if (token == null || itemData == null) return false;

            // 토큰 타입 일치 여부 검사
            if (token.tokenType != itemData.requiredTokenType)
            {
                Debug.Log($"[InventoryItem] 토큰 타입 불일치! 필요: {itemData.requiredTokenType}, 입력: {token.tokenType}");
                return false;
            }

            currentTokenCount++;
            UpdateTokenText();

            Debug.Log($"[InventoryItem] '{itemData.itemName}' 토큰 주입 성공! ({currentTokenCount}/{itemData.requiredTokenCount})");

            // 필요 토큰 수량 충족 시 발동
            if (currentTokenCount >= itemData.requiredTokenCount)
            {
                TriggerItemEffect();
            }

            return true;
        }

        /// <summary>
        /// 아이템 충전 완료 시 효과 발동 및 GameEvents 발행
        /// </summary>
        public void TriggerItemEffect()
        {
            currentTokenCount = 0;
            UpdateTokenText();

            Debug.Log($"<color=yellow>[InventoryItem] ✨ '{itemData.itemName}' 효과 발동! {itemData.itemType} (값: {itemData.effectValue})</color>");
            GameEvents.OnItemTriggered?.Invoke(itemData.itemType, itemData.effectValue);

            // 간단한 발동 피드백 연출 (반짝임)
            StartCoroutine(FlashEffectCoroutine());
        }

        private IEnumerator FlashEffectCoroutine()
        {
            Color origColor = backgroundImage != null ? backgroundImage.color : Color.white;
            if (backgroundImage != null)
                backgroundImage.color = Color.yellow;

            yield return new WaitForSeconds(0.15f);

            if (backgroundImage != null)
                backgroundImage.color = origColor;
        }

        /// <summary>
        /// [Mock 테스트용] 마우스 우클릭 시 가상 토큰을 주입하여 단독 테스트 가능하도록 처리
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                Debug.Log($"[InventoryItem Mock] 우클릭 디버그: '{itemData.itemName}'에 가상 {itemData.requiredTokenType} 토큰을 강제 주입합니다.");
                currentTokenCount++;
                UpdateTokenText();

                if (currentTokenCount >= itemData.requiredTokenCount)
                {
                    TriggerItemEffect();
                }
            }
        }
        #endregion

        private bool CheckRotateKey()
        {
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Keyboard.current != null)
            {
                if (UnityEngine.InputSystem.Keyboard.current.rKey.wasPressedThisFrame)
                    return true;
            }
#endif
            try
            {
                return Input.GetKeyDown(KeyCode.R);
            }
            catch
            {
                return false;
            }
        }
    }
}
