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
        Debug.Log($"CombatVisualService: Received {e.ActionType} for {e.Actor?.name}");
        if (e.Actor == null) return;
        var controller = e.Actor.GetComponentInChildren<CombatAnimatorController>();
        if (controller == null)
        {
            Debug.LogWarning($"CombatVisualService: No CombatAnimatorController found on {e.Actor.name}");
            return;
        }

        switch (e.ActionType)
        {
            case CombatActionType.Attack: controller.PlayAttack(); break;
            case CombatActionType.Hit: controller.PlayHit(); break;
            case CombatActionType.Die: controller.PlayDie(); break;
        }
    }
}
