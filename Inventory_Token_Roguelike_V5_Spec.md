# Inventory Token Roguelike V5.0 Complete Master Specification
## Production AI Development Specification (Autonomous OpenCode Ready)

- **Target Engine:** Unity 6 (6000.x)
- **Language / Runtime:** C# (.NET Standard 2.1)
- **Primary Architecture:** Unified Item-Container Pattern + Decoupled EventBus + Service Layer
- **Economic Paradigm:** No Shop / No Gold / Pure Reward Draft & Spatial Compression
- **Grid Paradigm:** Fixed 6x6 Permanent Grid (Multi-Cell Spatial Tetris / Pruning & Compression)
- **Progression Paradigm:** Infinite Scaling Stages + 10-Stage Boss Cycle + Dynamic Enemy Archetypes
- **Design Target:** Autonomous Code Generation by AI Agents (Zero Ambiguity, Non-Breaking Additive)

---

## 1. High-Level Design & System Architecture

### 1.1 Complete Game Flow Diagram
```text
[Run Start / State Restore]
       │
       ▼
┌──► [Preparation Phase] ◄────────────────────────────────────────────────────────────────────────┐
│    - Free Item Relocation within 6x6 Fixed Player Grid (Multi-Cell Support)                     │
│    - Spatial Coexistence: Weapons (Offense) and Armors (Defense) share the same 6x6 Grid       │
│    - Equipment Compression & Pruning: Drag item to [Discard Zone] to destroy and free cells   │
│    - Enhancement Token: Drag Token onto Equipment/Armor to permanently buff stats               │
│    - Combat Token: Drag Token onto HP Bar (Player or Enemy) for instant effect & consume       │
│    - Player clicks "Start Combat"                                                              │
│           │                                                                                     │
│           ▼                                                                                     │
│    [Auto Combat Phase (0.1s Tick Loop)]                                                         │
│    - 0.1s Tick-Based Cooldown Loop                                                             │
│    - Equipment & Armor triggers execute automatically (OnCooldown, OnHit, OnDamaged)           │
│    - Combat Tokens can be dragged onto HP Bar during combat in real time                       │
│    - EventBus publishes CombatActionEvent -> CombatVisualService triggers Animator              │
│           │                                                                                     │
│           ▼                                                                                     │
│      Player HP <= 0? ──► [Defeat Screen / RunEnded Tombstone]                                   │
│           │ (No)                                                                                │
│      Enemy HP <= 0?                                                                             │
│           │ (Yes)                                                                               │
│           ▼                                                                                     │
│      Is Stage % 10 == 0? (Boss Stage Clear Check)                                               │
│      ├─ [YES: Boss Reward Phase]                                                                │
│      │   ├─ Guaranteed High-Tier Equipment Draft (Boss Armors / High-Tier Equipment)            │
│      │   └─ High-Grade 5x1 Token Reward Tray                                                   │
│      └─ [NO: Standard Reward Phase]                                                             │
│          ├─ Standard 3-Choice Equipment Draft (Select 1 or Skip)                                │
│          └─ Standard 5x1 Token Reward Tray                                                      │
│           │                                                                                     │
│           ▼                                                                                     │
│    [Token Tray Phase Details]                                                                   │
│    - Drag tokens directly from Tray (5x1) to Inventory (6x6)                                    │
│    - Spatial Constraint: Must have free cells in 6x6 Grid to acquire                            │
│    - Uncollected tokens in Tray are destroyed upon "Proceed"                                    │
│           │                                                                                     │
│           ▼                                                                                     │
│    [Stage Advance: CurrentStage++] ─────────────────────────────────────────────────────────────┘
│    (Next Enemy generated via Deterministic Infinite Scaling Formula)
```

### 1.2 Core Architectural Axioms
1. **Unified Item Model (Additive Extension):**
   - 무기, 방어구, 토큰은 별개의 클래스로 나뉘지 않고 동일한 `ItemDataSO` 및 `ItemInstance`를 공유합니다.
   - 방어구는 `ItemType.Equipment`에 속하며, `BaseDamageReduction` 및 방어 트리거(`OnDamaged`, `OnCooldown`)를 통해 동작합니다.
