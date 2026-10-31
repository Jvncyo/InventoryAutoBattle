using UnityEngine;
using InventoryAutoBattle.Core;

namespace InventoryAutoBattle.Combat
{
    /// <summary>
    /// 자동 전투 관리 시스템.
    /// 플레이어와 적의 전투 루프, 장비 발동(OnItemTriggered) 처리, 승패(OnBattleFinished) 판정을 담당합니다.
    /// </summary>
    public class CombatManager : MonoBehaviour
    {
        [Header("전투 엔티티")]
        [SerializeField] private PlayerEntity player;
        [SerializeField] private EnemyEntity enemy;

        [Header("전투 상태")]
        [SerializeField] private bool isBattleRunning = false;

        public PlayerEntity Player => player;
        public EnemyEntity Enemy => enemy;
        public bool IsBattleRunning => isBattleRunning;

        private void Awake()
        {
            if (player == null)
                player = GetComponentInChildren<PlayerEntity>();

            if (enemy == null)
                enemy = GetComponentInChildren<EnemyEntity>();
        }

        private void OnEnable()
        {
            GameEvents.OnItemTriggered += HandleItemTriggered;
            GameEvents.OnStageStarted += HandleStageStarted;
            GameEvents.OnGameStateChanged += HandleGameStateChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnItemTriggered -= HandleItemTriggered;
            GameEvents.OnStageStarted -= HandleStageStarted;
            GameEvents.OnGameStateChanged -= HandleGameStateChanged;
        }

        private void Start()
        {
            if (player != null && enemy != null)
            {
                player.SetTarget(enemy);
                enemy.SetTarget(player);
            }
        }

        private void Update()
        {
            if (!isBattleRunning) return;

            // 전투 종료 조건 검사
            if (enemy != null && enemy.IsDead)
            {
                EndBattle(true); // 플레이어 승리
            }
            else if (player != null && player.IsDead)
            {
                EndBattle(false); // 플레이어 패배
            }
        }

        public void StartBattle()
        {
            if (isBattleRunning) return;
            isBattleRunning = true;

            if (player != null && enemy != null)
            {
                player.SetTarget(enemy);
                enemy.SetTarget(player);
                player.SetCombatActive(true);
                enemy.SetCombatActive(true);
            }

            Debug.Log("[CombatManager] ⚔️ 자동 전투 시작!");
        }

        public void StopBattle()
        {
            isBattleRunning = false;
            if (player != null) player.SetCombatActive(false);
            if (enemy != null) enemy.SetCombatActive(false);
            Debug.Log("[CombatManager] ⏹ 자동 전투 중지");
        }

        public void ResetCombat()
        {
            StopBattle();
            if (player != null)
            {
                player.Initialize(100f, 12f, 2f, 1.5f);
                player.ResetPosition();
            }
            if (enemy != null)
            {
                enemy.Initialize(100f, 8f, 0f, 2.0f);
                enemy.ResetPosition();
            }
        }

        private void EndBattle(bool isPlayerWin)
        {
            StopBattle();

            string resultStr = isPlayerWin ? "<color=green>🎉 [전투 승리] 적을 물리쳤습니다!</color>" : "<color=red>💀 [전투 패배] 플레이어가 패배했습니다...</color>";
            Debug.Log($"[CombatManager] {resultStr}");

            // 전역 이벤트 규약 발행 (Dev3 -> Dev4)
            GameEvents.OnBattleFinished?.Invoke(isPlayerWin);
        }

        #region 이벤트 수신 처리
        /// <summary>
        /// 인벤토리/토큰 모듈에서 아이템 발동 신호가 왔을 때 전투 효과 적용
        /// </summary>
        private void HandleItemTriggered(ItemType type, float value)
        {
            Debug.Log($"[CombatManager] 💥 아이템 발동 수신: {type} (값: {value})");

            switch (type)
            {
                case ItemType.Weapon:
                case ItemType.Greatsword:
                case ItemType.Bomb:
                case ItemType.Bow:
                    // 적에게 즉각 피해 적용
                    if (enemy != null && !enemy.IsDead)
                    {
                        enemy.TakeDamage(value);
                    }
                    break;

                case ItemType.Shield:
                    // 플레이어에게 보호막 부여
                    if (player != null && !player.IsDead)
                    {
                        player.AddShield(value);
                    }
                    break;

                case ItemType.Potion:
                    // 플레이어 HP 회복
                    if (player != null && !player.IsDead)
                    {
                        player.Heal(value);
                    }
                    break;
            }
        }

        private void HandleStageStarted(StageData stage)
        {
            if (stage != null && enemy != null)
            {
                enemy.ApplyStageData(stage);
            }
            if (player != null)
            {
                player.ResetPosition();
            }
        }

        private void HandleGameStateChanged(GameState state)
        {
            if (state == GameState.Battle)
            {
                StartBattle();
            }
            else
            {
                StopBattle();
            }
        }
        #endregion
    }
}
