#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using InventoryAutoBattle.Core;
using InventoryAutoBattle.Combat;

namespace InventoryAutoBattle.Combat.Editor
{
    public static class Dev3SceneBuilder
    {
        [MenuItem("InventoryAutoBattle/Build Dev3 Combat Scene & Prefab")]
        public static void BuildDev3Scene()
        {
            Debug.Log("[Dev3SceneBuilder] Dev3 자동 전투 씬 및 프리팹 빌드 시작...");

            // 1. 새 씬 생성
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 2. 메인 카메라
            GameObject cameraObj = new GameObject("Main Camera");
            Camera cam = cameraObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.12f, 0.13f, 0.17f, 1f);
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cameraObj.transform.position = new Vector3(0, 0, -10);
            cameraObj.tag = "MainCamera";

            // 3. 스프라이트 에셋 로드 (Soldier & Orc)
            Sprite soldierSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Tiny RPG Character Asset Pack 01 v2.0 -Free Soldier&Orc/Tiny RPG Character Asset Pack 01 v2.0 -Free Soldier&Orc/Characters(100x100 split)/Soldier/Soldier with shadows/Soldier.png");
            Sprite orcSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Tiny RPG Character Asset Pack 01 v2.0 -Free Soldier&Orc/Tiny RPG Character Asset Pack 01 v2.0 -Free Soldier&Orc/Characters(100x100 split)/Orc/Orc with shadows/Orc.png");

            // 4. CombatSystem (프리팹 루트)
            GameObject combatSystemRoot = new GameObject("CombatSystem");
            combatSystemRoot.transform.position = Vector3.zero;

            // 4-1. 플레이어 오브젝트
            GameObject playerObj = new GameObject("PlayerEntity");
            playerObj.transform.SetParent(combatSystemRoot.transform, false);
            playerObj.transform.position = new Vector3(-3.5f, 0.5f, 0f);
            playerObj.transform.localScale = new Vector3(2.5f, 2.5f, 1f);

            SpriteRenderer playerSr = playerObj.AddComponent<SpriteRenderer>();
            playerSr.sprite = soldierSprite;
            playerSr.sortingOrder = 5;
            PlayerEntity playerEntity = playerObj.AddComponent<PlayerEntity>();

            // 4-2. 적 오브젝트
            GameObject enemyObj = new GameObject("EnemyEntity");
            enemyObj.transform.SetParent(combatSystemRoot.transform, false);
            enemyObj.transform.position = new Vector3(3.5f, 0.5f, 0f);
            enemyObj.transform.localScale = new Vector3(2.5f, 2.5f, 1f);

            SpriteRenderer enemySr = enemyObj.AddComponent<SpriteRenderer>();
            enemySr.sprite = orcSprite;
            enemySr.flipX = true; // 플레이어를 바라보도록 좌우 반전
            enemySr.sortingOrder = 5;
            EnemyEntity enemyEntity = enemyObj.AddComponent<EnemyEntity>();

            // 4-3. 바닥 배경 라인 (전투 무대)
            GameObject arenaFloor = new GameObject("ArenaFloor");
            arenaFloor.transform.SetParent(combatSystemRoot.transform, false);
            arenaFloor.transform.position = new Vector3(0f, -0.6f, 0f);
            SpriteRenderer floorSr = arenaFloor.AddComponent<SpriteRenderer>();
            floorSr.color = new Color(0.2f, 0.22f, 0.28f, 0.8f);
            arenaFloor.transform.localScale = new Vector3(12f, 0.15f, 1f);

            // 4-4. CombatManager 컴포넌트 추가 및 바인딩
            CombatManager combatManager = combatSystemRoot.AddComponent<CombatManager>();
            var cmSerialized = new SerializedObject(combatManager);
            cmSerialized.FindProperty("player").objectReferenceValue = playerEntity;
            cmSerialized.FindProperty("enemy").objectReferenceValue = enemyEntity;
            cmSerialized.ApplyModifiedProperties();

            // 5. EventSystem (Input System)
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<EventSystem>();
            eventSystemObj.AddComponent<InputSystemUIInputModule>();

            // 6. 메인 UI 캔버스
            GameObject canvasObj = new GameObject("Canvas_CombatTest");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();

            // 6-1. 상단 HUD 체력바 영역
            GameObject hudPanel = CreateUIObject("HUD_HpBars", canvasObj.transform);
            RectTransform hudRt = hudPanel.GetComponent<RectTransform>();
            hudRt.anchorMin = new Vector2(0.5f, 1);
            hudRt.anchorMax = new Vector2(0.5f, 1);
            hudRt.pivot = new Vector2(0.5f, 1);
            hudRt.anchoredPosition = new Vector2(0, -20);
            hudRt.sizeDelta = new Vector2(1100, 100);

            // 플레이어 HP 바 (좌측)
            var (pSlider, pText) = CreateHpBar(hudPanel.transform, "PlayerHpBar", new Vector2(-280, 0), "용사 HP: 100 / 100", new Color(0.2f, 0.85f, 0.35f));

