#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using InventoryAutoBattle.Core;
using InventoryAutoBattle.GameLoop;

namespace InventoryAutoBattle.GameLoop.Editor
{
    public static class Dev4SceneBuilder
    {
        [MenuItem("InventoryAutoBattle/Build Dev4 GameLoop Scene & Prefab")]
        public static void BuildDev4Scene()
        {
            Debug.Log("[Dev4SceneBuilder] Dev4 게임 루프 씬 및 MainUI 프리팹 빌드 시작...");

            // 1. 새 씬 생성
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 2. 메인 카메라
            GameObject cameraObj = new GameObject("Main Camera");
            Camera cam = cameraObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.1f, 0.11f, 0.15f, 1f);
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cameraObj.transform.position = new Vector3(0, 0, -10);
            cameraObj.tag = "MainCamera";

            // 3. EventSystem (Input System)
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<EventSystem>();
            eventSystemObj.AddComponent<InputSystemUIInputModule>();

            // 4. MainUI 프리팹 루트 (Canvas 포함)
            GameObject mainUIRoot = new GameObject("MainUI");
            Canvas canvas = mainUIRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = mainUIRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            mainUIRoot.AddComponent<GraphicRaycaster>();

            // 4-1. 상단 HUD 패널
            GameObject topHudObj = CreateUIObject("TopHUD", mainUIRoot.transform);
            RectTransform topHudRt = topHudObj.GetComponent<RectTransform>();
            topHudRt.anchorMin = new Vector2(0, 1);
            topHudRt.anchorMax = new Vector2(1, 1);
            topHudRt.pivot = new Vector2(0.5f, 1);
            topHudRt.anchoredPosition = Vector2.zero;
            topHudRt.sizeDelta = new Vector2(0, 110);
            Image topHudBg = topHudObj.AddComponent<Image>();
            topHudBg.color = new Color(0.12f, 0.14f, 0.18f, 0.95f);

            // 스테이지 텍스트
            Text txtStage = CreateText(topHudObj.transform, "Text_Stage", "<b>Stage 1</b> - <color=#FF8888>슬라임</color>", 24, TextAnchor.MiddleCenter, Color.white);
            RectTransform stgRt = txtStage.rectTransform;
            stgRt.anchorMin = new Vector2(0.5f, 1);
            stgRt.anchorMax = new Vector2(0.5f, 1);
            stgRt.pivot = new Vector2(0.5f, 1);
            stgRt.anchoredPosition = new Vector2(0, -10);
            stgRt.sizeDelta = new Vector2(400, 35);

            // 인벤토리 여유 공간 텍스트 (좌상단)
            Text txtFreeCells = CreateText(topHudObj.transform, "Text_FreeCells", "인벤토리 여유: <b>36</b> / 36", 16, TextAnchor.MiddleLeft, new Color(0.7f, 0.9f, 0.7f));
            RectTransform fcRt = txtFreeCells.rectTransform;
            fcRt.anchorMin = new Vector2(0, 1);
            fcRt.anchorMax = new Vector2(0, 1);
            fcRt.pivot = new Vector2(0, 1);
            fcRt.anchoredPosition = new Vector2(30, -15);
            fcRt.sizeDelta = new Vector2(250, 30);

            // 확장 조각 텍스트 (우상단)
            Text txtFragments = CreateText(topHudObj.transform, "Text_Fragments", "확장 조각: <b>0</b> / 3", 16, TextAnchor.MiddleRight, new Color(1f, 0.85f, 0.3f));
            RectTransform fragRt = txtFragments.rectTransform;
            fragRt.anchorMin = new Vector2(1, 1);
            fragRt.anchorMax = new Vector2(1, 1);
            fragRt.pivot = new Vector2(1, 1);
            fragRt.anchoredPosition = new Vector2(-30, -15);
            fragRt.sizeDelta = new Vector2(250, 30);

            // 체력바 컨테이너
            GameObject hpContainer = CreateUIObject("HpBarContainer", topHudObj.transform);
            RectTransform hpcRt = hpContainer.GetComponent<RectTransform>();
            hpcRt.anchorMin = new Vector2(0.5f, 0);
            hpcRt.anchorMax = new Vector2(0.5f, 0);
            hpcRt.pivot = new Vector2(0.5f, 0);
            hpcRt.anchoredPosition = new Vector2(0, 12);
            hpcRt.sizeDelta = new Vector2(900, 45);

