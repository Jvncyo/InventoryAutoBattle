using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class PhaseBViewTests
{
    private readonly List<Object> _created = new();
    private T Track<T>(T obj) where T : Object { _created.Add(obj); return obj; }

    [TearDown]
    public void Cleanup()
    {
        for (int i = _created.Count - 1; i >= 0; i--)
            if (_created[i] != null) Object.DestroyImmediate(_created[i]);
        _created.Clear();
    }

    private ItemInstance Item(int width = 1, bool token = false)
    {
        var data = Track(ScriptableObject.CreateInstance<ItemDataSO>());
        data.Width = width;
        data.ItemType = token ? ItemType.Token : ItemType.Equipment;
        var go = Track(new GameObject("TestItem"));
        var item = go.AddComponent<ItemInstance>();
        item.Initialize(data);
        return item;
    }

    [Test]
    public void PlacementEventsAreSingleAndSameContainerMoveDoesNotRemoveView()
    {
        var a = Track(new GameObject("A")).AddComponent<ItemContainer>();
        var b = Track(new GameObject("B")).AddComponent<ItemContainer>();
        var item = Item();
        int placed = 0, removed = 0;
        System.Action<ItemPlacedEvent> onPlaced = e => { if (e.Item == item) placed++; };
        System.Action<ItemRemovedEvent> onRemoved = e => { if (e.Item == item) removed++; };
        EventBus.Subscribe(onPlaced);
        EventBus.Subscribe(onRemoved);
        try
        {
            Assert.IsTrue(a.PlaceItem(item, 0, 0));
            Assert.IsTrue(a.PlaceItem(item, 1, 0));
            Assert.AreEqual(2, placed);
            Assert.AreEqual(0, removed);
            Assert.IsTrue(b.PlaceItem(item, 0, 0));
            Assert.AreEqual(3, placed);
            Assert.AreEqual(1, removed);
            Assert.IsNull(a.GetItemAt(1, 0));
            b.ClearContainer(false);
            Assert.AreEqual(2, removed);
            Assert.IsNull(item.CurrentContainer);
        }
        finally
        {
            EventBus.Unsubscribe(onPlaced);
            EventBus.Unsubscribe(onRemoved);
        }
    }

    [Test]
    public void DropRejectsCollisionAndMissingEffectWithoutMovingToken()
    {
        var container = Track(new GameObject("Container")).AddComponent<ItemContainer>();
        var handler = Track(new GameObject("Handler")).AddComponent<UniversalDragDropHandler>();
        var equipment = Item(2);
        equipment.Data.AcceptableTokens.Add(TokenType.Attack);
        var token = Item(1, true);
        token.Data.TokenType = TokenType.Attack;
        container.PlaceItem(equipment, 0, 0);
        container.PlaceItem(token, 3, 0);
        handler.ExecuteDrop(token, container, 0, 0);
        Assert.AreSame(token, container.GetItemAt(3, 0));
        handler.ExecuteDrop(equipment, container, 5, 0);
        Assert.AreEqual(0, equipment.GridX);
        handler.ExecuteDrop(token, container, -1, 0);
        Assert.AreEqual(3, token.GridX);
    }

    [Test]
    public void MultiCellViewUsesSlotFootprintAndBuildGridReusesSlots()
    {
        var root = Track(new GameObject("Canvas", typeof(RectTransform), typeof(Canvas)));
        var go = new GameObject("Container", typeof(RectTransform), typeof(ItemContainer));
        go.transform.SetParent(root.transform, false);
        var slots = new GameObject("Slots", typeof(RectTransform), typeof(GridLayoutGroup));
        slots.transform.SetParent(go.transform, false);
        var layout = slots.GetComponent<GridLayoutGroup>();
        layout.cellSize = new Vector2(70, 70);
        layout.spacing = new Vector2(5, 5);
        var view = go.AddComponent<ItemContainerView>();
        var so = new SerializedObject(view);
        so.FindProperty("slotsParent").objectReferenceValue = slots.transform;
        so.ApplyModifiedPropertiesWithoutUndo();
        view.BuildGrid();
        var first = slots.transform.GetChild(0);
        view.BuildGrid();
        Assert.AreEqual(36, slots.transform.childCount);
        Assert.AreSame(first, slots.transform.GetChild(0));
        var item = Item(2);
        Assert.IsTrue(go.GetComponent<ItemContainer>().PlaceItem(item, 0, 0));
        var visual = new GameObject("Visual", typeof(RectTransform), typeof(ItemView));
        visual.transform.SetParent(go.transform, false);
        var itemView = visual.GetComponent<ItemView>();
        itemView.Initialize(item, view);
        Assert.AreEqual(new Vector2(145, 70), visual.GetComponent<RectTransform>().rect.size);
        Assert.AreEqual(new Vector2(0, 1), visual.GetComponent<RectTransform>().pivot);
    }
}
