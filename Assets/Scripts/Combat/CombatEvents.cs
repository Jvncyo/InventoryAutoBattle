using UnityEngine;

public enum CombatActionType { Attack, Hit, Die }

public class CombatActionEvent
{
    public GameObject Actor;
    public CombatActionType ActionType;

    public CombatActionEvent(GameObject actor, CombatActionType actionType)
    {
        Actor = actor;
        ActionType = actionType;
    }
}
