using System;

namespace InventoryAutoBattle.Core
{
    /// <summary>
    /// 모듈 간 결합도를 낮추기 위한 전역 이벤트 버스.
    /// 4명의 개발자가 상호 직접 참조 없이 이벤트를 구독/발행하여 통신합니다.
    /// </summary>
    public static class GameEvents
    {
        #region 개발자 2 (토큰) -> 개발자 1 (인벤토리)
        /// <summary>
        /// 토큰이 인벤토리 특정 아이템 슬롯에 드래그 앤 드롭되었을 때 발행.
        /// 매개변수: (적용된 토큰 데이터, 대상 아이템 식별자/객체)
        /// </summary>
        public static Action<TokenData, object> OnTokenApplied;
        #endregion

        #region 개발자 1 (인벤토리) -> 개발자 3 (전투)
        /// <summary>
        /// 아이템에 토큰 충전이 완료되어 효과가 발동할 때 발행.
        /// 매개변수: (발동 아이템 종류, 수치 - 공격력/방어량/회복량 등)
        /// </summary>
        public static Action<ItemType, float> OnItemTriggered;

        /// <summary>
        /// 인벤토리 여유 공간(빈 칸) 상태가 변경되었을 때 발행 (자원 관리 UI/전략용).
        /// 매개변수: (남은 빈 칸 수, 전체 칸 수)
        /// </summary>
        public static Action<int, int> OnFreeCellCountChanged;
        #endregion

        #region 개발자 3 (전투) -> 개발자 4 (게임 루프 & UI)
        /// <summary>
        /// 전투 종료 시 승리 또는 패배 결과를 알림.
        /// 매개변수: (isPlayerWin: true면 승리, false면 패배)
        /// </summary>
        public static Action<bool> OnBattleFinished;

        /// <summary>
        /// 플레이어 HP 실시간 변화 알림 (UI 체력바 갱신용).
        /// 매개변수: (현재 HP, 최대 HP)
        /// </summary>
        public static Action<float, float> OnPlayerHpChanged;

        /// <summary>
        /// 적 몬스터 HP 실시간 변화 알림 (UI 체력바 갱신용).
        /// 매개변수: (현재 HP, 최대 HP)
        /// </summary>
        public static Action<float, float> OnEnemyHpChanged;
        #endregion

        #region 개발자 4 (게임 루프 & UI) -> 전체 모듈
        /// <summary>
        /// 새로운 스테이지가 시작될 때 시스템 초기화를 위해 발행.
        /// 매개변수: (스테이지 정보 데이터)
        /// </summary>
        public static Action<StageData> OnStageStarted;

        /// <summary>
        /// 게임 상태가 전환될 때 발행 (Ready -> Battle -> Reward -> GameOver).
        /// 매개변수: (새로운 게임 상태)
        /// </summary>
        public static Action<GameState> OnGameStateChanged;
        #endregion

        /// <summary>
        /// 씬 리로드 또는 테스트 샌드박스 초기화 시 이벤트 메모리 누수 방지용 클리어
        /// </summary>
        public static void ClearAllListeners()
        {
            OnTokenApplied = null;
            OnItemTriggered = null;
            OnFreeCellCountChanged = null;
            OnBattleFinished = null;
            OnPlayerHpChanged = null;
            OnEnemyHpChanged = null;
            OnStageStarted = null;
            OnGameStateChanged = null;
        }
    }
}
