using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class EquipmentDraftUI : MonoBehaviour
{
    [SerializeField] private GameObject modalRoot;
    [SerializeField] private Button[] choiceButtons;
    [SerializeField] private Text[] choiceLabels;
    [SerializeField] private Image[] choiceIcons;
    [SerializeField] private Text messageText;
    [SerializeField] private Button skipButton;

    private readonly List<ItemDataSO> candidates = new();
    private readonly List<Button> listenerButtons = new();
    private readonly List<UnityAction> choiceListeners = new();
    private Button boundSkipButton;
    private Action<int> onItemSelected;
    private Action onSkip;
    private bool isOpen;
    private bool isInvoking;
    private bool skipSubmitted;
    private int draftVersion;

    public bool IsConfigured
    {
        get
        {
            if (choiceButtons == null || choiceButtons.Length == 0 || skipButton == null)
                return false;

            for (int i = 0; i < choiceButtons.Length; i++)
            {
                if (choiceButtons[i] == null || choiceButtons[i] == skipButton) return false;
                for (int j = 0; j < i; j++)
                    if (choiceButtons[i] == choiceButtons[j]) return false;
            }
            return true;
        }
    }

    private void OnEnable()
    {
        if (isOpen) BindListeners();
    }

    private void OnDisable()
    {
        RemoveOwnedListeners();
    }

    public void ShowDraft(List<ItemDataSO> enhancementTokenPool, Action<int> onSelected, Action onSkipClicked)
    {
        Close();
        if (!IsConfigured || (enhancementTokenPool != null && enhancementTokenPool.Count > choiceButtons.Length))
        {
            Debug.LogWarning("Enhancement token draft UI has insufficient configured choices; skipping enhancement token reward.", this);
            onSkipClicked?.Invoke();
            return;
        }

        // RewardManager owns sampling. Preserve its exact ordering, including null slots.
        if (enhancementTokenPool != null) candidates.AddRange(enhancementTokenPool);
        onItemSelected = onSelected;
        onSkip = onSkipClicked;
        isOpen = true;
        isInvoking = false;
        skipSubmitted = false;
        ShowMessage(string.Empty);

        gameObject.SetActive(true);
        if (modalRoot != null) modalRoot.SetActive(true);
        RefreshChoices();
        BindListeners();
    }

    public void ShowMessage(string message)
    {
        if (messageText != null) messageText.text = message ?? string.Empty;
    }

    public void Close()
    {
        draftVersion++;
        isOpen = false;
        isInvoking = false;
        skipSubmitted = false;
        RemoveOwnedListeners();
        onItemSelected = null;
        onSkip = null;
        candidates.Clear();
        ShowMessage(string.Empty);
        if (modalRoot != null) modalRoot.SetActive(false);
        else gameObject.SetActive(false);
    }

    private void BindListeners()
    {
        RemoveOwnedListeners();
        if (!isOpen || !isActiveAndEnabled) return;

        for (int i = 0; i < choiceButtons.Length; i++)
        {
            Button button = choiceButtons[i];
            if (button == null) continue;
            int index = i;
            UnityAction listener = () => SelectChoice(index);
            button.onClick.AddListener(listener);
            listenerButtons.Add(button);
            choiceListeners.Add(listener);
        }

        if (skipButton != null)
        {
            boundSkipButton = skipButton;
            boundSkipButton.onClick.AddListener(Skip);
        }
    }

    private void RemoveOwnedListeners()
    {
        for (int i = 0; i < listenerButtons.Count; i++)
            if (listenerButtons[i] != null)
                listenerButtons[i].onClick.RemoveListener(choiceListeners[i]);
        listenerButtons.Clear();
        choiceListeners.Clear();
        if (boundSkipButton != null) boundSkipButton.onClick.RemoveListener(Skip);
        boundSkipButton = null;
    }

    private void RefreshChoices()
    {
        for (int i = 0; i < choiceButtons.Length; i++)
        {
            ItemDataSO item = i < candidates.Count ? candidates[i] : null;
            if (choiceButtons[i] != null)
            {
                choiceButtons[i].gameObject.SetActive(item != null);
                choiceButtons[i].interactable = isOpen && !isInvoking && !skipSubmitted && item != null;
            }
            if (choiceLabels != null && i < choiceLabels.Length && choiceLabels[i] != null)
                choiceLabels[i].text = item != null ? $"{item.DisplayName}\n{item.Width} x {item.Height}\n{GetEnhancementSummary(item)}" : string.Empty;
            if (choiceIcons != null && i < choiceIcons.Length && choiceIcons[i] != null)
            {
                choiceIcons[i].sprite = item != null ? item.Icon : null;
                choiceIcons[i].enabled = item != null && item.Icon != null;
            }
        }
        if (skipButton != null) skipButton.interactable = isOpen && !isInvoking && !skipSubmitted;
    }

    private static string GetEnhancementSummary(ItemDataSO token)
    {
        if (token.TokenEffect is StatBuffEffectSO effect)
        {
            var bonuses = new List<string>();
            if (effect.DamageBonus != 0f) bonuses.Add($"Damage {effect.DamageBonus:+0.##;-0.##;0}");
            if (effect.ShieldBonus != 0f) bonuses.Add($"Shield {effect.ShieldBonus:+0.##;-0.##;0}");
            if (effect.CooldownDeltaRate != 0f) bonuses.Add($"Cooldown {effect.CooldownDeltaRate * 100f:+0.##;-0.##;0}%");
            if (bonuses.Count > 0) return string.Join(" / ", bonuses);
        }
        return "Enhancement token";
    }

    private void SelectChoice(int index)
    {
        if (!isOpen || isInvoking || skipSubmitted || index < 0 || index >= candidates.Count || candidates[index] == null)
            return;

        int version = draftVersion;
        isInvoking = true;
        RefreshChoices();
        try
        {
            onItemSelected?.Invoke(index);
        }
        finally
        {
            // A failed placement leaves the same draft available for another attempt.
            if (isOpen && draftVersion == version)
            {
                isInvoking = false;
                RefreshChoices();
            }
        }
    }

    private void Skip()
    {
        if (!isOpen || isInvoking || skipSubmitted) return;
        skipSubmitted = true;
        RefreshChoices();
        onSkip?.Invoke();
    }
}
