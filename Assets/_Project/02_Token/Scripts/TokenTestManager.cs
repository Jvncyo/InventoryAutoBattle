using UnityEngine;
using UnityEngine.UI;
using InventoryAutoBattle.Core;

namespace InventoryAutoBattle.Token
{
    /// <summary>
    /// Dev2_Token_Test 씬 전용 테스트 매니저.
    /// 실시간 토큰 스폰, 5초 수명 타이머, 드래그 앤 드롭 주입 및 GameEvents 통신을 실시간 검증합니다.
    /// </summary>
    public class TokenTestManager : MonoBehaviour
    {
        [Header("참조 컴포넌트")]
        [SerializeField] private TokenSpawner tokenSpawner;
        [SerializeField] private Button btnToggleBattle;
        [SerializeField] private Text txtToggleBattle;
        [SerializeField] private Text txtStatus;
        [SerializeField] private Text txtLog;
        [SerializeField] private Text txtGuide;

        private bool isBattleState = false;

        private void OnEnable()
        {
            GameEvents.OnTokenApplied += HandleTokenApplied;
            GameEvents.OnItemTriggered += HandleItemTriggered;
        }

        private void OnDisable()
        {
            GameEvents.OnTokenApplied -= HandleTokenApplied;
            GameEvents.OnItemTriggered -= HandleItemTriggered;
        }

        private void Start()
        {
            if (txtGuide != null)
            {
                txtGuide.text = "<b>[조작 가이드]</b>\n" +
                                "• <b>토큰 드래그</b>: 생성된 토큰을 좌측 더미 타깃으로 드롭\n" +
                                "• <b>5초 시한부</b>: 토큰은 5초 후 자동 만료 및 소멸 (1.5초 전 깜빡임)\n" +
                                "• <b>전투 상태</b>: '전투 시작' 클릭 시 주기적 자동 스폰 가동";
            }

            UpdateStateUI();
        }

        #region 버튼 연동 함수
        public void OnClickToggleBattleState()
        {
            isBattleState = !isBattleState;
            GameState newState = isBattleState ? GameState.Battle : GameState.Ready;

            // 게임 상태 변경 이벤트 발행 (Dev4 규약)
            GameEvents.OnGameStateChanged?.Invoke(newState);

            AddLog(isBattleState ? "▶ [전투 시작] GameState.Battle 전환 -> 토큰 자동 스폰 시작!" 
                                 : "⏹ [전투 중지] GameState.Ready 전환 -> 토큰 스폰 중지 및 정리");

            UpdateStateUI();
        }

        public void OnClickSpawnAttackToken()
        {
            if (tokenSpawner != null)
            {
                var t = tokenSpawner.SpawnToken(TokenType.Attack);
                if (t != null) AddLog("수동 스폰: ⚔️ 공격 토큰");
            }
        }

        public void OnClickSpawnDefenseToken()
        {
            if (tokenSpawner != null)
            {
                var t = tokenSpawner.SpawnToken(TokenType.Defense);
                if (t != null) AddLog("수동 스폰: 🛡️ 방어 토큰");
            }
        }

        public void OnClickSpawnHealToken()
        {
            if (tokenSpawner != null)
            {
                var t = tokenSpawner.SpawnToken(TokenType.Heal);
                if (t != null) AddLog("수동 스폰: ❤️ 회복 토큰");
            }
        }

        public void OnClickClearTokens()
        {
            if (tokenSpawner != null)
            {
                tokenSpawner.ClearAllTokens();
                AddLog("모든 활성 토큰 제거 완료");
            }
        }
        #endregion

        #region 이벤트 수신 핸들러
        private void HandleTokenApplied(TokenData token, object itemTarget)
        {
            string targetName = itemTarget != null ? itemTarget.ToString() : "타깃";
            AddLog($"<color=cyan>[토큰 주입 성공] '{token.tokenName}' -> {targetName}</color>");
        }

        private void HandleItemTriggered(ItemType type, float value)
        {
            AddLog($"<color=yellow>💥 [장비 발동] {type} 효과 발동! (수치: {value})</color>");
        }

        private void UpdateStateUI()
        {
            if (txtToggleBattle != null)
            {
                txtToggleBattle.text = isBattleState ? "전투 중지 (Stop Battle)" : "전투 시작 (Start Battle)";
            }

            if (txtStatus != null)
            {
                string stateStr = isBattleState ? "<color=#FF5555>전투 진행 중 (BATTLE)</color>" : "<color=#55FF55>대기 중 (READY)</color>";
                int tokenCount = tokenSpawner != null ? tokenSpawner.ActiveTokenCount : 0;
                int maxTokens = tokenSpawner != null ? tokenSpawner.MaxTokens : 5;
                txtStatus.text = $"현재 상태: {stateStr} | 보유 토큰: <b>{tokenCount}</b> / {maxTokens}";
            }
        }

        private void Update()
        {
            if (txtStatus != null && tokenSpawner != null)
            {
                string stateStr = isBattleState ? "<color=#FF5555>전투 진행 중 (BATTLE)</color>" : "<color=#55FF55>대기 중 (READY)</color>";
                txtStatus.text = $"현재 상태: {stateStr} | 보유 토큰: <b>{tokenSpawner.ActiveTokenCount}</b> / {tokenSpawner.MaxTokens}";
            }
        }

        private void AddLog(string msg)
        {
            Debug.Log($"[Dev2_Test] {msg}");
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