            var (pSlider, pText) = CreateHpBar(hpContainer.transform, "PlayerHpBar", new Vector2(-240, 0), "용사 HP: 100 / 100", new Color(0.2f, 0.85f, 0.35f));
            CreateText(hpContainer.transform, "Text_VS", "⚔️", 20, TextAnchor.MiddleCenter, Color.yellow)
                .rectTransform.anchoredPosition = Vector2.zero;
            var (eSlider, eText) = CreateHpBar(hpContainer.transform, "EnemyHpBar", new Vector2(240, 0), "적 HP: 100 / 100", new Color(0.9f, 0.25f, 0.25f));

            // 4-2. 준비 상태 패널 (하단 전투 시작 버튼)
            GameObject readyPanel = CreateUIObject("ReadyPanel", mainUIRoot.transform);
            RectTransform rpRt = readyPanel.GetComponent<RectTransform>();
            rpRt.anchorMin = new Vector2(0.5f, 0);
            rpRt.anchorMax = new Vector2(0.5f, 0);
            rpRt.pivot = new Vector2(0.5f, 0);
            rpRt.anchoredPosition = new Vector2(0, 35);
            rpRt.sizeDelta = new Vector2(280, 65);

            Button btnStartBattle = CreateButton(readyPanel.transform, "Btn_StartBattle", "⚔️ 전투 시작 (Battle!)", new Color(0.85f, 0.25f, 0.2f), 19);
            SetStretch(btnStartBattle.GetComponent<RectTransform>());

            // 4-3. 승리 / 보상 팝업
            GameObject rewardPopup = CreateUIObject("RewardPopup", mainUIRoot.transform);
            RectTransform rewRt = rewardPopup.GetComponent<RectTransform>();
            rewRt.anchorMin = new Vector2(0.5f, 0.5f);
            rewRt.anchorMax = new Vector2(0.5f, 0.5f);
            rewRt.pivot = new Vector2(0.5f, 0.5f);
            rewRt.anchoredPosition = Vector2.zero;
            rewRt.sizeDelta = new Vector2(550, 320);
            Image rewBg = rewardPopup.AddComponent<Image>();
            rewBg.color = new Color(0.12f, 0.18f, 0.15f, 0.98f);

            Text rewTitle = CreateText(rewardPopup.transform, "Title", "🎉 스테이지 승리!", 26, TextAnchor.MiddleCenter, new Color(0.3f, 0.95f, 0.5f));
            rewTitle.rectTransform.anchoredPosition = new Vector2(0, 95);
            rewTitle.rectTransform.sizeDelta = new Vector2(480, 45);

            Text rewDetail = CreateText(rewardPopup.transform, "Detail", "적을 물리쳤습니다! 확장 조각 (+1) 획득!", 16, TextAnchor.MiddleCenter, Color.white);
            rewDetail.rectTransform.anchoredPosition = new Vector2(0, 20);
            rewDetail.rectTransform.sizeDelta = new Vector2(480, 70);

            Button btnNext = CreateButton(rewardPopup.transform, "Btn_NextStage", "다음 스테이지로 ➡️", new Color(0.2f, 0.65f, 0.35f), 18);
            RectTransform bnRt = btnNext.GetComponent<RectTransform>();
            bnRt.anchoredPosition = new Vector2(0, -95);
            bnRt.sizeDelta = new Vector2(250, 50);
            rewardPopup.SetActive(false);

            // 4-4. 패배 / 게임오버 팝업
            GameObject gameOverPopup = CreateUIObject("GameOverPopup", mainUIRoot.transform);
            RectTransform goRt = gameOverPopup.GetComponent<RectTransform>();
            goRt.anchorMin = new Vector2(0.5f, 0.5f);
            goRt.anchorMax = new Vector2(0.5f, 0.5f);
            goRt.pivot = new Vector2(0.5f, 0.5f);
            goRt.anchoredPosition = Vector2.zero;
            goRt.sizeDelta = new Vector2(550, 320);
            Image goBg = gameOverPopup.AddComponent<Image>();
            goBg.color = new Color(0.2f, 0.12f, 0.12f, 0.98f);

            Text goTitle = CreateText(gameOverPopup.transform, "Title", "💀 패 배", 28, TextAnchor.MiddleCenter, new Color(0.95f, 0.3f, 0.3f));
            goTitle.rectTransform.anchoredPosition = new Vector2(0, 95);
            goTitle.rectTransform.sizeDelta = new Vector2(480, 45);

            Text goDetail = CreateText(gameOverPopup.transform, "Detail", "용사가 쓰러졌습니다...\n전열을 가다듬고 다시 도전하세요!", 16, TextAnchor.MiddleCenter, Color.white);
            goDetail.rectTransform.anchoredPosition = new Vector2(0, 20);
            goDetail.rectTransform.sizeDelta = new Vector2(480, 70);

