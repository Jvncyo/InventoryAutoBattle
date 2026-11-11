using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// InputSystemUIInputModule의 null action 필드를 ActionsAsset에서 InputActionReference로 재할당합니다.
/// Unity 6 + 임베디드 InputActionAsset 환경에서 액션 바인딩이 초기화되지 않는 문제를 보완합니다.
/// </summary>
public class InputSystemBootstrapper : MonoBehaviour
{
    private bool _fixed;

    private void Update()
    {
        if (_fixed) return;

        var es = EventSystem.current;
        if (es == null) return;

        bool ok = FixInputSystemModule(es);
        if (!ok) AddFallbackStandaloneInputModule(es);
        else _fixed = true;
    }

    private bool FixInputSystemModule(EventSystem es)
    {
        var mod = es.currentInputModule as MonoBehaviour;
        if (mod == null) return false;

        var t = mod.GetType();
        if (!t.Name.Contains("InputSystem")) return false;

        var assetField = t.GetField("m_ActionsAsset",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var asset = assetField?.GetValue(mod) as InputActionAsset;
        if (asset == null) return false;

        // Already set?
        var pointRef = GetRefField(t, mod, "m_PointAction") as InputActionReference;
        if (pointRef != null && pointRef.action != null) return true;

        bool assignedAny = false;
        AssignNullRef(t, mod, "m_PointAction", asset, "Point", ref assignedAny);
        AssignNullRef(t, mod, "m_LeftClickAction", asset, "Click", ref assignedAny);
        AssignNullRef(t, mod, "m_SubmitAction", asset, "Submit", ref assignedAny);
        AssignNullRef(t, mod, "m_CancelAction", asset, "Cancel", ref assignedAny);
        AssignNullRef(t, mod, "m_ScrollWheelAction", asset, "ScrollWheel", ref assignedAny);
        AssignNullRef(t, mod, "m_MoveAction", asset, "Navigate", ref assignedAny);
        AssignNullRef(t, mod, "m_MiddleClickAction", asset, "MiddleClick", ref assignedAny);
        AssignNullRef(t, mod, "m_RightClickAction", asset, "RightClick", ref assignedAny);

        if (assignedAny) Destroy(this);
        return assignedAny;
    }

    private static void AssignNullRef(System.Type t, MonoBehaviour mod,
        string fieldName, InputActionAsset asset, string actionName, ref bool assignedAny)
    {
        var f = t.GetField(fieldName,
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (f == null || f.GetValue(mod) != null) return;

        var act = FindAction(asset, actionName);
        if (act != null)
        {
            try
            {
                var createMethod = typeof(InputActionReference).GetMethod("Create", new[] { typeof(InputAction) });
                if (createMethod != null)
                    f.SetValue(mod, createMethod.Invoke(null, new object[] { act }));
            }
            catch { /* silently skip */ }
            assignedAny = true;
        }
    }

    private static object GetRefField(System.Type t, MonoBehaviour mod, string fieldName)
    {
        return t.GetField(fieldName,
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.GetValue(mod);
    }

    private static InputAction FindAction(InputActionAsset asset, string name)
    {
        foreach (var map in asset.actionMaps)
            foreach (var act in map.actions)
                if (act.name == name) return act;
        return null;
    }

    private static void AddFallbackStandaloneInputModule(EventSystem es)
    {
        if (es.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>() != null) return;
        es.gameObject.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
    }
}