using System;
using System.Collections.Generic;

[Serializable]
public class RunSaveData
{
    public int DataVersion = 3;
    public int CurrentStage = 1;
    public int CurrentWave; // v2 backward compatibility (CurrentStage = CurrentWave)
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
    public float BonusDamageReduction;
    public float CooldownMultiplier = 1.0f;
}

public enum SaveLoadStatus
{
    Missing,
    Success,
    Failed,
    Ended
}