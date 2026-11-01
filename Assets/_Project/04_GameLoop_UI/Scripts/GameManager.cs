using UnityEngine;
using InventoryAutoBattle.Core;

namespace InventoryAutoBattle.GameLoop
{
    /// <summary>
    /// 게임 전체 루프와 유한 상태 머신(FSM)을 총괄하는 중앙 매니저.
    /// Ready(정비) -> Battle(전투) -> Reward(보상) / GameOver(패배) 순환을 제어합니다.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("하위 매니저 참조")]
        [SerializeField] private StageManager stageManager;
        [SerializeField] private UIManager uiManager;

        [Header("게임 상태")]
        [SerializeField] private GameState currentState = GameState.Ready;

        [Header("인벤토리 확장 조각 시스템")]
        [SerializeField] private int collectedFragments = 0;
        [SerializeField] private int fragmentsRequiredForExpansion = 3;

        public GameState CurrentState => currentState;
        public int CollectedFragments => collectedFragments;
        public StageManager Stages => stageManager;
        public UIManager UI => uiManager;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            if (stageManager == null)
                stageManager = GetComponentInChildren<StageManager>();

            if (uiManager == null)
                uiManager = GetComponentInChildren<UIManager>();
        }

        private void OnEnable()
        {
            GameEvents.OnBattleFinished += HandleBattleFinished;
        }

        private void OnDisable()
        {
            GameEvents.OnBattleFinished -= HandleBattleFinished;
        }

        private void Start()
        {
            InitializeGame();
        }

        public void InitializeGame()
        {
            collectedFragments = 0;
            if (stageManager != null)
            {
                stageManager.Initialize();
            }

            SetState(GameState.Ready);
            NotifyStageStart();
        }

        public void SetState(GameState newState)
        {
            currentState = newState;
            Debug.Log($"<color=magenta>[GameManager] 🔄 상태 전환: {newState}</color>");

            GameEvents.OnGameStateChanged?.Invoke(newState);

            if (uiManager != null)
            {
                uiManager.UpdateGameStateUI(newState);
                uiManager.UpdateFragments(collectedFragments, fragmentsRequiredForExpansion);
            }
        }

        public void StartBattle()
        {
            if (currentState != GameState.Ready) return;
            SetState(GameState.Battle);
        }

        public void ProceedToNextStage()
        {
            if (stageManager != null)
            {
                stageManager.NextStage();
            }

            SetState(GameState.Ready);
            NotifyStageStart();
        }

        public void RetryStage()
        {
            if (stageManager != null)
            {
                stageManager.RestartStage();
            }

            SetState(GameState.Ready);
            NotifyStageStart();
        }

        private void NotifyStageStart()
        {
            StageData stage = stageManager != null ? stageManager.CurrentStage : null;
            if (stage != null)
            {
                if (uiManager != null)
                    uiManager.SetStageInfo(stage);

                // 전체 모듈에 새 스테이지 개시 알림 (전투/인벤토리 리셋 등)
                GameEvents.OnStageStarted?.Invoke(stage);
            }
        }

        private void HandleBattleFinished(bool isPlayerWin)
        {
            if (currentState != GameState.Battle) return;

            if (isPlayerWin)
            {
                // 승리: 조각 획득 및 Reward 상태 진입
                collectedFragments++;
                bool canExpand = collectedFragments >= fragmentsRequiredForExpansion;
                if (canExpand)
                {
                    collectedFragments -= fragmentsRequiredForExpansion;
                }

                SetState(GameState.Reward);

                string title = "🎉 스테이지 승리!";
                string detail = canExpand 
                    ? $"축하합니다! 확장 조각 {fragmentsRequiredForExpansion}개를 모아 <b>인벤토리 추가 공간</b>을 개방했습니다!"
                    : $"적을 물리쳤습니다! 확장 조각 (+1) 획득! ({collectedFragments}/{fragmentsRequiredForExpansion})";

                if (uiManager != null)
                {
                    uiManager.ShowRewardPopup(title, detail);
                }
            }
            else
            {
                // 패배: GameOver 상태 진입
                SetState(GameState.GameOver);

                string detail = "용사가 쓰러졌습니다...\n전열을 가다듬고 다시 도전하세요!";
                if (uiManager != null)
                {
                    uiManager.ShowGameOverPopup(detail);
                }
            }
        }
    }
}
