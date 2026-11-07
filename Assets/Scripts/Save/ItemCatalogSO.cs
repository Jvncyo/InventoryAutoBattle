using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemCatalog", menuName = "Roguelike/Item Catalog")]
public class ItemCatalogSO : ScriptableObject
{
    [SerializeField] private List<ItemDataSO> _items = new();

    public IReadOnlyList<ItemDataSO> Items => _items;

    private Dictionary<string, ItemDataSO> _idLookup;
    private bool _validated;

    public bool Validate(out string error)
    {
        error = null;
        _idLookup = new Dictionary<string, ItemDataSO>();
        var seen = new HashSet<string>();

        foreach (var item in _items)
        {
            if (item == null)
            {
                error = "Catalog contains null item entry.";
                return false;
            }

            if (string.IsNullOrEmpty(item.Id))
            {
                error = $"Item '{item.DisplayName}' has empty ID.";
                return false;
            }

            if (seen.Contains(item.Id))
            {
                error = $"Duplicate item ID in catalog: {item.Id}";
                return false;
            }

            seen.Add(item.Id);
            _idLookup[item.Id] = item;
        }

        _validated = true;
        return true;
    }

    public bool TryGet(string id, out ItemDataSO data)
    {
        data = null;
        if (!_validated) Validate(out _);
        return _idLookup != null && _idLookup.TryGetValue(id, out data);
    }

    public ItemDataSO Get(string id)
    {
        if (!_validated) Validate(out _);
        _idLookup.TryGetValue(id, out ItemDataSO data);
        return data;
    }

    public bool Contains(string id)
    {
        if (!_validated) Validate(out _);
        return _idLookup != null && _idLookup.ContainsKey(id);
    }

    public void ResetValidation() => _validated = false;
}