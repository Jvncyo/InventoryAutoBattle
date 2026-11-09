using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private RunStateMachine stateMachine;
    [SerializeField] private CombatManager combatManager;
    [SerializeField] private RewardManager rewardManager;
    [SerializeField] private SaveManager saveManager;

    [SerializeField] private ItemContainer playerInventory;

    public ItemContainer PlayerInventory => playerInventory;

    private void Awake()
    {
        Instance = this;
    }
}