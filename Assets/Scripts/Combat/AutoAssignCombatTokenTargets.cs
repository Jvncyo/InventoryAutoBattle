using UnityEngine;

/// <summary>
/// PlayerHpBar의 CombatTokenDropTarget에 PlayerCombatActor를 자동 연결합니다.
/// 씬 로드 시 또는 활성화 시 PlayerEntity를 찾아 Actor 필드에 할당합니다.
/// </summary>
public class AutoAssignCombatTokenTargets : MonoBehaviour
{
    private void Start()
    {
        AssignTargets();
    }

    private void OnEnable()
    {
        // 씬 리로드 등으로 인해 Start가 호출되지 않을 수 있으므로 OnEnable에서도 시도
        Invoke(nameof(AssignTargets), 0f);
    }

    public void AssignTargets()
    {
        // PlayerHpBar 찾기
        var playerHpBar = GameObject.Find("PlayerHpBar");
        if (playerHpBar != null)
        {
            var target = playerHpBar.GetComponent<CombatTokenDropTarget>();
            if (target != null && target.Actor == null)
            {
                var playerActor = FindObjectOfType<PlayerCombatActor>();
                if (playerActor != null)
                {
                    target.GetType().GetField("actor", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(target, playerActor);
                    Debug.Log($"[AutoAssignCombatTokenTargets] PlayerHpBar Actor assigned: {playerActor.name}");
                }
            }
        }

        // EnemyHpBar도 안전하게 확인
        var enemyHpBar = GameObject.Find("EnemyHpBar");
        if (enemyHpBar != null)
        {
            var target = enemyHpBar.GetComponent<CombatTokenDropTarget>();
            if (target != null && target.Actor == null)
            {
                var enemyActor = FindObjectOfType<EnemyCombatActor>();
                if (enemyActor != null)
                {
                    target.GetType().GetField("actor", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(target, enemyActor);
                    Debug.Log($"[AutoAssignCombatTokenTargets] EnemyHpBar Actor assigned: {enemyActor.name}");
                }
            }
        }
    }
}