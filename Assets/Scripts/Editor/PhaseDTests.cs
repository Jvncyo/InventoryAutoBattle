using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class PhaseDTests
{
    private const float Tolerance = 0.0001f;
    private readonly List<Object> _created = new();
    private FieldInfo _instanceField;
    private CombatManager _previousCombatManager;
    private CombatManager _combat;
    private PlayerCombatActor _player;
    private EnemyCombatActor _enemy;

    private T Track<T>(T obj) where T : Object
    {
        _created.Add(obj);
        return obj;
    }

    private T Component<T>(string name) where T : Component
    {
        var go = Track(new GameObject(name));
        // Keep lifecycle callbacks out of these synchronous EditMode tests.
        go.SetActive(false);
        return go.AddComponent<T>();
    }

    [SetUp]
    public void Setup()
    {
        _instanceField = typeof(CombatManager).GetField("<Instance>k__BackingField",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.IsNotNull(_instanceField, "CombatManager singleton backing field not found.");
        _previousCombatManager = CombatManager.Instance;

        _player = Component<PlayerCombatActor>("PhaseD_Player");
        _player.Initialize(100f, 100f);
        _enemy = Component<EnemyCombatActor>("PhaseD_Enemy");
        var enemyData = Track(ScriptableObject.CreateInstance<EnemyDataSO>());
        enemyData.MaxHP = 100f;
        enemyData.AttackInterval = 2f;
        _enemy.Initialize(enemyData, 1, 1f, 1f, 1f); // stageIndex=1, default multipliers

        _combat = Component<CombatManager>("PhaseD_CombatManager");
        var serialized = new SerializedObject(_combat);
        serialized.FindProperty("playerActor").objectReferenceValue = _player;
        // Set enemyRoot transform (new field in refactored CombatManager)
        var enemyRootGO = Track(new GameObject("EnemyRoot"));
        serialized.FindProperty("enemyRoot").objectReferenceValue = enemyRootGO.transform;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        // Set the private _enemyActor field so CombatTokenEffectSO.CanApply works
        var fEnemyActor = typeof(CombatManager).GetField("_enemyActor", BindingFlags.NonPublic | BindingFlags.Instance);
        fEnemyActor?.SetValue(_combat, _enemy);
        var inCombatField = typeof(CombatManager).GetField("_inCombat",
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(inCombatField);
        inCombatField.SetValue(_combat, true);
        _instanceField.SetValue(null, _combat);

        // Do not call StartCombat/ResolveOutcome: they publish events that may
        // reach scene FSM/save subscribers. Only direct effect APIs are tested.
        Assert.AreSame(_combat, CombatManager.Instance);
        Assert.IsTrue(_combat.InCombat);
        Assert.IsFalse(_player.IsDead);
        Assert.IsFalse(_enemy.IsDead);
    }

    [TearDown]
    public void Cleanup()
    {
        try
        {
            for (int i = _created.Count - 1; i >= 0; i--)
                if (_created[i] != null) Object.DestroyImmediate(_created[i]);
        }
        finally
        {
            _created.Clear();
            // Preserve pre-existing scene objects instead of destroying their singletons.
            _instanceField?.SetValue(null, _previousCombatManager);
        }
    }

    private ItemInstance Equipment(float cooldownMultiplier)
    {
        var data = Track(ScriptableObject.CreateInstance<ItemDataSO>());
        data.Id = "phase_d_equipment";
        data.ItemType = ItemType.Equipment;
        data.BaseCooldown = 5f;
        data.BaseDamage = 10f;
        var equipment = Component<ItemInstance>("PhaseD_Equipment");
        equipment.Initialize(data);
        equipment.CooldownMultiplier = cooldownMultiplier;
        equipment.CooldownTimer = 0.5f;
        equipment.BonusDamage = 2f;
        equipment.BonusShield = 3f;
        return equipment;
    }

    private CombatTokenEffectSO CombatEffect(CombatTokenAction action, float amount)
    {
        var effect = Track(ScriptableObject.CreateInstance<CombatTokenEffectSO>());
        effect.Action = action;
        effect.Amount = amount;
        return effect;
    }

    [Test]
    public void StatBuff_CooldownReduction_ClampsAtPointTwoAndScalesRemainingTimer()
    {
        var equipment = Equipment(0.25f);
        var effect = Track(ScriptableObject.CreateInstance<StatBuffEffectSO>());
        effect.CooldownDeltaRate = -0.1f;

        Assert.IsTrue(effect.CanApply(equipment, out _));
        Assert.AreEqual(0.25f, equipment.CooldownMultiplier, Tolerance);
        Assert.AreEqual(0.5f, equipment.CooldownTimer, Tolerance);
        Assert.IsTrue(effect.ApplyToEquipment(equipment));
        Assert.AreEqual(0.2f, equipment.CooldownMultiplier, Tolerance);
        Assert.AreEqual(0.4f, equipment.CooldownTimer, Tolerance);
        Assert.AreEqual(2f, equipment.BonusDamage, Tolerance);
        Assert.AreEqual(3f, equipment.BonusShield, Tolerance);
    }

    [Test]
    public void StatBuff_CooldownOnlyAtCap_RejectsWithoutChangingEquipment()
    {
        var equipment = Equipment(0.2f);
        var effect = Track(ScriptableObject.CreateInstance<StatBuffEffectSO>());
        effect.CooldownDeltaRate = -0.1f;

        Assert.IsFalse(effect.CanApply(equipment, out string message));
        Assert.IsNotEmpty(message);
        Assert.IsFalse(effect.ApplyToEquipment(equipment));
        Assert.AreEqual(0.2f, equipment.CooldownMultiplier, Tolerance);
        Assert.AreEqual(0.5f, equipment.CooldownTimer, Tolerance);
        Assert.AreEqual(2f, equipment.BonusDamage, Tolerance);
        Assert.AreEqual(3f, equipment.BonusShield, Tolerance);
    }

    [Test]
    public void StatBuff_CompoundEffectAtCooldownCap_StillAddsDamage()
    {
        var equipment = Equipment(0.2f);
        var effect = Track(ScriptableObject.CreateInstance<StatBuffEffectSO>());
        effect.CooldownDeltaRate = -0.1f;
        effect.DamageBonus = 4f;

        Assert.IsTrue(effect.CanApply(equipment, out _));
        Assert.AreEqual(2f, equipment.BonusDamage, Tolerance);
        Assert.IsTrue(effect.ApplyToEquipment(equipment));
        Assert.AreEqual(6f, equipment.BonusDamage, Tolerance);
        Assert.AreEqual(16f, equipment.Data.BaseDamage + equipment.BonusDamage, Tolerance);
        Assert.AreEqual(0.2f, equipment.CooldownMultiplier, Tolerance);
        Assert.AreEqual(0.5f, equipment.CooldownTimer, Tolerance);
        Assert.AreEqual(3f, equipment.BonusShield, Tolerance);
    }

    [TestCase(5f, 5f, 100f)]
    [TestCase(10f, 0f, 100f)]
    [TestCase(15f, 0f, 95f)]
    public void CombatToken_Damage_ConsumesShieldBeforeHP(float amount, float expectedShield, float expectedHP)
    {
        _enemy.AddShield(10f);
        var effect = CombatEffect(CombatTokenAction.Damage, amount);

        Assert.IsTrue(effect.CanApply(_enemy, out _));
        Assert.AreEqual(100f, _enemy.CurrentHP, Tolerance, "Preview must not apply damage.");
        Assert.AreEqual(10f, _enemy.Shield, Tolerance);
        Assert.IsFalse(effect.CanApply(_player, out _));
        Assert.IsFalse(effect.ApplyToActor(_player));
        Assert.IsTrue(effect.ApplyToActor(_enemy));
        Assert.AreEqual(expectedShield, _enemy.Shield, Tolerance);
        Assert.AreEqual(expectedHP, _enemy.CurrentHP, Tolerance);
        Assert.AreEqual(100f, _player.CurrentHP, Tolerance);
    }

    [TestCase(10f, 70f)]
    [TestCase(50f, 100f)]
    public void CombatToken_Heal_PartiallyInjuredPlayer_RestoresHPWithoutOverheal(float amount, float expectedHP)
    {
        _player.Initialize(100f, 60f);
        _player.AddShield(7f);
        var effect = CombatEffect(CombatTokenAction.Heal, amount);

        Assert.IsTrue(effect.CanApply(_player, out _));
        Assert.AreEqual(60f, _player.CurrentHP, Tolerance, "Preview must not heal.");
        Assert.IsTrue(effect.ApplyToActor(_player));
        Assert.AreEqual(expectedHP, _player.CurrentHP, Tolerance);
        Assert.AreEqual(100f, _player.MaxHP, Tolerance);
        Assert.AreEqual(7f, _player.Shield, Tolerance);
        Assert.AreEqual(100f, _enemy.CurrentHP, Tolerance);
    }

    [Test]
    public void CombatToken_Heal_FullHP_RejectsWithoutChangingActor()
    {
        _player.AddShield(7f);
        var effect = CombatEffect(CombatTokenAction.Heal, 20f);

        Assert.IsFalse(effect.CanApply(_player, out string message));
        Assert.IsNotEmpty(message);
        Assert.IsFalse(effect.ApplyToActor(_player));
        Assert.AreEqual(100f, _player.CurrentHP, Tolerance);
        Assert.AreEqual(7f, _player.Shield, Tolerance);
    }

    [Test]
    public void CombatToken_Shield_AddsToExistingShieldAndStacksAtFullHP()
    {
        _player.AddShield(7f);
        var effect = CombatEffect(CombatTokenAction.Shield, 12f);

        Assert.IsTrue(effect.CanApply(_player, out _));
        Assert.AreEqual(7f, _player.Shield, Tolerance, "Preview must not grant shield.");
        Assert.IsFalse(effect.CanApply(_enemy, out _));
        Assert.IsFalse(effect.ApplyToActor(_enemy));
        Assert.IsTrue(effect.ApplyToActor(_player));
        Assert.AreEqual(19f, _player.Shield, Tolerance);
        Assert.IsTrue(effect.CanApply(_player, out _));
        Assert.IsTrue(effect.ApplyToActor(_player));
        Assert.AreEqual(31f, _player.Shield, Tolerance);
        Assert.AreEqual(100f, _player.CurrentHP, Tolerance);
        Assert.AreEqual(0f, _enemy.Shield, Tolerance);
    }
}
