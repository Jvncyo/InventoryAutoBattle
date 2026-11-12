using NUnit.Framework;
using UnityEngine;

public class InventoryTests
{
    [Test]
    public void TestContainerPlacementAndBounds()
    {
        var go = new GameObject("TestContainer");
        var container = go.AddComponent<ItemContainer>();

        var itemGo = new GameObject("TestItem");
        var item = itemGo.AddComponent<ItemInstance>();
        
        var itemData = ScriptableObject.CreateInstance<ItemDataSO>();
        itemData.Width = 2;
        itemData.Height = 1;
        item.Initialize(itemData);

        // Test valid placement
        Assert.IsTrue(container.CanPlace(item, 0, 0));
        Assert.IsTrue(container.PlaceItem(item, 0, 0));
        Assert.AreEqual(item, container.GetItemAt(0, 0));
        Assert.AreEqual(item, container.GetItemAt(1, 0));

        // Test out of bounds
        Assert.IsFalse(container.CanPlace(item, 5, 0));

        // Test removal
        container.RemoveItem(item);
        Assert.IsNull(container.GetItemAt(0, 0));

        Object.DestroyImmediate(go);
        Object.DestroyImmediate(itemGo);
        Object.DestroyImmediate(itemData);
    }
}