            // VS 텍스트
            Text vsText = CreateText(hudPanel.transform, "Text_VS", "⚔️ VS ⚔️", 24, TextAnchor.MiddleCenter, new Color(0.9f, 0.85f, 0.6f));
            RectTransform vsRt = vsText.rectTransform;
            vsRt.anchoredPosition = Vector2.zero;
            vsRt.sizeDelta = new Vector2(100, 50);

            // 적 HP 바 (우측)
            var (eSlider, eText) = CreateHpBar(hudPanel.transform, "EnemyHpBar", new Vector2(280, 0), "적 HP: 100 / 100", new Color(0.9f, 0.25f, 0.25f));

            // 6-2. 우측 제어 및 로그 패널
            GameObject sidePanel = CreateUIObject("SidePanel_Controls", canvasObj.transform);
            RectTransform sideRt = sidePanel.GetComponent<RectTransform>();
            sideRt.anchorMin = new Vector2(1, 0.5f);
            sideRt.anchorMax = new Vector2(1, 0.5f);
            sideRt.pivot = new Vector2(1, 0.5f);
            sideRt.anchoredPosition = new Vector2(-40, 0);
            sideRt.sizeDelta = new Vector2(440, 720);
            Image sideBg = sidePanel.AddComponent<Image>();
            sideBg.color = new Color(0.14f, 0.16f, 0.2f, 0.95f);

            // 상태 텍스트
            GameObject statusObj = CreateUIObject("StatusText", sidePanel.transform);
            RectTransform stRt = statusObj.GetComponent<RectTransform>();
            stRt.anchorMin = new Vector2(0, 1);
            stRt.anchorMax = new Vector2(1, 1);
            stRt.pivot = new Vector2(0.5f, 1);
            stRt.anchoredPosition = new Vector2(0, -15);
            stRt.sizeDelta = new Vector2(-20, 35);
            Text txtStatus = CreateText(statusObj.transform, "Text", "현재 상태: 대기 중", 17, TextAnchor.MiddleCenter, Color.white);

            // 가이드 박스
            GameObject guideObj = CreateUIObject("GuideBox", sidePanel.transform);
            RectTransform gRt = guideObj.GetComponent<RectTransform>();
            gRt.anchorMin = new Vector2(0, 1);
            gRt.anchorMax = new Vector2(1, 1);
            gRt.pivot = new Vector2(0.5f, 1);
            gRt.anchoredPosition = new Vector2(0, -60);
            gRt.sizeDelta = new Vector2(-30, 135);
            Image guideBg = guideObj.AddComponent<Image>();
            guideBg.color = new Color(0.2f, 0.23f, 0.28f, 0.8f);
            Text txtGuide = CreateText(guideObj.transform, "GuideText", "", 13, TextAnchor.MiddleLeft, Color.white);
            RectTransform gtRt = txtGuide.rectTransform;
            gtRt.anchorMin = Vector2.zero;
            gtRt.anchorMax = Vector2.one;
            gtRt.offsetMin = new Vector2(12, 6);
            gtRt.offsetMax = new Vector2(-12, -6);

            // 메인 전투 토글 버튼
            GameObject toggleBtnObj = CreateUIObject("Btn_ToggleBattle", sidePanel.transform);
            RectTransform tbRt = toggleBtnObj.GetComponent<RectTransform>();
            tbRt.anchorMin = new Vector2(0, 1);
            tbRt.anchorMax = new Vector2(1, 1);
            tbRt.pivot = new Vector2(0.5f, 1);
            tbRt.anchoredPosition = new Vector2(0, -210);
            tbRt.sizeDelta = new Vector2(-30, 50);
            Image tbImg = toggleBtnObj.AddComponent<Image>();
            tbImg.color = new Color(0.85f, 0.3f, 0.25f, 1f);
            Button btnToggle = toggleBtnObj.AddComponent<Button>();
            btnToggle.targetGraphic = tbImg;
            Text txtToggle = CreateText(toggleBtnObj.transform, "Label", "전투 시작 (Start Battle)", 17, TextAnchor.MiddleCenter, Color.white);
            SetStretch(txtToggle.rectTransform);

            // Mock 스킬 버튼 그리드
            GameObject btnGroup = CreateUIObject("ButtonGrid", sidePanel.transform);
            RectTransform bgRt = btnGroup.GetComponent<RectTransform>();
            bgRt.anchorMin = new Vector2(0, 1);
            bgRt.anchorMax = new Vector2(1, 1);
            bgRt.pivot = new Vector2(0.5f, 1);
            bgRt.anchoredPosition = new Vector2(0, -275);
            bgRt.sizeDelta = new Vector2(-30, 110);
            GridLayoutGroup gridLayout = btnGroup.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(195, 45);
            gridLayout.spacing = new Vector2(10, 10);

