using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 스테이지 진행을 총괄합니다. 기존 WaveManager의 역할을 흡수하며,
/// 인스펙터에서 등록된 StageDataSO 리스트를 순차적으로 순회합니다.
/// </summary>
public class StageProgressionManager : MonoBehaviour
{
    public static StageProgressionManager Instance { get; private set; }

    [Header("Stage Sequence")]
    [Tooltip("인스펙터에서 스테이지 순서대로 StageDataSO를 드래그하여 배치하세요.")]
    public List<StageDataSO> stageDataList = new();

    public int CurrentStage { get; private set; } = 1;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public bool IsBossStage() => (CurrentStage % 10 == 0);

    public StageDataSO GetCurrentStageData()
    {
        if (stageDataList == null || stageDataList.Count == 0) return null;

        int index = CurrentStage - 1;
        if (index >= 0 && index < stageDataList.Count)
            return stageDataList[index];

        // 정의된 스테이지를 초과하면 순환
        index = index % stageDataList.Count;
        return stageDataList[index];
    }

    public EnemyDataSO GetCurrentStageEnemy()
    {
        var stageData = GetCurrentStageData();
        return stageData?.ResolveEnemy(CurrentStage);
    }

    public void AdvanceStage()
    {
        CurrentStage++;
    }

    public void RestoreStage(int stage)
    {
        CurrentStage = Mathf.Max(1, stage);
    }

    public List<ItemDataSO> GetEnhancementTokenPool()
    {
        var stageData = GetCurrentStageData();
        return stageData?.equipmentDropPool ?? new List<ItemDataSO>();
    }

    public List<ItemDataSO> GetCombatTokenPool()
    {
        var stageData = GetCurrentStageData();
        return stageData?.tokenDropPool ?? new List<ItemDataSO>();
    }

    public GameObject GetCurrentBackgroundPrefab()
    {
        var stageData = GetCurrentStageData();
        return stageData?.backgroundPrefab;
    }
}