2. **Fixed 6x6 Grid & Equipment Compression:**
   - 플레이어 인벤토리는 영구적으로 **6x6(36셀)** 크기를 유지하며 그리드 확장은 없습니다.
   - 무기와 방어구가 동일한 36칸 그리드를 공유하므로 공격과 방어의 물리적 저울질이 발생합니다.
   - [Discard Zone]을 통해 쓸모없어진 저티어 장비를 영구 파괴하여 고티어 장비 및 토큰 수용 공간을 확보합니다.
3. **Multi-Cell Grid & Strict UI Layering:**
   - 아이템은 1x1부터 다중 셀 크기(`Width`, `Height` 및 선택적 `CustomShapeOffsets`)를 가집니다.
   - 인벤토리 뷰 계층 구조: `[SlotsContainer (Z:0) → SpriteLayer (Z:1) → ItemsContainer (Z:2)]`를 반드시 준수합니다.
4. **Deterministic Infinite Scaling & Boss Cycle:**
   - 스테이지가 무한히 증가함에 따라 적 스탯은 수학적 곡선 함수로 결정론적으로 계산됩니다.
   - 매 10스테이지마다 보스가 등장(`CurrentStage % 10 == 0`)하며, 전용 드래프트 풀과 2페이즈 격노 기믹을 가집니다.
5. **Decoupled Combat Visual Service:**
   - 전투 로직은 애니메이션을 직접 제어하지 않고 `EventBus`를 통해 `CombatActionEvent`를 발행합니다.
   - `CombatVisualService`와 `CombatAnimatorController`가 트리거(`Attack`, `Hit`, `Die`)를 수신하여 독립 재생합니다.
6. **Robust Persistence (v3) & Auto-Recovery:**
   - 원자적 임시 저장(`run_save.json.tmp`)과 백업(`run_save.json.bak`) 이중 구조를 사용합니다.
   - 기존 세이브의 `CurrentWave` 필드는 `CurrentStage`로 자동 마이그레이션 호환됩니다.
7. **No Shop / No Gold:**
   - 상점 매니저, 화폐(골드/코인), 상점 UI, 리롤 관련 로직은 전면 배제됩니다.

---

## 2. Directory Structure

```text
Assets/
├── GraphicAssets/
│   ├── DarkAgesUi_v1.0/
│   └── Tiny RPG Character Asset Pack 01 v2.0 -Free Soldier&Orc/
├── Prefabs/
│   ├── Character/
│   │   ├── PlayerEntity.prefab
│   │   ├── EnemyEntity.prefab
│   │   ├── BossEntity.prefab
│   │   ├── PlayerAnimater.controller
│   │   └── OrchAnimater.controller
│   ├── Inventory/
│   │   ├── ItemContainerView.prefab
│   │   ├── CellSlotView.prefab
│   │   ├── ItemView.prefab
│   │   └── DiscardZoneView.prefab
│   └── UI/
│       ├── EquipmentDraftModal.prefab
│       └── RunRecoveryModal.prefab
├── ScriptableObjects/
│   ├── Effects/
│   │   ├── Effect_ImmediateDefense.asset
│   │   └── Effect_StatBuff.asset
│   ├── Items/
│   │   ├── Armors/
│   │   │   ├── Armor_Shield_Base.asset
│   │   │   └── Armor_PlateMail.asset
│   │   └── Weapons/
│   │       ├── Weapon_Dagger.asset
│   │       └── Weapon_Sword_Base.asset
│   ├── Tokens/
│   │   ├── Token_Attack.asset
│   │   ├── Token_Defense.asset
│   │   ├── Token_DirectDamage.asset
│   │   ├── Token_Heal.asset
│   │   ├── Token_ImmediateDefense.asset
│   │   └── Token_Speed.asset
│   ├── Enemies/
│   │   ├── Common/
│   │   └── Bosses/
│   └── Stages/
├── Scripts/
│   ├── Combat/
│   │   ├── AutoAssignCombatTokenTargets.cs
│   │   ├── BossCombatActor.cs
│   │   ├── CombatActor.cs
│   │   ├── CombatAnimatorController.cs
│   │   ├── CombatEvents.cs
│   │   ├── CombatManager.cs
│   │   ├── CombatVisualService.cs
│   │   ├── EnemyCombatActor.cs
│   │   └── PlayerCombatActor.cs
│   ├── Core/
│   │   ├── Constants.cs
│   │   ├── GameManager.cs
│   │   ├── RunStateMachine.cs
│   │   └── StageProgressionManager.cs   (기존 WaveManager를 흡수/확장)
│   ├── Data/
│   │   ├── Effects/
│   │   │   ├── CombatTokenEffectSO.cs
│   │   │   ├── StatBuffEffectSO.cs
│   │   │   └── TokenEffectSO.cs
│   │   ├── EnemyDataSO.cs
│   │   ├── ItemDataSO.cs
│   │   └── StageScalingConfigSO.cs
│   ├── Events/
│   │   ├── EventBus.cs
│   │   └── GameEvents.cs
│   ├── Inventory/
│   │   ├── CellSlotView.cs
│   │   ├── CombatTokenDropTarget.cs
│   │   ├── DiscardDropTarget.cs
│   │   ├── InventoryInteractionPolicy.cs
│   │   ├── ItemContainer.cs
│   │   ├── ItemContainerView.cs
│   │   ├── ItemInstance.cs
│   │   ├── ItemView.cs
│   │   ├── TokenUseService.cs
│   │   └── UniversalDragDropHandler.cs
│   ├── Reward/
│   │   ├── EquipmentDraftUI.cs
│   │   └── RewardManager.cs
│   ├── Save/
│   │   ├── ItemCatalogSO.cs
│   │   ├── SaveDataModels.cs
│   │   └── SaveManager.cs
│   └── UI/
│       └── UIRunRoot.cs
└── _Project/
    └── Scenes/
        └── MainScene.unity
```

