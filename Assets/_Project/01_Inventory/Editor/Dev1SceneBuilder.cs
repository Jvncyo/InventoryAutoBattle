#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using InventoryAutoBattle.Core;
using InventoryAutoBattle.Inventory;

namespace InventoryAutoBattle.Inventory.Editor
{
    public static class Dev1SceneBuilder
    {
        [MenuItem("InventoryAutoBattle/Build Dev1 Inventory Scene & Prefab")]
        public static void BuildDev1Scene()
        {
            Debug.Log("[Dev1SceneBuilder] 시험용 씬 및 프리팹 빌드 시작...");

            // 1. 새 씬 생성
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 2. 메인 카메라 생성
            GameObject cameraObj = new GameObject("Main Camera");
            Camera cam = cameraObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.12f, 0.13f, 0.16f, 1f); // 다크 톤 배경
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cameraObj.transform.position = new Vector3(0, 0, -10);
            cameraObj.tag = "MainCamera";

            // 3. EventSystem 생성 (신규 Input System 대응)
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<EventSystem>();
            eventSystemObj.AddComponent<InputSystemUIInputModule>();

            // 4. 메인 캔버스 생성
            GameObject canvasObj = new GameObject("Canvas_InventoryTest");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();

            // 5. 배경 패널
            GameObject bgObj = CreateUIObject("Background", canvasObj.transform);
            SetStretch(bgObj.GetComponent<RectTransform>());
            Image bgImg = bgObj.AddComponent<Image>();
            bgImg.color = new Color(0.1f, 0.11f, 0.14f, 1f);

            // 6. 타이틀 및 헤더 영역
            GameObject headerObj = CreateUIObject("Header", canvasObj.transform);
            RectTransform headerRt = headerObj.GetComponent<RectTransform>();
            headerRt.anchorMin = new Vector2(0, 1);
            headerRt.anchorMax = new Vector2(1, 1);
            headerRt.pivot = new Vector2(0.5f, 1);
            headerRt.anchoredPosition = new Vector2(0, 0);
            headerRt.sizeDelta = new Vector2(0, 80);

            Text titleText = CreateText(headerObj.transform, "TitleText", "🎒 [개발자 1] 6x6 인벤토리 & 그리드 시스템 테스트", 28, TextAnchor.MiddleCenter, Color.white);
            SetStretch(titleText.rectTransform);

            // 7. 인벤토리 메인 시스템 프리팹용 루트 오브젝트 생성
            GameObject inventorySystemRoot = CreateUIObject("InventorySystem", canvasObj.transform);
            RectTransform invSysRt = inventorySystemRoot.GetComponent<RectTransform>();
            invSysRt.anchorMin = new Vector2(0.5f, 0.5f);
            invSysRt.anchorMax = new Vector2(0.5f, 0.5f);
            invSysRt.pivot = new Vector2(0.5f, 0.5f);
            invSysRt.anchoredPosition = new Vector2(-150, -20);
            invSysRt.sizeDelta = new Vector2(520, 520);

            // 인벤토리 배경 프레임
            Image invSysBg = inventorySystemRoot.AddComponent<Image>();
            invSysBg.color = new Color(0.15f, 0.17f, 0.22f, 0.95f);

            // 인벤토리 헤더 텍스트
            GameObject invTitle = CreateUIObject("InvTitle", inventorySystemRoot.transform);
            RectTransform invTitleRt = invTitle.GetComponent<RectTransform>();
            invTitleRt.anchorMin = new Vector2(0, 1);
            invTitleRt.anchorMax = new Vector2(1, 1);
            invTitleRt.pivot = new Vector2(0.5f, 1);
            invTitleRt.anchoredPosition = new Vector2(0, -10);
            invTitleRt.sizeDelta = new Vector2(0, 30);
            CreateText(invTitle.transform, "Text", "6 × 6 Grid Inventory", 18, TextAnchor.MiddleCenter, new Color(0.9f, 0.85f, 0.6f));

            // 슬롯 컨테이너
            GameObject slotsContainer = CreateUIObject("SlotsContainer", inventorySystemRoot.transform);
            RectTransform slotsRt = slotsContainer.GetComponent<RectTransform>();
            slotsRt.anchorMin = new Vector2(0.5f, 0.5f);
            slotsRt.anchorMax = new Vector2(0.5f, 0.5f);
            slotsRt.pivot = new Vector2(0.5f, 0.5f);
            // 6개 셀 (70px * 6 + 6px * 5 = 420 + 30 = 450px)
            slotsRt.sizeDelta = new Vector2(450, 450);
            slotsRt.anchoredPosition = new Vector2(0, -15);

