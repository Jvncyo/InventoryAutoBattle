using UnityEngine;
using UnityEngine.UI;
using InventoryAutoBattle.Core;

namespace InventoryAutoBattle.GameLoop
{
    /// <summary>
    /// 메인 HUD 및 게임 상태별 팝업(승리/패배/보상) UI 매니저.
    /// 체력바 갱신, 스테이지 표시, 보상/게임오버 팝업 제어를 담당합니다.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Header("상단 HUD")]
        [SerializeField] private Text txtStageNumber;
        [SerializeField] private Slider playerHpSlider;
        [SerializeField] private Text playerHpText;
        [SerializeField] private Slider enemyHpSlider;
        [SerializeField] private Text enemyHpText;
        [SerializeField] private Text txtInventoryFreeCells;
        [SerializeField] private Text txtExpansionFragments;

        [Header("전투 준비 UI")]
        [SerializeField] private GameObject readyPanel;
        [SerializeField] private Button btnStartBattle;

        [Header("승리 및 보상 팝업")]
        [SerializeField] private GameObject rewardPopup;
        [SerializeField] private Text txtRewardTitle;
        [SerializeField] private Text txtRewardDetail;
        [SerializeField] private Button btnNextStage;

        [Header("패배 / 게임오버 팝업")]
        [SerializeField] private GameObject gameOverPopup;
        [SerializeField] private Text txtGameOverDetail;
        [SerializeField] private Button btnRetry;

        private void Awake()
        {
            if (btnStartBattle != null)
                btnStartBattle.onClick.AddListener(OnStartBattleClicked);

            if (btnNextStage != null)
                btnNextStage.onClick.AddListener(OnNextStageClicked);

            if (btnRetry != null)
                btnRetry.onClick.AddListener(OnRetryClicked);
        }

        private void OnEnable()
        {
            GameEvents.OnPlayerHpChanged += UpdatePlayerHp;
            GameEvents.OnEnemyHpChanged += UpdateEnemyHp;
            GameEvents.OnFreeCellCountChanged += UpdateFreeCellCount;
            GameEvents.OnGameStateChanged += UpdateGameStateUI;
        }

        private void OnDisable()
        {
            GameEvents.OnPlayerHpChanged -= UpdatePlayerHp;
            GameEvents.OnEnemyHpChanged -= UpdateEnemyHp;
            GameEvents.OnFreeCellCountChanged -= UpdateFreeCellCount;
            GameEvents.OnGameStateChanged -= UpdateGameStateUI;
        }

        public void SetupComponents(
            Text stageText, Slider pHpSlider, Text pHpTxt, Slider eHpSlider, Text eHpTxt,
            Text freeCellTxt, Text fragTxt,
            GameObject readyPnl, Button startBtn,
            GameObject rewardPnl, Text rewardTit, Text rewardDet, Button nextBtn,
            GameObject overPnl, Text overDet, Button retryBtn)
        {
            txtStageNumber = stageText;
            playerHpSlider = pHpSlider;
            playerHpText = pHpTxt;
            enemyHpSlider = eHpSlider;
            enemyHpText = eHpTxt;
            txtInventoryFreeCells = freeCellTxt;
            txtExpansionFragments = fragTxt;

            readyPanel = readyPnl;
            btnStartBattle = startBtn;

            rewardPopup = rewardPnl;
            txtRewardTitle = rewardTit;
            txtRewardDetail = rewardDet;
            btnNextStage = nextBtn;

            gameOverPopup = overPnl;
            txtGameOverDetail = overDet;
            btnRetry = retryBtn;

            if (btnStartBattle != null) btnStartBattle.onClick.AddListener(OnStartBattleClicked);
            if (btnNextStage != null) btnNextStage.onClick.AddListener(OnNextStageClicked);
            if (btnRetry != null) btnRetry.onClick.AddListener(OnRetryClicked);
        }

        public void SetStageInfo(StageData stage)
        {
            if (txtStageNumber != null && stage != null)
            {
                txtStageNumber.text = $"<b>{stage.stageName}</b> - <color=#FF8888>{stage.enemyName}</color>";
            }
        }

        public void UpdatePlayerHp(float current, float max)
        {
            if (playerHpSlider != null)
                playerHpSlider.value = Mathf.Clamp01(current / max);

            if (playerHpText != null)
                playerHpText.text = $"용사 HP: {current:F0} / {max:F0}";
        }

        public void UpdateEnemyHp(float current, float max)
        {
            if (enemyHpSlider != null)
                enemyHpSlider.value = Mathf.Clamp01(current / max);

            if (enemyHpText != null)
                enemyHpText.text = $"적 HP: {current:F0} / {max:F0}";
        }

        public void UpdateFreeCellCount(int free, int total)
        {
            if (txtInventoryFreeCells != null)
                txtInventoryFreeCells.text = $"인벤토리 여유: <b>{free}</b> / {total}";
        }

        public void UpdateFragments(int current, int maxForExpansion)
        {
            if (txtExpansionFragments != null)
                txtExpansionFragments.text = $"확장 조각: <b>{current}</b> / {maxForExpansion}";
        }

        public void UpdateGameStateUI(GameState state)
        {
            if (readyPanel != null)
                readyPanel.SetActive(state == GameState.Ready);

            if (rewardPopup != null && state != GameState.Reward)
                rewardPopup.SetActive(false);

            if (gameOverPopup != null && state != GameState.GameOver)
                gameOverPopup.SetActive(false);
        }

        public void ShowRewardPopup(string title, string detail)
        {
            if (rewardPopup != null)
            {
                rewardPopup.SetActive(true);
                if (txtRewardTitle != null) txtRewardTitle.text = title;
                if (txtRewardDetail != null) txtRewardDetail.text = detail;
            }
        }

        public void ShowGameOverPopup(string detail)
        {
            if (gameOverPopup != null)
            {
                gameOverPopup.SetActive(true);
                if (txtGameOverDetail != null) txtGameOverDetail.text = detail;
            }
        }

        private void OnStartBattleClicked()
        {
            GameManager.Instance?.StartBattle();
        }

        private void OnNextStageClicked()
        {
            GameManager.Instance?.ProceedToNextStage();
        }

        private void OnRetryClicked()
        {
            GameManager.Instance?.RetryStage();
        }
    }
}
