using System.Collections.Generic;
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
            if (token.CurrentContainer != inventory || !(token.Data.TokenEffect is CombatTokenEffectSO effect))
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
            token.CurrentContainer.RemoveItem(token);
            EventBus.Publish(new TokenConsumedEvent(token, equipment));
            Object.Destroy(token.gameObject);
            // Finish consumption before a lethal effect opens the reward UI.
            CombatManager.Instance?.ResolveOutcome();
            return true;
        }
        finally { Processing.Remove(token); }
    }
}
