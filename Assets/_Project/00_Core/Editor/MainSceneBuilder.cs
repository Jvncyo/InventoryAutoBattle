#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using InventoryAutoBattle.Core;
using InventoryAutoBattle.Combat;
using InventoryAutoBattle.Inventory;
using InventoryAutoBattle.Token;
using InventoryAutoBattle.GameLoop;

namespace InventoryAutoBattle.Core.Editor
{
    /// <summary>
    /// 4개 모듈(인벤토리, 토큰, 전투, 게임 루프 & UI)을 하나의 완성된 게임으로 조립하는 통합 씬 빌더.
    /// 에디터 상단 메뉴: InventoryAutoBattle -> Build Final Main Scene
    /// </summary>
    public static class MainSceneBuilder
    {
        private const string SceneDirectory = "Assets/_Project/Scenes";
        private const string ScenePath = "Assets/_Project/Scenes/MainScene.unity";

        private const string InventoryPrefabPath = "Assets/_Project/01_Inventory/Prefabs/InventorySystem.prefab";
        private const string TokenPrefabPath = "Assets/_Project/02_Token/Prefabs/TokenSystem.prefab";
        private const string CombatPrefabPath = "Assets/_Project/03_Combat/Prefabs/CombatSystem.prefab";
        private const string MainUIPrefabPath = "Assets/_Project/04_GameLoop_UI/Prefabs/MainUI.prefab";

        [MenuItem("InventoryAutoBattle/Build Final Main Scene")]
        public static void BuildMainScene()
        {
            Debug.Log("[MainSceneBuilder] 🚀 최종 통합 씬(MainScene.unity) 빌드 시작...");

            // 1. 디렉토리 확인 및 새 씬 생성
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Scenes"))
            {
                AssetDatabase.CreateFolder("Assets/_Project", "Scenes");
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 2. 메인 카메라 (2D 전투 필드 뷰포트)
            GameObject cameraObj = new GameObject("Main Camera");
            Camera cam = cameraObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.1f, 0.11f, 0.15f, 1f);
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cameraObj.transform.position = new Vector3(0, 0, -10);
            cameraObj.tag = "MainCamera";

            // 3. EventSystem (New Input System 연동)
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<EventSystem>();
            eventSystemObj.AddComponent<InputSystemUIInputModule>();

            // 4. CombatSystem 프리팹 인스턴스화 (월드 2D)
            GameObject combatPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CombatPrefabPath);
            GameObject combatInstance;
            if (combatPrefab != null)
            {
                combatInstance = (GameObject)PrefabUtility.InstantiatePrefab(combatPrefab);
                combatInstance.name = "CombatSystem";
            }
            else
            {
                combatInstance = new GameObject("CombatSystem");
                combatInstance.AddComponent<CombatManager>();
                Debug.LogWarning("[MainSceneBuilder] CombatSystem 프리팹을 찾지 못해 기본 오브젝트 생성");
            }
            combatInstance.transform.position = Vector3.zero;

            // 5. MainUI 프리팹 인스턴스화 (Canvas Overlay)
            GameObject uiPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MainUIPrefabPath);
            GameObject uiInstance;
            if (uiPrefab != null)
            {
                uiInstance = (GameObject)PrefabUtility.InstantiatePrefab(uiPrefab);
                uiInstance.name = "MainUI";
            }
            else
            {
                uiInstance = new GameObject("MainUI");
                uiInstance.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                uiInstance.AddComponent<CanvasScaler>();
                uiInstance.AddComponent<GraphicRaycaster>();
                uiInstance.AddComponent<UIManager>();
                Debug.LogWarning("[MainSceneBuilder] MainUI 프리팹을 찾지 못해 기본 오브젝트 생성");
            }

            Canvas mainCanvas = uiInstance.GetComponent<Canvas>();
            Transform canvasTransform = uiInstance.transform;

            // 6. InventorySystem 프리팹 인스턴스화 (UI 좌측 하단 배치)
            GameObject invPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(InventoryPrefabPath);
            GameObject invInstance;
            if (invPrefab != null)
            {
                invInstance = (GameObject)PrefabUtility.InstantiatePrefab(invPrefab);
                invInstance.name = "InventorySystem";
            }
            else
            {
                invInstance = new GameObject("InventorySystem");
                invInstance.AddComponent<InventoryGrid>();
                Debug.LogWarning("[MainSceneBuilder] InventorySystem 프리팹을 찾지 못해 기본 오브젝트 생성");
            }

            invInstance.transform.SetParent(canvasTransform, false);
            RectTransform invRt = invInstance.GetComponent<RectTransform>();
            if (invRt != null)
            {
                invRt.anchorMin = new Vector2(0, 0);
                invRt.anchorMax = new Vector2(0, 0);
                invRt.pivot = new Vector2(0, 0);
                invRt.anchoredPosition = new Vector2(100, 60);
            }

            // 인벤토리 초기 아이템 스타터 장착
            InventoryStarter starter = invInstance.GetComponent<InventoryStarter>();
            if (starter == null)
            {
                starter = invInstance.AddComponent<InventoryStarter>();
            }

            // 7. TokenSystem 프리팹 인스턴스화 (UI 우측 하단 배치)
            GameObject tokenPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TokenPrefabPath);
            GameObject tokenInstance;
            if (tokenPrefab != null)
            {
                tokenInstance = (GameObject)PrefabUtility.InstantiatePrefab(tokenPrefab);
                tokenInstance.name = "TokenSystem";
            }
            else
            {
                tokenInstance = new GameObject("TokenSystem");
                tokenInstance.AddComponent<TokenSpawner>();
                Debug.LogWarning("[MainSceneBuilder] TokenSystem 프리팹을 찾지 못해 기본 오브젝트 생성");
            }

            tokenInstance.transform.SetParent(canvasTransform, false);
            RectTransform tokenRt = tokenInstance.GetComponent<RectTransform>();
            if (tokenRt != null)
            {
                tokenRt.anchorMin = new Vector2(1, 0);
                tokenRt.anchorMax = new Vector2(1, 0);
                tokenRt.pivot = new Vector2(1, 0);
                tokenRt.anchoredPosition = new Vector2(-100, 60);
            }

            // 8. GameManager 및 StageManager 시스템 생성 및 바인딩
            GameObject gmObj = new GameObject("GameManager");
            StageManager stageManager = gmObj.AddComponent<StageManager>();
            GameManager gameManager = gmObj.AddComponent<GameManager>();

            UIManager uiManager = uiInstance.GetComponent<UIManager>();

            var gmSerialized = new SerializedObject(gameManager);
            gmSerialized.FindProperty("stageManager").objectReferenceValue = stageManager;
            gmSerialized.FindProperty("uiManager").objectReferenceValue = uiManager;
            gmSerialized.ApplyModifiedProperties();

            // 9. 씬 저장
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[MainSceneBuilder] ✅ 최종 통합 씬 저장 완료: {ScenePath}");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[MainSceneBuilder] 🎉 모든 시스템 조립 완료!");
        }
    }
}
#endif
