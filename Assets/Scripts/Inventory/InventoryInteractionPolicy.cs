public static class InventoryInteractionPolicy
{
    public static bool CanInteract(ItemInstance item, ItemContainer target = null)
    {
        if (item == null || item.Data == null || item.CurrentContainer == null) return false;
        var state = RunStateMachine.Instance;
        bool combat = CombatManager.Instance != null && CombatManager.Instance.InCombat;
        if (state != null)
        {
            if (state.CurrentState == RunStateMachine.RunState.GameOver) return false;
            // 보상 선택 중: 인벤토리 내부 이동/삭제용 드래그만 허용
            if (state.CurrentState == RunStateMachine.RunState.RewardPhase)
            {
                var inv = GameManager.Instance != null ? GameManager.Instance.PlayerInventory : null;
                return item.CurrentContainer == inv && (target == null || target == inv);
            }
            combat |= state.CurrentState == RunStateMachine.RunState.Combat;
        }
        if (!combat) return true;
        var inventory = GameManager.Instance != null ? GameManager.Instance.PlayerInventory : null;
        return item.Data.ItemType == ItemType.Token && inventory != null
            && item.CurrentContainer == inventory && (target == null || target == inventory);
    }
}
