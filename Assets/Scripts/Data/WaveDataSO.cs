using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "Wave_", menuName = "Roguelike/Wave Data")]
public class WaveDataSO : ScriptableObject
{
    public int WaveNumber;
    public EnemyDataSO Enemy;
    [FormerlySerializedAs("EquipmentDropPool")]
    public List<ItemDataSO> EnhancementTokenPool;
    [FormerlySerializedAs("TokenDropPool")]
    public List<ItemDataSO> CombatTokenPool;
}