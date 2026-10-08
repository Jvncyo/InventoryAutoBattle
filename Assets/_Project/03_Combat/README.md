# 03. 자동 전투 & 엔티티 시스템 (개발자 3)

## 담당 역할
- 플레이어 및 적(Monster) 스탯 관리 (HP, ATK, DEF)
- 캐릭터 자동 이동, 적 조우 및 기본 공격 자동 쿨다운 로직
- 인벤토리 장비 효과(`OnItemTriggered`) 수신 및 즉시 데미지/방어/회복 연산 반영
- HP 변동 이벤트 발행 및 승리/패배(`OnBattleFinished`) 판정

## 주요 에셋
- `Scripts/CharacterBase.cs`
- `Scripts/PlayerEntity.cs`
- `Scripts/EnemyEntity.cs`
- `Scripts/CombatManager.cs`
- `Prefabs/CombatSystem.prefab`
- `Scenes/Dev3_Combat_Test.unity` (독립 테스트 씬)

## 인터페이스 연동
- **수신:**
  - `GameEvents.OnItemTriggered(ItemType type, float value)`: 장비 효과 발동 수치 수신
  - `GameEvents.OnStageStarted(StageData stage)`: 적 스탯 세팅 및 플레이어 초기화
- **발행:**
  - `GameEvents.OnBattleFinished(bool isPlayerWin)`: 승리(적 HP 0) 또는 패배(플레이어 HP 0) 전달
  - `GameEvents.OnPlayerHpChanged(float current, float max)`: 플레이어 체력 갱신
  - `GameEvents.OnEnemyHpChanged(float current, float max)`: 적 체력 갱신
