using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// PlayerHpBar와 EnemyRoot의 CombatTokenDropTarget에 Player/Enemy CombatActor를 자동 연결합니다.
/// 씬 로드 시 또는 활성화 시 Entity를 찾아 Actor 필드에 할당합니다.
/// EnemyRoot에 RectTransform/Image/CombatTokenDropTarget이 없으면 자동 추가합니다.
/// </summary>
public class AutoAssignCombatTokenTargets : MonoBehaviour
{
    private void Start()
    {
        EnsureEnemyRootDropTarget();
        AssignTargets();
    }

    private void OnEnable()
    {
        Invoke(nameof(AssignTargets), 0f);
    }

    /// <summary>
    /// EnemyRoot가 전체 전장 드롭 타겟으로 동작하도록 RectTransform, Image, CombatTokenDropTarget을 보장합니다.
    /// </summary>
    private void EnsureEnemyRootDropTarget()
    {
        var enemyRoot = GameObject.Find("EnemyRoot");
        if (enemyRoot == null) return;

        // RectTransform 보장 (Transform -> RectTransform 변환 또는 추가)
        var rt = enemyRoot.GetComponent<RectTransform>();
        if (rt == null)
        {
            // Transform을 RectTransform으로 교체 (Unity에서는 AddComponent<RectTransform> 가능)
            rt = enemyRoot.AddComponent<RectTransform>();
        }
        // 전체 화면 앵커 설정 (0,0)-(1,1)
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);

        // Image 컴포넌트 보장 (레이캐스트 타겟용)
        var image = enemyRoot.GetComponent<Image>();
        if (image == null)
        {
            image = enemyRoot.AddComponent<Image>();
            image.color = new Color(0, 0, 0, 0); // 투명
            image.raycastTarget = true;
        }

        // CombatTokenDropTarget 보장
        var target = enemyRoot.GetComponent<CombatTokenDropTarget>();
        if (target == null)
        {
            target = enemyRoot.AddComponent<CombatTokenDropTarget>();
            // highlight는 Inspector에서 연결하거나 런타임에 찾아서 설정
            var highlightImg = enemyRoot.GetComponent<Image>();
            target.GetType().GetField("highlight", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(target, highlightImg);
        }
    }

    public void AssignTargets()
    {
        var playerActor = FindAnyObjectByType<PlayerCombatActor>();
        var enemyActor = GetEnemyActor();

        // 씬의 모든 CombatTokenDropTarget에 대해 actor 자동 할당
        foreach (var target in FindObjectsByType<CombatTokenDropTarget>(FindObjectsSortMode.None))
        {
            if (target == null || target.Actor != null) continue;

            CombatActor actor = null;
            // Enemy 영역 오브젝트인지 확인 (이름 기반)
            var goName = target.transform.root.name;
            if (goName.Contains("Enemy") || goName.Contains("EnemyRoot"))
                actor = enemyActor;
            else if (goName.Contains("Player") || goName.Contains("PlayerHpBar"))
                actor = playerActor;
            else
            {
                // 위치 기반으로 Player/Enemy 구분 (y좌표가 높으면 Enemy)
                var y = target.transform.position.y;
                actor = y > 0 ? enemyActor : playerActor;
            }

            if (actor != null)
                target.GetType().GetField("actor", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(target, actor);
        }
    }

    private EnemyCombatActor GetEnemyActor()
    {
        var cm = CombatManager.Instance;
        if (cm != null && cm.InCombat && cm.EnemyActor != null) return cm.EnemyActor;
        return FindAnyObjectByType<EnemyCombatActor>();
    }
}
