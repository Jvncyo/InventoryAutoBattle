using UnityEngine;

public abstract class TokenEffectSO : ScriptableObject
{
    public abstract bool ApplyToEquipment(ItemInstance targetEquipment);
}