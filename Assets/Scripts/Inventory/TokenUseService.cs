using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 전투 토큰(Combat Token) 사용 검증 및 적용 서비스.
/// 강화 토큰(Enhancement Token)은 더 이상 인벤토리에 존재하지 않으므로 관련 로직을 제거했다.
/// </summary>
public static class TokenUseService
{
    private static readonly HashSet<ItemInstance> Processing = new();
    public static string Message { get; private set; } = "";
    public static void ClearMessage() => Message = "";

    public static bool Preview(ItemInstance token, CombatActor actor = null)
    {
        bool valid = Check(token, actor, out var message);
        Message = message;
        return valid;
    }

    public static bool Check(ItemInstance token, CombatActor actor, out string message)
    {
        if (token == null || token.Data == null || token.Data.ItemType != ItemType.Token
            || token.CurrentContainer == null || Processing.Contains(token))
        { message = "Token unavailable."; return false; }
        // 보상 선택 중에는 토큰 사용 불가 (이동/삭제만 가능)
        var state = RunStateMachine.Instance;
        if (state != null && state.CurrentState == RunStateMachine.RunState.RewardPhase)
        { message = "Tokens can only be used in combat."; return false; }
        if (!InventoryInteractionPolicy.CanInteract(token))
        { message = "Cannot use in this state."; return false; }
        var inventory = GameManager.Instance != null ? GameManager.Instance.PlayerInventory : null;

        // 오직 전투 토큰만 처리
        if (token.Data.TokenCategory != TokenCategory.Combat)
        { message = "Only combat tokens can be used this way."; return false; }

        // 드래그 중에는 CurrentContainer가 임시로 바뀔 수 있으므로,
        // 인벤토리에 속해 있었거나 현재 인벤토리에 있으면 허용
        bool wasInInventory = inventory != null &&
            (token.CurrentContainer == inventory ||
             inventory.Items.Contains(token));
        if (!wasInInventory || !(token.Data.TokenEffect is CombatTokenEffectSO effect))
        { message = "Move combat tokens into your inventory first."; return false; }
        return effect.CanApply(actor, out message);
    }

    public static bool TryUse(ItemInstance token, CombatActor actor = null)
    {
        bool valid = Check(token, actor, out var message);
        Message = message;
        if (!valid) return false;
        Processing.Add(token);
        try
        {
            bool applied = ((CombatTokenEffectSO)token.Data.TokenEffect).ApplyToActor(actor);
            if (!applied) return false;

            PlayTokenEffect((CombatTokenEffectSO)token.Data.TokenEffect, actor);

            var inventory = GameManager.Instance != null ? GameManager.Instance.PlayerInventory : null;
            // 드래그 중 CurrentContainer가 Canvas 등으로 바뀔 수 있으므로,
            // 토큰이 인베토리에 속해 있었으면 인벤토리에서 직접 제거
            bool wasInInventory = inventory != null && inventory.Items.Contains(token);
            if (wasInInventory && token.CurrentContainer != inventory)
            {
                inventory.RemoveItem(token);
            }
            else
            {
                token.CurrentContainer?.RemoveItem(token);
            }

            EventBus.Publish(new TokenConsumedEvent(token, null));
            Object.Destroy(token.gameObject);
            CombatManager.Instance?.ResolveOutcome();
            return true;
        }
        finally { Processing.Remove(token); }
    }

    private static void PlayTokenEffect(CombatTokenEffectSO effect, CombatActor actor)
    {
        if (effect == null) return;

        // 1. EffectPrefab 필드 사용 (에디터에서 개별 할당)
        GameObject prefab = effect.EffectPrefab;
        if (prefab == null)
        {
            // 2. 폴백: Resources에서 자동 로드
            prefab = Resources.Load<GameObject>("Prefabs/Effect/Lightning");
        }
        if (prefab == null) return;

        Transform parent = actor != null ? actor.transform : null;
        GameObject go = Object.Instantiate(prefab, parent);
        if (go == null) return;

        // 프리셋 오브셋 제거
        go.transform.localPosition = Vector3.zero;
        go.transform.localScale = Vector3.one;

        // 애니메이션 재생
        var animator = go.GetComponent<Animator>();
        if (animator != null && animator.enabled) animator.Play("lightning");

        // 재생 후 파괴 (0.5초 = 애니메이션 0.25초 + 여유)
        Object.Destroy(go, 0.5f);
    }
}