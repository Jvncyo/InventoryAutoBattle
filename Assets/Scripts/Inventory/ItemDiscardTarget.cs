using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 인벤토리 아이템 삭제존(Discard Zone) 드롭 타겟.
/// Preparation 상태에서 아이템을 여기에 드롭하면 영구 삭제된다.
/// </summary>
public class ItemDiscardTarget : MonoBehaviour
{
    [SerializeField] private Image highlight;

    public Image Highlight => highlight;
    public void SetHighlight(Image img) { highlight = img; }

    private Color _normal;

    private void Awake() { if (highlight != null) _normal = highlight.color; }

    public bool Contains(Vector2 point, Camera camera)
        => isActiveAndEnabled && RectTransformUtility.RectangleContainsScreenPoint(transform as RectTransform, point, camera);

    public void Preview(ItemInstance item, bool hovered)
    {
        if (highlight == null) return;
        bool valid = item != null && ItemDiscardService.Check(item, out _);
        highlight.color = item == null ? _normal : valid
            ? (hovered ? new Color(0.9f, 0.25f, 0.25f) : new Color(0.6f, 0.3f, 0.3f))
            : _normal;
    }
}
