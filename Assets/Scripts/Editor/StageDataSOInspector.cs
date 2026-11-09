using UnityEditor;
using UnityEngine;

/// <summary>
/// StageDataSO 커스텀 인스펙터: 스테이지 별 적 스탯 미리보기 제공.
/// 디자이너가 인스펙터에서 바로 적의 HP/드미지/냉각시간을 확인할 수 있습니다.
/// </summary>
[CustomEditor(typeof(StageDataSO))]
public class StageDataSOInspector : Editor
{
    private SerializedProperty stageNumberProp;
    private SerializedProperty biomeNameProp;
    private SerializedProperty backgroundPrefabProp;
    private SerializedProperty commonEnemyPoolProp;
    private SerializedProperty bossEnemyProp;
    private SerializedProperty hpMultiplierProp;
    private SerializedProperty damageMultiplierProp;
    private SerializedProperty cooldownMultiplierProp;
    private SerializedProperty equipmentDropPoolProp;
    private SerializedProperty tokenDropPoolProp;

    private bool showPreview = true;
    private int previewStage = 1;

    private void OnEnable()
    {
        stageNumberProp = serializedObject.FindProperty("StageNumber");
        biomeNameProp = serializedObject.FindProperty("BiomeName");
        backgroundPrefabProp = serializedObject.FindProperty("backgroundPrefab");
        commonEnemyPoolProp = serializedObject.FindProperty("commonEnemyPool");
        bossEnemyProp = serializedObject.FindProperty("bossEnemy");
        hpMultiplierProp = serializedObject.FindProperty("hpMultiplier");
        damageMultiplierProp = serializedObject.FindProperty("damageMultiplier");
        cooldownMultiplierProp = serializedObject.FindProperty("cooldownMultiplier");
        equipmentDropPoolProp = serializedObject.FindProperty("equipmentDropPool");
        tokenDropPoolProp = serializedObject.FindProperty("tokenDropPool");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Identity
        EditorGUILayout.LabelField("Identity", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(stageNumberProp, new GUIContent("Stage Number"));
        EditorGUILayout.PropertyField(biomeNameProp, new GUIContent("Biome Name"));

        // Visual
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Visual", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(backgroundPrefabProp);

        // Enemy Composition
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Enemy Composition", EditorStyles.boldLabel);
        
        // Boss enemy
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(bossEnemyProp, new GUIContent("Boss Enemy (if set, this is a boss stage)"));
        if (EditorGUI.EndChangeCheck())
        {
            bossEnemyProp.objectReferenceValue = (EnemyDataSO)bossEnemyProp.objectReferenceValue;
        }

        // Common enemy pool
        EditorGUILayout.PropertyField(commonEnemyPoolProp);

        // Stat Multipliers
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Stage Stat Multipliers", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "스테이지 전체에 적용되는 배율입니다. V5 스케일링과 곱셈으로 적용됩니다.",
            MessageType.Info);
        
        EditorGUILayout.PropertyField(hpMultiplierProp);
        EditorGUILayout.PropertyField(damageMultiplierProp);
        EditorGUILayout.PropertyField(cooldownMultiplierProp);

        // Reward Drop Pools
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Reward Drop Pools", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(equipmentDropPoolProp);
        EditorGUILayout.PropertyField(tokenDropPoolProp);

        // Enemy Stats Preview
        EditorGUILayout.Space();
        EditorGUILayout.Space();
        
        showPreview = EditorGUILayout.Foldout(showPreview, "Enemy Stats Preview", true, EditorStyles.foldoutHeader);
        if (showPreview)
        {
            DrawEnemyStatsPreview();
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawEnemyStatsPreview()
    {
        var stageData = (StageDataSO)target;

        // Preview stage selector
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Preview Stage:", GUILayout.Width(100));
        previewStage = EditorGUILayout.IntSlider(previewStage, 1, 100, GUILayout.MinWidth(200));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // Show common enemies from pool
        if (stageData.commonEnemyPool != null && stageData.commonEnemyPool.Count > 0)
        {
            EditorGUILayout.LabelField($"Common Enemies ({stageData.commonEnemyPool.Count}):", EditorStyles.boldLabel);
            
            foreach (var enemy in stageData.commonEnemyPool)
            {
                if (enemy == null) continue;
                DrawEnemyStatPreview(enemy, previewStage, stageData);
            }
        }

        // Show boss enemy if exists
        if (stageData.bossEnemy != null)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Boss Enemy:", EditorStyles.boldLabel);
            DrawEnemyStatPreview(stageData.bossEnemy, previewStage, stageData, true);
        }

        // Show empty state message
        if ((stageData.commonEnemyPool == null || stageData.commonEnemyPool.Count == 0) && 
            stageData.bossEnemy == null)
        {
            EditorGUILayout.HelpBox("No enemies assigned. Add enemies to 'Common Enemy Pool' or 'Boss Enemy'.", 
                MessageType.Warning);
        }
    }

    private void DrawEnemyStatPreview(EnemyDataSO enemy, int stage, StageDataSO stageData, bool isBoss = false)
    {
        EditorGUILayout.BeginVertical("box");
        
        // Enemy header
        EditorGUILayout.BeginHorizontal();
        if (enemy.Icon != null)
        {
            EditorGUILayout.ObjectField(enemy.Icon, typeof(Sprite), false, GUILayout.Width(32), GUILayout.Height(32));
        }
        else
        {
            EditorGUILayout.Space(32);
        }
        EditorGUILayout.LabelField(isBoss ? $"👑 {enemy.EnemyName}" : enemy.EnemyName, EditorStyles.boldLabel);
        EditorGUILayout.EndHorizontal();

        // Calculate and display stats
        var stats = enemy.CalculateStatsForStage(stage);
        
        // Apply stage multipliers
        float finalHP = stats.hp * stageData.hpMultiplier;
        float finalDamage = stats.damage * stageData.damageMultiplier;
        float finalCooldown = stats.cooldown / stageData.cooldownMultiplier;
        float finalShield = stats.shield * stageData.hpMultiplier;

        EditorGUILayout.BeginVertical("insideBox");
        
        // HP row
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("HP:", GUILayout.Width(40));
        EditorGUILayout.FloatField(finalHP, GUILayout.Width(80));
        EditorGUILayout.LabelField($"({stats.hp:0} × {stageData.hpMultiplier})", GUILayout.MaxWidth(100));
        EditorGUILayout.EndHorizontal();

        // Damage row
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("DMG:", GUILayout.Width(40));
        EditorGUILayout.FloatField(finalDamage, GUILayout.Width(80));
        EditorGUILayout.LabelField($"({stats.damage:0} × {stageData.damageMultiplier})", GUILayout.MaxWidth(100));
        EditorGUILayout.EndHorizontal();

        // Cooldown row
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("CD:", GUILayout.Width(40));
        EditorGUILayout.FloatField(finalCooldown, GUILayout.Width(80));
        EditorGUILayout.LabelField($"({stats.cooldown:0.00}s ÷ {stageData.cooldownMultiplier})", GUILayout.MaxWidth(120));
        EditorGUILayout.EndHorizontal();

        // Shield row (if applicable)
        if (finalShield > 0f)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("SHD:", GUILayout.Width(40));
            EditorGUILayout.FloatField(finalShield, GUILayout.Width(80));
            EditorGUILayout.LabelField($"({stats.shield:0} × {stageData.hpMultiplier})", GUILayout.MaxWidth(100));
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space(4);
    }
}
