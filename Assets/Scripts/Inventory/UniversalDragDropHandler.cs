using UnityEngine;

public class UniversalDragDropHandler : MonoBehaviour
{
    public static UniversalDragDropHandler Instance { get; private set; }

    private void Awake() => Instance = this;

    public void ExecuteDrop(ItemInstance draggedItem, ItemContainer targetContainer, int targetX, int targetY)
    {
        if (draggedItem == null || draggedItem.Data == null)
        {
            CancelDrag(draggedItem);
            return;
        }

        // Validate again at drop time: state may have changed during a drag.
        if (!InventoryInteractionPolicy.CanInteract(draggedItem, targetContainer))
        {
            CancelDrag(draggedItem);
            return;
        }

        var inventory = GameManager.Instance != null ? GameManager.Instance.PlayerInventory : null;
        if (inventory != null && draggedItem.CurrentContainer == inventory && targetContainer != inventory)
        {
            CancelDrag(draggedItem);
            return;
        }

        // 1. Boundary & Container Validation
        if (targetContainer == null || !targetContainer.IsInBounds(targetX, targetY, 1, 1))
        {
            CancelDrag(draggedItem);
            return;
        }

        ItemInstance targetItem = targetContainer.GetItemAt(targetX, targetY);

        // CASE A: Dropping an Item on another Item (Token -> Equipment Consumption)
        if (targetItem != null && targetItem != draggedItem)
        {
            if (draggedItem.Data.ItemType == ItemType.Token && targetItem.Data != null
                && targetItem.Data.ItemType == ItemType.Equipment)
            {
                ExecuteTokenConsumption(draggedItem, targetItem);
                return;
            }

            // Other Item-on-Item collisions are disallowed
            CancelDrag(draggedItem);
            return;
        }

        // CASE B: Dropping onto Free Grid Cells (Container-to-Container or Move)
        if (targetContainer.CanPlace(draggedItem, targetX, targetY))
        {
            if (!targetContainer.PlaceItem(draggedItem, targetX, targetY)) CancelDrag(draggedItem);
        }
        else
        {
            CancelDrag(draggedItem);
        }
    }

    private void ExecuteTokenConsumption(ItemInstance token, ItemInstance equipment)
    {
        if (!TokenUseService.TryUse(token, equipment)) CancelDrag(token);
    }

    public void CancelDrag(ItemInstance item)
    {
        if (item == null) return;
        // Reposition view back to item.GridX, item.GridY in item.CurrentContainer
        EventBus.Publish(new ItemPlacementResetEvent(item));
    }
}