---

## 3. Data Schema & Core C# Definitions

### 3.1 Item & Multi-Cell Shape Data (`ItemDataSO.cs`)

기존 V4.4의 필드를 100% 보존하면서, 방어구 구분을 위한 `EquipmentSubtype`, 방어력 프로퍼티 `BaseDamageReduction`, 다각형 형태 확장을 위한 `CustomShapeOffsets`를 추가합니다.

```csharp
using System.Collections.Generic;
using UnityEngine;

public enum ItemType { Equipment, Token }
public enum EquipmentSubtype { None, Weapon, Armor, Relic }
public enum TokenType { None, Attack, Defense, Utility, Heal, DirectDamage, ImmediateDefense }
public enum TokenCategory { Enhancement, Combat }
public enum Rarity { Common, Rare, Epic, Legendary }
public enum TriggerType { OnCooldown, OnHit, OnDamaged }

[System.Serializable]
public struct EquipmentTrigger
{
    public TriggerType Type;
    public float Value;
    public float Cooldown;
}

[CreateAssetMenu(fileName = "Item_", menuName = "Roguelike/Item Data")]
public class ItemDataSO : ScriptableObject
{
    [Header("Universal Properties")]
    public string Id;
    public string DisplayName;
    public ItemType ItemType;
    public EquipmentSubtype EquipmentSubtype = EquipmentSubtype.None;
    public Rarity Rarity;
    [Range(1, 4)] public int Width = 1;
    [Range(1, 4)] public int Height = 1;
    [Tooltip("비어있을 경우 Width x Height 직사각형 사용. L자 등 특수 형태는 (0,0) 기준 상대 좌표")]
    public List<Vector2Int> CustomShapeOffsets = new();
    public Sprite Icon;

    [Header("Equipment Exclusive (ItemType == Equipment)")]
    public float BaseCooldown = 3.0f;
    public float BaseDamage = 0f;
    public float BaseShield = 0f;
    public float BaseDamageReduction = 0f; // 방어구 전용 피격 경감치
    public float BaseHeal = 0f;
    public List<EquipmentTrigger> Triggers = new();
    public List<TokenType> AcceptableTokens = new();

    [Header("Token Exclusive (ItemType == Token)")]
    public TokenCategory TokenCategory = TokenCategory.Enhancement;
    public TokenType TokenType = TokenType.None;
    public TokenEffectSO TokenEffect;

    public List<Vector2Int> GetOccupiedOffsets()
    {
        if (CustomShapeOffsets != null && CustomShapeOffsets.Count > 0)
            return CustomShapeOffsets;

        var offsets = new List<Vector2Int>();
        for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
                offsets.Add(new Vector2Int(x, y));
        return offsets;
    }
}
```

