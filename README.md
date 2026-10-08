# 🎒 Inventory Auto Battle

> **제한된 인벤토리를 전략적으로 구성하고, 자동으로 진행되는 전투 속에서 제한 시간 내에 토큰을 장비에 공급하여 승리하는 전략 오토배틀러 게임**

---

## 📌 목차
1. [프로젝트 개요](#1-프로젝트-개요)
2. [폴더 구조 및 아키텍처](#2-폴더-구조-및-아키텍처)
3. [4인 개발자 역할 분담 (R&R)](#3-4인-개발자-역할-분담-rr)
4. [공통 이벤트 및 데이터 규약 (Shared Protocol)](#4-공통-이벤트-및-데이터-규약-shared-protocol)
5. [Git 협업 및 충돌 방지 규칙](#5-git-협업-및-충돌-방지-규칙)
6. [🤖 AI를 활용한 파트별 개발 가이드 (팀원 필독)](#6--ai를-활용한-파트별-개발-가이드-팀원-필독)

---

## 1. 프로젝트 개요

- **엔진/환경:** Unity 6 (6000.3.x) / 2D URP
- **핵심 루프:**
  ```text
  [인벤토리 정비 (6x6 공간 배치)] 
         ↓
  [자동 전투 시작 (이동 & 기본 타격)] 
         ↓
  [실시간 토큰 생성 (공격/방어/회복, 5초 시한부)]
         ↓
  [플레이어가 토큰을 장비에 실시간 드래그 주입]
         ↓
  [적 처치 & 보상 획득 (아이템 / 인벤토리 확장 조각)]
  ```

---

## 2. 폴더 구조 및 아키텍처

모든 작업은 `Assets/_Project/` 산하에서 이루어지며, 각 개발자는 자신의 모듈 폴더 내에서만 작업하여 **Git 충돌을 원천 차단**합니다.

```text
Assets/
└── _Project/
    ├── 00_Core/              # [공통] 데이터, 열거형, 전역 이벤트 버스
    │   ├── Scripts/          # Enums.cs, GameEvents.cs
    │   │   └── Data/         # ItemData.cs, TokenData.cs, StageData.cs
    │   └── ScriptableObjects/# 실제 생성된 .asset 데이터 파일들
    ├── 01_Inventory/         # [개발자 1] 6x6 그리드 & 아이템 배치
    │   ├── Scripts/          # InventoryGrid.cs 등
    │   ├── Prefabs/          # InventorySystem.prefab
    │   └── Scenes/           # Dev1_Inventory_Test.unity (개인 테스트 씬)
    ├── 02_Token/             # [개발자 2] 실시간 토큰 생성/소멸/드래그
    │   ├── Scripts/          # TokenSpawner.cs, TokenInstance.cs 등
    │   ├── Prefabs/          # TokenSystem.prefab
    │   └── Scenes/           # Dev2_Token_Test.unity
    ├── 03_Combat/            # [개발자 3] 자동 전투 & 플레이어/적 엔티티
    │   ├── Scripts/          # CharacterBase.cs, CombatManager.cs 등
    │   ├── Prefabs/          # CombatSystem.prefab
    │   └── Scenes/           # Dev3_Combat_Test.unity
    ├── 04_GameLoop_UI/       # [개발자 4] 게임 상태 머신, 보상, HUD UI
    │   ├── Scripts/          # GameManager.cs, StageManager.cs 등
    │   ├── Prefabs/          # MainUI.prefab
    │   └── Scenes/           # Dev4_GameLoop_Test.unity
    └── Scenes/
        └── MainScene.unity   # [통합 씬] 4개 프리팹 조립용 (개별 작업 금지)
```

---

## 3. 4인 개발자 역할 분담 (R&R)

| 역할 | 담당자 | 주요 구현 기능 | 핵심 산출물 (Prefab) |
|---|---|---|---|
| **개발자 1** | 인벤토리 & 그리드 | 6×6 격자 인벤토리, 회전(R키), 충돌 검사, 빈 슬롯 수 추적 | `InventorySystem.prefab` |
| **개발자 2** | 실시간 토큰 | 타이머 기반 토큰 스폰, 5초 만료 FX/소멸, 드래그 앤 드롭 주입 | `TokenSystem.prefab` |
| **개발자 3** | 전투 & 엔티티 | 자동 이동, 공격 쿨타임, 스탯(HP/ATK/DEF), 데미지 계산 및 승패 판정 | `CombatSystem.prefab` |
| **개발자 4** | 게임 루프 & UI | 상태 머신(Ready/Battle/Reward), 스테이지 난이도, 보상 및 HUD | `MainUI.prefab` |

---

## 4. 공통 이벤트 및 데이터 규약 (Shared Protocol)

다른 팀원의 코드를 직접 참조(`GetComponent`, 싱글톤 직접 호출 등)하지 않고, **`GameEvents.cs`의 정적 C# Action**을 통해서만 소통합니다.

```text
[개발자 2: 토큰] ── OnTokenApplied ──▶ [개발자 1: 인벤토리]
                                              │
                                       OnItemTriggered
                                              ▼
[개발자 4: 루프] ◀── OnBattleFinished ── [개발자 3: 전투]
```

### 주요 이벤트 명세 (`Assets/_Project/00_Core/Scripts/GameEvents.cs`)

1. **`GameEvents.OnTokenApplied(TokenData token, object itemTarget)`**
   - 토큰이 장비에 드래그되어 주입되었을 때 발행 (Dev 2 → Dev 1)
2. **`GameEvents.OnItemTriggered(ItemType type, float value)`**
   - 장비의 필요 토큰 충전이 완료되어 효과 발동 (Dev 1 → Dev 3)
3. **`GameEvents.OnBattleFinished(bool isPlayerWin)`**
   - 전투 승리/패배 판정 시 발행 (Dev 3 → Dev 4)
4. **`GameEvents.OnStageStarted(StageData stage)`**
   - 새로운 스테이지 시작 시 초기화 (Dev 4 → 전체)
5. **`GameEvents.OnGameStateChanged(GameState state)`**
   - 게임 상태 전환 (`Ready`, `Battle`, `Reward`, `GameOver`) (Dev 4 → 전체)
6. **`GameEvents.OnPlayerHpChanged(float current, float max)` / `OnEnemyHpChanged`**
   - 체력 변동 시 UI 갱신용 (Dev 3 → Dev 4)

---

## 5. Git 협업 및 충돌 방지 규칙

1. **통합 씬(`MainScene.unity`) 직접 수정 금지**
   - 개발 중에는 절대 `MainScene.unity`를 열어서 작업하지 마세요.
   - 각자의 개인 테스트 씬(`Assets/_Project/0X_.../Scenes/`)에서 작업합니다.
2. **모든 작업물은 프리팹(`Prefab`)으로 패키징**
   - 각자 맡은 시스템은 하나의 메인 프리팹(`InventorySystem`, `TokenSystem` 등) 안에 완성된 상태로 커밋합니다.
3. **브랜치 규칙**
   - `feat/dev1-inventory`, `feat/dev2-token`, `feat/dev3-combat`, `feat/dev4-gameloop`
   - 각 브랜치에서 프리팹과 테스트 씬 작업 완료 후 `develop`으로 PR(Pull Request)합니다.

---

## 6. 🤖 AI를 활용한 파트별 개발 가이드 (팀원 필독)

Cursor, ChatGPT, Claude, Gemini 등의 AI 코딩 도구를 사용할 때, **아래 가이드와 프롬프트 템플릿을 복사하여 사용**하면 다른 팀원의 코드와 완벽히 호환되는 코드를 생성할 수 있습니다.

### 💡 AI 사용 시 3대 원칙
1. **네임스페이스와 폴더를 명시하세요**: 자신의 폴더(`Assets/_Project/0X_...`)에 들어갈 코드임을 명시합니다.
2. **공통 코어 타입을 반드시 프롬프트에 포함하세요**: `InventoryAutoBattle.Core`의 `GameEvents`, `ItemData`, `TokenData`, `Enums`를 AI에게 알려주어야 엉뚱한 클래스를 만들지 않습니다.
3. **Mocking(모의 테스트) 코드를 함께 요청하세요**: 다른 팀원의 파트가 없어도 내 씬에서 키보드 단축키나 버튼으로 테스트할 수 있는 디버그 로직을 포함해 달라고 요청하세요.

---

### 📋 AI 프롬프트 템플릿 (각자 역할에 맞게 복사해서 사용)

#### 🔹 [개발자 1] 인벤토리 & 그리드 파트
```markdown
[시스템 및 역할]
Unity 6 (2D) 프로젝트에서 "인벤토리 & 그리드 시스템"을 개발하고 있습니다.
저의 작업 위치는 Assets/_Project/01_Inventory/ 폴더입니다.

[프로젝트 공통 규격]
- 네임스페이스: using InventoryAutoBattle.Core;
- 공통 클래스: ItemData (size: Vector2Int, requiredTokenType, effectValue), ItemType, TokenType
- 이벤트 통신:
  - 수신: GameEvents.OnTokenApplied(TokenData token, object itemTarget)
  - 발행: GameEvents.OnItemTriggered(ItemType type, float value), GameEvents.OnFreeCellCountChanged(int free, int total)

[요청 사항]
6x6 타일 그리드 인벤토리 시스템(InventoryGrid.cs)을 작성해주세요.
1. 마우스 드래그 앤 드롭으로 다양한 크기(1x1, 1x3, 2x2)의 아이템을 배치하는 로직
2. R키를 눌러 아이템을 90도 회전시키는 기능
3. 그리드 경계를 벗어나거나 이미 다른 아이템이 있는 셀에는 배치 불가 처리
4. 빈 셀(자유 공간)의 개수를 실시간으로 계산하여 GameEvents.OnFreeCellCountChanged 발행
5. [테스트용 Mock]: 다른 팀원의 토큰 시스템이 없어도, 마우스 우클릭 시 해당 아이템에 토큰이 들어간 것으로 간주하고 GameEvents.OnItemTriggered를 즉시 호출하는 디버그 코드 포함
```

---

#### 🔹 [개발자 2] 실시간 토큰 파트
```markdown
[시스템 및 역할]
Unity 6 (2D) 프로젝트에서 "실시간 토큰 생성 및 주입 시스템"을 개발하고 있습니다.
저의 작업 위치는 Assets/_Project/02_Token/ 폴더입니다.

[프로젝트 공통 규격]
- 네임스페이스: using InventoryAutoBattle.Core;
- 공통 클래스: TokenData (tokenType, duration = 5f), TokenType, GameState
- 이벤트 통신:
  - 수신: GameEvents.OnGameStateChanged(GameState state) (Battle 상태일 때만 토큰 생성)
  - 발행: GameEvents.OnTokenApplied(TokenData token, object itemTarget)

[요청 사항]
실시간 토큰 스포너(TokenSpawner.cs)와 개별 토큰(TokenInstance.cs)을 작성해주세요.
1. GameState.Battle 상태가 되면 N초 주기로 무작위 토큰(공격/방어/회복)을 스폰
2. 토큰은 5초 동안 유효하며, 남은 시간을 원형 게이지(UI Image fillAmount)로 표시
3. 5초가 지나면 깜빡이다가 자동 소멸
4. 플레이어가 마우스로 토큰을 드래그하여 화면의 목표 영역(슬롯)에 드롭했을 때 GameEvents.OnTokenApplied 발행
5. [테스트용 Mock]: 아직 인벤토리 시스템이 없으므로, 화면에 더미 타깃 3개를 두고 드롭 시 정상 발행되는지 콘솔 로그를 찍는 테스트 씬용 코드 포함
```

---

#### 🔹 [개발자 3] 자동 전투 & 엔티티 파트
```markdown
[시스템 및 역할]
Unity 6 (2D) 프로젝트에서 "자동 전투 및 캐릭터/적 시스템"을 개발하고 있습니다.
저의 작업 위치는 Assets/_Project/03_Combat/ 폴더입니다.

[프로젝트 공통 규격]
- 네임스페이스: using InventoryAutoBattle.Core;
- 공통 클래스: StageData (enemyMaxHp, enemyAttack 등), ItemType, GameState
- 이벤트 통신:
  - 수신: 
    - GameEvents.OnItemTriggered(ItemType type, float value)
    - GameEvents.OnStageStarted(StageData stage)
  - 발행: 
    - GameEvents.OnBattleFinished(bool isPlayerWin)
    - GameEvents.OnPlayerHpChanged(float cur, float max), GameEvents.OnEnemyHpChanged(float cur, float max)

[요청 사항]
전투 매니저(CombatManager.cs)와 캐릭터 베이스(CharacterBase.cs, PlayerEntity.cs, EnemyEntity.cs)를 작성해주세요.
1. 전투 시작 시 플레이어와 적이 서로를 향해 자동 이동 후 사거리 도달 시 기본 공격 주기마다 타격
2. GameEvents.OnItemTriggered 수신 시:
   - ItemType.Weapon: 적에게 즉시 추가 피해
   - ItemType.Shield: 방어력/보호막 활성화
   - ItemType.Potion: 플레이어 HP 즉시 회복
3. HP 변동 시 GameEvents.OnPlayerHpChanged / OnEnemyHpChanged 발행
4. 적 HP가 0이 되면 OnBattleFinished(true), 플레이어 HP가 0이 되면 OnBattleFinished(false) 발행
5. [테스트용 Mock]: 인벤토리 시스템이 없어도, 숫자키 1(공격), 2(방어), 3(힐)을 누르면 OnItemTriggered를 스스로 호출해 전투 상황을 테스트할 수 있는 인스펙터/키보드 디버그 기능 포함
```

---

#### 🔹 [개발자 4] 게임 루프 & UI 파트
```markdown
[시스템 및 역할]
Unity 6 (2D) 프로젝트에서 "전체 게임 루프(FSM) 및 HUD UI/보상 시스템"을 개발하고 있습니다.
저의 작업 위치는 Assets/_Project/04_GameLoop_UI/ 폴더입니다.

[프로젝트 공통 규격]
- 네임스페이스: using InventoryAutoBattle.Core;
- 공통 클래스: StageData, ItemData, GameState
- 이벤트 통신:
  - 수신: 
    - GameEvents.OnBattleFinished(bool isPlayerWin)
    - GameEvents.OnPlayerHpChanged(float cur, float max), GameEvents.OnEnemyHpChanged(float cur, float max)
    - GameEvents.OnFreeCellCountChanged(int free, int total)
  - 발행: 
    - GameEvents.OnStageStarted(StageData stage)
    - GameEvents.OnGameStateChanged(GameState state)

[요청 사항]
게임 루프 매니저(GameManager.cs)와 UI 매니저(UIManager.cs)를 작성해주세요.
1. 게임 상태 머신(Ready -> Battle -> Reward -> 다음 Stage / GameOver) 구현
2. "전투 시작" 버튼 클릭 시 GameState.Battle로 전환
3. GameEvents.OnBattleFinished 수신 시 승리/패배 팝업 출력 및 Reward 상태 진입
4. 상단 HUD: 스테이지 번호 텍스트, 플레이어 HP 바, 적 HP 바 슬라이더 실시간 연동
5. 보상 획득 로직: 스테이지 승리 시 보상 아이템 지급 및 확장 조각 수집 처리
6. [테스트용 Mock]: 전투 시스템이 없어도, UI에 '강제 승리', '강제 패배' 테스트 버튼을 두어 스테이지 전환 및 UI 루프를 단독 검증할 수 있는 디버그 패널 포함
```