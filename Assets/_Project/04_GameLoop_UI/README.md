# 04. 게임 루프 & UI / 보상 시스템 (개발자 4)

## 담당 역할
- 전체 게임 흐름 제어 (FSM: Ready → Battle → Reward / GameOver)
- 스테이지 진행 관리 (스테이지 순차 증가, 난이도 스케일링)
- 전투 승리 시 보상 처리 (아이템 획득, 인벤토리 확장 조각 적립)
- 상단 HUD(스테이지 번호, 플레이어/적 HP 바) 및 결과/보상 팝업 UI

## 주요 에셋
- `Scripts/GameManager.cs`
- `Scripts/StageManager.cs`
- `Scripts/UIManager.cs`
- `Prefabs/MainUI.prefab`
- `Scenes/Dev4_GameLoop_Test.unity` (독립 테스트 씬)

## 인터페이스 연동
- **수신:**
  - `GameEvents.OnBattleFinished(bool isPlayerWin)`: 전투 결과 수신 후 Reward 또는 GameOver 상태 전환
  - `GameEvents.OnPlayerHpChanged(float current, float max)`: UI 플레이어 HP 바 갱신
  - `GameEvents.OnEnemyHpChanged(float current, float max)`: UI 적 HP 바 갱신
- **발행:**
  - `GameEvents.OnStageStarted(StageData stage)`: 다음 스테이지 시작 신호
  - `GameEvents.OnGameStateChanged(GameState state)`: 게임 페이즈 전환 알림