            // 아이템 컨테이너 (슬롯 위에 그려지도록 뒤에 배치)
            GameObject itemsContainer = CreateUIObject("ItemsContainer", inventorySystemRoot.transform);
            RectTransform itemsRt = itemsContainer.GetComponent<RectTransform>();
            itemsRt.anchorMin = slotsRt.anchorMin;
            itemsRt.anchorMax = slotsRt.anchorMax;
            itemsRt.pivot = slotsRt.pivot;
            itemsRt.sizeDelta = slotsRt.sizeDelta;
            itemsRt.anchoredPosition = slotsRt.anchoredPosition;

            // InventoryGrid 컴포넌트 추가
            InventoryGrid grid = inventorySystemRoot.AddComponent<InventoryGrid>();
            var gridSerialized = new SerializedObject(grid);
            gridSerialized.FindProperty("slotsContainer").objectReferenceValue = slotsRt;
            gridSerialized.FindProperty("itemsContainer").objectReferenceValue = itemsRt;
            gridSerialized.FindProperty("cellSize").floatValue = 70f;
            gridSerialized.FindProperty("cellSpacing").floatValue = 6f;
            gridSerialized.ApplyModifiedProperties();

            // 슬롯들 생성 실행
            grid.InitializeGrid();

            // 8. 우측 사이드 패널 (테스트 제어 및 로그)
            GameObject sidePanel = CreateUIObject("SidePanel_TestControls", canvasObj.transform);
            RectTransform sideRt = sidePanel.GetComponent<RectTransform>();
            sideRt.anchorMin = new Vector2(0.5f, 0.5f);
            sideRt.anchorMax = new Vector2(0.5f, 0.5f);
            sideRt.pivot = new Vector2(0.5f, 0.5f);
            sideRt.anchoredPosition = new Vector2(360, -20);
            sideRt.sizeDelta = new Vector2(460, 680);
            Image sideBg = sidePanel.AddComponent<Image>();
            sideBg.color = new Color(0.14f, 0.16f, 0.2f, 0.95f);

            // 8-1. 빈 슬롯 카운터 UI
            GameObject freeCellObj = CreateUIObject("FreeCellCountText", sidePanel.transform);
            RectTransform fcRt = freeCellObj.GetComponent<RectTransform>();
            fcRt.anchorMin = new Vector2(0, 1);
            fcRt.anchorMax = new Vector2(1, 1);
            fcRt.pivot = new Vector2(0.5f, 1);
            fcRt.anchoredPosition = new Vector2(0, -15);
            fcRt.sizeDelta = new Vector2(-20, 35);
            Text freeCellText = CreateText(freeCellObj.transform, "Text", "남은 빈 슬롯: <b>36</b> / 36", 20, TextAnchor.MiddleCenter, new Color(0.3f, 0.9f, 0.5f));

            // 8-2. 조작 가이드 박스
            GameObject guideObj = CreateUIObject("GuideBox", sidePanel.transform);
            RectTransform gRt = guideObj.GetComponent<RectTransform>();
            gRt.anchorMin = new Vector2(0, 1);
            gRt.anchorMax = new Vector2(1, 1);
            gRt.pivot = new Vector2(0.5f, 1);
            gRt.anchoredPosition = new Vector2(0, -60);
            gRt.sizeDelta = new Vector2(-30, 95);
            Image guideBg = guideObj.AddComponent<Image>();
            guideBg.color = new Color(0.2f, 0.23f, 0.28f, 0.8f);
            Text guideText = CreateText(guideObj.transform, "GuideText", "", 13, TextAnchor.MiddleLeft, Color.white);
            RectTransform gtRt = guideText.rectTransform;
            gtRt.anchorMin = Vector2.zero;
            gtRt.anchorMax = Vector2.one;
            gtRt.offsetMin = new Vector2(12, 6);
            gtRt.offsetMax = new Vector2(-12, -6);

            // 8-3. 보관함(아이템 생성 대기) 영역
            GameObject storageObj = CreateUIObject("ItemStorageArea", sidePanel.transform);
            RectTransform stRt = storageObj.GetComponent<RectTransform>();
            stRt.anchorMin = new Vector2(0, 1);
            stRt.anchorMax = new Vector2(1, 1);
            stRt.pivot = new Vector2(0.5f, 1);
            stRt.anchoredPosition = new Vector2(0, -170);
            stRt.sizeDelta = new Vector2(-30, 150);
            Image stBg = storageObj.AddComponent<Image>();
            stBg.color = new Color(0.18f, 0.2f, 0.25f, 0.7f);
            CreateText(storageObj.transform, "StorageTitle", "[새 아이템 대기소]", 14, TextAnchor.UpperCenter, new Color(0.7f, 0.75f, 0.85f));

