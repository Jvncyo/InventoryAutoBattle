using UnityEngine;
using UnityEngine.UI;
using InventoryAutoBattle.Core;

namespace InventoryAutoBattle.GameLoop
{
    /// <summary>
    /// Dev4_GameLoop_Test 씬 전용 테스트 매니저.
    /// 전투 모듈 없이도 '강제 승리', '강제 패배' 디버그 버튼으로 게임 루프/FSM과 HUD/팝업을 단독 검증합니다.
    /// </summary>
    public class GameLoopTestManager : MonoBehaviour
    {
        [Header("참조 컴포넌트")]
        [SerializeField] private GameManager gameManager;
        [SerializeField] private Text txtStatus;
        [SerializeField] private Text txtLog;
        [SerializeField] private Text txtGuide;

        [Header("디버그 버튼")]
        [SerializeField] private Button btnWin;
        [SerializeField] private Button btnLose;
        [SerializeField] private Button btnDmgPlayer;
        [SerializeField] private Button btnDmgEnemy;
        [SerializeField] private Button btnSimulateInventory;

        private void Awake()
        {
            if (btnWin != null) btnWin.onClick.AddListener(OnClickForceWin);
            if (btnLose != null) btnLose.onClick.AddListener(OnClickForceLose);
            if (btnDmgPlayer != null) btnDmgPlayer.onClick.AddListener(OnClickDamagePlayer);
            if (btnDmgEnemy != null) btnDmgEnemy.onClick.AddListener(OnClickDamageEnemy);
            if (btnSimulateInventory != null) btnSimulateInventory.onClick.AddListener(OnClickSimulateInventory);
        }

        private void OnEnable()
        {
            GameEvents.OnGameStateChanged += HandleGameStateChanged;
            GameEvents.OnBattleFinished += HandleBattleFinished;
            GameEvents.OnStageStarted += HandleStageStarted;
        }

        private void OnDisable()
        {
            GameEvents.OnGameStateChanged -= HandleGameStateChanged;
            GameEvents.OnBattleFinished -= HandleBattleFinished;
            GameEvents.OnStageStarted -= HandleStageStarted;
        }

        private void Start()
        {
            if (txtGuide != null)
            {
                txtGuide.text = "<b>[조작 가이드]</b>\n" +
                                "• <b>'전투 시작' 버튼 (Space)</b>: GameState.Battle 진입\n" +
                                "• <b>'강제 승리' 버튼 (W키)</b>: [Mock] OnBattleFinished(true)\n" +
                                "• <b>'강제 패배' 버튼 (L키)</b>: [Mock] OnBattleFinished(false)\n" +
                                "• <b>P키</b>: 플레이어 피격 / <b>E키</b>: 적 피격\n" +
                                "• <b>승리 3회 달성 시</b>: 인벤토리 확장 조각 3개 완충 보상 연출";
            }

            UpdateStatus();
        }

        private void Update()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb == null) return;

            if (kb.wKey.wasPressedThisFrame) OnClickForceWin();
            if (kb.lKey.wasPressedThisFrame) OnClickForceLose();
            if (kb.pKey.wasPressedThisFrame) OnClickDamagePlayer();
            if (kb.eKey.wasPressedThisFrame) OnClickDamageEnemy();
            if (kb.iKey.wasPressedThisFrame) OnClickSimulateInventory();
            if (kb.spaceKey.wasPressedThisFrame)
            {
                if (gameManager != null && gameManager.CurrentState == GameState.Ready)
                {
                    AddLog("⌨️ Space키 입력: 전투 시작");
                    gameManager.StartBattle();
                }
            }
        }

        #region 디버그 버튼 함수
        public void OnClickForceWin()
        {
            AddLog("🏆 [Mock] 강제 승리 트리거 (OnBattleFinished -> true)");
            GameEvents.OnBattleFinished?.Invoke(true);
        }

        public void OnClickForceLose()
        {
            AddLog("💀 [Mock] 강제 패배 트리거 (OnBattleFinished -> false)");
            GameEvents.OnBattleFinished?.Invoke(false);
        }

        public void OnClickDamagePlayer()
        {
            AddLog("🩸 [Mock] 플레이어 체력 변동 (50 / 100)");
            GameEvents.OnPlayerHpChanged?.Invoke(50f, 100f);
        }

        public void OnClickDamageEnemy()
        {
            AddLog("🩸 [Mock] 적 체력 변동 (30 / 100)");
            GameEvents.OnEnemyHpChanged?.Invoke(30f, 100f);
        }

        public void OnClickSimulateInventory()
        {
            int free = Random.Range(10, 36);
            AddLog($"🎒 [Mock] 인벤토리 빈 셀 변동 ({free} / 36)");
            GameEvents.OnFreeCellCountChanged?.Invoke(free, 36);
        }
        #endregion

        #region 이벤트 수신 핸들러
        private void HandleGameStateChanged(GameState state)
        {
            AddLog($"<color=magenta>[FSM] 상태 변경: {state}</color>");
            UpdateStatus();
        }

        private void HandleBattleFinished(bool isWin)
        {
            string res = isWin ? "<color=green>전투 승리</color>" : "<color=red>전투 패배</color>";
            AddLog($"[전투 결과 수신] {res}");
        }

        private void HandleStageStarted(StageData stage)
        {
            AddLog($"🚩 [새 스테이지 시작] {stage.stageName} - {stage.enemyName}");
            // 초기 체력바 Mock 세팅
            GameEvents.OnPlayerHpChanged?.Invoke(100f, 100f);
            GameEvents.OnEnemyHpChanged?.Invoke(stage.enemyMaxHp, stage.enemyMaxHp);
            UpdateStatus();
        }

        private void UpdateStatus()
        {
            if (txtStatus != null && gameManager != null)
            {
                int stageNum = gameManager.Stages != null ? gameManager.Stages.CurrentStageNumber : 1;
                txtStatus.text = $"현재 상태: <b>{gameManager.CurrentState}</b> | 스테이지: <b>{stageNum}</b>";
            }
        }

        private void AddLog(string msg)
        {
            Debug.Log($"[Dev4_Test] {msg}");
            if (txtLog != null)
            {
                txtLog.text = $"[{System.DateTime.Now:HH:mm:ss}] {msg}\n" + txtLog.text;
                if (txtLog.text.Length > 500)
                {
                    txtLog.text = txtLog.text.Substring(0, 500);
                }
            }
        }
        #endregion
    }
}
