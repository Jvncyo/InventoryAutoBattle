using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

/// <summary>
/// Phase G: ItemDiscardService & ItemDiscardTarget integration tests.
/// Verifies discard only works in Preparation state and requires Canvas parent.
/// </summary>
public class PhaseGDiscardTests
{
    private GameObject _discardZone;
    private ItemDiscardTarget _discardTarget;
    private ItemContainer _playerInventory;
    private GameObject _canvasGO;

    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        // Create Canvas hierarchy if not present
        _canvasGO = GameObject.Find("Canvas");
        if (_canvasGO == null)
        {
            _canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(GraphicRaycaster));
            var canvas = _canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }

        // Create or find DiscardZone
        _discardZone = GameObject.Find("Canvas/DiscardZone");
        if (_discardZone == null)
        {
            _discardZone = new GameObject("DiscardZone");
            _discardZone.AddComponent<RectTransform>();
            _discardZone.AddComponent<CanvasRenderer>();
            var img = _discardZone.AddComponent<Image>();
            img.color = new Color(0.8f, 0.2f, 0.2f, 0.3f);
            _discardTarget = _discardZone.AddComponent<ItemDiscardTarget>();
            _discardTarget.SetHighlight(img);
            _discardZone.transform.SetParent(_canvasGO.transform, false);
        }
        else
        {
            _discardTarget = _discardZone.GetComponent<ItemDiscardTarget>();
        }
        Assert.IsNotNull(_discardTarget, "ItemDiscardTarget missing on DiscardZone");

        // Create or find PlayerInventory under Canvas
        var inventorySystem = GameObject.Find("Canvas/InventorySystem");
        if (inventorySystem == null)
        {
            inventorySystem = new GameObject("InventorySystem", typeof(ItemContainer), typeof(ItemContainerView));
            inventorySystem.transform.SetParent(_canvasGO.transform, false);
        }
        _playerInventory = inventorySystem.GetComponent<ItemContainer>();
        Assert.IsNotNull(_playerInventory, "ItemContainer missing");
    }

    private ItemInstance CreateTestItem()
    {
        var swordData = ScriptableObject.CreateInstance<ItemDataSO>();
        swordData.Id = "TestSword";
        swordData.DisplayName = "Test Sword";
        swordData.Width = 1;
        swordData.Height = 1;
        swordData.ItemType = ItemType.Equipment;
        
        var item = new GameObject("TestSword").AddComponent<ItemInstance>();
        item.Initialize(swordData);
        _playerInventory.PlaceItem(item, 0, 0);
        item.transform.SetParent(_discardZone.transform.parent, true);
        return item;
    }

    private void DestroyIfNeeded(GameObject go)
    {
        if (go == null) return;
        try { Object.DestroyImmediate(go); } catch { /* ignore */ }
    }

    [TearDown]
    public void Teardown()
    {
        // Clear inventory safely - snapshot first to avoid modifying collection while enumerating
        var items = _playerInventory.Items;
        if (items != null)
        {
            var snapshot = new System.Collections.Generic.List<ItemInstance>(items);
            foreach (var item in snapshot)
            {
                if (item == null) continue;
                try { _playerInventory.RemoveItem(item); } catch { }
                try { if (item != null && item.transform != null) Object.DestroyImmediate(item.transform.root); } catch { }
            }
        }
    }

    [Test]
    public void DiscardZone_HasRequiredComponents()
    {
        Assert.IsNotNull(_discardZone.GetComponent<RectTransform>());
        Assert.IsNotNull(_discardZone.GetComponent<Image>());
        Assert.IsNotNull(_discardTarget);
        Assert.IsNotNull(_discardTarget.Highlight, "highlight Image not assigned");
    }

    [Test]
    public void DiscardService_Check_ReturnsTrue_ForInventoryItemInPreparation()
    {
        var item = CreateTestItem();
        var result = ItemDiscardService.Check(item, out var msg);
        DestroyIfNeeded(item.gameObject);
        Assert.IsTrue(result, msg);
        Assert.IsTrue(msg.Contains("Drop to discard"));
    }

    [Test]
    public void DiscardService_Check_ReturnsFalse_ForNullItem()
    {
        var result = ItemDiscardService.Check(null, out var msg);
        Assert.IsFalse(result);
        Assert.IsTrue(msg.Contains("Nothing"));
    }

    [Test]
    public void DiscardService_TryDiscard_RemovesItemFromInventory()
    {
        var item = CreateTestItem();
        Assert.IsTrue(_playerInventory.Items.Any(i => i == item));
        
        var result = ItemDiscardService.TryDiscard(item);
        Assert.IsTrue(result);
        Assert.IsFalse(_playerInventory.Items.Any(i => i == item), "Item should be removed from inventory");
    }

    [Test]
    public void DiscardService_TryDiscard_FiresEvents()
    {
        bool removedFired = false;
        bool discardedFired = false;
        
        EventBus.Subscribe<ItemRemovedEvent>(e => removedFired = true);
        EventBus.Subscribe<ItemDiscardedEvent>(e => discardedFired = true);

        var item = CreateTestItem();
        ItemDiscardService.TryDiscard(item);
        
        Assert.IsTrue(removedFired, "ItemRemovedEvent not fired");
        Assert.IsTrue(discardedFired, "ItemDiscardedEvent not fired");
        
        EventBus.Unsubscribe<ItemRemovedEvent>(e => removedFired = true);
        EventBus.Unsubscribe<ItemDiscardedEvent>(e => discardedFired = true);
    }

    [Test]
    public void DiscardService_Check_ReturnsFalse_WhenNotInPreparation()
    {
        if (RunStateMachine.Instance == null) Assert.Inconclusive("No RunStateMachine in scene");
        
        // Change to Combat state
        RunStateMachine.Instance.ChangeState(RunStateMachine.RunState.Combat);
        
        var item = CreateTestItem();
        var result = ItemDiscardService.Check(item, out var msg);
        DestroyIfNeeded(item.gameObject);
        Assert.IsFalse(result);
        Assert.IsTrue(msg.Contains("before combat") || msg.Contains("combat"));
    }

    [Test]
    public void DiscardService_Check_ReturnsTrue_ForInventoryItemInRewardToken()
    {
        if (RunStateMachine.Instance == null) Assert.Inconclusive("No RunStateMachine in scene");
        
        RunStateMachine.Instance.ChangeState(RunStateMachine.RunState.RewardToken);
        
        var item = CreateTestItem();
        var result = ItemDiscardService.Check(item, out var msg);
        DestroyIfNeeded(item.gameObject);
        Assert.IsTrue(result, msg);
        Assert.IsTrue(msg.Contains("Drop to discard"));
    }

    [Test]
    public void DiscardService_Check_ReturnsFalse_ForItemWithoutCanvasParent()
    {
        // Create item outside Canvas hierarchy (like test-only objects)
        var helmetData = ScriptableObject.CreateInstance<ItemDataSO>();
        helmetData.Id = "TestHelmet";
        helmetData.DisplayName = "Test Helmet";
        helmetData.Width = 1;
        helmetData.Height = 1;
        helmetData.ItemType = ItemType.Equipment;
        
        var noCanvasGO = new GameObject("NoCanvasItem");
        var trayItem = noCanvasGO.AddComponent<ItemInstance>();
        trayItem.Initialize(helmetData);
        
        var result = ItemDiscardService.Check(trayItem, out var msg);
        Assert.IsFalse(result);
        Assert.IsTrue(msg.Contains("inventory") || msg.Contains("Inventory"));
        
        DestroyIfNeeded(noCanvasGO);
        Object.DestroyImmediate(helmetData);
    }
}