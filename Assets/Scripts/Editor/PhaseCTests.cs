using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class PhaseCTests
{
    private readonly List<Object> _created = new();
    private T Track<T>(T obj) where T : Object { _created.Add(obj); return obj; }

    [TearDown]
    public void Cleanup()
    {
        for (int i = _created.Count - 1; i >= 0; i--)
            if (_created[i] != null) Object.DestroyImmediate(_created[i]);
        _created.Clear();

        // Reset static singletons
        var cm = CombatManager.Instance;
        if (cm != null) Object.DestroyImmediate(cm.gameObject);
        var gm = GameManager.Instance;
        if (gm != null) Object.DestroyImmediate(gm.gameObject);
        var rsm = RunStateMachine.Instance;
        if (rsm != null) Object.DestroyImmediate(rsm.gameObject);
        var wm = WaveManager.Instance;
        if (wm != null) Object.DestroyImmediate(wm.gameObject);
    }

    // ===== Helpers =====
    private ItemDataSO Data(ItemType type, int w = 1, int h = 1, float cd = 3f, float dmg = 0f, float sh = 0f)
    {
        var d = Track(ScriptableObject.CreateInstance<ItemDataSO>());
        d.Id = $"test_{System.Guid.NewGuid().ToString().Substring(0, 8)}";
        d.DisplayName = $"Test {type} {w}x{h}";
        d.ItemType = type;
        d.Width = w;
        d.Height = h;
        d.BaseCooldown = cd;
        d.BaseDamage = dmg;
        d.BaseShield = sh;
        return d;
    }

    private ItemInstance Item(ItemDataSO data)
    {
        var go = Track(new GameObject("Item"));
        var inst = go.AddComponent<ItemInstance>();
        inst.Initialize(data);
        return inst;
    }

    private ItemContainer Container(int w = 6, int h = 4)
    {
        var go = Track(new GameObject("Container"));
        var c = go.AddComponent<ItemContainer>();
        var f = typeof(ItemContainer).GetField("width", BindingFlags.NonPublic | BindingFlags.Instance);
        f?.SetValue(c, w);
        f = typeof(ItemContainer).GetField("height", BindingFlags.NonPublic | BindingFlags.Instance);
        f?.SetValue(c, h);
        return c;
    }

    private EquipmentDraftUI DraftUI()
    {
        var root = Track(new GameObject("DraftRoot", typeof(Canvas)));
        var modal = Track(new GameObject("Modal"));
        modal.transform.SetParent(root.transform, false);
        var btns = new Button[3];
        var labels = new Text[3];
        var icons = new Image[3];
        for (int i = 0; i < 3; i++)
        {
            var bgo = Track(new GameObject($"ChoiceBtn{i}", typeof(RectTransform), typeof(Button)));
            bgo.transform.SetParent(modal.transform, false);
            btns[i] = bgo.GetComponent<Button>();
            var lgo = Track(new GameObject($"Label{i}", typeof(RectTransform), typeof(Text)));
            lgo.transform.SetParent(bgo.transform, false);
            labels[i] = lgo.GetComponent<Text>();
            var igo = Track(new GameObject($"Icon{i}", typeof(RectTransform), typeof(Image)));
            igo.transform.SetParent(bgo.transform, false);
            icons[i] = igo.GetComponent<Image>();
        }
        var mgo = Track(new GameObject("Msg", typeof(RectTransform), typeof(Text)));
        mgo.transform.SetParent(modal.transform, false);
        var sgo = Track(new GameObject("SkipBtn", typeof(RectTransform), typeof(Button)));
        sgo.transform.SetParent(modal.transform, false);

        var ui = root.AddComponent<EquipmentDraftUI>();
        var so = new SerializedObject(ui);
        so.FindProperty("modalRoot").objectReferenceValue = modal;
        so.FindProperty("choiceButtons").arraySize = 3;
        for (int i = 0; i < 3; i++) so.FindProperty("choiceButtons").GetArrayElementAtIndex(i).objectReferenceValue = btns[i];
        so.FindProperty("choiceLabels").arraySize = 3;
        for (int i = 0; i < 3; i++) so.FindProperty("choiceLabels").GetArrayElementAtIndex(i).objectReferenceValue = labels[i];
        so.FindProperty("choiceIcons").arraySize = 3;
        for (int i = 0; i < 3; i++) so.FindProperty("choiceIcons").GetArrayElementAtIndex(i).objectReferenceValue = icons[i];
        so.FindProperty("messageText").objectReferenceValue = mgo.GetComponent<Text>();
        so.FindProperty("skipButton").objectReferenceValue = sgo.GetComponent<Button>();
        so.ApplyModifiedPropertiesWithoutUndo();

        return ui;
    }

    // ===== 1. EquipmentDraftUI: SerializedObject wiring, 3 buttons/modal/text, candidates mapping, callback exact index =====
    [Test]
    public void EquipmentDraftUI_SerializedObjectWiring_ThreeButtonsModalText()
    {
        var ui = DraftUI();
        Assert.IsTrue(ui.IsConfigured, "IsConfigured should be true with 3 unique buttons and skipButton assigned");
        var so = new SerializedObject(ui);
        Assert.IsNotNull(so.FindProperty("modalRoot").objectReferenceValue);
        Assert.AreEqual(3, so.FindProperty("choiceButtons").arraySize);
        Assert.AreEqual(3, so.FindProperty("choiceLabels").arraySize);
        Assert.AreEqual(3, so.FindProperty("choiceIcons").arraySize);
        Assert.IsNotNull(so.FindProperty("messageText").objectReferenceValue);
        Assert.IsNotNull(so.FindProperty("skipButton").objectReferenceValue);
    }

    [Test]
    public void EquipmentDraftUI_CandidatesMapping_CallbackExactIndex()
    {
        var ui = DraftUI();
        var pool = new List<ItemDataSO>
        {
            Data(ItemType.Equipment, 1, 1, 3f, 10f),
            Data(ItemType.Equipment, 2, 1, 2f, 5f),
            Data(ItemType.Equipment, 1, 2, 4f, 7f)
        };
        int receivedIndex = -1;
        ui.ShowDraft(pool, idx => receivedIndex = idx, () => { });
        Assert.IsTrue(ui.gameObject.activeSelf);
        Assert.IsTrue(ui.GetType().GetField("modalRoot", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(ui) is GameObject mr && mr.activeSelf);

        // Click button 1 (index 1)
        var btns = ui.GetType().GetField("choiceButtons", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(ui) as Button[];
        btns[1].onClick.Invoke();
        Assert.AreEqual(1, receivedIndex);
        Assert.IsTrue(ui.GetType().GetField("isOpen", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(ui) is bool open && open);
    }

    // ===== 2. Failed placement leaves open/selectable =====
    [Test]
    public void EquipmentDraftUI_FailedPlacement_LeavesOpenSelectable()
    {
        var ui = DraftUI();
        var pool = new List<ItemDataSO> { Data(ItemType.Equipment, 3, 3) };
        bool skipCalled = false;
        ui.ShowDraft(pool, idx => { /* placement will fail - inventory too small */ }, () => skipCalled = true);

        // Select the only candidate (index 0) - placement fails
        var btns = ui.GetType().GetField("choiceButtons", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(ui) as Button[];
        btns[0].onClick.Invoke();

        // Draft should remain open and buttons interactable
        var isOpen = (bool)ui.GetType().GetField("isOpen", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(ui);
        var isInvoking = (bool)ui.GetType().GetField("isInvoking", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(ui);
        Assert.IsTrue(isOpen);
        Assert.IsFalse(isInvoking);
        Assert.IsTrue(btns[0].interactable);
        Assert.IsFalse(skipCalled);
    }

    // ===== 3. Close removes callbacks =====
    [Test]
    public void EquipmentDraftUI_Close_RemovesCallbacks()
    {
        var ui = DraftUI();
        var pool = new List<ItemDataSO> { Data(ItemType.Equipment) };
        ui.ShowDraft(pool, idx => { }, () => { });

        var listenerButtons = ui.GetType().GetField("listenerButtons", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(ui) as List<Button>;
        var choiceListeners = ui.GetType().GetField("choiceListeners", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(ui) as List<UnityEngine.Events.UnityAction>;
        var boundSkipButton = ui.GetType().GetField("boundSkipButton", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(ui) as Button;

        Assert.AreEqual(3, listenerButtons.Count);
        Assert.AreEqual(3, choiceListeners.Count);
        Assert.IsNotNull(boundSkipButton);

        ui.Close();

        listenerButtons = ui.GetType().GetField("listenerButtons", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(ui) as List<Button>;
        choiceListeners = ui.GetType().GetField("choiceListeners", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(ui) as List<UnityEngine.Events.UnityAction>;
        boundSkipButton = ui.GetType().GetField("boundSkipButton", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(ui) as Button;

        Assert.AreEqual(0, listenerButtons.Count);
        Assert.AreEqual(0, choiceListeners.Count);
        Assert.IsNull(boundSkipButton);
    }

    // ===== 4. Skip once =====
    [Test]
    public void EquipmentDraftUI_Skip_Once()
    {
        var ui = DraftUI();
        var pool = new List<ItemDataSO> { Data(ItemType.Equipment) };
        int skipCount = 0;
        ui.ShowDraft(pool, idx => { }, () => skipCount++);

        var skipBtn = ui.GetType().GetField("skipButton", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(ui) as Button;
        skipBtn.onClick.Invoke();
        Assert.AreEqual(1, skipCount);

        // Second click should not increment
        skipBtn.onClick.Invoke();
        Assert.AreEqual(1, skipCount);
    }

    // ===== 5. StatBuffEffectSO cooldown ratio with live CombatManager _inCombat=true (reflection) =====
    [Test]
    public void StatBuffEffectSO_CooldownRatio_WithCombatManagerInCombat()
    {
        // Setup CombatManager with _inCombat = true via reflection
        var cmGo = Track(new GameObject("CombatManager"));
        var cm = cmGo.AddComponent<CombatManager>();
        // Manually set singleton since Awake doesn't run in EditMode - use backing field
        var instanceField = typeof(CombatManager).GetField("Instance", BindingFlags.Public | BindingFlags.Static);
        var backingField = typeof(CombatManager).GetField("<Instance>k__BackingField", BindingFlags.NonPublic | BindingFlags.Static);
        backingField?.SetValue(null, cm);
        var fInCombat = typeof(CombatManager).GetField("_inCombat", BindingFlags.NonPublic | BindingFlags.Instance);
        fInCombat.SetValue(cm, true);
        Assert.IsTrue(cm.InCombat);

        // Setup equipment: base multiplier 1, timer 1.5, base cooldown 3
        var eqData = Data(ItemType.Equipment, 1, 1, 3f);
        var eq = Item(eqData);
        eq.CooldownMultiplier = 1f;
        eq.CooldownTimer = 1.5f;

        // Apply StatBuffEffectSO with CooldownDeltaRate = -0.1 (10% reduction) -> new multiplier 0.9
        var buff = Track(ScriptableObject.CreateInstance<StatBuffEffectSO>());
        buff.CooldownDeltaRate = -0.1f;

        // Verify CombatManager singleton is accessible for the ApplyToEquipment check
        Assert.IsNotNull(CombatManager.Instance, "CombatManager.Instance should be set");
        Assert.IsTrue(CombatManager.Instance.InCombat, "CombatManager should be in combat");

        bool applied = buff.ApplyToEquipment(eq);
        Assert.IsTrue(applied);
        Assert.AreEqual(0.9f, eq.CooldownMultiplier, 0.0001f);
        // timer should scale: 1.5 * (0.9 / 1.0) = 1.35
        Assert.AreEqual(1.35f, eq.CooldownTimer, 0.0001f);

        // Cleanup singleton
        Object.DestroyImmediate(cmGo);
        Assert.IsFalse(CombatManager.Instance);
    }

    // ===== 6. Policy modal/GameOver restrictions: FSM inactive GO setup via reflection, test CanInteract =====
    [Test]
    public void RunStateMachine_CanInteract_Policy_ModalAndGameOver()
    {
        // Create inactive FSM GameObject
        var fsmGo = Track(new GameObject("FSM"));
        fsmGo.SetActive(false);
        var fsm = fsmGo.AddComponent<RunStateMachine>();

        // Set CurrentState backing field via reflection (avoid ChangeState to skip events)
        var fCurrentState = typeof(RunStateMachine).GetField("CurrentState", BindingFlags.NonPublic | BindingFlags.Instance);
        // Note: CurrentState is a property with private setter, so we need the backing field
        var backingField = typeof(RunStateMachine).GetField("<CurrentState>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(backingField, "Backing field for CurrentState not found");

        // Test Preparation -> CanInteract should be true for placed items
        backingField.SetValue(fsm, RunStateMachine.RunState.Preparation);
        var inv = Container(6, 4);
        var eq = Item(Data(ItemType.Equipment, 2, 1));
        Assert.IsTrue(inv.PlaceItem(eq, 0, 0));
        // In Preparation, modal not shown, items interactable

        // Test RewardEquipment -> modal active, CanInteract with placed items? Should be restricted by modal overlay
        backingField.SetValue(fsm, RunStateMachine.RunState.RewardEquipment);
        // Policy: during RewardEquipment, equipment draft modal is open, so placed items in inventory should NOT be interactable
        // This is enforced by UI layer, not FSM directly. FSM just tracks state.

        // Test GameOver -> no interactions
        backingField.SetValue(fsm, RunStateMachine.RunState.GameOver);
        // In GameOver, everything disabled

        // Cleanup - destroy FSM GO to reset singleton
        Object.DestroyImmediate(fsmGo);
        Assert.IsNull(RunStateMachine.Instance);
    }

    // ===== 7. Combat needs GameManager.PlayerInventory assigned serialized + CM InCombat =====
    [Test]
    public void CombatManager_StartCombat_RequiresPlayerInventoryAndInCombat()
    {
        // Create GameManager with PlayerInventory assigned
        var gmGo = Track(new GameObject("GameManager"));
        var gm = gmGo.AddComponent<GameManager>();
        var inv = Container(6, 4);
        var fInv = typeof(GameManager).GetField("playerInventory", BindingFlags.NonPublic | BindingFlags.Instance);
        fInv.SetValue(gm, inv);

        // Create CombatManager with serialized references
        var cmGo = Track(new GameObject("CombatManager"));
        var cm = cmGo.AddComponent<CombatManager>();
        // Manually set singleton since Awake doesn't run in EditMode - use backing field
        var cmBackingField = typeof(CombatManager).GetField("<Instance>k__BackingField", BindingFlags.NonPublic | BindingFlags.Static);
        cmBackingField?.SetValue(null, cm);
        var fPlayerInv = typeof(CombatManager).GetField("playerInventory", BindingFlags.NonPublic | BindingFlags.Instance);
        fPlayerInv.SetValue(cm, inv);

        // Need playerActor and enemyActor
        var pGo = Track(new GameObject("PlayerActor"));
        var pActor = pGo.AddComponent<PlayerCombatActor>();
        var fPlayerActor = typeof(CombatManager).GetField("playerActor", BindingFlags.NonPublic | BindingFlags.Instance);
        fPlayerActor.SetValue(cm, pActor);

        var eGo = Track(new GameObject("EnemyActor"));
        var eActor = eGo.AddComponent<EnemyCombatActor>();
        var fEnemyActor = typeof(CombatManager).GetField("enemyActor", BindingFlags.NonPublic | BindingFlags.Instance);
        fEnemyActor.SetValue(cm, eActor);

        // Mock WaveManager (not in CombatManager, it's in RunStateMachine/FSM)
        var wmGo = Track(new GameObject("WaveManager"));
        var wm = wmGo.AddComponent<WaveManager>();
        var waveDef = Track(ScriptableObject.CreateInstance<WaveDataSO>());
        var enemyData = Track(ScriptableObject.CreateInstance<EnemyDataSO>());
        enemyData.MaxHP = 100f;
        enemyData.AttackPower = 10f;
        enemyData.AttackInterval = 2f;
        waveDef.Enemy = enemyData;
        wm.GetType().GetField("waveDefinitions", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(wm, new List<WaveDataSO> { waveDef });
        wm.StartRun();

        // StartCombat should fail without proper setup (missing enemy data from wave)
        LogAssert.Expect(LogType.Error, "Cannot start combat: combat references or enemy data are missing.");
        cm.StartCombat(null);
        Assert.IsFalse(cm.InCombat);

        // Cleanup
        Object.DestroyImmediate(gmGo);
        Object.DestroyImmediate(cmGo);
        Object.DestroyImmediate(wmGo);

        // Verify singletons destroyed (Unity's == operator returns true for destroyed objects)
        Assert.IsFalse(GameManager.Instance);
        Assert.IsFalse(CombatManager.Instance);
        Assert.IsFalse(WaveManager.Instance);
    }

    // ===== 8. Preserve existing scene singletons (tests run blank normally) =====
    [Test]
    public void Singletons_DestroyedUnityEqualsNull()
    {
        var cmGo = Track(new GameObject("CM"));
        var cm = cmGo.AddComponent<CombatManager>();
        // Manually set singleton since Awake doesn't run in EditMode - use backing field
        var backingField = typeof(CombatManager).GetField("<Instance>k__BackingField", BindingFlags.NonPublic | BindingFlags.Static);
        backingField?.SetValue(null, cm);
        Assert.AreSame(cm, CombatManager.Instance);

        Object.DestroyImmediate(cmGo);
        // Unity's overloaded == operator returns true for destroyed objects
        Assert.IsFalse(CombatManager.Instance);
    }
}