            Button btnRetry = CreateButton(gameOverPopup.transform, "Btn_Retry", "다시 도전 🔄", new Color(0.7f, 0.25f, 0.25f), 18);
            RectTransform brRt = btnRetry.GetComponent<RectTransform>();
            brRt.anchoredPosition = new Vector2(0, -95);
            brRt.sizeDelta = new Vector2(250, 50);
            gameOverPopup.SetActive(false);

            // UIManager 컴포넌트 추가 및 바인딩
            UIManager uiManager = mainUIRoot.AddComponent<UIManager>();
            uiManager.SetupComponents(
                txtStage, pSlider, pText, eSlider, eText,
                txtFreeCells, txtFragments,
                readyPanel, btnStartBattle,
                rewardPopup, rewTitle, rewDetail, btnNext,
                gameOverPopup, goDetail, btnRetry);

            // 5. 우측 사이드 패널 (Dev4 테스트 컨트롤 & 로그창)
            GameObject sidePanel = CreateUIObject("SidePanel_TestControls", mainUIRoot.transform);
            RectTransform sideRt = sidePanel.GetComponent<RectTransform>();
            sideRt.anchorMin = new Vector2(1, 0.5f);
            sideRt.anchorMax = new Vector2(1, 0.5f);
            sideRt.pivot = new Vector2(1, 0.5f);
            sideRt.anchoredPosition = new Vector2(-30, -20);
            sideRt.sizeDelta = new Vector2(430, 680);
            Image sideBg = sidePanel.AddComponent<Image>();
            sideBg.color = new Color(0.14f, 0.16f, 0.2f, 0.95f);

            // 상태 텍스트
            Text txtStatus = CreateText(sidePanel.transform, "StatusText", "현재 상태: Ready", 17, TextAnchor.MiddleCenter, Color.white);
            txtStatus.rectTransform.anchorMin = new Vector2(0, 1);
            txtStatus.rectTransform.anchorMax = new Vector2(1, 1);
            txtStatus.rectTransform.pivot = new Vector2(0.5f, 1);
            txtStatus.rectTransform.anchoredPosition = new Vector2(0, -15);
            txtStatus.rectTransform.sizeDelta = new Vector2(-20, 35);

            // 가이드 박스
            GameObject guideObj = CreateUIObject("GuideBox", sidePanel.transform);
            RectTransform gRt = guideObj.GetComponent<RectTransform>();
            gRt.anchorMin = new Vector2(0, 1);
            gRt.anchorMax = new Vector2(1, 1);
            gRt.pivot = new Vector2(0.5f, 1);
            gRt.anchoredPosition = new Vector2(0, -60);
            gRt.sizeDelta = new Vector2(-30, 120);
            Image guideBg = guideObj.AddComponent<Image>();
            guideBg.color = new Color(0.2f, 0.23f, 0.28f, 0.8f);
            Text txtGuide = CreateText(guideObj.transform, "GuideText", "", 13, TextAnchor.MiddleLeft, Color.white);
            SetStretch(txtGuide.rectTransform);
            txtGuide.rectTransform.offsetMin = new Vector2(12, 6);
            txtGuide.rectTransform.offsetMax = new Vector2(-12, -6);

            // Mock 디버그 버튼 그리드
            GameObject btnGroup = CreateUIObject("ButtonGrid", sidePanel.transform);
            RectTransform bgRt = btnGroup.GetComponent<RectTransform>();
            bgRt.anchorMin = new Vector2(0, 1);
            bgRt.anchorMax = new Vector2(1, 1);
            bgRt.pivot = new Vector2(0.5f, 1);
            bgRt.anchoredPosition = new Vector2(0, -200);
            bgRt.sizeDelta = new Vector2(-30, 130);
            GridLayoutGroup gridLayout = btnGroup.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(190, 45);
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
            Text txtLog = CreateText(logBox.transform, "LogContent", "[시스템 준비 완료] 게임 루프 테스트를 시작합니다...", 13, TextAnchor.UpperLeft, new Color(0.85f, 0.85f, 0.9f));
            SetStretch(txtLog.rectTransform);
            txtLog.rectTransform.offsetMin = new Vector2(10, 8);
            txtLog.rectTransform.offsetMax = new Vector2(-10, -8);

            // 6. GameManager 및 StageManager 시스템 오브젝트 생성
            GameObject gameManagerObj = new GameObject("GameManager");
            StageManager stageManager = gameManagerObj.AddComponent<StageManager>();
            GameManager gameManager = gameManagerObj.AddComponent<GameManager>();