            // 로그 박스
            GameObject logBox = CreateUIObject("LogBox", sidePanel.transform);
            RectTransform lbRt = logBox.GetComponent<RectTransform>();
            lbRt.anchorMin = new Vector2(0, 0);
            lbRt.anchorMax = new Vector2(1, 0);
            lbRt.pivot = new Vector2(0.5f, 0);
            lbRt.anchoredPosition = new Vector2(0, 15);
            lbRt.sizeDelta = new Vector2(-30, 290);
            Image lbBg = logBox.AddComponent<Image>();
            lbBg.color = new Color(0.08f, 0.09f, 0.12f, 0.9f);
            Text txtLog = CreateText(logBox.transform, "LogContent", "[시스템 준비 완료] 자동 전투 테스트를 시작합니다...", 13, TextAnchor.UpperLeft, new Color(0.85f, 0.85f, 0.9f));
            RectTransform ltRt = txtLog.rectTransform;
            ltRt.anchorMin = Vector2.zero;
            ltRt.anchorMax = Vector2.one;
            ltRt.offsetMin = new Vector2(10, 8);
            ltRt.offsetMax = new Vector2(-10, -8);

            // 7. CombatTestManager 생성 및 바인딩
            GameObject testManagerObj = new GameObject("CombatTestManager");
            CombatTestManager testManager = testManagerObj.AddComponent<CombatTestManager>();
            var tmSerialized = new SerializedObject(testManager);
            tmSerialized.FindProperty("combatManager").objectReferenceValue = combatManager;
            tmSerialized.FindProperty("playerHpSlider").objectReferenceValue = pSlider;
            tmSerialized.FindProperty("playerHpText").objectReferenceValue = pText;
            tmSerialized.FindProperty("enemyHpSlider").objectReferenceValue = eSlider;
            tmSerialized.FindProperty("enemyHpText").objectReferenceValue = eText;
            tmSerialized.FindProperty("txtStatus").objectReferenceValue = txtStatus;
            tmSerialized.FindProperty("txtLog").objectReferenceValue = txtLog;
            tmSerialized.FindProperty("txtGuide").objectReferenceValue = txtGuide;
            tmSerialized.FindProperty("btnToggleBattle").objectReferenceValue = btnToggle;
            tmSerialized.FindProperty("txtToggleBattle").objectReferenceValue = txtToggle;
            tmSerialized.ApplyModifiedProperties();

            btnToggle.onClick.AddListener(() => testManager.OnClickToggleBattle());
            CreateTestButton(btnGroup.transform, "Btn_Atk", "⚔️ [1] 무기 발동 (25 피해)", () => testManager.OnClickTriggerWeapon());
            CreateTestButton(btnGroup.transform, "Btn_Shield", "🛡️ [2] 방패 발동 (실드+20)", () => testManager.OnClickTriggerShield());
            CreateTestButton(btnGroup.transform, "Btn_Potion", "❤️ [3] 물약 발동 (HP+30)", () => testManager.OnClickTriggerPotion());
            CreateTestButton(btnGroup.transform, "Btn_Reset", "🔄 전투 리셋 (초기화)", () => testManager.OnClickResetCombat());

            // 8. 프리팹 저장 (Assets/_Project/03_Combat/Prefabs/CombatSystem.prefab)
            string prefabPath = "Assets/_Project/03_Combat/Prefabs/CombatSystem.prefab";
            PrefabUtility.SaveAsPrefabAsset(combatSystemRoot, prefabPath);
            Debug.Log($"[Dev3SceneBuilder] CombatSystem 프리팹 저장 완료: {prefabPath}");

            // 9. 씬 저장
            string scenePath = "Assets/_Project/03_Combat/Scenes/Dev3_Combat_Test.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"[Dev3SceneBuilder] Dev3_Combat_Test 씬 저장 완료: {scenePath}");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Dev3SceneBuilder] Dev3 빌드 성공 완료!");
        }

        private static (Slider, Text) CreateHpBar(Transform parent, string name, Vector2 pos, string defaultText, Color fillColor)
        {
            GameObject root = CreateUIObject(name, parent);
            RectTransform rt = root.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(360, 45);

            // 배경
            Image bg = root.AddComponent<Image>();
            bg.color = new Color(0.18f, 0.2f, 0.25f, 0.9f);

            // Fill Area
            GameObject fillArea = CreateUIObject("Fill Area", root.transform);
            RectTransform faRt = fillArea.GetComponent<RectTransform>();
            SetStretch(faRt);
            faRt.offsetMin = new Vector2(4, 4);
            faRt.offsetMax = new Vector2(-4, -4);

            GameObject fill = CreateUIObject("Fill", fillArea.transform);
            RectTransform fillRt = fill.GetComponent<RectTransform>();
            SetStretch(fillRt);
            Image fillImg = fill.AddComponent<Image>();
            fillImg.color = fillColor;

            // Slider 컴포넌트
            Slider slider = root.AddComponent<Slider>();
            slider.fillRect = fillRt;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;
            slider.interactable = false;

            // 텍스트
            Text txt = CreateText(root.transform, "Text", defaultText, 14, TextAnchor.MiddleCenter, Color.white);
            SetStretch(txt.rectTransform);

            return (slider, txt);
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
            text.raycastTarget = false;
            return text;
        }

        private static GameObject CreateTestButton(Transform parent, string name, string label, UnityEngine.Events.UnityAction onClick)
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
            return btnObj;
        }
    }
}
#endif
