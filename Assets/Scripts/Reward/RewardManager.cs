using System.Collections.Generic;
using UnityEngine;

public class RewardManager : MonoBehaviour
{
    [SerializeField] private ItemContainer tokenRewardTray; // 5x1 Container
    [SerializeField] private GameObject itemPrefab;
    [SerializeField] private EquipmentDraftUI equipmentDraftUI;
    [SerializeField] private GameManager gameManager;
    private bool _draftActive;

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
            // Runtime data object; ItemContainerView owns the separate visual prefab.
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

    public void TriggerRewardPhase(List<ItemDataSO> enhancementTokenPool)
    {
        if (_draftActive) return;
        _draftActive = true;

        // Deduplicate before sampling so repeated pool entries cannot occupy two choices.
        var availableTokens = new List<ItemDataSO>();
        var uniqueTokens = new HashSet<ItemDataSO>();
        if (enhancementTokenPool != null)
        {
            foreach (ItemDataSO token in enhancementTokenPool)
            {
                if (token != null && token.ItemType == ItemType.Token &&
                    token.TokenCategory == TokenCategory.Enhancement && uniqueTokens.Add(token))
                    availableTokens.Add(token);
            }
        }

        if (availableTokens.Count == 0)
        {
            // No candidates: use the same single-completion path as Skip.
            OnRewardSkip();
            return;
        }

        var draftTokens = new List<ItemDataSO>();
        while (draftTokens.Count < 3 && availableTokens.Count > 0)
        {
            int randomIndex = Random.Range(0, availableTokens.Count);
            draftTokens.Add(availableTokens[randomIndex]);
            availableTokens.RemoveAt(randomIndex);
        }

        if (equipmentDraftUI == null)
        {
            Debug.LogWarning("Enhancement token draft UI is not assigned; skipping enhancement token reward.");
            OnRewardSkip();
            return;
        }

        equipmentDraftUI.ShowDraft(draftTokens,
            idx => OnEnhancementTokenSelected(idx, draftTokens),
            OnRewardSkip);
    }

    private void OnEnhancementTokenSelected(int index, List<ItemDataSO> draftTokens)
    {
        if (!_draftActive || draftTokens == null || index < 0 || index >= draftTokens.Count) return;

        ItemDataSO selectedToken = draftTokens[index];
        bool placed = PlaceEnhancementTokenInInventory(selectedToken);

        // Selecting an enhancement token advances to the combat token reward stage.
        if (placed)
        {
            _draftActive = false;
            equipmentDraftUI?.Close();
            EventBus.Publish(new RewardPhaseFinishedEvent());
        }
        else equipmentDraftUI?.ShowMessage("Not enough inventory space for this enhancement token. Choose another enhancement token or Skip.");
    }

    private bool PlaceEnhancementTokenInInventory(ItemDataSO tokenData)
    {
        if (gameManager?.PlayerInventory == null || tokenData == null ||
            tokenData.ItemType != ItemType.Token || tokenData.TokenCategory != TokenCategory.Enhancement)
            return false;

        // Store the token in the inventory; applying it to equipment is a separate action.
        for (int y = 0; y < gameManager.PlayerInventory.Height; y++)
        {
            for (int x = 0; x < gameManager.PlayerInventory.Width; x++)
            {
                if (!gameManager.PlayerInventory.CanPlace(tokenData, x, y)) continue;

                GameObject go = new GameObject($"EnhancementToken_{tokenData.Id}");
                ItemInstance instance = go.AddComponent<ItemInstance>();
                instance.Initialize(tokenData);

                if (gameManager.PlayerInventory.PlaceItem(instance, x, y)) return true;
                Destroy(go);
            }
        }
        return false;
    }

    private void OnRewardSkip()
    {
        if (!_draftActive) return;
        _draftActive = false;
        equipmentDraftUI?.Close();
        EventBus.Publish(new RewardPhaseFinishedEvent());
    }
}