using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ItemView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public ItemInstance ItemInstance { get; private set; }
    public ItemContainerView ContainerView { get; private set; }
    public bool IsDragging { get; private set; }

    [SerializeField] private Image iconImage;
    [SerializeField] private Text nameText;

    private Canvas _canvas;
    private RectTransform _rectTransform;
    private CanvasGroup _canvasGroup;
    private Transform _originalParent;
    private Vector2 _pointerOffset;
    private readonly Dictionary<Image, Color> _previewColors = new();
    private ItemDiscardTarget _discardTarget;

    private void ClearEquipmentPreview()
    {
        foreach (var pair in _previewColors) if (pair.Key != null) pair.Key.color = pair.Value;
        _previewColors.Clear();
    }

    public void Initialize(ItemInstance instance, ItemContainerView containerView)
    {
        ItemInstance = instance;
        ContainerView = containerView;
        _rectTransform = GetComponent<RectTransform>();
        _canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
        var canvas = GetComponentInParent<Canvas>();
        _canvas = canvas != null ? canvas.rootCanvas : null;
        if (instance.Data != null)
        {
            if (iconImage != null)
            {
                iconImage.sprite = instance.Data.Icon;
                iconImage.color = instance.InstanceColor;
                iconImage.enabled = instance.Data.Icon != null;
            }
            if (nameText != null) nameText.text = instance.Data.DisplayName;
        }
        ContainerView.UpdateItemPosition(instance, this);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (IsDragging || ItemInstance == null || _canvas == null || _canvasGroup == null
            || UniversalDragDropHandler.Instance == null
            || !InventoryInteractionPolicy.CanInteract(ItemInstance)) return;
        _originalParent = transform.parent;
        transform.SetParent(_canvas.transform, true);
        transform.SetAsLastSibling();
        var canvasRect = _canvas.transform as RectTransform;
        Camera camera = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, eventData.position, camera, out var local))
            _pointerOffset = (Vector2)_rectTransform.localPosition - local;
        else _pointerOffset = Vector2.zero;
        IsDragging = true;
        _canvasGroup.blocksRaycasts = false;

        // 삭제존 캐시
        _discardTarget = FindFirstObjectByType<ItemDiscardTarget>();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!IsDragging) return;
        TokenUseService.ClearMessage();
        ClearEquipmentPreview();

        // 다중 셀 아이템 하이라이트 로직
        var containerViews = FindObjectsByType<ItemContainerView>(FindObjectsSortMode.None);
        foreach (var view in containerViews)
        {
            view.ResetAllHighlights();
            var affectedSlots = view.GetAffectedSlots(eventData.position, ItemInstance.Data.Width, ItemInstance.Data.Height);
            foreach (var slot in affectedSlots)
                slot.SetHighlight(true);
        }

        // 삭제존 하이라이트
        if (_discardTarget != null)
        {
            bool hovered = _discardTarget.Contains(eventData.position, ContainerView.EventCamera);
            _discardTarget.Preview(ItemInstance, hovered);
        }

        // 토큰 장비 프리뷰
        if (ItemInstance.Data.ItemType == ItemType.Token)
            foreach (var candidate in FindObjectsByType<ItemView>(FindObjectsSortMode.None))
            {
                if (candidate.ItemInstance == null || candidate.ItemInstance.Data.ItemType != ItemType.Equipment) continue;
                var image = candidate.GetComponent<Image>();
                if (image == null) continue;
                _previewColors[image] = image.color;
                image.color = TokenUseService.Check(ItemInstance, null, out _)
                    ? new Color(0.25f, 0.65f, 0.35f) : new Color(0.55f, 0.25f, 0.25f);
            }
        foreach (var target in FindObjectsByType<CombatTokenDropTarget>(FindObjectsSortMode.None))
        {
            bool hovered = target.Contains(eventData.position, ContainerView.EventCamera);
            target.Preview(ItemInstance, hovered);
            if (hovered) TokenUseService.Preview(ItemInstance, target.Actor);
        }

        Camera camera = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvas.transform as RectTransform, eventData.position, camera, out var local))
            _rectTransform.localPosition = new Vector3(local.x + _pointerOffset.x, local.y + _pointerOffset.y, 0);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!IsDragging) return;

        // 모든 하이라이트 리셋
        foreach (var view in FindObjectsByType<ItemContainerView>(FindObjectsSortMode.None))
            view.ResetAllHighlights();

        RestoreDragState();

        // 1. CombatTokenDropTarget (HP 바) 드롭 체크 - 즉시 사용 토큰 처리
        if (ItemInstance.Data.ItemType == ItemType.Token && ItemInstance.Data.TokenCategory == TokenCategory.Combat)
        {
            foreach (var target in FindObjectsByType<CombatTokenDropTarget>(FindObjectsSortMode.None))
            {
                if (target.Contains(eventData.position, ContainerView.EventCamera))
                {
                    if (TokenUseService.TryUse(ItemInstance, target.Actor))
                    {
                        return; // 성공 시 종료 (토큰 소모됨)
                    }
                    break; // 타겟은 찾았으나 사용 실패 시 아래 로직으로 fallback
                }
            }
        }

        // 2. ItemDiscardTarget - 삭제존 체크
        if (_discardTarget != null && _discardTarget.Contains(eventData.position, ContainerView.EventCamera))
        {
            if (ItemDiscardService.TryDiscard(ItemInstance))
            {
                return; // 삭제 성공 시 종료
            }
            // 삭제 실패 시 아래 로직으로 fallback
        }

        // 3. 기존 그리드 기반 드롭 처리 (장비 강화, 이동 등)
        var handler = UniversalDragDropHandler.Instance;
        if (handler == null)
        {
            ResetPosition();
            return;
        }
        foreach (var target in FindObjectsByType<ItemContainerView>(FindObjectsSortMode.None))
        {
            if (target.ScreenPointToGrid(eventData.position, out int x, out int y))
            {
                handler.ExecuteDrop(ItemInstance, target.Container, x, y);
                return;
            }
        }
        handler.CancelDrag(ItemInstance);
    }

    private void RestoreDragState()
    {
        IsDragging = false;
        ClearEquipmentPreview();
        if (_discardTarget != null) _discardTarget.Preview(null, false);
        foreach (var target in FindObjectsByType<CombatTokenDropTarget>(FindObjectsSortMode.None)) target.Preview(null, false);
        if (_canvasGroup != null) _canvasGroup.blocksRaycasts = true;
        if (_originalParent != null) transform.SetParent(_originalParent, true);
        _originalParent = null;
    }

    public void CancelActiveDrag()
    {
        if (!IsDragging) return;
        RestoreDragState();
        ResetPosition();
    }

    private void OnDisable() => CancelActiveDrag();

    public void ResetPosition()
    {
        if (ContainerView != null && ItemInstance != null)
            ContainerView.UpdateItemPosition(ItemInstance, this);
    }
}
