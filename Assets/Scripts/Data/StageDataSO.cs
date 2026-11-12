using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 디자이너가 인스펙터에서 스테이지를 직접 기획하는 데이터 소스.
/// 등장 적 풀, 보스 지정, 배경 비주얼, 스탯 배율, 보상 드롭 풀을 정의합니다.
/// </summary>
[CreateAssetMenu(fileName = "Stage_", menuName = "Roguelike/Stage Data")]
public class StageDataSO : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("스테이지 번호 (1부터). 10의 배수일 경우 보스 스테이지로 취급할 수 있습니다.")]
    public int StageNumber;
    [Tooltip("비옴/스테이지 이름 (UI 및 디버깅용)")]
    public string BiomeName;

    [Header("Visual")]
    [Tooltip("스테이지 배경 비주얼 프리팹. null이면 배경 없이 진행")]
    public GameObject backgroundPrefab;

    [Header("Enemy Composition")]
    [Tooltip("일반 적 풀. 런타임에 스테이지 순번으로 결정론적으로 선택됩니다.")]
    public List<EnemyDataSO> commonEnemyPool = new();
    [Tooltip("보스 적. 지정되면 이 스테이지는 보스 스테이지가 됩니다.")]
    public EnemyDataSO bossEnemy;

    [Header("Stage Stat Multipliers (Biome Tuning)")]
    [Range(0.1f, 10f)] public float hpMultiplier = 1f;
    [Range(0.1f, 10f)] public float damageMultiplier = 1f;
    [Range(0.1f, 10f)] public float cooldownMultiplier = 1f;

    [Header("Reward Drop Pools (null/empty = global default pool)")]
    [Tooltip("장비 보상 드래프트 풀. 비어있으면 전역 기본 풀을 사용합니다.")]
    public List<ItemDataSO> equipmentDropPool = new();
    [Tooltip("토큰 보상 트레이 풀. 비어있으면 전역 기본 풀을 사용합니다.")]
    public List<ItemDataSO> tokenDropPool = new();

    public bool IsBossStage => bossEnemy != null;

    /// <summary>
    /// 해당 스테이지에서 출현할 적을 결정론적으로 반환합니다.
    /// 보스 스테이지면 bossEnemy, 아니면 commonEnemyPool을 순환 선택.
    /// </summary>
    public EnemyDataSO ResolveEnemy(int stageNumber)
    {
        if (IsBossStage) return bossEnemy;
        if (commonEnemyPool == null || commonEnemyPool.Count == 0) return null;
        int index = (Mathf.Max(1, stageNumber) - 1) % commonEnemyPool.Count;
        return commonEnemyPool[index];
    }
}