### 3.2 Dynamic Item Instance (`ItemInstance.cs`)

```csharp
using System;
using UnityEngine;

[Serializable]
public class ItemInstance
{
    public string InstanceId;
    public ItemDataSO Data;
    public int GridX;
    public int GridY;

    // 강화 토큰으로 영구 누적된 수치
    public float BonusDamage;
    public float BonusShield;
    public float BonusDamageReduction;
    public float CooldownMultiplier = 1.0f;

    public float CurrentCooldownTimer;

    public ItemInstance(ItemDataSO data, int x = -1, int y = -1)
    {
        InstanceId = Guid.NewGuid().ToString();
        Data = data;
        GridX = x;
        GridY = y;
        BonusDamage = 0f;
        BonusShield = 0f;
        BonusDamageReduction = 0f;
        CooldownMultiplier = 1.0f;
        CurrentCooldownTimer = 0f;
    }

    public float GetEffectiveDamage() => Data.BaseDamage + BonusDamage;
    public float GetEffectiveShield() => Data.BaseShield + BonusShield;
    public float GetEffectiveDamageReduction() => Data.BaseDamageReduction + BonusDamageReduction;
    public float GetEffectiveCooldown() => Mathf.Max(0.2f, Data.BaseCooldown * CooldownMultiplier);
}
```

### 3.3 Token Effects (`StatBuffEffectSO.cs`, `CombatTokenEffectSO.cs`)

```csharp
// TokenEffectSO.cs
using UnityEngine;

public abstract class TokenEffectSO : ScriptableObject
{
    public abstract bool ApplyToEquipment(ItemInstance targetEquipment);
}

// StatBuffEffectSO.cs (Enhancement Tokens)
[CreateAssetMenu(fileName = "Effect_StatBuff", menuName = "Roguelike/Token Effects/Stat Buff")]
public class StatBuffEffectSO : TokenEffectSO
{
    public float DamageBonus = 0f;
    public float ShieldBonus = 0f;
    public float DamageReductionBonus = 0f;
    public float CooldownDeltaRate = 0f; // e.g. -0.1f = 10% 쿨다운 단축

    public override bool ApplyToEquipment(ItemInstance targetEquipment)
    {
        if (targetEquipment == null) return false;
        targetEquipment.BonusDamage += DamageBonus;
        targetEquipment.BonusShield += ShieldBonus;
        targetEquipment.BonusDamageReduction += DamageReductionBonus;
        targetEquipment.CooldownMultiplier = Mathf.Max(0.2f, targetEquipment.CooldownMultiplier + CooldownDeltaRate);
        return true;
    }
}

// CombatTokenEffectSO.cs (Combat Tokens)
public enum CombatTokenAction { Damage, Heal, Shield }

[CreateAssetMenu(fileName = "Effect_Combat", menuName = "Roguelike/Token Effects/Combat")]
public class CombatTokenEffectSO : TokenEffectSO
{
    public CombatTokenAction Action;
    [Min(0)] public float Amount = 10f;

    public override bool ApplyToEquipment(ItemInstance targetEquipment) => false;

    public bool ApplyToActor(CombatActor actor)
    {
        if (actor == null || !actor.IsAlive) return false;
        switch (Action)
        {
            case CombatTokenAction.Damage:
                actor.TakeDamage(Amount);
                break;
            case CombatTokenAction.Heal:
                actor.RestoreHP(Amount);
                break;
            case CombatTokenAction.Shield:
                actor.GainShield(Amount);
                break;
        }
        return true;
    }
}
```

---

## 4. Infinite Scaling & Boss System

### 4.1 Deterministic Infinite Scaling Formula
공격력과 체력은 스테이지에 따라 지수적으로 증가하되, 쿨다운은 물리적 한계점(0.8s)을 유지하도록 Clamp합니다.

