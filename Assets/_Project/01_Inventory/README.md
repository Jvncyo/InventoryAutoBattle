# 01. 인벤토리 & 그리드 모듈 (개발자 1)

## 담당 역할
- 6×6 셀 기반 그리드 인벤토리 UI 및 내부 데이터 레이어
- 아이템 크기별(1×1, 1×3, 2×2 등) 배치 및 회전(R 키) 로직
- 드래그 앤 드롭 검증 및 셀 중복 배치 방지
- 빈 공간(남은 셀 수) 실시간 계산 및 `GameEvents.OnFreeCellCountChanged` 발행

## 주요 에셋
- `Scripts/InventoryGrid.cs`
- `Scripts/InventorySlot.cs`
- `Prefabs/InventorySystem.prefab`
- `Scenes/Dev1_Inventory_Test.unity` (독립 테스트 씬)

## 인터페이스 연동
- **수신:** `GameEvents.OnTokenApplied(TokenData token, object itemTarget)`
  - 토큰이 장비에 주입되었을 때 필요 토큰 충족 여부 검사
- **발행:** `GameEvents.OnItemTriggered(ItemType type, float value)`
  - 토큰 충전 완료 시 전투 모듈로 장비 발동 신호 전달
