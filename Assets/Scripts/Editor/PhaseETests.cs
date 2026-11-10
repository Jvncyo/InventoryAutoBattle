using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UObject = UnityEngine.Object;

public class PhaseETests
{
    private string _tempPath;
    private SaveManager _saveManager;
    private ItemCatalogSO _catalog;
    private ItemContainer _inventory;
    private PlayerCombatActor _player;

    private const string CatalogPaths =
        "Assets/ScriptableObjects/Items/Sword_Base.asset";

    [SetUp]
    public void Setup()
    {
        _tempPath = Path.Combine(Application.temporaryCachePath, "test_run_save_" + Environment.TickCount + "_" + UnityEngine.Random.Range(0, 100000) + ".json");

        _catalog = ScriptableObject.CreateInstance<ItemCatalogSO>();
        var listField = typeof(ItemCatalogSO).GetField("_items", BindingFlags.NonPublic | BindingFlags.Instance);
        var items = new List<ItemDataSO>();
        string[] paths = new string[]
        {
            "Assets/ScriptableObjects/Items/Sword_Base.asset",
            "Assets/ScriptableObjects/Items/Shield_Base.asset",
            "Assets/ScriptableObjects/Tokens/Token_Attack.asset",
            "Assets/ScriptableObjects/Tokens/Token_Defense.asset",
            "Assets/ScriptableObjects/Tokens/Token_Speed.asset",
            "Assets/ScriptableObjects/Tokens/Token_DirectDamage.asset",
            "Assets/ScriptableObjects/Tokens/Token_Heal.asset",
            "Assets/ScriptableObjects/Tokens/Token_ImmediateDefense.asset"
        };
        foreach (var p in paths)
        {
            var d = AssetDatabase.LoadAssetAtPath<ItemDataSO>(p);
            Assert.IsNotNull(d, "Catalog asset missing: " + p);
            items.Add(d);
        }
        listField.SetValue(_catalog, items);

        var saveGO = new GameObject("TestSaveManager");
        saveGO.SetActive(false);
        _saveManager = saveGO.AddComponent<SaveManager>();
        var so = new SerializedObject(_saveManager);
        so.FindProperty("_catalog").objectReferenceValue = _catalog;
        so.ApplyModifiedPropertiesWithoutUndo();
        _saveManager.PathOverride = _tempPath;

        var invGO = new GameObject("TestInventory");
        invGO.SetActive(false);
        _inventory = invGO.AddComponent<ItemContainer>();
        var invSo = new SerializedObject(_inventory);
        invSo.FindProperty("width").intValue = 6;
        invSo.FindProperty("height").intValue = 6;
        invSo.ApplyModifiedPropertiesWithoutUndo();

        var playerGO = new GameObject("TestPlayer");
        playerGO.SetActive(false);
        _player = playerGO.AddComponent<PlayerCombatActor>();
        _player.Initialize(100f, 100f);
    }

    [TearDown]
    public void Teardown()
    {
        try
        {
            CleanupFile(_tempPath + ".tmp");
            CleanupFile(_tempPath + ".bak");
            CleanupFile(_tempPath);
            CleanupFile(_tempPath + ".old");
            CleanupFile(_tempPath + ".bak.old");
        }
        catch { }
        try
        {
            if (_inventory != null && !ReferenceEquals(_inventory, null))
            {
                foreach (var item in _inventory.Items)
                    if (item != null && item.gameObject != null)
                        UObject.DestroyImmediate(item.gameObject);
                if (_inventory.gameObject != null) UObject.DestroyImmediate(_inventory.gameObject);
            }
            if (_player != null && _player.gameObject != null) UObject.DestroyImmediate(_player.gameObject);
            if (_saveManager != null && _saveManager.gameObject != null) UObject.DestroyImmediate(_saveManager.gameObject);
            if (_catalog != null) UObject.DestroyImmediate(_catalog);
        }
        catch { }
    }