```csharp
// EnemyDataSO.cs
using System.Collections.Generic;
using UnityEngine;

public enum EnemyArchetype { Standard, Tank, Rusher, Disrupter }

[CreateAssetMenu(fileName = "Enemy_", menuName = "Roguelike/Enemy Data")]
public class EnemyDataSO : ScriptableObject
{
    public string EnemyId;
    public string EnemyName;
    public EnemyArchetype Archetype;
    public bool IsBoss;
    public Sprite Portrait;

    [Header("Level 1 Base Stats")]
    public float BaseMaxHP = 80f;
    public float BaseDamage = 8f;
    public float BaseCooldown = 3.0f;
    public float BaseShield = 0f;

    [Header("Boss / Special Triggers")]
    public List<EquipmentTrigger> SpecialTriggers = new();

    public (float hp, float damage, float cooldown, float shield) CalculateStatsForStage(int stageIndex)
    {
        float stageFactor = Mathf.Max(0, stageIndex - 1);

        float scaledHP = BaseMaxHP * Mathf.Pow(1.0f + 0.15f * stageFactor, 1.12f);
        float scaledDmg = BaseDamage * Mathf.Pow(1.0f + 0.10f * stageFactor, 1.08f);
        float scaledCd = Mathf.Max(0.8f, BaseCooldown * Mathf.Pow(0.97f, stageFactor));
        float scaledShield = BaseShield > 0 ? BaseShield * (1.0f + 0.12f * stageFactor) : 0f;

        if (IsBoss)
        {
            scaledHP *= 2.5f;
            scaledDmg *= 1.3f;
        }

        return (scaledHP, scaledDmg, scaledCd, scaledShield);
    }
}
```

### 4.2 Stage Progression Manager (`StageProgressionManager.cs`)
기존 `WaveManager`의 역할을 포함하며, 10단계 보스 분기 및 결정론적 적 생성을 총괄합니다.

```csharp
using System.Collections.Generic;
using UnityEngine;

public class StageProgressionManager : MonoBehaviour
{
    public static StageProgressionManager Instance { get; private set; }

    public int CurrentStage { get; private set; } = 1;

    [SerializeField] private List<EnemyDataSO> commonEnemies = new();
    [SerializeField] private List<EnemyDataSO> bossEnemies = new();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public bool IsBossStage() => (CurrentStage % 10 == 0);

    public EnemyDataSO GetCurrentStageEnemy()
    {
        if (IsBossStage())
        {
            int bossIndex = ((CurrentStage / 10) - 1) % bossEnemies.Count;
            return bossEnemies[bossIndex];
        }

        int index = (CurrentStage - 1) % commonEnemies.Count;
        return commonEnemies[index];
    }

    public void AdvanceStage()
    {
        CurrentStage++;
        SaveManager.Instance.SaveRun(RunState.Preparation);
    }

    public void SetStage(int stage)
    {
        CurrentStage = Mathf.Max(1, stage);
    }
}
```

---

## 5. Fixed 6x6 Inventory, Drag & Drop, & Compression

### 5.1 UI Layering & Cell Highlighting
- 인벤토리는 `[SlotsContainer (Z:0) → SpriteLayer (Z:1) → ItemsContainer (Z:2)]` 계층을 엄격히 준수합니다.
- `ItemView` 드래그 중 `ItemContainerView.GetAffectedSlots()`를 통해 크기(`Width`, `Height` 또는 `CustomShapeOffsets`)에 해당하는 모든 슬롯을 실시간으로 녹색(배치 가능) 또는 적색(충돌/초과)으로 하이라이트합니다.
- 드래그 종료 시 `ResetAllHighlights()`를 호출하여 원상 복구합니다.

### 5.2 Discard Zone for Equipment Compression (`DiscardDropTarget.cs`)
플레이어가 36칸 그리드를 최적화하고 낡은 장비를 폐기할 수 있도록 지원하는 드롭 타겟입니다.

```csharp
using UnityEngine;
using UnityEngine.EventSystems;

public class DiscardDropTarget : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        var itemView = eventData.pointerDrag?.GetComponent<ItemView>();
        if (itemView == null || itemView.Instance == null) return;

        // 준비 단계에서만 파괴 가능
        if (RunStateMachine.Instance.CurrentState != RunState.Preparation) return;

        var container = itemView.ParentContainer;
        if (container != null)
        {
            container.RemoveItem(itemView.Instance);
        }

        Destroy(itemView.gameObject);
        SaveManager.Instance.SaveRun(RunState.Preparation);
    }
}
```

