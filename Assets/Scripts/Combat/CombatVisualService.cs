using UnityEngine;

public class CombatVisualService : MonoBehaviour
{
    private void OnEnable()
    {
        EventBus.Subscribe<CombatActionEvent>(OnCombatAction);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<CombatActionEvent>(OnCombatAction);
    }

    private void OnCombatAction(CombatActionEvent e)
    {
        // Multiple defensive checks for destroyed/null GameObjects
        if (e.Actor == null) return;
        if (e.Actor.Equals(null)) return;
        if (!e.Actor) return;
        if (!e.Actor.activeInHierarchy) return;
        
        var controller = e.Actor.GetComponentInChildren<CombatAnimatorController>();
        if (controller == null) return;

        Debug.Log($"CombatVisualService: Received {e.ActionType} for {e.Actor.name}");

        switch (e.ActionType)
        {
            case CombatActionType.Attack: controller.PlayAttack(); break;
            case CombatActionType.Hit: controller.PlayHit(); break;
            case CombatActionType.Die: controller.PlayDie(); break;
        }
    }
}
