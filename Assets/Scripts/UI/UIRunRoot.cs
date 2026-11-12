using UnityEngine;
using UnityEngine.UI;

public class UIRunRoot : MonoBehaviour
{
    [Header("Run References")]
    [SerializeField] private RunStateMachine runStateMachine;
    [SerializeField] private CombatManager combatManager;
    [SerializeField] private ItemContainer playerInventory;
    [SerializeField] private StageProgressionManager stageProgressionManager;

    [Header("Actions")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button proceedButton;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button recoveryRetryButton;
    [SerializeField] private Button recoveryNewRunButton;

    [Header("HUD")]
    [SerializeField] private Text stageText;
    [SerializeField] private Text freeCellsText;
    [SerializeField] private Text playerHPText;
    [SerializeField] private Text enemyHPText;
    [SerializeField] private Text statusText;
    [SerializeField] private Text saveStatusText;
    [SerializeField] private Slider playerHealthSlider;
    [SerializeField] private Slider enemyHealthSlider;
    [SerializeField] private GameObject gameOverPopup;
    [SerializeField] private GameObject recoveryPanel;
    [SerializeField] private Text recoveryMessageText;

    private Button boundStartButton;
    private Button boundProceedButton;
    private Button boundRetryButton;
    private Button boundRecoveryRetryButton;
    private Button boundRecoveryNewRunButton;

    private void OnEnable()
    {
        boundStartButton = startButton;
        boundProceedButton = proceedButton;
        boundRetryButton = retryButton;
        boundRecoveryRetryButton = recoveryRetryButton;
        boundRecoveryNewRunButton = recoveryNewRunButton;
        if (boundStartButton != null) boundStartButton.onClick.AddListener(StartCombat);
        if (boundProceedButton != null) boundProceedButton.onClick.AddListener(Proceed);
        if (boundRetryButton != null) boundRetryButton.onClick.AddListener(Retry);
        if (boundRecoveryRetryButton != null) boundRecoveryRetryButton.onClick.AddListener(RecoveryRetry);
        if (boundRecoveryNewRunButton != null) boundRecoveryNewRunButton.onClick.AddListener(RecoveryNewRun);
        Refresh();
    }

    private void OnDisable()
    {
        if (boundStartButton != null) boundStartButton.onClick.RemoveListener(StartCombat);
        if (boundProceedButton != null) boundProceedButton.onClick.RemoveListener(Proceed);
        if (boundRetryButton != null) boundRetryButton.onClick.RemoveListener(Retry);
        if (boundRecoveryRetryButton != null) boundRecoveryRetryButton.onClick.RemoveListener(RecoveryRetry);
        if (boundRecoveryNewRunButton != null) boundRecoveryNewRunButton.onClick.RemoveListener(RecoveryNewRun);
        boundStartButton = null;
        boundProceedButton = null;
        boundRetryButton = null;
        boundRecoveryRetryButton = null;
        boundRecoveryNewRunButton = null;
    }

    private void LateUpdate()
    {
        Refresh();
    }

    private RunStateMachine StateMachine => runStateMachine != null ? runStateMachine : RunStateMachine.Instance;
    private StageProgressionManager StageManager => stageProgressionManager != null ? stageProgressionManager : StageProgressionManager.Instance;

    private void StartCombat()
    {
        var machine = StateMachine;
        if (machine != null && machine.CurrentState == RunStateMachine.RunState.Preparation)
            machine.OnStartCombatButtonClicked();
    }

    private void Proceed()
    {
        var machine = StateMachine;
        if (machine != null && machine.CurrentState == RunStateMachine.RunState.RewardToken)
            machine.OnProceedTokenRewardButtonClicked();
    }

    private void Retry()
    {
        var machine = StateMachine;
        if (machine != null && machine.CurrentState == RunStateMachine.RunState.GameOver)
            machine.RestartRun();
    }

    private void RecoveryRetry()
    {
        var machine = StateMachine;
        if (machine != null && machine.RecoveryRequired)
            machine.RetryLoad();
    }

    private void RecoveryNewRun()
    {
        var machine = StateMachine;
        if (machine != null && (machine.RecoveryRequired || machine.CurrentState == RunStateMachine.RunState.GameOver))
            machine.StartNewAfterRecovery();
    }

    private void Refresh()
    {
        var machine = StateMachine;
        var combat = combatManager != null ? combatManager : CombatManager.Instance;
        var stages = StageManager;
        bool preparation = machine != null && machine.CurrentState == RunStateMachine.RunState.Preparation;
        bool tokenReward = machine != null && machine.CurrentState == RunStateMachine.RunState.RewardToken;
        bool gameOver = machine != null && machine.CurrentState == RunStateMachine.RunState.GameOver;
        bool recovery = machine != null && machine.RecoveryRequired;

        SetButtonState(startButton, preparation && !recovery);
        SetButtonState(proceedButton, tokenReward && !recovery);
        SetButtonState(retryButton, gameOver && !recovery);
        SetButtonState(recoveryRetryButton, recovery);
        SetButtonState(recoveryNewRunButton, recovery);
        if (gameOverPopup != null && gameOverPopup.activeSelf != (gameOver && !recovery))
            gameOverPopup.SetActive(gameOver && !recovery);
        if (recoveryPanel != null && recoveryPanel.activeSelf != recovery)
            recoveryPanel.SetActive(recovery);
        if (recoveryMessageText != null)
            recoveryMessageText.text = recovery && machine != null ? machine.StatusMessage : string.Empty;
        if (saveStatusText != null)
            saveStatusText.text = machine != null && machine.SaveFailed ? "Save failed: " + machine.StatusMessage : string.Empty;

        if (stageText != null)
            stageText.text = stages != null ? $"Stage {stages.CurrentStage}" : "Stage --";
        if (freeCellsText != null)
        {
            if (playerInventory == null)
            {
                freeCellsText.text = "Free cells: --";
            }
            else
            {
                int freeCells = 0;
                for (int y = 0; y < playerInventory.Height; y++)
                    for (int x = 0; x < playerInventory.Width; x++)
                        if (playerInventory.GetItemAt(x, y) == null) freeCells++;
                freeCellsText.text = $"남은 공간 : {freeCells}/{playerInventory.Width * playerInventory.Height}";
            }
        }

        string enemyName = (combat != null && combat.EnemyActor != null) ? combat.EnemyActor.EnemyName : "오크";
        UpdateHealth(combat != null ? combat.PlayerActor : null, playerHPText, playerHealthSlider, "플레이어");
        UpdateHealth(combat != null ? combat.EnemyActor : null, enemyHPText, enemyHealthSlider, enemyName);
        if (statusText != null)
            statusText.text = machine != null
                ? $"{machine.CurrentState}  {machine.StatusMessage}  {TokenUseService.Message}" : string.Empty;

        // Inventory interaction and reward modal visibility belong to the FSM's policy.
    }

    private static void SetButtonState(Button button, bool available)
    {
        if (button == null) return;
        button.interactable = available;
        if (button.gameObject.activeSelf != available) button.gameObject.SetActive(available);
    }

    private static void UpdateHealth(CombatActor actor, Text label, Slider slider, string actorName)
    {
        float maximum = actor != null ? Mathf.Max(0f, actor.MaxHP) : 0f;
        float current = actor != null ? Mathf.Clamp(actor.CurrentHP, 0f, maximum) : 0f;
        if (label != null)
            label.text = actor != null ? $"{actorName} HP: {current:0.#}/{maximum:0.#}  Shield: {actor.Shield:0.#}" : $"{actorName} HP: --";
        if (slider != null)
        {
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.SetValueWithoutNotify(maximum > 0f ? current / maximum : 0f);
        }
    }
}