            // 8-4. 버튼 영역
            GameObject btnGroup = CreateUIObject("ButtonGrid", sidePanel.transform);
            RectTransform bgRt = btnGroup.GetComponent<RectTransform>();
            bgRt.anchorMin = new Vector2(0, 1);
            bgRt.anchorMax = new Vector2(1, 1);
            bgRt.pivot = new Vector2(0.5f, 1);
            bgRt.anchoredPosition = new Vector2(0, -335);
            bgRt.sizeDelta = new Vector2(-30, 110);
            GridLayoutGroup gridLayout = btnGroup.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(205, 45);
            gridLayout.spacing = new Vector2(10, 10);

            // 8-5. 이벤트 로그 창
            GameObject logBox = CreateUIObject("LogBox", sidePanel.transform);
            RectTransform lbRt = logBox.GetComponent<RectTransform>();
            lbRt.anchorMin = new Vector2(0, 0);
            lbRt.anchorMax = new Vector2(1, 0);
            lbRt.pivot = new Vector2(0.5f, 0);
            lbRt.anchoredPosition = new Vector2(0, 15);
            lbRt.sizeDelta = new Vector2(-30, 180);
            Image lbBg = logBox.AddComponent<Image>();
            lbBg.color = new Color(0.08f, 0.09f, 0.12f, 0.9f);
            Text logText = CreateText(logBox.transform, "LogContent", "[시스템 준비 완료] 테스트를 시작합니다...", 13, TextAnchor.UpperLeft, new Color(0.85f, 0.85f, 0.9f));
            RectTransform ltRt = logText.rectTransform;
            ltRt.anchorMin = Vector2.zero;
            ltRt.anchorMax = Vector2.one;
            ltRt.offsetMin = new Vector2(10, 8);
            ltRt.offsetMax = new Vector2(-10, -8);

            // 9. InventoryTestManager 연결
            GameObject testManagerObj = new GameObject("InventoryTestManager");
            InventoryTestManager testManager = testManagerObj.AddComponent<InventoryTestManager>();
            var tmSerialized = new SerializedObject(testManager);
            tmSerialized.FindProperty("inventoryGrid").objectReferenceValue = grid;
            tmSerialized.FindProperty("itemStorageArea").objectReferenceValue = stRt;
            tmSerialized.FindProperty("freeCellText").objectReferenceValue = freeCellText;
            tmSerialized.FindProperty("guideText").objectReferenceValue = guideText;
            tmSerialized.FindProperty("logText").objectReferenceValue = logText;
            tmSerialized.ApplyModifiedProperties();

            // 버튼들 생성 및 연결
            CreateTestButton(btnGroup.transform, "Btn_Sword", "+ 초보자의 검 (1x2)", () => testManager.OnClickSpawnSword());
            CreateTestButton(btnGroup.transform, "Btn_Shield", "+ 나무 방패 (2x2)", () => testManager.OnClickSpawnShield());
            CreateTestButton(btnGroup.transform, "Btn_Potion", "+ 체력 물약 (1x1)", () => testManager.OnClickSpawnPotion());
            CreateTestButton(btnGroup.transform, "Btn_Clear", "🗑️ 인벤토리 비우기", () => testManager.OnClickClearInventory());

            // 10. 프리팹 저장 (InventorySystem.prefab)
            string prefabPath = "Assets/_Project/01_Inventory/Prefabs/InventorySystem.prefab";
            PrefabUtility.SaveAsPrefabAsset(inventorySystemRoot, prefabPath);
            Debug.Log($"[Dev1SceneBuilder] 프리팹 생성 완료: {prefabPath}");

            // 11. 씬 저장
            string scenePath = "Assets/_Project/01_Inventory/Scenes/Dev1_Inventory_Test.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"[Dev1SceneBuilder] 씬 생성 및 저장 완료: {scenePath}");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Dev1SceneBuilder] 모든 빌드 절차 완료!");
        }

        private static GameObject CreateUIObject(string name, Transform parent)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            return obj;
        }

        private static void SetStretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
        }

        private static Text CreateText(Transform parent, string name, string content, int fontSize, TextAnchor alignment, Color color)
        {
            GameObject textObj = CreateUIObject(name, parent);
            Text text = textObj.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.supportRichText = true;
            return text;
        }

        private static void CreateTestButton(Transform parent, string name, string label, UnityEngine.Events.UnityAction onClick)
        {
            GameObject btnObj = CreateUIObject(name, parent);
            Image img = btnObj.AddComponent<Image>();
            img.color = new Color(0.24f, 0.28f, 0.36f, 0.95f);

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;
            ColorBlock cb = btn.colors;
            cb.highlightedColor = new Color(0.32f, 0.38f, 0.48f, 1f);
            cb.pressedColor = new Color(0.18f, 0.22f, 0.3f, 1f);
            btn.colors = cb;
            btn.onClick.AddListener(onClick);

            Text t = CreateText(btnObj.transform, "Label", label, 14, TextAnchor.MiddleCenter, Color.white);
            SetStretch(t.rectTransform);
        }
    }
}
#endif
