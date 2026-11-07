using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RunStateMachine : MonoBehaviour
{
    public enum RunState { Preparation, Combat, RewardEquipment, RewardToken, GameOver }
    public static RunStateMachine Instance { get; private set; }
    public RunState CurrentState { get; private set; }
    public string StatusMessage { get; private set; }

    [SerializeField] private CombatManager combatManager;
    [SerializeField] private RewardManager rewardManager;
    [SerializeField] private EquipmentDraftUI equipmentDraftUI;
    [SerializeField] private ItemContainer playerInventory;
    [SerializeField] private WaveManager waveManager;
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
        // On scene reload, Start may not be called immediately; ensure recovery runs once.
        if (!_initialized) RetryLoad();
    }

    private void Start()
    {
        // Already called in OnEnable for scene reload resilience
        ResolveReferences();
    }

    private void ResolveReferences()
    {
        if (_save == null) _save = GetComponent<SaveManager>();
        if (playerInventory == null && GameManager.Instance != null)
            playerInventory = GameManager.Instance.PlayerInventory;
        if (waveManager == null) waveManager = WaveManager.Instance;
        if (combatManager == null) combatManager = CombatManager.Instance;
    }

    public void RetryLoad()
    {
        ResolveReferences();
        if (_initialized && !RecoveryRequired) return;
        _restoring = true;
        RecoveryRequired = false;
        try
        {
            if (_save == null || playerInventory == null || waveManager == null || combatManager?.PlayerActor == null)
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
                if (!waveManager.RestoreWave(data.CurrentWave) || !_save.TryRestore(data, playerInventory))
                { FailRecovery(string.IsNullOrEmpty(_save.LastError) ? "Invalid wave configuration." : _save.LastError); return; }
                combatManager.PlayerActor.Initialize(data.PlayerMaxHP, data.PlayerMaxHP);
            }
            else
            {
                waveManager.StartRun();
                if (!waveManager.IsRunActive) { FailRecovery("Invalid wave configuration."); return; }
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
        SaveFailed = _save == null || !_save.TrySave(waveManager.CurrentWave, combatManager.PlayerActor, playerInventory);
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
            waveManager?.EndRun();
            ChangeState(RunState.GameOver);
            return;
        }
        ChangeState(RunState.RewardEquipment);
    }

    private void OnRewardPhaseFinished(RewardPhaseFinishedEvent _)
    {
        if (CurrentState == RunState.RewardEquipment) ChangeState(RunState.RewardToken);
    }

    public void ChangeState(RunState next)
    {
        if (_initialized && CurrentState == next) return;
        if (next == RunState.Combat)
        {
            if (CurrentState != RunState.Preparation || combatManager == null || waveManager == null
                || !waveManager.IsRunActive || waveManager.GetCurrentEnemyData() == null
                || combatManager.PlayerActor == null || combatManager.EnemyActor == null)
            {
                StatusMessage = "Cannot start combat: check wave and actor references.";
                return;
            }
        }
        foreach (var view in FindObjectsByType<ItemView>(FindObjectsSortMode.None)) view.CancelActiveDrag();
        if (CurrentState == RunState.RewardEquipment) equipmentDraftUI?.Close();
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
                
                // 사망한 액터의 애니메이션 상태 리셋 (Die → Idle)
                if (combatManager?.PlayerActor?.IsDead == true)
                    combatManager.PlayerActor.ResetCombatVisuals();
                if (combatManager?.EnemyActor?.IsDead == true)
                    combatManager.EnemyActor.ResetCombatVisuals();
                
                if (prev == RunState.RewardToken)
                {
                    if (_save != null && !_save.TrySave(waveManager.CurrentWave, combatManager.PlayerActor, playerInventory))
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
                    if (!_save.TrySave(waveManager.CurrentWave, combatManager.PlayerActor, playerInventory))
                        SaveFailed = true;
                    else { SaveFailed = false; _savePending = false; }
                }
                combatManager.StartCombat(waveManager.GetCurrentEnemyData());
                break;
            case RunState.RewardEquipment:
                if (prev == RunState.Combat && _save != null && !PersistenceBlocked)
                {
                    if (!_save.TrySave(waveManager.CurrentWave, combatManager.PlayerActor, playerInventory))
                        SaveFailed = true;
                    else SaveFailed = false;
                }
                if (rewardManager != null) rewardManager.TriggerRewardPhase(waveManager?.GetEnhancementTokenPool());
                else ChangeState(RunState.RewardToken);
                break;
            case RunState.RewardToken:
                rewardManager?.GenerateTokenRewardTray(waveManager?.GetCombatTokenPool());
                break;
            case RunState.GameOver:
                rewardManager?.ClearTokenTray();
                equipmentDraftUI?.Close();
                break;
        }
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
            if (waveManager != null && waveManager.IsRunActive)
            {
                waveManager.NextWave();
                ChangeState(waveManager.IsRunActive ? RunState.Preparation : RunState.GameOver);
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
