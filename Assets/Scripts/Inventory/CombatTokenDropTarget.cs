using UnityEngine;
using UnityEngine.UI;

public class CombatTokenDropTarget : MonoBehaviour
{
    [SerializeField] private CombatActor actor;
    [SerializeField] private Image highlight;
    private Color _normal;
    public CombatActor Actor => actor;
    private void Awake() { if (highlight != null) _normal = highlight.color; }
    public bool Contains(Vector2 point, Camera camera)
        => isActiveAndEnabled && RectTransformUtility.RectangleContainsScreenPoint(transform as RectTransform, point, camera);
    public void Preview(ItemInstance token, bool hovered)
    {
        if (highlight == null) return;
        bool valid = token != null && TokenUseService.Check(token, actor, out _);
        highlight.color = token == null ? _normal : valid
            ? (hovered ? Color.green : new Color(0.4f, 0.7f, 0.4f))
            : (hovered ? new Color(0.8f, 0.3f, 0.3f) : _normal);
    }
}
