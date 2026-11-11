using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RunStateMachine : MonoBehaviour
{
    public enum RunState { Preparation, Combat, RewardPhase, RewardToken, GameOver }
    public static RunStateMachine Instance { get; private set; }
    public RunState CurrentState { get; private set; }
    public string StatusMessage { get; private set; }

    [Header("Core References")]
    [SerializeField] private CombatManager combatManager;
    [SerializeField] private RewardManager rewardManager;
    [SerializeField] private UnifiedRewardUI unifiedRewardUI;
    [SerializeField] private UnifiedRewardService unifiedRewardService;
    [SerializeField] private ItemContainer playerInventory;
    [SerializeField] private StageProgressionManager stageProgressionManager;
    [SerializeField] private List<ItemDataSO> startingItems = new();
    [SerializeField] private GameObject tokenProceedButton;

    private bool _initialized;
    private bool _completingReward;
    private SaveManager _save;
    private bool _restoring;
    private bool _savePending;
    public bool RecoveryRequired { get; private set; }
    public bool PersistenceBlocked => RecoveryRequired || _restoring;
    public bool SaveFailed { get; private set; }
    public string SaveStatus { get; private set; } = "";

    private List<UnifiedRewardOption> _currentRewardOptions;

    private void Awake()
    {
        Instance = this;
        ResolveReferences();
    }
    private void OnDestroy() { if (Instance == this) Instance = null; }

    private void OnEnable()
    {
        EventBus.Subscribe<CombatEndedEvent>(OnCombatEnded);
        EventBus.Subscribe<RewardPhaseFinishedEvent>(OnRewardPhaseFinished);
        EventBus.Subscribe<ItemPlacedEvent>(OnInventoryPlaced);
        EventBus.Subscribe<ItemRemovedEvent>(OnInventoryRemoved);
        if (!_initialized) RetryLoad();
    }

    private void Start()
    {
        ResolveReferences();
    }

    private void ResolveReferences()
    {
        if (_save == null) _save = GetComponent<SaveManager>();
        if (playerInventory == null && GameManager.Instance != null)
            playerInventory = GameManager.Instance.PlayerInventory;
        if (stageProgressionManager == null) stageProgressionManager = StageProgressionManager.Instance;
        if (combatManager == null) combatManager = CombatManager.Instance;
        if (unifiedRewardService == null) unifiedRewardService = FindObjectOfType<UnifiedRewardService>();
        if (unifiedRewardUI == null) unifiedRewardUI = FindObjectOfType<UnifiedRewardUI>();
    }

    public void RetryLoad()
    {
        ResolveReferences();
        if (_initialized && !RecoveryRequired) return;
        _restoring = true;
        RecoveryRequired = false;
        try
        {
            if (_save == null || playerInventory == null || stageProgressionManager == null || combatManager?.PlayerActor == null)
            { FailRecovery("Missing save/run references."); return; }
            var result = _save.TryLoad(out var data, playerInventory.Width, playerInventory.Height);
            if (result == SaveLoadStatus.Failed) { FailRecovery(_save.LastError); return; }
            if (result == SaveLoadStatus.Ended)
            {
                ChangeState(RunState.GameOver);
                StatusMessage = "Previous run ended. Start a new run.";
                return;
            }
            if (result == SaveLoadStatus.Success)
            {
                int stageToRestore = data.CurrentStage > 0 ? data.CurrentStage : data.CurrentWave;
                stageProgressionManager.RestoreStage(stageToRestore);
                if (!_save.TryRestore(data, playerInventory))
                { FailRecovery(string.IsNullOrEmpty(_save.LastError) ? "Invalid stage configuration." : _save.LastError); return; }
                combatManager.PlayerActor.Initialize(data.PlayerMaxHP, data.PlayerMaxHP);
                
                // 복원 후 인벤토리가 비어있으면 시작 아이템 지급
                Debug.Log($"[RunStateMachine] After TryRestore - Items.Count={playerInventory.Items.Count}, startingItems={startingItems.Count}");
                if (playerInventory.Items.Count == 0 && startingItems.Count > 0)
                {
                    Debug.Log("[RunStateMachine] Inventory empty after restore, initializing starting items");
                    InitializeStartingInventory();
                }
            }
            else
            {
                stageProgressionManager.CurrentStage = 1;
                stageProgressionManager.RestoreStage(1);
                if (stageProgressionManager.GetCurrentStageData() == null) { FailRecovery("Invalid stage configuration."); return; }
                stageProgressionManager.StartRun();
                InitializeStartingInventory();
            }
            ChangeState(RunState.Preparation);
            foreach (var view in FindObjectsByType<ItemContainerView>(FindObjectsSortMode.None)) view.RefreshViews();
            if (result == SaveLoadStatus.Missing) _savePending = true;
            StatusMessage = result == SaveLoadStatus.Success
                ? (_save.LoadedBackup ? "Restored backup checkpoint." : "Preparation checkpoint restored.") : "New run.";
        }
        finally { _restoring = false; }
    }

    private void FailRecovery(string error)
    {
        RecoveryRequired = true;
        _savePending = false;
        StatusMessage = "Load failed: " + error + " Retry, or preserve files and start a new run.";
    }

    public void StartNewAfterRecovery()
    {
        if (!RecoveryRequired && CurrentState != RunState.GameOver) return;
        _save = GetComponent<SaveManager>();
        if (_save == null || !_save.ArchiveAndClear())
        { StatusMessage = _save != null ? _save.LastError : "SaveManager missing."; return; }
        combatManager?.StopCombat();
        SceneManager.LoadScene(SceneManager.GetActiveScene().path);
    }

    public bool SaveCheckpoint()
    {
        if (PersistenceBlocked || CurrentState != RunState.Preparation) return false;
        _savePending = false;
        SaveFailed = _save == null || !_save.TrySave(stageProgressionManager.CurrentStage, combatManager.PlayerActor, playerInventory);
        SaveStatus = SaveFailed ? "Save failed: " + (_save != null ? _save.LastError : "SaveManager missing.") : "";
        StatusMessage = SaveStatus;
        return !SaveFailed;
    }

    public void RetrySave() => SaveCheckpoint();

    private void OnInventoryPlaced(ItemPlacedEvent e) { if (e.Container == playerInventory) MarkCheckpointDirty(); }
    private void OnInventoryRemoved(ItemRemovedEvent e) { if (e.Container == playerInventory) MarkCheckpointDirty(); }
    private void MarkCheckpointDirty()
    {
        if (_initialized && !PersistenceBlocked && CurrentState == RunState.Preparation) _savePending = true;
    }
    private void LateUpdate() { if (_savePending && !PersistenceBlocked && CurrentState == RunState.Preparation) SaveCheckpoint(); }

    private void InitializeStartingInventory()
    {
        if (playerInventory == null || playerInventory.Items.Count > 0) return;
        foreach (var data in startingItems)
        {
            if (data == null) continue;
            var go = new GameObject($"Starting_{data.DisplayName}");
            var item = go.AddComponent<ItemInstance>();
            item.Initialize(data);
            bool placed = false;
            for (int y = 0; y < playerInventory.Height && !placed; y++)
                for (int x = 0; x < playerInventory.Width && !placed; x++)
                    placed = playerInventory.PlaceItem(item, x, y);
            if (!placed) Destroy(go);
        }
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<CombatEndedEvent>(OnCombatEnded);
        EventBus.Unsubscribe<RewardPhaseFinishedEvent>(OnRewardPhaseFinished);
        EventBus.Unsubscribe<ItemPlacedEvent>(OnInventoryPlaced);
        EventBus.Unsubscribe<ItemRemovedEvent>(OnInventoryRemoved);
    }

    private void OnCombatEnded(CombatEndedEvent ended)
    {
        if (CurrentState != RunState.Combat) return;
        combatManager?.StopCombat();
        if (!ended.IsVictory)
        {
            _save?.EndRun();
            stageProgressionManager?.EndRun();
            ChangeState(RunState.GameOver);
            return;
        }
        ChangeState(RunState.RewardPhase);
    }

    private void OnRewardPhaseFinished(RewardPhaseFinishedEvent _)
    {
        if (CurrentState == RunState.RewardPhase) ChangeState(RunState.RewardToken);
    }

    public void ChangeState(RunState next)
    {
        if (_initialized && CurrentState == next) return;
        if (next == RunState.Combat)
        {
            if (CurrentState != RunState.Preparation || combatManager == null || stageProgressionManager == null
                || !stageProgressionManager.IsRunActive() || stageProgressionManager.GetCurrentStageEnemy() == null
                || combatManager.PlayerActor == null)
            {
                StatusMessage = "Cannot start combat: check stage and actor references.";
                return;
            }
        }
        foreach (var view in FindObjectsByType<ItemView>(FindObjectsSortMode.None)) view.CancelActiveDrag();
        if (CurrentState == RunState.RewardPhase) unifiedRewardUI?.Close();
        if (next != RunState.Combat) combatManager?.StopCombat();
        var prev = CurrentState;
        CurrentState = next;
        _initialized = true;
        StatusMessage = "";
        if (tokenProceedButton != null) tokenProceedButton.SetActive(next == RunState.RewardToken);
        EventBus.Publish(new RunStateChangedEvent(next));

        switch (next)
        {
            case RunState.Preparation:
                rewardManager?.ClearTokenTray();
                combatManager?.PlayerActor?.ResetForNewWave();

                if (combatManager?.PlayerActor?.IsDead == true)
                    combatManager.PlayerActor.ResetCombatVisuals();
                if (combatManager?.EnemyActor?.IsDead == true)
                    combatManager.EnemyActor.ResetCombatVisuals();

                if (prev == RunState.RewardToken)
                {
                    if (_save != null && !_save.TrySave(stageProgressionManager.CurrentStage, combatManager.PlayerActor, playerInventory))
                        SaveFailed = true;
                    else { SaveFailed = false; _savePending = false; }
                }
                else
                {
                    _savePending = false;
                }
                break;
            case RunState.Combat:
                if (_save != null && !PersistenceBlocked)
                {
                    if (!_save.TrySave(stageProgressionManager.CurrentStage, combatManager.PlayerActor, playerInventory))
                        SaveFailed = true;
                    else { SaveFailed = false; _savePending = false; }
                }
                combatManager.StartCombat(stageProgressionManager.GetCurrentStageEnemy());
                break;
            case RunState.RewardPhase:
                if (prev == RunState.Combat && _save != null && !PersistenceBlocked)
                {
                    if (!_save.TrySave(stageProgressionManager.CurrentStage, combatManager.PlayerActor, playerInventory))
                        SaveFailed = true;
                    else SaveFailed = false;
                }
                GenerateAndShowUnifiedReward();
                break;
            case RunState.RewardToken:
                rewardManager?.GenerateTokenRewardTray(stageProgressionManager.GetCombatTokenPool());
                break;
            case RunState.GameOver:
                rewardManager?.ClearTokenTray();
                unifiedRewardUI?.Close();
                break;
        }
    }

    private void GenerateAndShowUnifiedReward()
    {
        if (unifiedRewardService == null || unifiedRewardUI == null || playerInventory == null || stageProgressionManager == null)
        {
            Debug.LogWarning("Unified reward references missing; skipping to token reward.");
            ChangeState(RunState.RewardToken);
            return;
        }

        var stageData = stageProgressionManager.GetCurrentStageData();
        var equipPool = stageData?.equipmentDropPool ?? new List<ItemDataSO>();

        _currentRewardOptions = unifiedRewardService.GenerateOptions(playerInventory, equipPool, 3);

        if (_currentRewardOptions == null || _currentRewardOptions.Count == 0)
        {
            // 옵션이 없으면(장비도 없고 강화할 것도 없음) 바로 토큰 단계로
            ChangeState(RunState.RewardToken);
            return;
        }

        unifiedRewardUI.ShowDraft(_currentRewardOptions,
            OnUnifiedRewardSelected,
            OnUnifiedRewardSkip);
    }

    private void OnUnifiedRewardSelected(int index)
    {
        if (_currentRewardOptions == null || index < 0 || index >= _currentRewardOptions.Count)
        {
            Debug.LogWarning("Invalid reward option index.");
            return;
        }

        var option = _currentRewardOptions[index];
        bool success = false;

        if (option.Type == RewardOptionType.Equipment)
        {
            success = PlaceEquipmentInInventory(option.EquipmentData);
        }
        else // Enhancement
        {
            success = option.Apply(); // StatBuffEffectSO.ApplyToEquipment 호출
            if (success)
            {
                // 강화 적용 시 아이템 뷰 갱신
                foreach (var view in FindObjectsByType<ItemContainerView>(FindObjectsSortMode.None)) view.RefreshViews();
            }
        }

        if (success)
        {
            // 즉시 저장
            if (_save != null && !_save.TrySave(stageProgressionManager.CurrentStage, combatManager.PlayerActor, playerInventory))
                SaveFailed = true;
            else SaveFailed = false;

            _currentRewardOptions = null;
            EventBus.Publish(new RewardPhaseFinishedEvent());
        }
        else
        {
            unifiedRewardUI?.ShowMessage("적용 실패. 다른 옵션을 선택하거나 스킵하세요.");
        }
    }

    private bool PlaceEquipmentInInventory(ItemDataSO equipmentData)
    {
        if (playerInventory == null || equipmentData == null || equipmentData.ItemType != ItemType.Equipment)
            return false;

        for (int y = 0; y < playerInventory.Height; y++)
        {
            for (int x = 0; x < playerInventory.Width; x++)
            {
                if (!playerInventory.CanPlace(equipmentData, x, y)) continue;

                GameObject go = new GameObject($"Equipment_{equipmentData.Id}");
                ItemInstance instance = go.AddComponent<ItemInstance>();
                instance.Initialize(equipmentData);

                if (playerInventory.PlaceItem(instance, x, y)) return true;
                Destroy(go);
            }
        }
        return false;
    }

    private void OnUnifiedRewardSkip()
    {
        _currentRewardOptions = null;
        EventBus.Publish(new RewardPhaseFinishedEvent());
    }

    public void OnStartCombatButtonClicked()
    {
        if (CurrentState == RunState.Preparation) ChangeState(RunState.Combat);
    }

    public void CompleteRewardPhase()
    {
        if (CurrentState != RunState.RewardToken || _completingReward) return;
        _completingReward = true;
        try
        {
            rewardManager?.FinalizeRewardPhase();
            if (stageProgressionManager != null)
            {
                stageProgressionManager.AdvanceStage();
                ChangeState(stageProgressionManager.IsRunActive() ? RunState.Preparation : RunState.GameOver);
            }
            else ChangeState(RunState.GameOver);
        }
        finally { _completingReward = false; }
    }

    public void OnProceedTokenRewardButtonClicked() => CompleteRewardPhase();

    public void RestartRun()
    {
        if (CurrentState != RunState.GameOver) return;
        _save?.ArchiveAndClear();
        combatManager?.StopCombat();
        SceneManager.LoadScene(SceneManager.GetActiveScene().path);
    }
}