### 5.3 Token Pipeline & Buffer Space Principle
- 토큰 보상 트레이(5x1)에서 플레이어 인벤토리(6x6)로 토큰을 가져올 때, 인벤토리에 물리적인 빈 셀(1x1)이 없으면 드롭이 거부됩니다.
- Enhancement Token은 장비 위에 드롭 시 즉시 능력치를 영구 상승시키고 토큰 오브젝트를 소멸시켜 슬롯을 반환합니다.
- Combat Token은 HP 바(`CombatTokenDropTarget`)에 드롭 시 즉시 효과를 발휘하고 소멸합니다.

---

## 6. Combat Engine & Armor Calculation

### 6.1 Player Armor Mitigation (`PlayerCombatActor.cs`)
플레이어 피격 시 인벤토리에 배치된 모든 방어구의 고정 경감치(`BaseDamageReduction`)와 `OnDamaged` 방어 트리거가 발동합니다.

```csharp
using UnityEngine;

public class PlayerCombatActor : CombatActor
{
    [SerializeField] private ItemContainer playerInventory;

    public override void TakeDamage(float incomingDamage)
    {
        float totalReduction = 0f;

        if (playerInventory != null)
        {
            foreach (var item in playerInventory.Items)
            {
                if (item.Data.ItemType == ItemType.Equipment && 
                    item.Data.EquipmentSubtype == EquipmentSubtype.Armor)
                {
                    totalReduction += item.GetEffectiveDamageReduction();

                    foreach (var trigger in item.Data.Triggers)
                    {
                        if (trigger.Type == TriggerType.OnDamaged)
                        {
                            GainShield(trigger.Value);
                        }
                    }
                }
            }
        }

        float finalDamage = Mathf.Max(1f, incomingDamage - totalReduction);
        base.TakeDamage(finalDamage);
    }
}
```

### 6.2 Boss Combat Actor (`BossCombatActor.cs`)
체력이 50% 이하로 내려가면 2페이즈(격노 모드)로 전환되어 공격 주기가 30% 빨라집니다.

```csharp
using UnityEngine;

public class BossCombatActor : EnemyCombatActor
{
    private bool _isEnraged = false;
    private float _baseCooldown;

    public override void Initialize(EnemyDataSO data, int stageIndex)
    {
        base.Initialize(data, stageIndex);
        _baseCooldown = ActionCooldown;
    }

    protected override void Update()
    {
        base.Update();

        if (!_isEnraged && CurrentHP <= MaxHP * 0.5f)
        {
            _isEnraged = true;
            ActionCooldown = Mathf.Max(0.5f, _baseCooldown * 0.7f);
            EventBus.Publish(new CombatActionEvent(gameObject, CombatActionType.Attack));
        }
    }
}
```

### 6.3 Decoupled Visual Pipeline
- 전투 로직은 `EventBus.Publish(new CombatActionEvent(actor, actionType))`만 호출합니다.
- `CombatVisualService`가 이벤트를 수신하여 해당 액터의 `CombatAnimatorController`에 `Attack`, `Hit`, `Die` 트리거를 실행합니다.
- 전투 종료 시 `CombatAnimatorController.ResetToIdle()`이 호출되어 모든 상태를 초기화합니다.

---

## 7. Reward & Draft Pipeline

### 7.1 Standard vs. Boss Reward Branching (`RewardManager.cs`)
```csharp
using UnityEngine;

public class RewardManager : MonoBehaviour
{
    public static RewardManager Instance { get; private set; }

    [SerializeField] private EquipmentDraftUI draftUI;
    [SerializeField] private ItemContainer tokenTrayContainer; // 5x1 Tray

    public void OnCombatVictory()
    {
        bool isBoss = StageProgressionManager.Instance.IsBossStage();

        if (isBoss)
        {
            // 보스 보상: 에픽/전설 등급 방어구 및 특수 장비 확정 드래프트
            draftUI.OpenModal(RarityFilter.EpicOrHigher);
            PopulateTokenTray(highGrade: true);
        }
        else
        {
            // 일반 보상: 일반 장비 드래프트 (스킵 가능)
            draftUI.OpenModal(RarityFilter.All);
            PopulateTokenTray(highGrade: false);
        }
    }

    private void PopulateTokenTray(bool highGrade)
    {
        tokenTrayContainer.ClearContainer();
        for (int i = 0; i < 5; i++)
        {
            ItemDataSO tokenData = TokenCatalog.GetRandomToken(highGrade);
            tokenTrayContainer.TryAddItem(new ItemInstance(tokenData), i, 0);
        }
    }

    public void OnProceedNextStage()
    {
        tokenTrayContainer.ClearContainer(); // 미수령 토큰 파기
        StageProgressionManager.Instance.AdvanceStage();
        RunStateMachine.Instance.ChangeState(RunState.Preparation);
    }
}
```

