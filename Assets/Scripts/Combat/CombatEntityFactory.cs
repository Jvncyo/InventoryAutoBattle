using UnityEngine;

/// <summary>
/// 런타임에 EnemyDataSO의 visualPrefab을 인스턴스화하고,
/// 적절한 Actor 컴포넌트(BossCombatActor / EnemyCombatActor)를 부착하며
/// 스탯과 비주얼을 자동 바인딩하는 팩토리.
/// </summary>
public static class CombatEntityFactory
{
    /// <summary>
    /// 적 엔티티를 스폰하고 초기화합니다.
    /// </summary>
    /// <param name="enemyData">적 데이터 (visualPrefab 포함)</param>
    /// <param name="stageNumber">현재 스테이지 번호</param>
    /// <param name="parent">스폰 위치 부모 트랜스폼 (EnemyRoot)</param>
    /// <param name="hpMultiplier">HP 배율</param>
    /// <param name="attackMultiplier">공격력 배율</param>
    /// <param name="speedMultiplier">속도 배율</param>
    /// <returns>생성된 EnemyCombatActor (BossCombatActor 또는 EnemyCombatActor)</returns>
    public static EnemyCombatActor SpawnEnemy(
        EnemyDataSO enemyData,
        int stageNumber,
        Transform parent,
        float hpMultiplier = 1f,
        float attackMultiplier = 1f,
        float speedMultiplier = 1f)
    {
        if (enemyData == null)
        {
            Debug.LogError("[CombatEntityFactory] enemyData is null");
            return null;
        }

        // 1. 적 비주얼 프리팹 인스턴스화
        GameObject visualObj;
        if (enemyData.visualPrefab != null)
        {
            visualObj = UnityEngine.Object.Instantiate(enemyData.visualPrefab, parent);
            visualObj.name = enemyData.EnemyName + "_Visual";
        }
        else
        {
            Debug.LogWarning($"[CombatEntityFactory] {enemyData.EnemyName} visualPrefab is null. Creating empty visual object.");
            visualObj = new GameObject(enemyData.EnemyName + "_Visual", typeof(SpriteRenderer));
            visualObj.transform.SetParent(parent, false);
        }

        // 2. 적절한 Actor 컴포넌트 부착
        EnemyCombatActor actor;
        if (enemyData.IsBoss)
        {
            actor = visualObj.AddComponent<BossCombatActor>();
        }
        else
        {
            actor = visualObj.AddComponent<EnemyCombatActor>();
        }

        // 3. 자식 트랜스에서 CombatAnimatorController 자동 바인딩
        var animatorController = visualObj.GetComponentInChildren<CombatAnimatorController>();
        if (animatorController != null)
        {
            // 애니메이터가 이미 자식에 붙어있으므로 별도 설정 불필요
        }
        else
        {
            Debug.LogWarning($"[CombatEntityFactory] {enemyData.EnemyName} visual prefab has no CombatAnimatorController in children.");
        }

        // 4. 스탯 계산 및 초기화
        var (hp, damage, cooldown, shield) = enemyData.CalculateStatsForStage(stageNumber);

        // StageProgressionManager의 배율 적용
        var stageManager = StageProgressionManager.Instance;
        if (stageManager != null)
        {
            var stageData = stageManager.GetCurrentStageData();
            if (stageData != null)
            {
                hp *= stageData.hpMultiplier;
                damage *= stageData.damageMultiplier;
                cooldown *= stageData.cooldownMultiplier;
            }
        }

        // EnemyCombatActor.Initialize 호출
        actor.Initialize(enemyData, hpMultiplier, attackMultiplier, speedMultiplier);

        // Initialize는 이미 CalculateStatsForStage를 다시 호출하므로,
        // 여기서는 이미 계산된 스탯을 직접 적용 (Initialize 후 덮어쓰기)
        actor.MaxHP = hp;
        actor.CurrentHP = hp;
        actor.Shield = shield;
        
        // Cooldown 타이머 설정
        actor.SetCooldownOverride(cooldown);

        return actor;
    }
}