    private static void CleanupFile(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, true);
            return;
        }
        if (!File.Exists(path)) return;
        try { File.SetAttributes(path, FileAttributes.Normal); } catch { }
        File.Delete(path);
    }

    private ItemInstance Place(string id, int x, int y)
    {
        var data = _catalog.Get(id);
        Assert.IsNotNull(data, "Catalog lookup failed for " + id);
        var go = new GameObject("Item_" + id);
        var item = go.AddComponent<ItemInstance>();
        item.Initialize(data);
        Assert.IsTrue(_inventory.PlaceItem(item, x, y), "PlaceItem failed at " + x + "," + y);
        return item;
    }

    private static string ReplaceJson(string json, string pattern, string replacement)
    {
        var result = Regex.Replace(json, pattern, replacement);
        Assert.AreNotEqual(json, result, "Pattern not found in JSON: " + pattern);
        return result;
    }

    [Test]
    public void SaveRestore_RoundTrip_AllTokenTypesAndModifiers()
    {
        var sword = Place("item_sword", 0, 0);
        sword.BonusDamage = 5f;
        sword.BonusShield = 3f;
        sword.CooldownMultiplier = 0.6f;
        var attack = Place("token_attack", 2, 0);
        var heal = Place("token_heal", 4, 0);
        var damage = Place("token_directdamage", 2, 1);
        var shield = Place("item_shield", 3, 3);

        _player.Initialize(100f, 80f);

        Assert.IsTrue(_saveManager.TrySave(3, _player, _inventory), "Save failed: " + _saveManager.LastError);
        Assert.IsTrue(File.Exists(_tempPath));

        // Simulate restart: clear runtime items and reload from disk.
        _inventory.ClearContainer(true);

        var result = _saveManager.TryLoad(out var data, 6, 6);
        Assert.AreEqual(SaveLoadStatus.Success, result, "Load failed: " + _saveManager.LastError);
        Assert.IsFalse(_saveManager.LoadedBackup);

        Assert.IsTrue(_saveManager.TryRestore(data, _inventory), "Restore failed: " + _saveManager.LastError);

        Assert.AreEqual(5, _inventory.Items.Count);
        var restoredSword = _inventory.GetItemAt(0, 0);
        Assert.IsNotNull(restoredSword);
        Assert.AreEqual("item_sword", restoredSword.Data.Id);
        Assert.AreEqual(5f, restoredSword.BonusDamage, 0.01f);
        Assert.AreEqual(3f, restoredSword.BonusShield, 0.01f);
        Assert.AreEqual(0.6f, restoredSword.CooldownMultiplier, 0.01f);
        Assert.AreEqual("token_attack", _inventory.GetItemAt(2, 0).Data.Id);
        Assert.AreEqual("token_heal", _inventory.GetItemAt(4, 0).Data.Id);
        Assert.AreEqual("token_directdamage", _inventory.GetItemAt(2, 1).Data.Id);
        Assert.AreEqual("item_shield", _inventory.GetItemAt(3, 3).Data.Id);
    }

    [Test]
    public void SaveRestore_MultiCellEquipment_RestoresFootprint()
    {
        // Build a synthetic 2x1 item to exercise footprint restore.
        var data = ScriptableObject.CreateInstance<ItemDataSO>();
        data.Id = "phase_e_wide_sword";
        data.DisplayName = "Wide Sword";
        data.ItemType = ItemType.Equipment;
        data.Width = 2;
        data.Height = 1;
        data.BaseCooldown = 4f;
        var listField = typeof(ItemCatalogSO).GetField("_items", BindingFlags.NonPublic | BindingFlags.Instance);
        var items = new List<ItemDataSO>(_catalog.Items) { data };
        listField.SetValue(_catalog, items);

        var wide = Place("phase_e_wide_sword", 3, 1);
        wide.BonusDamage = 7f;
        Assert.IsTrue(_saveManager.TrySave(1, _player, _inventory), "Save failed: " + _saveManager.LastError);

        foreach (var item in _inventory.Items)
            UObject.DestroyImmediate(item.gameObject);
        _inventory.ClearContainer(false);

        var result = _saveManager.TryLoad(out var d, 6, 6);
        Assert.AreEqual(SaveLoadStatus.Success, result, "Load failed: " + _saveManager.LastError);
        Assert.IsTrue(_saveManager.TryRestore(d, _inventory), "Restore failed: " + _saveManager.LastError);

        var restored = _inventory.GetItemAt(3, 1);
        Assert.IsNotNull(restored);
        Assert.AreEqual("phase_e_wide_sword", restored.Data.Id);
        // The container tracks the whole footprint: both cells resolve to the same item.
        Assert.AreSame(restored, _inventory.GetItemAt(4, 1), "Footprint cell should resolve to the wide item.");
        Assert.AreEqual(7f, restored.BonusDamage, 0.01f);
        // The occupied footprint blocks further placement.
        Assert.IsFalse(_inventory.CanPlace(_catalog.Get("phase_e_wide_sword"), 3, 1));
    }

    [Test]
    public void Restore_UnknownId_Rejected()
    {
        // Manually craft a save with an unknown id to ensure validation rejects it.
        var data = new RunSaveData
        {
            DataVersion = 2,
            CurrentWave = 1,
            PlayerHP = 100f,
            PlayerMaxHP = 100f,
            RunState = "Preparation",
            RunEnded = false
        };
        data.InventoryItems.Add(new SavedItemRecord { ItemDataId = "unknown_item_xyz", GridX = 0, GridY = 0 });
        File.WriteAllText(_tempPath, JsonUtility.ToJson(data, true));

        var result = _saveManager.TryLoad(out var load, 6, 6);
        Assert.AreEqual(SaveLoadStatus.Failed, result, "Unknown id must be rejected.");
        Assert.IsTrue(_saveManager.LastError != null && _saveManager.LastError.Contains("not in catalog"));
        Assert.IsTrue(File.Exists(_tempPath), "File must be preserved.");
    }

    [Test]
    public void Restore_OutOfBounds_Rejected()
    {
        Place("item_sword", 0, 0);
        Assert.IsTrue(_saveManager.TrySave(1, _player, _inventory));

        var json = File.ReadAllText(_tempPath);
        File.WriteAllText(_tempPath, ReplaceJson(json, "\"GridX\"\\s*:\\s*0", "\"GridX\": 10"));

        var result = _saveManager.TryLoad(out var data, 6, 6);
        Assert.AreEqual(SaveLoadStatus.Failed, result);
    }

    [Test]
    public void Restore_Overlap_Rejected()
    {
        // Manually craft a JSON with two items at the same position to force overlap.
        var data = new RunSaveData
        {
            DataVersion = 2,
            CurrentWave = 1,
            PlayerHP = 100f,
            PlayerMaxHP = 100f,
            RunState = "Preparation",
            RunEnded = false
        };
        data.InventoryItems.Add(new SavedItemRecord { ItemDataId = "item_sword", GridX = 0, GridY = 0 });
        data.InventoryItems.Add(new SavedItemRecord { ItemDataId = "item_shield", GridX = 0, GridY = 0 });
        File.WriteAllText(_tempPath, JsonUtility.ToJson(data, true));

        var result = _saveManager.TryLoad(out var loadData, 6, 6);
        Assert.AreEqual(SaveLoadStatus.Failed, result, "Should detect overlap. Error: " + _saveManager.LastError);
        Assert.IsTrue(_saveManager.LastError != null && _saveManager.LastError.Contains("Overlap"));
    }

    [Test]
    public void Restore_CooldownBelowCap_Rejected()
    {
        var item = Place("item_sword", 0, 0);
        item.CooldownMultiplier = 0.5f;
        Assert.IsTrue(_saveManager.TrySave(1, _player, _inventory));

        var json = File.ReadAllText(_tempPath);
        File.WriteAllText(_tempPath, ReplaceJson(json, "\"CooldownMultiplier\"\\s*:\\s*0\\.5", "\"CooldownMultiplier\": 0.1"));

        var result = _saveManager.TryLoad(out var data, 6, 6);
        Assert.AreEqual(SaveLoadStatus.Failed, result);
    }

    [Test]
    public void Restore_NegativeWave_Rejected()
    {
        Place("item_sword", 0, 0);
        Assert.IsTrue(_saveManager.TrySave(1, _player, _inventory));

        var json = File.ReadAllText(_tempPath);
        File.WriteAllText(_tempPath, ReplaceJson(json, "\"CurrentStage\"\\s*:\\s*1", "\"CurrentStage\": 0"));
        File.WriteAllText(_tempPath, ReplaceJson(File.ReadAllText(_tempPath), "\"CurrentWave\"\\s*:\\s*\\d+", "\"CurrentWave\": 0"));

        var result = _saveManager.TryLoad(out var data, 6, 6);
        Assert.AreEqual(SaveLoadStatus.Failed, result);
    }

    [Test]
    public void CorruptJson_FallbackToValidBackup()
    {
        Place("item_sword", 0, 0);
        Assert.IsTrue(_saveManager.TrySave(1, _player, _inventory));

        File.Copy(_tempPath, _tempPath + ".bak", true);
        File.WriteAllText(_tempPath, "{ this is not valid json }");

        var result = _saveManager.TryLoad(out var data, 6, 6);
        Assert.AreEqual(SaveLoadStatus.Success, result, "Backup should load: " + _saveManager.LastError);
        Assert.IsTrue(_saveManager.LoadedBackup);
        Assert.AreEqual(1, data.InventoryItems.Count);
    }

    [Test]
    public void BothFilesCorrupt_FailedAndFilesPreserved()
    {
        Place("item_sword", 0, 0);
        Assert.IsTrue(_saveManager.TrySave(1, _player, _inventory));

        File.WriteAllText(_tempPath, "{ bad }}}");

        var result = _saveManager.TryLoad(out var data, 6, 6);
        Assert.AreEqual(SaveLoadStatus.Failed, result);
        Assert.IsTrue(File.Exists(_tempPath));
        Assert.IsTrue(File.Exists(_tempPath + ".bak"));
    }

    [Test]
    public void MissingPrimary_ValidBackup_Loads()
    {
        Place("item_sword", 0, 0);
        Assert.IsTrue(_saveManager.TrySave(1, _player, _inventory));
        File.Copy(_tempPath, _tempPath + ".bak", true);
        File.Delete(_tempPath);

        var result = _saveManager.TryLoad(out var data, 6, 6);
        Assert.AreEqual(SaveLoadStatus.Success, result, "Backup should load: " + _saveManager.LastError);
        Assert.IsTrue(_saveManager.LoadedBackup);
    }

    [Test]
    public void NoFiles_ReturnsMissing()
    {
        var result = _saveManager.TryLoad(out var data, 6, 6);
        Assert.AreEqual(SaveLoadStatus.Missing, result);
        Assert.IsNull(data);
    }

    [Test]
    public void EndRun_Tombstone_PreventsResurrection()
    {
        Place("item_sword", 0, 0);
        Assert.IsTrue(_saveManager.TrySave(1, _player, _inventory));

        Assert.IsTrue(_saveManager.EndRun(), "EndRun failed: " + _saveManager.LastError);

        var result = _saveManager.TryLoad(out var data, 6, 6);
        Assert.AreEqual(SaveLoadStatus.Ended, result);

        // Primary should have RunEnded=true; backup (if any) is deleted.
        var ended = JsonUtility.FromJson<RunSaveData>(File.ReadAllText(_tempPath));
        Assert.IsTrue(ended.RunEnded);
    }

    [Test]
    public void SaveFailure_PreservesPreviousFile()
    {
        Place("item_sword", 0, 0);
        Assert.IsTrue(_saveManager.TrySave(1, _player, _inventory));
        var original = File.ReadAllText(_tempPath);

        // Block the temp write: occupy the temp path with a directory.
        Directory.CreateDirectory(_tempPath + ".tmp");
        bool saved = _saveManager.TrySave(2, _player, _inventory);

        Directory.Delete(_tempPath + ".tmp", true);
        Assert.IsFalse(saved, "Save should fail while temp path is blocked.");
        Assert.AreEqual(original, File.ReadAllText(_tempPath), "Previous file must be preserved.");
    }

    [Test]
    public void FutureVersion_Rejected()
    {
        Place("item_sword", 0, 0);
        Assert.IsTrue(_saveManager.TrySave(1, _player, _inventory));

        var json = File.ReadAllText(_tempPath);
        File.WriteAllText(_tempPath, ReplaceJson(json, "\"DataVersion\"\\s*:\\s*3", "\"DataVersion\": 99"));

        var result = _saveManager.TryLoad(out var data, 6, 6);
        Assert.AreEqual(SaveLoadStatus.Failed, result);
        Assert.IsTrue(_saveManager.LastError != null && _saveManager.LastError.Contains("future"),
            "Expected future-version message, got: " + _saveManager.LastError);
    }

    [Test]
    public void LegacyV0_MigratedAndRestored()
    {
        // Build a v0-shaped file (no DataVersion, no RunEnded) with valid content.
        var data = new RunSaveData
        {
            CurrentWave = 2,
            PlayerHP = 100f,
            PlayerMaxHP = 100f,
            RunSeed = "",
            RunState = "Preparation"
        };
        data.InventoryItems.Add(new SavedItemRecord { ItemDataId = "item_sword", GridX = 1, GridY = 1, BonusDamage = 1f, BonusShield = 1f, CooldownMultiplier = 1f });
        string json = JsonUtility.ToJson(data, true);
        json = Regex.Replace(json, "\"DataVersion\"\\s*:\\s*\\d+,?\\s*", "");
        json = Regex.Replace(json, "\"RunEnded\"\\s*:\\s*\\w+,?\\s*", "");
        File.WriteAllText(_tempPath, json);

        var result = _saveManager.TryLoad(out var loaded, 6, 6);
        Assert.AreEqual(SaveLoadStatus.Success, result, "v0 migration failed: " + _saveManager.LastError);
        Assert.AreEqual(2, loaded.CurrentWave);
        Assert.IsTrue(_saveManager.TryRestore(loaded, _inventory), "Restore failed: " + _saveManager.LastError);
        Assert.AreEqual("item_sword", _inventory.GetItemAt(1, 1).Data.Id);
    }

    [Test]
    public void Restore_FailureDoesNotWipeCurrentItems()
    {
        // Live inventory has items; restoring an invalid save must leave them untouched.
        var live = Place("item_sword", 0, 0);
        var liveShield = Place("item_shield", 1, 0);
        int countBefore = _inventory.Items.Count;

        var badData = new RunSaveData
        {
            DataVersion = 2,
            CurrentWave = 1,
            PlayerHP = 100f,
            PlayerMaxHP = 100f,
            RunState = "Preparation",
            RunEnded = false
        };
        badData.InventoryItems.Add(new SavedItemRecord { ItemDataId = "no_such_id", GridX = 0, GridY = 0 });

        Assert.IsFalse(_saveManager.TryRestore(badData, _inventory));
        Assert.AreEqual(countBefore, _inventory.Items.Count, "Live inventory must be preserved on failed restore.");
        Assert.AreSame(live, _inventory.GetItemAt(0, 0));
        Assert.AreSame(liveShield, _inventory.GetItemAt(1, 0));
    }

    [Test]
    public void ArchiveAndClear_MovesPrimaryToOld()
    {
        Place("item_sword", 0, 0);
        Assert.IsTrue(_saveManager.TrySave(1, _player, _inventory));
        string original = File.ReadAllText(_tempPath);

        Assert.IsTrue(_saveManager.ArchiveAndClear(), "Archive failed: " + _saveManager.LastError);
        Assert.IsFalse(File.Exists(_tempPath));
        Assert.IsTrue(File.Exists(_tempPath + ".old"), "Primary should be moved to .old");
        Assert.AreEqual(original, File.ReadAllText(_tempPath + ".old"));

        // After archive, a fresh load starts a new run (Missing primary, backup is an old run).
        var result = _saveManager.TryLoad(out var data, 6, 6);
        Assert.AreEqual(SaveLoadStatus.Missing, result, "After archive, no valid primary or backup should exist: " + _saveManager.LastError);