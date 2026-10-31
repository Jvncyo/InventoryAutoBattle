using UnityEngine;
using UnityEngine.UI;
using InventoryAutoBattle.Core;

namespace InventoryAutoBattle.Combat
{
    /// <summary>
    /// Dev3_Combat_Test 씬 전용 테스트 매니저.
    /// 플레이어/적 체력 UI 실시간 연동, 자동 이동 및 타격, 아이템 효과(1, 2, 3 키 Mock), 승패 판정을 검증합니다.
    /// </summary>
    public class CombatTestManager : MonoBehaviour
    {
        [Header("참조 컴포넌트")]
        [SerializeField] private CombatManager combatManager;
        [SerializeField] private Slider playerHpSlider;
        [SerializeField] private Text playerHpText;
        [SerializeField] private Slider enemyHpSlider;
        [SerializeField] private Text enemyHpText;
        [SerializeField] private Text txtStatus;
        [SerializeField] private Text txtLog;
        [SerializeField] private Text txtGuide;
        [SerializeField] private Button btnToggleBattle;
        [SerializeField] private Text txtToggleBattle;

        private void OnEnable()
        {
            GameEvents.OnPlayerHpChanged += HandlePlayerHpChanged;
            GameEvents.OnEnemyHpChanged += HandleEnemyHpChanged;
            GameEvents.OnBattleFinished += HandleBattleFinished;
            GameEvents.OnItemTriggered += HandleItemTriggered;
        }

        private void OnDisable()
        {
            GameEvents.OnPlayerHpChanged -= HandlePlayerHpChanged;
            GameEvents.OnEnemyHpChanged -= HandleEnemyHpChanged;
            GameEvents.OnBattleFinished -= HandleBattleFinished;
            GameEvents.OnItemTriggered -= HandleItemTriggered;
        }

        private void Start()
        {
            if (txtGuide != null)
            {
                txtGuide.text = "<b>[조작 가이드]</b>\n" +
                                "• <b>전투 시작 버튼</b>: 양측이 마주보고 이동하여 자동 타격\n" +
                                "• <b>숫자 1키</b>: [Mock] 공격 발동 (적에게 25 피해)\n" +
                                "• <b>숫자 2키</b>: [Mock] 방패 발동 (플레이어 실드 +20)\n" +
                                "• <b>숫자 3키</b>: [Mock] 물약 발동 (플레이어 HP +30 회복)\n" +
                                "• 한쪽 체력이 0이 되면 OnBattleFinished 자동 판정";
            }

            UpdateStateUI();
        }

        private void Update()
        {
            // 인벤토리 모듈 없이도 키보드로 아이템 효과를 Mock 트리거
            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
            {
                OnClickTriggerWeapon();
            }
            if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
            {
                OnClickTriggerShield();
            }
            if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
            {
                OnClickTriggerPotion();
            }
        }

        #region 버튼 연동 함수
        public void OnClickToggleBattle()
        {
            if (combatManager == null) return;

            if (combatManager.IsBattleRunning)
            {
                combatManager.StopBattle();
                GameEvents.OnGameStateChanged?.Invoke(GameState.Ready);
                AddLog("⏹ 전투 중지");
            }
            else
            {
                combatManager.StartBattle();
                GameEvents.OnGameStateChanged?.Invoke(GameState.Battle);
                AddLog("⚔️ 전투 시작");
            }

            UpdateStateUI();
        }

        public void OnClickResetCombat()
        {
            if (combatManager != null)
            {
                combatManager.ResetCombat();
                GameEvents.OnGameStateChanged?.Invoke(GameState.Ready);
                AddLog("🔄 전투 리셋 (초기화 완료)");
            }
            UpdateStateUI();
        }

        public void OnClickTriggerWeapon()
        {
            AddLog("⚔️ [Mock] 무기 효과 발동 (공격력 25)");
            GameEvents.OnItemTriggered?.Invoke(ItemType.Weapon, 25f);
        }

        public void OnClickTriggerShield()
        {
            AddLog("🛡️ [Mock] 방패 효과 발동 (보호막 +20)");
            GameEvents.OnItemTriggered?.Invoke(ItemType.Shield, 20f);
        }

        public void OnClickTriggerPotion()
        {
            AddLog("❤️ [Mock] 물약 효과 발동 (HP 회복 +30)");
            GameEvents.OnItemTriggered?.Invoke(ItemType.Potion, 30f);
        }
        #endregion

        #region 이벤트 수신 핸들러
        private void HandlePlayerHpChanged(float cur, float max)
        {
            if (playerHpSlider != null)
            {
                playerHpSlider.value = Mathf.Clamp01(cur / max);
            }
            if (playerHpText != null)
            {
                float shield = combatManager != null && combatManager.Player != null ? combatManager.Player.CurrentShield : 0f;
                string shieldStr = shield > 0f ? $" <color=#66CCFF>(+{shield:F0})</color>" : "";
                playerHpText.text = $"용사 HP: {cur:F0} / {max:F0}{shieldStr}";
            }
        }

        private void HandleEnemyHpChanged(float cur, float max)
        {
            if (enemyHpSlider != null)
            {
                enemyHpSlider.value = Mathf.Clamp01(cur / max);
            }
            if (enemyHpText != null)
            {
                enemyHpText.text = $"적 HP: {cur:F0} / {max:F0}";
            }
        }

        private void HandleBattleFinished(bool isPlayerWin)
        {
            string msg = isPlayerWin ? "<color=#55FF55>🎉 [승리] 플레이어 승리!</color>" : "<color=#FF5555>💀 [패배] 플레이어 패배...</color>";
            AddLog(msg);
            UpdateStateUI();
        }

        private void HandleItemTriggered(ItemType type, float value)
        {
            // 로그창에 아이템 발동 내역 표기
        }

        private void UpdateStateUI()
        {
            bool running = combatManager != null && combatManager.IsBattleRunning;

            if (txtToggleBattle != null)
            {
                txtToggleBattle.text = running ? "전투 중지 (Stop)" : "전투 시작 (Start)";
            }

            if (txtStatus != null)
            {
                txtStatus.text = running ? "현재 상태: <color=#FF5555>전투 진행 중 (BATTLE)</color>" : "현재 상태: <color=#55FF55>대기 중 (READY)</color>";
            }
        }

        private void AddLog(string msg)
        {
            Debug.Log($"[Dev3_Test] {msg}");
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
