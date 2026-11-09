using UnityEngine;

/// <summary>
/// 인벤토리 아이템 삭제(Discard) 서비스.
/// Preparation 상태에서 Canvas 위의 모든 아이템(인벤토리, 장비 드래프트, 토큰 트레이)을 삭제할 수 있다.
/// </summary>
public static class ItemDiscardService
{
    private static readonly System.Collections.Generic.HashSet<ItemInstance> Processing = new();

    public static bool Check(ItemInstance item, out string message)
    {
        if (item == null || item.Data == null)
        { message = "Nothing to discard."; return false; }
        if (Processing.Contains(item)) { message = "Item is being processed."; return false; }

        // Prep 또는 RewardToken 상태에서만 삭제 가능
        var state = RunStateMachine.Instance;
        if (state != null && state.CurrentState != RunStateMachine.RunState.Preparation
            && state.CurrentState != RunStateMachine.RunState.RewardToken)
        { message = "Can only discard before combat."; return false; }

        // 컨테이너에 속해 있는 아이템만 삭제 대상.
        // 플레이어 인벤토리 소유일 때 허용, GameManager가 없는 테스트 환경에서는
        // 컨테이너가 Canvas 위(=UI 영역)에 있으면 허용한다.
        var container = item.CurrentContainer;
        if (container == null)
        { message = "Only items in your inventory can be discarded."; return false; }

        var playerInv = GameManager.Instance?.PlayerInventory;
        bool isPlayerItem = playerInv != null && container == playerInv;
        if (!isPlayerItem && container.GetComponentInParent<Canvas>() == null)
        { message = "Only items in your inventory can be discarded."; return false; }

        // 유일한 무기면 삭제 불가 (방패/방어구는 개수 상관없이 버림 가능)
        if (item.Data.Subtype == EquipmentSubtype.Weapon)
        {
            int weaponCount = 0;
            var invToCount = playerInv != null ? playerInv : container;
            foreach (var i in invToCount.Items)
                if (i != null && i.Data != null && i.Data.Subtype == EquipmentSubtype.Weapon) weaponCount++;
            if (weaponCount <= 1)
            { message = "Cannot discard your only weapon."; return false; }
        }

        message = "Drop to discard.";
        return true;
    }

    public static bool TryDiscard(ItemInstance item)
    {
        if (!Check(item, out var message))
        {
            Debug.LogWarning($"[ItemDiscardService] Cannot discard: {message}");
            return false;
        }

        Processing.Add(item);
        try
        {
            // 컨테이너에서 제거 (ItemRemovedEvent 발화 → 체크포인트 저장 트리거)
            var container = item.CurrentContainer;
            if (container != null) container.RemoveItem(item);

            // 드래프트/보상 관리는 이벤트 구독으로 처리되며,
            // 해당 이벤트가 없으면 수동 제거를 시도한다.
            EventBus.Publish(new ItemDiscardedEvent(item, container));
            Object.DestroyImmediate(item.gameObject);
            Debug.Log($"[ItemDiscardService] Discarded item: {item.Data.DisplayName}");
            return true;
        }
        finally { Processing.Remove(item); }
    }
}
