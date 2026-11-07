using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class TokenUseService
{
    private static readonly HashSet<ItemInstance> Processing = new();
    public static string Message { get; private set; } = "";
    public static void ClearMessage() => Message = "";
    public static bool Preview(ItemInstance token, ItemInstance equipment = null, CombatActor actor = null)
    {
        bool valid = Check(token, equipment, actor, out var message);
        Message = message;
        return valid;
    }
    public static bool Check(ItemInstance token, ItemInstance equipment, CombatActor actor, out string message)
    {
        if (token == null || token.Data == null || token.Data.ItemType != ItemType.Token
            || token.CurrentContainer == null || Processing.Contains(token))
        { message = "Token unavailable."; return false; }
        if (!InventoryInteractionPolicy.CanInteract(token))
        { message = "Cannot use in this state."; return false; }
        var inventory = GameManager.Instance != null ? GameManager.Instance.PlayerInventory : null;
        if (token.Data.TokenCategory == TokenCategory.Combat)
        {
            // 드래그 중에는 CurrentContainer가 임시로 바뀔 수 있으므로, 
            // 인벤토리에 속해 있었거나 현재 인벤토리에 있으면 허용
            bool wasInInventory = inventory != null && 
                (token.CurrentContainer == inventory || 
                 inventory.Items.Contains(token));
            if (!wasInInventory || !(token.Data.TokenEffect is CombatTokenEffectSO effect))
            { message = "Move combat tokens into your inventory first."; return false; }
            return effect.CanApply(actor, out message);
        }
        if (equipment == null || equipment.Data == null || equipment.CurrentContainer == null
            || (inventory != null && equipment.CurrentContainer != inventory)
            || equipment.CurrentContainer.GetItemAt(equipment.GridX, equipment.GridY) != equipment
            || equipment.Data.ItemType != ItemType.Equipment
            || !equipment.Data.AcceptableTokens.Contains(token.Data.TokenType))
        { message = "Drop on compatible inventory equipment."; return false; }
        if (token.Data.TokenEffect is StatBuffEffectSO buff) return buff.CanApply(equipment, out message);
        message = "Missing enhancement effect.";
        return false;
    }

    public static bool TryUse(ItemInstance token, ItemInstance equipment = null, CombatActor actor = null)
    {
        bool valid = Check(token, equipment, actor, out var message);
        Message = message;
        if (!valid) return false;
        Processing.Add(token);
        try
        {
            bool applied = token.Data.TokenCategory == TokenCategory.Combat
                ? ((CombatTokenEffectSO)token.Data.TokenEffect).ApplyToActor(actor)
                : token.Data.TokenEffect.ApplyToEquipment(equipment);
            if (!applied) return false;

            var inventory = GameManager.Instance != null ? GameManager.Instance.PlayerInventory : null;
            // 드래그 중 CurrentContainer가 Canvas 등으로 바뀔 수 있으므로,
            // 토큰이 인벤토리에 속해 있었다면 인벤토리에서 직접 제거
            bool wasInInventory = inventory != null && inventory.Items.Contains(token);
            if (wasInInventory && token.CurrentContainer != inventory)
            {
                inventory.RemoveItem(token);
            }
            else
            {
                token.CurrentContainer?.RemoveItem(token);
            }

            EventBus.Publish(new TokenConsumedEvent(token, equipment));
            Object.Destroy(token.gameObject);
            // Finish consumption before a lethal effect opens the reward UI.
            CombatManager.Instance?.ResolveOutcome();
            return true;
        }
        finally { Processing.Remove(token); }
    }
}
