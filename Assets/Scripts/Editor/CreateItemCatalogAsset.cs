using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

public static class CreateItemCatalogAsset
{
    [MenuItem("Tools/Create Item Catalog")]
    public static void CreateCatalog()
    {
        const string assetPath = "Assets/ScriptableObjects/ItemCatalogSO.asset";
        
        var catalog = ScriptableObject.CreateInstance<ItemCatalogSO>();
        var itemsField = typeof(ItemCatalogSO).GetField("_items", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var itemsList = new List<ItemDataSO>();

        string[] paths = {
            "Assets/ScriptableObjects/Tokens/Token_Attack.asset",
            "Assets/ScriptableObjects/Tokens/Token_Defense.asset",
            "Assets/ScriptableObjects/Tokens/Token_Speed.asset",
            "Assets/ScriptableObjects/Tokens/Token_DirectDamage.asset",
            "Assets/ScriptableObjects/Tokens/Token_Heal.asset",
            "Assets/ScriptableObjects/Tokens/Token_ImmediateDefense.asset",
            "Assets/ScriptableObjects/Items/Sword_Base.asset",
            "Assets/ScriptableObjects/Items/Shield_Base.asset"
        };

        foreach (var p in paths)
        {
            var item = AssetDatabase.LoadAssetAtPath<ItemDataSO>(p);
            if (item == null)
            {
                Debug.LogWarning($"[ItemCatalog] Could not load asset at {p}");
                continue;
            }
            if (!itemsList.Contains(item))
                itemsList.Add(item);
        }

        itemsField.SetValue(catalog, itemsList);

        if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects"))
            AssetDatabase.CreateFolder("Assets", "ScriptableObjects");

        AssetDatabase.CreateAsset(catalog, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Successfully created ItemCatalogSO at {assetPath} with {itemsList.Count} items.");
    }
}