using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CellSlotView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public int X { get; private set; }
    public int Y { get; private set; }
    public ItemContainerView ContainerView { get; private set; }

    [SerializeField] private Image backgroundImage;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color highlightColor = Color.yellow;

    public void Initialize(int x, int y, ItemContainerView containerView)
    {
        X = x;
        Y = y;
        ContainerView = containerView;
        if (backgroundImage == null) backgroundImage = GetComponent<Image>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (backgroundImage != null)
            backgroundImage.color = highlightColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (backgroundImage != null)
            backgroundImage.color = normalColor;
    }
}