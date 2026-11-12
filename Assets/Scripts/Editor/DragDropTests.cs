using NUnit.Framework;
using UnityEngine;

public class DragDropTests
{
    [Test]
    public void TestTokenConsumptionLogic()
    {
        var eqGo = new GameObject("Equipment");
        var eqInstance = eqGo.AddComponent<ItemInstance>();
        var eqData = ScriptableObject.CreateInstance<ItemDataSO>();
        eqData.ItemType = ItemType.Equipment;
        eqData.BaseDamage = 10f;
        eqData.AcceptableTokens.Add(TokenType.Attack);
        eqInstance.Initialize(eqData);

        var tokenGo = new GameObject("Token");
        var tokenInstance = tokenGo.AddComponent<ItemInstance>();
        var tokenData = ScriptableObject.CreateInstance<ItemDataSO>();
        tokenData.ItemType = ItemType.Token;
        tokenData.TokenType = TokenType.Attack;
        
        var buffEffect = ScriptableObject.CreateInstance<StatBuffEffectSO>();
        buffEffect.DamageBonus = 5f;
        tokenData.TokenEffect = buffEffect;
        tokenInstance.Initialize(tokenData);

        // Apply effect directly
        bool applied = tokenData.TokenEffect.ApplyToEquipment(eqInstance);
        Assert.IsTrue(applied);
        Assert.AreEqual(15f, eqInstance.Data.BaseDamage + eqInstance.BonusDamage);

        Object.DestroyImmediate(eqGo);
        Object.DestroyImmediate(tokenGo);
        Object.DestroyImmediate(eqData);
        Object.DestroyImmediate(tokenData);
        Object.DestroyImmediate(buffEffect);
    }
}