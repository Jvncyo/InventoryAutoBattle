using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// 통합 보상 모달 UI (장비 획득 + 장비 강화).
/// EquipmentDraftUI를 기반으로 하되, 옵션 타입에 따라 색상/텍스트를 다르게 표시한다.
/// </summary>
public class UnifiedRewardUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject modalRoot;
    [SerializeField] private Button[] choiceButtons;
    [SerializeField] private Text[] choiceLabels;
    [SerializeField] private Image[] choiceIcons;
    [SerializeField] private Image[] choiceFrames; // 타입별 색상 구분용 프레임/배경
    [SerializeField] private Text messageText;
    [SerializeField] private Button skipButton;

    [Header("Colors")]
    [SerializeField] private Color equipmentColor = new Color(0.2f, 0.5f, 1f, 1f); // 파란 계열
    [SerializeField] private Color enhancementColor = new Color(1f, 0.7f, 0.1f, 1f); // 황금/주황 계열

    private readonly List<UnifiedRewardOption> candidates = new();
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

    public void ShowDraft(List<UnifiedRewardOption> options, Action<int> onSelected, Action onSkipClicked)
    {
        Close();
        if (!IsConfigured || (options != null && options.Count > choiceButtons.Length))
        {
            Debug.LogWarning("UnifiedRewardUI has insufficient configured choices; skipping reward phase.", this);
            onSkipClicked?.Invoke();
            return;
        }

        if (options != null) candidates.AddRange(options);
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
            UnifiedRewardOption option = i < candidates.Count ? candidates[i] : null;
            if (choiceButtons[i] != null)
            {
                choiceButtons[i].gameObject.SetActive(option != null);
                choiceButtons[i].interactable = isOpen && !isInvoking && !skipSubmitted && option != null;
            }
            if (choiceLabels != null && i < choiceLabels.Length && choiceLabels[i] != null)
                choiceLabels[i].text = option != null ? option.DisplayText : string.Empty;
            if (choiceIcons != null && i < choiceIcons.Length && choiceIcons[i] != null)
            {
                choiceIcons[i].sprite = option != null ? option.Icon : null;
                choiceIcons[i].enabled = option != null && option.Icon != null;
            }
            // 타입별 프레임 색상 변경
            if (choiceFrames != null && i < choiceFrames.Length && choiceFrames[i] != null)
            {
                choiceFrames[i].color = option != null
                    ? (option.Type == RewardOptionType.Equipment ? equipmentColor : enhancementColor)
                    : Color.white;
                choiceFrames[i].enabled = option != null;
            }
        }
        if (skipButton != null) skipButton.interactable = isOpen && !isInvoking && !skipSubmitted;
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
            // 실패 시 같은 드래프트 유지
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