            var gmSerialized = new SerializedObject(gameManager);
            gmSerialized.FindProperty("stageManager").objectReferenceValue = stageManager;
            gmSerialized.FindProperty("uiManager").objectReferenceValue = uiManager;
            gmSerialized.ApplyModifiedProperties();

            // 7. GameLoopTestManager 생성 및 바인딩
            GameObject testManagerObj = new GameObject("GameLoopTestManager");
            GameLoopTestManager testManager = testManagerObj.AddComponent<GameLoopTestManager>();
            var tmSerialized = new SerializedObject(testManager);
            tmSerialized.FindProperty("gameManager").objectReferenceValue = gameManager;
            tmSerialized.FindProperty("txtStatus").objectReferenceValue = txtStatus;
            tmSerialized.FindProperty("txtLog").objectReferenceValue = txtLog;
            tmSerialized.FindProperty("txtGuide").objectReferenceValue = txtGuide;
            tmSerialized.ApplyModifiedProperties();

            CreateButton(btnGroup.transform, "Btn_Win", "🏆 [Mock] 강제 승리", new Color(0.25f, 0.6f, 0.3f), 14)
                .onClick.AddListener(() => testManager.OnClickForceWin());
            CreateButton(btnGroup.transform, "Btn_Lose", "💀 [Mock] 강제 패배", new Color(0.7f, 0.25f, 0.25f), 14)
                .onClick.AddListener(() => testManager.OnClickForceLose());
            CreateButton(btnGroup.transform, "Btn_DmgP", "🩸 [Mock] 플레이어 피격", new Color(0.3f, 0.35f, 0.45f), 13)
                .onClick.AddListener(() => testManager.OnClickDamagePlayer());
            CreateButton(btnGroup.transform, "Btn_DmgE", "🩸 [Mock] 적 피격", new Color(0.45f, 0.3f, 0.35f), 13)
                .onClick.AddListener(() => testManager.OnClickDamageEnemy());
            CreateButton(btnGroup.transform, "Btn_Inv", "🎒 [Mock] 인벤토리 변동", new Color(0.35f, 0.4f, 0.3f), 13)
                .onClick.AddListener(() => testManager.OnClickSimulateInventory());

            // 8. 프리팹 저장 (Assets/_Project/04_GameLoop_UI/Prefabs/MainUI.prefab)
            string prefabPath = "Assets/_Project/04_GameLoop_UI/Prefabs/MainUI.prefab";
            PrefabUtility.SaveAsPrefabAsset(mainUIRoot, prefabPath);
            Debug.Log($"[Dev4SceneBuilder] MainUI 프리팹 저장 완료: {prefabPath}");

            // 9. 씬 저장
            string scenePath = "Assets/_Project/04_GameLoop_UI/Scenes/Dev4_GameLoop_Test.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"[Dev4SceneBuilder] Dev4_GameLoop_Test 씬 저장 완료: {scenePath}");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Dev4SceneBuilder] Dev4 빌드 완료!");
        }

        private static (Slider, Text) CreateHpBar(Transform parent, string name, Vector2 pos, string defaultText, Color fillColor)
        {
            GameObject root = CreateUIObject(name, parent);
            RectTransform rt = root.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(340, 38);

            Image bg = root.AddComponent<Image>();
            bg.color = new Color(0.18f, 0.2f, 0.25f, 0.9f);

            GameObject fillArea = CreateUIObject("Fill Area", root.transform);
            RectTransform faRt = fillArea.GetComponent<RectTransform>();
            SetStretch(faRt);
            faRt.offsetMin = new Vector2(3, 3);
            faRt.offsetMax = new Vector2(-3, -3);

            GameObject fill = CreateUIObject("Fill", fillArea.transform);
            RectTransform fillRt = fill.GetComponent<RectTransform>();
            SetStretch(fillRt);
            Image fillImg = fill.AddComponent<Image>();
            fillImg.color = fillColor;

            Slider slider = root.AddComponent<Slider>();
            slider.fillRect = fillRt;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;
            slider.interactable = false;

            Text txt = CreateText(root.transform, "Text", defaultText, 14, TextAnchor.MiddleCenter, Color.white);
            SetStretch(txt.rectTransform);

            return (slider, txt);
        }

        private static Button CreateButton(Transform parent, string name, string label, Color bgColor, int fontSize)
        {
            GameObject btnObj = CreateUIObject(name, parent);
            Image img = btnObj.AddComponent<Image>();
            img.color = bgColor;

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;

            Text t = CreateText(btnObj.transform, "Label", label, fontSize, TextAnchor.MiddleCenter, Color.white);
            SetStretch(t.rectTransform);

            return btn;
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
    }
}
#endif
