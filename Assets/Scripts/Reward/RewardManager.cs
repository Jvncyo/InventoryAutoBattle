using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 전투 토큰 보상 트레이만 관리합니다.
/// 장비 드래프트/강화는 RunStateMachine의 UnifiedRewardService/UI가 담당합니다.
/// </summary>
public class RewardManager : MonoBehaviour
{
    [SerializeField] private ItemContainer tokenRewardTray; // 5x1 Container

    public ItemContainer TokenRewardTray => tokenRewardTray;

    public void GenerateTokenRewardTray(List<ItemDataSO> combatTokenPool)
    {
        if (tokenRewardTray == null)
        {
            Debug.LogError("Cannot generate token rewards: token reward tray is not assigned.");
            return;
        }

        tokenRewardTray.ClearContainer(destroyGameObjects: true);
        tokenRewardTray.gameObject.SetActive(true);

        var availableTokens = combatTokenPool?.FindAll(item =>
            item != null && item.ItemType == ItemType.Token && item.TokenCategory == TokenCategory.Combat);

        for (int x = 0; x < Mathf.Min(5, tokenRewardTray.Width); x++)
        {
            if (availableTokens == null || availableTokens.Count == 0) break;

            // Sample with replacement: combat rewards may contain repeated tokens.
            ItemDataSO randomToken = availableTokens[Random.Range(0, availableTokens.Count)];
            GameObject go = new GameObject($"Token_{randomToken.Id}");
            ItemInstance instance = go.GetComponent<ItemInstance>();
            if (instance == null) instance = go.AddComponent<ItemInstance>();
            instance.Initialize(randomToken);

            if (!tokenRewardTray.PlaceItem(instance, x, 0)) Destroy(go);
        }

        EventBus.Publish(new TokenTrayOpenedEvent());
    }

    public void FinalizeRewardPhase()
    {
        ClearTokenTray();
        EventBus.Publish(new RewardPhaseFinishedEvent());
    }

    public void ClearTokenTray()
    {
        // 획득하지 않고 트레이에 남겨진 토큰 일괄 제거 (영구 소멸)
        if (tokenRewardTray != null)
        {
            tokenRewardTray.ClearContainer(destroyGameObjects: true);
            tokenRewardTray.gameObject.SetActive(false);
        }
    }
}