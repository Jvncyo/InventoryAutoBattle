using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public class PhaseFInventoryTests
{
    private GameObject _containerGO;
    private ItemContainer _container;
    private ItemDataSO _1x1Item;
    private ItemDataSO _2x2Item;

    [SetUp]
    public void SetUp()
    {
        _containerGO = new GameObject("TestContainer");
        _container = _containerGO.AddComponent<ItemContainer>();

        // Create 1x1 test item data
        _1x1Item = ScriptableObject.CreateInstance<ItemDataSO>();
        _1x1Item.Id = "item_1x1";
        _1x1Item.Width = 1;
        _1x1Item.Height = 1;

        // Create 2x2 test item data
        _2x2Item = ScriptableObject.CreateInstance<ItemDataSO>();
        _2x2Item.Id = "item_2x2";
        _2x2Item.Width = 2;
        _2x2Item.Height = 2;
    }

    [TearDown]
    public void TearDown()
    {
        if (_containerGO != null) Object.DestroyImmediate(_containerGO);
        if (_1x1Item != null) Object.DestroyImmediate(_1x1Item);
        if (_2x2Item != null) Object.DestroyImmediate(_2x2Item);
    }

    [Test]
    public void MultiCellItem_PlacementAndCollision_WorksCorrectly()
    {
        var go1 = new GameObject("Instance_2x2");
        var inst1 = go1.AddComponent<ItemInstance>();
        inst1.Initialize(_2x2Item);

        // Place 2x2 item at (0,0) -> should occupy (0,0), (1,0), (0,1), (1,1)
        bool placed = _container.PlaceItem(inst1, 0, 0);
        Assert.IsTrue(placed, "2x2 item should be placed at 0,0");

        // Try placing 1x1 item inside the 2x2 footprint -> should fail
        var go2 = new GameObject("Instance_1x1");
        var inst2 = go2.AddComponent<ItemInstance>();
        inst2.Initialize(_1x1Item);
        bool blocked = _container.PlaceItem(inst2, 1, 1);
        Assert.IsFalse(blocked, "1x1 item should be blocked by 2x2 item footprint");

        // Try placing 1x1 item outside footprint -> should succeed
        bool placedOutside = _container.PlaceItem(inst2, 2, 2);
        Assert.IsTrue(placedOutside, "1x1 item should be placed outside 2x2 footprint");

        Object.DestroyImmediate(go1);
        Object.DestroyImmediate(go2);
    }

    [Test]
    public void MultiCellItem_OutOfBounds_Rejected()
    {
        var go = new GameObject("Instance_2x2_OOB");
        var inst = go.AddComponent<ItemInstance>();
        inst.Initialize(_2x2Item);

        // Container is 6x6. Placing 2x2 at (5,5) requires (5,5), (6,5), (5,6), (6,6) -> Out of bounds
        bool placed = _container.PlaceItem(inst, 5, 5);
        Assert.IsFalse(placed, "2x2 item extending out of bounds should be rejected");

        Object.DestroyImmediate(go);
    }
}
