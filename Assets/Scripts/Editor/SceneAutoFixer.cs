using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// 씬 내 누락된 스크립트 참조를 정리하고 EnemyRoot에 올바른 컴포넌트를 설정합니다.
/// 메뉴에서 Tools/Fix Scene Components 실행 또는 플레이 모드 진입 시 자동 실행.
/// </summary>
public class SceneAutoFixer : MonoBehaviour
{
    private void OnEnable()
    {
        Invoke(nameof(FixScene), 0.1f);
    }

    [MenuItem("Tools/Fix Scene Components")]
    public static void FixScene()
    {
        Debug.Log("[SceneAutoFixer] Running scene fix...");

        // 1. Remove orphaned null components
        var allGos = Object.FindObjectsOfType<GameObject>();
        foreach (var go in allGos)
        {
            var comps = go.GetComponents<Component>();
            var toRemove = new List<Component>();
            foreach (var c in comps)
            {
                if (c == null) toRemove.Add(c);
            }
            if (toRemove.Count > 0)
            {
                Debug.LogWarning($"[SceneAutoFixer] Removing null refs from: {go.name} ({toRemove.Count})");
                foreach (var c in toRemove) Object.DestroyImmediate(c);
            }
        }

        // 2. Fix EnemyRoot
        var enemyRoot = GameObject.Find("EnemyRoot");
        if (enemyRoot != null)
        {
            FixRectTransform(enemyRoot);
            var img = enemyRoot.GetComponent<UnityEngine.UI.Image>();
            if (img == null) img = enemyRoot.AddComponent<UnityEngine.UI.Image>();
            img.color = new Color(0f, 0.5f, 1f, 0.05f);
            img.raycastTarget = true;

            var ctdt = enemyRoot.GetComponent<CombatTokenDropTarget>();
            if (ctdt == null) ctdt = enemyRoot.AddComponent<CombatTokenDropTarget>();

            var highlightField = typeof(CombatTokenDropTarget).GetField("highlight",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            highlightField?.SetValue(ctdt, img);

            Debug.Log($"[SceneAutoFixer] Fixed EnemyRoot CTD={ctdt!=null}, RT size={enemyRoot.GetComponent<RectTransform>().sizeDelta}");
        }
        else
        {
            Debug.LogError("[SceneAutoFixer] EnemyRoot not found!");
        }

        // 3. Fix HP bars
        FixHPBar("EnemyHpBar");
        FixHPBar("PlayerHpBar");

        Debug.Log("[SceneAutoFixer] Done.");
    }

    static void FixRectTransform(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        if (rt == null) rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(Screen.width, Screen.height);
    }

    static void FixHPBar(string name)
    {
        var hpBar = GameObject.Find(name);
        if (hpBar == null) return;
        var ctdt = hpBar.GetComponent<CombatTokenDropTarget>();
        if (ctdt != null)
        {
            var img = hpBar.GetComponent<UnityEngine.UI.Image>();
            var f = typeof(CombatTokenDropTarget).GetField("highlight",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (f != null && f.GetValue(ctdt) == null)
                f.SetValue(ctdt, img);
        }
    }
}
