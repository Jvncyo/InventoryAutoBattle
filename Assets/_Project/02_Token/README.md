# 02. 실시간 토큰 시스템 (개발자 2)

## 담당 역할
- 전투 진행 중 타이머 기반 무작위 토큰 생성 (공격/방어/회복)
- 토큰 5초 유효시간 타이머 UI/FX (원형 쿨다운/깜빡임 등) 및 만료 시 자동 소멸
- 토큰 드래그 앤 드롭 메커니즘
- 인벤토리 장비 슬롯에 드롭 시 검증 후 이벤트 전달

## 주요 에셋
- `Scripts/TokenSpawner.cs`
- `Scripts/TokenInstance.cs`
- `Prefabs/TokenSystem.prefab`
- `Scenes/Dev2_Token_Test.unity` (독립 테스트 씬)

## 인터페이스 연동
- **수신:** `GameEvents.OnGameStateChanged(GameState state)`
  - 전투 시작(`GameState.Battle`) 시 토큰 스폰 개시, 종료 시 스폰 중지 및 잔여 토큰 정리
- **발행:** `GameEvents.OnTokenApplied(TokenData token, object itemTarget)`
  - 플레이어가 토큰을 장비 슬롯에 떨어뜨렸을 때 발행
