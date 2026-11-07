using UnityEngine;

[CreateAssetMenu(fileName = "Enemy_", menuName = "Roguelike/Enemy Data")]
public class EnemyDataSO : ScriptableObject
{
    public string Id;
    public string EnemyName;
    public float MaxHP = 100f;
    public float AttackPower = 8f;
    public float AttackInterval = 2.0f;
    public Sprite Icon;
}