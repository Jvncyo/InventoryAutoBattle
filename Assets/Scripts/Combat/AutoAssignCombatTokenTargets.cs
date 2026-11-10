using UnityEngine;

/// <summary>
/// PlayerHpBar와 EnemyRoot의 CombatTokenDropTarget에 Player/Enemy CombatActor를 자동 연결합니다.
/// 씬 로드 시 또는 활성화 시 Entity를 찾아 Actor 필드에 할당합니다.
/// </summary>
public class AutoAssignCombatTokenTargets : MonoBehaviour
{
    private void Start()
    {
        AssignTargets();
    }

    private void OnEnable()
    {
        Invoke(nameof(AssignTargets), 0f);
    }

    public void AssignTargets()
    {
        // PlayerHpBar - PlayerCombatActor 연결
        var playerHpBar = GameObject.Find("PlayerHpBar");
        if (playerHpBar != null)
        {
            var target = playerHpBar.GetComponent<CombatTokenDropTarget>();
            if (target != null && target.Actor == null)
            {
                var playerActor = FindAnyObjectByType<PlayerCombatActor>();
                if (playerActor != null)
                    target.GetType().GetField("actor", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(target, playerActor);
            }
        }

        // EnemyHpBar & EnemyRoot - 전투 중이면 CombatManager.EnemyActor, 아니면 Scene의 적 사용
        EnemyCombatActor GetEnemyActor()
        {
            var cm = CombatManager.Instance;
            if (cm != null && cm.InCombat && cm.EnemyActor != null) return cm.EnemyActor;
            return FindAnyObjectByType<EnemyCombatActor>();
        }

        var enemyActor = GetEnemyActor();
        foreach (var name in new[] { "EnemyHpBar", "EnemyRoot" })
        {
            var go = GameObject.Find(name);
            if (go == null) continue;
            var target = go.GetComponent<CombatTokenDropTarget>();
            if (target == null || target.Actor != null) continue;
            if (enemyActor != null)
                target.GetType().GetField("actor", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(target, enemyActor);
        }
    }
}