---

## 8. Persistence & Recovery Pipeline (v3)

### 8.1 Save Data Models & Backward Compatibility (`SaveDataModels.cs`)
기존 v2 세이브 파일과의 호환을 위해 `CurrentWave` 필드를 역직렬화 시 지원하며, 새 형식은 `CurrentStage`를 사용합니다.

```csharp
using System;
using System.Collections.Generic;

[Serializable]
public class RunSaveData
{
    public int DataVersion = 3;
    public int CurrentStage = 1;
    public int CurrentWave; // v2 하위 호환 필드 (CurrentStage = CurrentWave)
    public float PlayerHP;
    public float PlayerMaxHP;
    public string RunSeed;
    public string RunState = "Preparation";
    public bool RunEnded;
    public List<SavedItemRecord> InventoryItems = new();
}

[Serializable]
public class SavedItemRecord
{
    public string ItemDataId;
    public int GridX;
    public int GridY;
    public float BonusDamage;
    public float BonusShield;
    public float BonusDamageReduction;
    public float CooldownMultiplier = 1.0f;
}

public enum SaveLoadStatus { Missing, Success, Failed, Ended }
```

### 8.2 Atomic Double-File Persistence (`SaveManager.cs`)
- **저장 시점:** 준비 단계 진입 시, 아이템 이동/폐기 시, 전투 시작 직전, 보상 확정 후.
- **원자적 저장 절차:**
  1. `run_save.json.tmp`에 직렬화 데이터를 기록합니다.
  2. 성공 시 기존 `run_save.json`을 `run_save.json.bak`으로 교체합니다.
  3. `run_save.json.tmp`를 `run_save.json`으로 이동(Move)합니다.
- **복구 메커니즘:**
  - 주 저장 파일 파손 시 `.bak` 파일을 자동으로 로드합니다.
  - 양쪽 파일 모두 파손된 경우 `RecoveryRequired = true` 플래그를 세우고 UI를 띄워 신규 런 시작(기존 파일은 `.old`로 격리 보존)을 유도합니다.

---

## 9. QA Acceptance Criteria

1. **하위 호환성 및 기존 기능 불변성 검증:**
   - 기존의 단일 아이템 구조(`ItemDataSO`), 토큰 드롭 규칙, 애니메이션 분리 구조가 그대로 동작해야 합니다.
   - 상점/골드 관련 로직이 프로젝트 어디에도 존재하지 않아야 합니다.
2. **무한 스케일링 공식 검증:**
   - 10, 50, 100스테이지 진입 시 적 HP와 공격력이 공식대로 비선형 증가해야 합니다.
   - 적 공격 쿨다운은 아무리 단축되어도 최소 0.8초 밑으로 떨어지지 않아야 합니다.
3. **10단계 보스 및 페이즈 검증:**
   - `CurrentStage % 10 == 0`일 때 보스 몬스터가 출현해야 합니다.
   - 보스 HP가 50% 이하가 되면 쿨다운이 30% 감소하는 2페이즈에 정상 진입해야 합니다.
   - 보스 처치 후 드래프트는 에픽/전설 등급의 방어구 및 장비가 확정 제시되어야 합니다.
4. **6x6 그리드 내 방어구 및 폐기(Discard) 검증:**
   - 방어구와 무기가 6x6 인벤토리 내에서 정상적으로 공존하며 배치되어야 합니다.
   - 준비 단계에서 장비를 `DiscardDropTarget`에 드롭하면 영구 파괴되고 그리드 셀이 반환되어야 합니다.
   - 피격 시 인벤토리에 있는 모든 방어구의 데미지 경감 수치가 합산되어 대미지를 흡수해야 합니다.
5. **토큰 버퍼 공간 병목 검증:**
   - 6x6 인벤토리가 장비로 36칸 가득 찬 경우, 5x1 트레이의 토큰을 인벤토리로 가져올 수 없어야(드롭 거부) 합니다.
```