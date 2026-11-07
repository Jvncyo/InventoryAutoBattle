using System.Collections.Generic;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }

    [SerializeField] private List<WaveDataSO> waveDefinitions;

    public int CurrentWave { get; private set; }
    public bool IsRunActive { get; private set; }
    public WaveDataSO CurrentWaveData { get; private set; }
    public float CurrentEnemyHPMultiplier { get; private set; } = 1f;
    public float CurrentEnemyAttackMultiplier { get; private set; } = 1f;
    public float CurrentEnemySpeedMultiplier { get; private set; } = 1f;

    private void Awake()
    {
        Instance = this;
    }

    public void StartRun()
    {
        CurrentWave = 1;
        IsRunActive = true;
        LoadCurrentWave();
    }

    public void NextWave()
    {
        CurrentWave++;
        LoadCurrentWave();
    }

    public bool RestoreWave(int wave)
    {
        if (wave < 1) return false;

        CurrentWave = wave;
        IsRunActive = true;
        LoadCurrentWave();
        return IsRunActive;
    }

    public void EndRun()
    {
        IsRunActive = false;
    }

    private void LoadCurrentWave()
    {
        if (waveDefinitions == null || waveDefinitions.Count == 0)
        {
            Debug.LogError("No wave definitions assigned to WaveManager!");
            EndRun();
            return;
        }

        // Loop waves if we exceed the definitions, but scale difficulty
        int waveIndex = (CurrentWave - 1) % waveDefinitions.Count;
        CurrentWaveData = waveDefinitions[waveIndex];

        if (CurrentWaveData == null || CurrentWaveData.Enemy == null)
        {
            Debug.LogError($"Invalid wave data at index {waveIndex}");
            EndRun();
            return;
        }

        // Apply difficulty scaling based on how many times we've looped
        int loopCount = (CurrentWave - 1) / waveDefinitions.Count;
        CurrentEnemyHPMultiplier = 1f + loopCount * 0.25f;
        CurrentEnemyAttackMultiplier = 1f + loopCount * 0.15f;
        CurrentEnemySpeedMultiplier = 1f + loopCount * 0.10f;
    }

    public EnemyDataSO GetCurrentEnemyData()
    {
        return CurrentWaveData?.Enemy;
    }

    public List<ItemDataSO> GetEnhancementTokenPool()
    {
        return CurrentWaveData?.EnhancementTokenPool ?? new List<ItemDataSO>();
    }

    public List<ItemDataSO> GetCombatTokenPool()
    {
        return CurrentWaveData?.CombatTokenPool ?? new List<ItemDataSO>();
    }

    [System.Obsolete("Use GetEnhancementTokenPool instead.")]
    public List<ItemDataSO> GetEquipmentDropPool() => GetEnhancementTokenPool();

    [System.Obsolete("Use GetCombatTokenPool instead.")]
    public List<ItemDataSO> GetTokenDropPool() => GetCombatTokenPool();
}