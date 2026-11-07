using System;
using System.Collections.Generic;

[Serializable]
public class RunSaveData
{
    public int DataVersion = 2;
    public int CurrentWave;
    public float PlayerHP;
    public float PlayerMaxHP;
    public string RunSeed;
    public string RunState = "Preparation";
    public bool RunEnded;
    public List<SavedItemRecord> InventoryItems = new();
}

[Serializable]
public class SavedItemRecord
{
    public string ItemDataId;
    public int GridX;
    public int GridY;
    public float BonusDamage;
    public float BonusShield;
    public float CooldownMultiplier = 1.0f;
}

public enum SaveLoadStatus
{
    Missing,
    Success,
    Failed,
    Ended
}