using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    [SerializeField] private ItemCatalogSO _catalog;
    [NonSerialized] private string _pathOverride;

    public ItemCatalogSO catalog => _catalog;
    public string PathOverride
    {
        get => _pathOverride;
        set => _pathOverride = value;
    }
    public string LastError { get; private set; }
    public bool LoadedBackup { get; private set; }

    private string PrimaryPath
    {
        get
        {
            if (!string.IsNullOrEmpty(_pathOverride)) return _pathOverride;
            return Path.Combine(Application.persistentDataPath, "run_save.json");
        }
    }

    private string BackupPath => PrimaryPath + ".bak";

    public SaveLoadStatus TryLoad(out RunSaveData data, int width, int height)
    {
        data = null;
        LastError = null;
        LoadedBackup = false;

        if (!File.Exists(PrimaryPath))
        {
            if (File.Exists(BackupPath))
            {
                var result = TryLoadFromFile(BackupPath, width, height, out data);
                if (result == SaveLoadStatus.Success)
                {
                    LoadedBackup = true;
                    return SaveLoadStatus.Success;
                }
                LastError = "Backup corrupted: " + LastError;
                return SaveLoadStatus.Failed;
            }
            return SaveLoadStatus.Missing;
        }

        var primaryResult = TryLoadFromFile(PrimaryPath, width, height, out data);
        if (primaryResult == SaveLoadStatus.Success)
        {
            if (data.RunEnded)
                return SaveLoadStatus.Ended;
            return SaveLoadStatus.Success;
        }

        if (File.Exists(BackupPath))
        {
            var backupResult = TryLoadFromFile(BackupPath, width, height, out data);
            if (backupResult == SaveLoadStatus.Success)
            {
                LoadedBackup = true;
                if (data.RunEnded)
                    return SaveLoadStatus.Ended;
                return SaveLoadStatus.Success;
            }
            LastError = "Both primary and backup corrupted: " + LastError;
            return SaveLoadStatus.Failed;
        }

        return SaveLoadStatus.Failed;
    }

    private SaveLoadStatus TryLoadFromFile(string path, int width, int height, out RunSaveData data)
    {
        data = null;
        try
        {
            string json = File.ReadAllText(path);
            data = JsonUtility.FromJson<RunSaveData>(json);
            if (data == null)
            {
                LastError = "JSON parse returned null.";
                return SaveLoadStatus.Failed;
            }

            if (data.DataVersion == 0)
            {
                data.DataVersion = 2;
            }
            else if (data.DataVersion > 2)
            {
                LastError = $"Unknown future data version {data.DataVersion}. Backup not downgraded.";
                return SaveLoadStatus.Failed;
            }

            if (!Validate(data, width, height, out var error))
            {
                LastError = error;
                return SaveLoadStatus.Failed;
            }

            return SaveLoadStatus.Success;
        }
        catch (Exception e)
        {
            LastError = "Load exception: " + e.Message;
            return SaveLoadStatus.Failed;
        }
    }

    public bool TrySave(int wave, PlayerCombatActor player, ItemContainer inventory)
    {
        LastError = null;

        if (wave < 1 || player == null || inventory == null)
        {
            LastError = "Invalid save arguments.";
            return false;
        }

        if (_catalog == null)
        {
            LastError = "ItemCatalogSO not assigned.";
            return false;
        }

        var data = new RunSaveData
        {
            DataVersion = 2,
            CurrentWave = wave,
            PlayerHP = player.CurrentHP,
            PlayerMaxHP = player.MaxHP,
            RunSeed = WaveManager.Instance != null ? WaveManager.Instance.CurrentWaveData?.Enemy?.Id : "",
            RunState = "Preparation",
            RunEnded = false
        };

        foreach (var item in inventory.Items)
        {
            if (item == null || item.Data == null) continue;
            data.InventoryItems.Add(new SavedItemRecord
            {
                ItemDataId = item.Data.Id,
                GridX = item.GridX,
                GridY = item.GridY,
                BonusDamage = item.BonusDamage,
                BonusShield = item.BonusShield,
                CooldownMultiplier = item.CooldownMultiplier
            });
        }

        string json = JsonUtility.ToJson(data, true);
        string tempPath = PrimaryPath + ".tmp";

        try
        {
            File.WriteAllText(tempPath, json);
            using (var fs = new FileStream(tempPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                fs.Flush();
            }
        }
        catch (Exception e)
        {
            LastError = "Temp write failed: " + e.Message;
            if (File.Exists(tempPath)) File.Delete(tempPath);
            return false;
        }

        try
        {
            if (File.Exists(BackupPath)) File.Delete(BackupPath);
            if (File.Exists(PrimaryPath))
            {
                File.Move(PrimaryPath, BackupPath);
            }
            File.Move(tempPath, PrimaryPath);
            return true;
        }
        catch (Exception e)
        {
            LastError = "Replace failed: " + e.Message;
            if (File.Exists(tempPath)) File.Delete(tempPath);
            return false;
        }
    }

    public bool TryRestore(RunSaveData data, ItemContainer inventory)
    {
        LastError = null;

        if (data == null || inventory == null)
        {
            LastError = "Null data or inventory.";
            return false;
        }

        if (!_catalog.Validate(out var catalogError))
        {
            LastError = "Catalog invalid: " + catalogError;
            return false;
        }

        // Validate the entire record set before touching the live inventory.
        if (!Validate(data, inventory.Width, inventory.Height, out var validationError))
        {
            LastError = validationError;
            return false;
        }

        // Stage all runtime instances in memory.
        var staged = new List<ItemInstance>();
        foreach (var record in data.InventoryItems)
        {
            var itemData = _catalog.Get(record.ItemDataId);
            var go = new GameObject($"Restored_{itemData.DisplayName}");
            var instance = go.AddComponent<ItemInstance>();
            instance.Initialize(itemData);
            instance.BonusDamage = record.BonusDamage;
            instance.BonusShield = record.BonusShield;
            instance.CooldownMultiplier = Mathf.Max(0.2f, record.CooldownMultiplier);
            staged.Add(instance);
        }

        // Snapshot old items for rollback.
        var oldItems = new List<ItemInstance>(inventory.Items);
        inventory.ClearContainer(destroyGameObjects: false);

        for (int i = 0; i < staged.Count; i++)
        {
            var record = data.InventoryItems[i];
            if (!inventory.PlaceItem(staged[i], record.GridX, record.GridY))
            {
                // Rollback: remove partially-placed new items, restore originals.
                for (int j = 0; j <= i; j++)
                {
                    if (staged[j] != null && staged[j].CurrentContainer == inventory)
                        inventory.RemoveItem(staged[j]);
                    if (staged[j] != null && staged[j].gameObject != null)
                        DestroyObject(staged[j].gameObject);
                }
                foreach (var old in oldItems)
                {
                    if (old != null && old.CurrentContainer != inventory)
                        inventory.PlaceItem(old, old.GridX, old.GridY);
                }
                LastError = "PlaceItem failed during restore.";
                return false;
            }
        }

        // Clean up old items that were removed from inventory.
        foreach (var old in oldItems)
        {
            if (old != null && old.gameObject != null)
                DestroyObject(old.gameObject);
        }

        return true;
    }

    private static void DestroyObject(GameObject go)
    {
        if (go == null) return;
        if (Application.isPlaying) Destroy(go);
        else DestroyImmediate(go);
    }

    public bool EndRun()
    {
        LastError = null;

        if (!File.Exists(PrimaryPath))
        {
            LastError = "No save file to end.";
            return false;
        }

        try
        {
            string json = File.ReadAllText(PrimaryPath);
            var data = JsonUtility.FromJson<RunSaveData>(json);
            if (data != null)
            {
                data.RunEnded = true;
                data.DataVersion = 2;
                string updated = JsonUtility.ToJson(data, true);
                File.WriteAllText(PrimaryPath, updated);
            }

            if (File.Exists(BackupPath))
            {
                File.Delete(BackupPath);
            }

            return true;
        }
        catch (Exception e)
        {
            LastError = "EndRun exception: " + e.Message;
            return false;
        }
    }

    public bool ArchiveAndClear()
    {
        LastError = null;

        try
        {
            // Move the files out of the load path (primary and backup) so a
            // corrupted checkpoint cannot resurrect itself, while preserving
            // the bytes for forensics. The most recent archive wins.
            if (File.Exists(PrimaryPath))
            {
                string oldPath = PrimaryPath + ".old";
                if (File.Exists(oldPath)) File.Delete(oldPath);
                File.Move(PrimaryPath, oldPath);
            }
            if (File.Exists(BackupPath))
            {
                string oldPath = BackupPath + ".old";
                if (File.Exists(oldPath)) File.Delete(oldPath);
                File.Move(BackupPath, oldPath);
            }
            return true;
        }
        catch (Exception e)
        {
            LastError = "ArchiveAndClear: " + e.Message;
            return false;
        }
    }

    public bool ClearRunFiles()
    {
        LastError = null;

        try
        {
            if (File.Exists(PrimaryPath)) File.Delete(PrimaryPath);
            if (File.Exists(BackupPath)) File.Delete(BackupPath);
            return true;
        }
        catch (Exception e)
        {
            LastError = "ClearRunFiles: " + e.Message;
            return false;
        }
    }

    public bool Validate(RunSaveData data, int width, int height, out string error)
    {
        error = null;

        if (data == null)
        {
            error = "Null data.";
            return false;
        }

        if (_catalog == null)
        {
            error = "ItemCatalogSO not assigned.";
            return false;
        }

        if (!_catalog.Validate(out var catalogError))
        {
            error = "Catalog invalid: " + catalogError;
            return false;
        }

        if (data.CurrentWave < 1)
        {
            error = "Wave must be positive.";
            return false;
        }

        if (!float.IsFinite(data.PlayerHP) || data.PlayerHP < 0)
        {
            error = "PlayerHP invalid.";
            return false;
        }

        if (!float.IsFinite(data.PlayerMaxHP) || data.PlayerMaxHP <= 0)
        {
            error = "PlayerMaxHP invalid.";
            return false;
        }

        if (data.PlayerHP > data.PlayerMaxHP * 1.0001f)
        {
            error = "PlayerHP exceeds MaxHP.";
            return false;
        }

        var seenCells = new HashSet<long>();

        foreach (var record in data.InventoryItems)
        {
            if (!_catalog.TryGet(record.ItemDataId, out var itemData))
            {
                error = $"ItemDataId not in catalog: {record.ItemDataId}";
                return false;
            }

            if (record.GridX < 0 || record.GridY < 0 || record.GridX + itemData.Width > width || record.GridY + itemData.Height > height)
            {
                error = $"Item {record.ItemDataId} at {record.GridX},{record.GridY} size {itemData.Width}x{itemData.Height} exceeds grid {width}x{height}";
                return false;
            }

            for (int ix = record.GridX; ix < record.GridX + itemData.Width; ix++)
            {
                for (int iy = record.GridY; iy < record.GridY + itemData.Height; iy++)
                {
                    long key = ((long)ix << 32) | (uint)iy;
                    if (seenCells.Contains(key))
                    {
                        error = $"Overlap at {ix},{iy} between items.";
                        return false;
                    }
                    seenCells.Add(key);
                }
            }

            if (record.CooldownMultiplier < 0.2f)
            {
                error = $"CooldownMultiplier too low: {record.CooldownMultiplier} for {record.ItemDataId}";
                return false;
            }

            if (!float.IsFinite(record.BonusDamage) || !float.IsFinite(record.BonusShield) || !float.IsFinite(record.CooldownMultiplier))
            {
                error = $"Non-finite modifier for {record.ItemDataId}";
                return false;
            }
        }

        return true;
    }

    public void SaveRun(int wave, PlayerCombatActor player, ItemContainer inventory, string seed, RunStateMachine.RunState currentState)
    {
        TrySave(wave, player, inventory);
    }

    public RunSaveData LoadRun()
    {
        if (TryLoad(out var data, 6, 6) == SaveLoadStatus.Success)
            return data;
        return null;
    }

    public void DeleteSave()
    {
        if (File.Exists(PrimaryPath)) File.Delete(PrimaryPath);
        if (File.Exists(BackupPath)) File.Delete(BackupPath);
    }
}