public static class InventoryInteractionPolicy
{
    public static bool CanInteract(ItemInstance item, ItemContainer target = null)
    {
        if (item == null || item.Data == null || item.CurrentContainer == null) return false;
        var state = RunStateMachine.Instance;
        bool combat = CombatManager.Instance != null && CombatManager.Instance.InCombat;
        if (state != null)
        {
            if (state.CurrentState == RunStateMachine.RunState.GameOver
                || state.CurrentState == RunStateMachine.RunState.RewardPhase) return false;
            combat |= state.CurrentState == RunStateMachine.RunState.Combat;
        }
        if (!combat) return true;
        var inventory = GameManager.Instance != null ? GameManager.Instance.PlayerInventory : null;
        return item.Data.ItemType == ItemType.Token && inventory != null
            && item.CurrentContainer == inventory && (target == null || target == inventory);
    }
}
