using System.Collections.Generic;
using UnityEngine;

public class ItemContainer : MonoBehaviour
{
    [SerializeField] private int width = 6;
    [SerializeField] private int height = 6;

    private ItemInstance[,] _cells;
    private readonly List<ItemInstance> _items = new();

    public int Width => width;
    public int Height => height;
    public IReadOnlyList<ItemInstance> Items => _items;

    private void Awake()
    {
        InitializeCells();
    }

    private void InitializeCells()
    {
        if (_cells == null || _cells.GetLength(0) != width || _cells.GetLength(1) != height)
            _cells = new ItemInstance[width, height];
    }

    public bool IsInBounds(int x, int y, int w, int h)
    {
        return x >= 0 && y >= 0 && x + w <= width && y + h <= height;
    }

    public bool CanPlace(ItemInstance item, int x, int y)
    {
        InitializeCells();
        if (item == null || item.Data == null) return false;
        return CanPlace(item.Data, x, y, item);
    }

    public bool CanPlace(ItemDataSO data, int x, int y)
    {
        return CanPlace(data, x, y, null);
    }

    private bool CanPlace(ItemDataSO data, int x, int y, ItemInstance ignoredItem)
    {
        InitializeCells();
        if (data == null || data.Width <= 0 || data.Height <= 0) return false;

        if (!IsInBounds(x, y, data.Width, data.Height)) return false;

        for (int ix = x; ix < x + data.Width; ix++)
        {
            for (int iy = y; iy < y + data.Height; iy++)
            {
                if (_cells[ix, iy] != null && _cells[ix, iy] != ignoredItem)
                    return false;
            }
        }
        return true;
    }

    public bool PlaceItem(ItemInstance item, int x, int y)
    {
        InitializeCells();
        if (!CanPlace(item, x, y)) return false;

        if (item.CurrentContainer != null)
            item.CurrentContainer.RemoveItem(item, item.CurrentContainer != this);

        item.GridX = x;
        item.GridY = y;
        item.CurrentContainer = this;

        for (int ix = x; ix < x + item.Data.Width; ix++)
            for (int iy = y; iy < y + item.Data.Height; iy++)
                _cells[ix, iy] = item;

        if (!_items.Contains(item)) _items.Add(item);
        EventBus.Publish(new ItemPlacedEvent(item, this, x, y));
        return true;
    }

    public void RemoveItem(ItemInstance item) => RemoveItem(item, true);

    private void RemoveItem(ItemInstance item, bool notify)
    {
        InitializeCells();
        if (item == null || item.CurrentContainer != this) return;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (_cells[x, y] == item)
                    _cells[x, y] = null;
            }
        }
        _items.Remove(item);
        item.CurrentContainer = null;
        if (notify) EventBus.Publish(new ItemRemovedEvent(item, this));
    }

    public ItemInstance GetItemAt(int x, int y)
    {
        InitializeCells();
        return !IsInBounds(x, y, 1, 1) ? null : _cells[x, y];
    }

    public void ClearContainer(bool destroyGameObjects = true)
    {
        for (int i = _items.Count - 1; i >= 0; i--)
        {
            var item = _items[i];
            RemoveItem(item);
            if (destroyGameObjects && item != null && item.gameObject != null)
            {
                Object.DestroyImmediate(item.gameObject);
            }
        }
    }
}