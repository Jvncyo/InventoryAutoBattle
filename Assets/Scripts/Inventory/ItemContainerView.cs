using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ItemContainerView : MonoBehaviour
{
    [SerializeField] private ItemContainer container;
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private Transform slotsParent;
    [SerializeField] private Transform itemsParent;
    [SerializeField] private GameObject itemPrefab;

    private CellSlotView[,] _slots;
    private readonly Dictionary<ItemInstance, ItemView> _itemViews = new();
    private bool _started;

    public ItemContainer Container => container;
    public Camera EventCamera
    {
        get
        {
            var canvas = GetComponentInParent<Canvas>();
            return canvas == null || canvas.rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null : canvas.rootCanvas.worldCamera;
        }
    }

    private void Start()
    {
        _started = true;
        RefreshViews();
    }

    private void OnEnable()
    {
        if (container == null) container = GetComponent<ItemContainer>();
        EventBus.Subscribe<ItemPlacedEvent>(OnItemPlaced);
        EventBus.Subscribe<ItemRemovedEvent>(OnItemRemoved);
        EventBus.Subscribe<ItemPlacementResetEvent>(OnPlacementReset);
        if (_started) RefreshViews();
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<ItemPlacedEvent>(OnItemPlaced);
        EventBus.Unsubscribe<ItemRemovedEvent>(OnItemRemoved);
        EventBus.Unsubscribe<ItemPlacementResetEvent>(OnPlacementReset);
    }

    private void OnItemPlaced(ItemPlacedEvent placed)
    {
        if (placed.Container != container || placed.Item == null) return;
        if (_slots == null) BuildGrid();
        if (_itemViews.TryGetValue(placed.Item, out var view) && view != null)
            UpdateItemPosition(placed.Item, view);
        else
            SpawnItemView(placed.Item);
    }

    private void OnItemRemoved(ItemRemovedEvent removed)
    {
        if (removed.Container == container) RemoveItemView(removed.Item);
    }

    private void OnPlacementReset(ItemPlacementResetEvent reset)
    {
        if (reset.Item != null && _itemViews.TryGetValue(reset.Item, out var view) && view != null)
            view.ResetPosition();
    }

    public void RefreshViews()
    {
        if (container == null) return;
        BuildGrid();
        var stale = new List<ItemInstance>();
        foreach (var pair in _itemViews)
            if (pair.Key == null || pair.Key.CurrentContainer != container || pair.Value == null)
                stale.Add(pair.Key);
        foreach (var item in stale) RemoveItemView(item);
        foreach (var item in container.Items)
            if (item != null && !_itemViews.ContainsKey(item)) SpawnItemView(item);
    }

    private void LateUpdate()
    {
        // Layout groups settle after Start; follow their final slot transforms.
        foreach (var pair in _itemViews)
            if (pair.Key != null && pair.Value != null && !pair.Value.IsDragging)
                UpdateItemPosition(pair.Key, pair.Value);
    }

    public void BuildGrid()
    {
        if (container == null) container = GetComponent<ItemContainer>();
        if (container == null || slotsParent == null || container.Width <= 0 || container.Height <= 0) return;
        _slots = new CellSlotView[container.Width, container.Height];
        var layout = slotsParent.GetComponent<GridLayoutGroup>();
        if (layout != null)
        {
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = container.Width;
        }
        var slots = new List<CellSlotView>();
        foreach (Transform child in slotsParent)
        {
            var slot = child.GetComponent<CellSlotView>();
            if (slot != null) slots.Add(slot);
        }
        int needed = container.Width * container.Height;
        while (slots.Count < needed)
        {
            GameObject go;
            if (slotPrefab != null) go = Instantiate(slotPrefab, slotsParent);
            else
            {
                go = new GameObject("Slot_Auto", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(slotsParent, false);
            }
            var slot = go.GetComponent<CellSlotView>();
            if (slot == null) slot = go.AddComponent<CellSlotView>();
            slots.Add(slot);
        }
        for (int i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];
            slot.gameObject.SetActive(i < needed);
            if (i >= needed) continue;
            int x = i % container.Width;
            int y = i / container.Width;
            slot.enabled = true;
            slot.Initialize(x, y, this);
            _slots[x, y] = slot;
        }
    }

    public ItemView SpawnItemView(ItemInstance instance)
    {
        if (instance == null || itemPrefab == null || itemsParent == null) return null;
        if (_itemViews.TryGetValue(instance, out var existing) && existing != null) return existing;
        var go = Instantiate(itemPrefab, itemsParent);
        var view = go.GetComponent<ItemView>();
        if (view == null)
        {
            Destroy(go);
            return null;
        }
        _itemViews[instance] = view;
        view.Initialize(instance, this);
        return view;
    }

    public void UpdateItemPosition(ItemInstance instance, ItemView view)
    {
        if (instance == null || instance.Data == null || view == null || _slots == null
            || !container.IsInBounds(instance.GridX, instance.GridY, instance.Data.Width, instance.Data.Height)) return;
        var slot = _slots[instance.GridX, instance.GridY];
        if (slot == null) return;
        var rect = view.GetComponent<RectTransform>();
        var slotRect = slot.GetComponent<RectTransform>();
        var layout = slotsParent.GetComponent<GridLayoutGroup>();
        Vector2 cell = layout != null ? layout.cellSize : slotRect.rect.size;
        Vector2 spacing = layout != null ? layout.spacing : Vector2.zero;
        rect.pivot = new Vector2(0, 1);
        // Convert slot-local footprint into the item parent's coordinate space.
        Vector3 topLeft = slotRect.TransformPoint(new Vector3(slotRect.rect.xMin, slotRect.rect.yMax));
        Vector3 bottomRight = slotRect.TransformPoint(new Vector3(slotRect.rect.xMin
            + cell.x * instance.Data.Width + spacing.x * (instance.Data.Width - 1),
            slotRect.rect.yMax - cell.y * instance.Data.Height - spacing.y * (instance.Data.Height - 1)));
        var parent = rect.parent;
        Vector3 localTop = parent.InverseTransformPoint(topLeft);
        Vector3 localBottom = parent.InverseTransformPoint(bottomRight);
        rect.localScale = Vector3.one;
        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, localBottom.x - localTop.x);
        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, localTop.y - localBottom.y);
        rect.position = topLeft;
    }

    public void RemoveItemView(ItemInstance instance)
    {
        if (ReferenceEquals(instance, null)) return;
        if (_itemViews.TryGetValue(instance, out var view))
        {
            if (view != null)
            {
                view.gameObject.SetActive(false);
                Destroy(view.gameObject);
            }
            _itemViews.Remove(instance);
        }
    }

    public bool ScreenPointToGrid(Vector2 point, out int x, out int y)
    {
        x = y = -1;
        if (!isActiveAndEnabled || _slots == null) return false;
        for (int iy = 0; iy < container.Height; iy++)
            for (int ix = 0; ix < container.Width; ix++)
            {
                var slot = _slots[ix, iy];
                if (slot != null && RectTransformUtility.RectangleContainsScreenPoint(
                    slot.GetComponent<RectTransform>(), point, EventCamera))
                {
                    x = ix;
                    y = iy;
                    return true;
                }
            }
        return false;
    }

    public IEnumerable<CellSlotView> GetAffectedSlots(Vector2 screenPoint, int width, int height)
    {
        if (!ScreenPointToGrid(screenPoint, out int x, out int y)) yield break;
        for (int iy = y; iy < y + height; iy++)
        {
            for (int ix = x; ix < x + width; ix++)
            {
                if (ix >= 0 && ix < container.Width && iy >= 0 && iy < container.Height)
                    yield return _slots[ix, iy];
            }
        }
    }

    public void ResetAllHighlights()
    {
        if (_slots == null) return;
        foreach (var slot in _slots)
            if (slot != null) slot.SetHighlight(false);
    }

    public ItemView GetItemView(ItemInstance instance)
    {
        if (instance == null) return null;
        _itemViews.TryGetValue(instance, out var view);
        return view;
    }
}
