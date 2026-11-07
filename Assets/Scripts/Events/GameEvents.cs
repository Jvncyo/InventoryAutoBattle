public readonly struct RunStateChangedEvent { public readonly RunStateMachine.RunState State; public RunStateChangedEvent(RunStateMachine.RunState state) => State = state; }
public readonly struct CombatStartedEvent { }
public readonly struct CombatEndedEvent { public readonly bool IsVictory; public CombatEndedEvent(bool v) => IsVictory = v; }
public readonly struct WaveClearedEvent { public readonly int Wave; public WaveClearedEvent(int w) => Wave = w; }
public readonly struct EquipmentTriggeredEvent { public readonly ItemInstance Equipment; public EquipmentTriggeredEvent(ItemInstance eq) => Equipment = eq; }
public readonly struct TokenConsumedEvent { public readonly ItemInstance Token; public readonly ItemInstance TargetEquipment; public TokenConsumedEvent(ItemInstance t, ItemInstance eq) { Token = t; TargetEquipment = eq; } }
public readonly struct TokenTrayOpenedEvent { }
public readonly struct RewardPhaseFinishedEvent { }
public readonly struct ItemPlacedEvent { public readonly ItemInstance Item; public readonly ItemContainer Container; public readonly int X; public readonly int Y; public ItemPlacedEvent(ItemInstance i, ItemContainer c, int x, int y) { Item = i; Container = c; X = x; Y = y; } }
public readonly struct ItemRemovedEvent { public readonly ItemInstance Item; public readonly ItemContainer Container; public ItemRemovedEvent(ItemInstance item, ItemContainer container) { Item = item; Container = container; } }
public readonly struct ItemPlacementResetEvent { public readonly ItemInstance Item; public ItemPlacementResetEvent(ItemInstance i) => Item = i; }
public readonly struct EnemyAttackedEvent { public readonly float Damage; public EnemyAttackedEvent(float d) => Damage = d; }