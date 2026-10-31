#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using InventoryAutoBattle.Core;
using InventoryAutoBattle.Token;

namespace InventoryAutoBattle.Token.Editor
{
    public static class Dev2SceneBuilder
    {
        [MenuItem("InventoryAutoBattle/Build Dev2 Token Scene & Prefab")]
        public static void BuildDev2Scene()
        {
            Debug.Log("[Dev2SceneBuilder] Dev2 실시간 토큰 씬 및 프리팹 빌드 시작...");

            // 1. 새 씬 생성
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 2. 메인 카메라
            GameObject cameraObj = new GameObject("Main Camera");
            Camera cam = cameraObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.11f, 0.12f, 0.16f, 1f);
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cameraObj.transform.position = new Vector3(0, 0, -10);
            cameraObj.tag = "MainCamera";

            // 3. EventSystem (Input System)
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<EventSystem>();
            eventSystemObj.AddComponent<InputSystemUIInputModule>();

            // 4. 메인 캔버스
            GameObject canvasObj = new GameObject("Canvas_TokenTest");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();

            // 5. 배경
            GameObject bgObj = CreateUIObject("Background", canvasObj.transform);
            SetStretch(bgObj.GetComponent<RectTransform>());
            Image bgImg = bgObj.AddComponent<Image>();
            bgImg.color = new Color(0.09f, 0.1f, 0.13f, 1f);

            // 6. 타이틀 헤더
            GameObject headerObj = CreateUIObject("Header", canvasObj.transform);
            RectTransform headerRt = headerObj.GetComponent<RectTransform>();
            headerRt.anchorMin = new Vector2(0, 1);
            headerRt.anchorMax = new Vector2(1, 1);
            headerRt.pivot = new Vector2(0.5f, 1);
            headerRt.anchoredPosition = Vector2.zero;
            headerRt.sizeDelta = new Vector2(0, 80);
            CreateText(headerObj.transform, "TitleText", "⚡ [개발자 2] 실시간 토큰 생성 및 주입 시스템 테스트", 28, TextAnchor.MiddleCenter, Color.white);

            // 7. 좌측: Mock 더미 타깃 영역 (장비 슬롯 3개)
            GameObject targetsPanel = CreateUIObject("TargetsPanel_MockEquipment", canvasObj.transform);
            RectTransform tpRt = targetsPanel.GetComponent<RectTransform>();
            tpRt.anchorMin = new Vector2(0.5f, 0.5f);
            tpRt.anchorMax = new Vector2(0.5f, 0.5f);
            tpRt.pivot = new Vector2(0.5f, 0.5f);
            tpRt.anchoredPosition = new Vector2(-280, 50);
            tpRt.sizeDelta = new Vector2(400, 520);
            Image tpBg = targetsPanel.AddComponent<Image>();
            tpBg.color = new Color(0.14f, 0.16f, 0.22f, 0.95f);

            CreateText(targetsPanel.transform, "Title", "🎯 [더미 장비 타깃] 토큰을 여기로 드롭하세요", 16, TextAnchor.UpperCenter, new Color(0.9f, 0.85f, 0.6f))
                .rectTransform.anchoredPosition = new Vector2(0, -15);

            // 타깃 컨테이너
            GameObject tcObj = CreateUIObject("TargetsContainer", targetsPanel.transform);
            RectTransform tcRt = tcObj.GetComponent<RectTransform>();
            tcRt.anchorMin = new Vector2(0.5f, 0.5f);
            tcRt.anchorMax = new Vector2(0.5f, 0.5f);
            tcRt.pivot = new Vector2(0.5f, 0.5f);
            tcRt.anchoredPosition = new Vector2(0, -20);
            tcRt.sizeDelta = new Vector2(360, 420);
            VerticalLayoutGroup tcVlg = tcObj.AddComponent<VerticalLayoutGroup>();
            tcVlg.spacing = 15;
            tcVlg.childAlignment = TextAnchor.MiddleCenter;
            tcVlg.childControlWidth = true;
            tcVlg.childControlHeight = false;

            // 더미 타깃 3종 생성 (검, 방패, 포션)
            CreateDummyTargetUI(tcObj.transform, "Target_Sword", "초보자의 검 (Weapon)", ItemType.Weapon, TokenType.Attack, 2, 20f, new Color(0.35f, 0.15f, 0.15f, 0.95f));
            CreateDummyTargetUI(tcObj.transform, "Target_Shield", "나무 방패 (Shield)", ItemType.Shield, TokenType.Defense, 2, 25f, new Color(0.15f, 0.25f, 0.45f, 0.95f));
            CreateDummyTargetUI(tcObj.transform, "Target_Potion", "체력 물약 (Potion)", ItemType.Potion, TokenType.Heal, 1, 30f, new Color(0.15f, 0.4f, 0.2f, 0.95f));

            // 8. 하단 중앙: TokenSystem (프리팹 루트)
            GameObject tokenSystemRoot = CreateUIObject("TokenSystem", canvasObj.transform);
            RectTransform tsRt = tokenSystemRoot.GetComponent<RectTransform>();
            tsRt.anchorMin = new Vector2(0.5f, 0);
            tsRt.anchorMax = new Vector2(0.5f, 0);
            tsRt.pivot = new Vector2(0.5f, 0);
            tsRt.anchoredPosition = new Vector2(-150, 40);
            tsRt.sizeDelta = new Vector2(620, 160);
            Image tsBg = tokenSystemRoot.AddComponent<Image>();
            tsBg.color = new Color(0.12f, 0.14f, 0.19f, 0.95f);

            CreateText(tokenSystemRoot.transform, "TrayTitle", "🎒 실시간 토큰 트레이 (최대 5개, 5초 시한부)", 15, TextAnchor.UpperCenter, new Color(0.7f, 0.8f, 0.95f))
                .rectTransform.anchoredPosition = new Vector2(0, -10);

            // 토큰 나열 트레이 컨테이너
            GameObject trayObj = CreateUIObject("TokenTrayContainer", tokenSystemRoot.transform);
            RectTransform trayRt = trayObj.GetComponent<RectTransform>();
            trayRt.anchorMin = new Vector2(0.5f, 0.5f);
            trayRt.anchorMax = new Vector2(0.5f, 0.5f);
            trayRt.pivot = new Vector2(0.5f, 0.5f);
            trayRt.anchoredPosition = new Vector2(0, -15);
            trayRt.sizeDelta = new Vector2(580, 95);
            HorizontalLayoutGroup trayHlg = trayObj.AddComponent<HorizontalLayoutGroup>();
            trayHlg.spacing = 15;
            trayHlg.childAlignment = TextAnchor.MiddleCenter;
            trayHlg.childControlWidth = false;
            trayHlg.childControlHeight = false;

            // 8-1. 토큰 프리팹 생성 및 저장 (Assets/_Project/02_Token/Prefabs/TokenItem.prefab)
            GameObject tokenPrefabObj = CreateTokenPrefabObject();
            string tokenPrefabPath = "Assets/_Project/02_Token/Prefabs/TokenItem.prefab";
            GameObject tokenPrefab = PrefabUtility.SaveAsPrefabAsset(tokenPrefabObj, tokenPrefabPath);
            Object.DestroyImmediate(tokenPrefabObj);
            Debug.Log($"[Dev2SceneBuilder] TokenItem 프리팹 생성 완료: {tokenPrefabPath}");

            // TokenSpawner 컴포넌트 추가 및 프리팹 연결
            TokenSpawner spawner = tokenSystemRoot.AddComponent<TokenSpawner>();
            var spSerialized = new SerializedObject(spawner);
            spSerialized.FindProperty("tokenTray").objectReferenceValue = trayRt;
            spSerialized.FindProperty("tokenPrefab").objectReferenceValue = tokenPrefab;
            spSerialized.FindProperty("spawnInterval").floatValue = 1.8f;
            spSerialized.FindProperty("maxTokens").intValue = 5;
            spSerialized.ApplyModifiedProperties();

            // 9. 우측 사이드 패널 (제어 & 로그)
            GameObject sidePanel = CreateUIObject("SidePanel_Controls", canvasObj.transform);
            RectTransform sideRt = sidePanel.GetComponent<RectTransform>();
            sideRt.anchorMin = new Vector2(0.5f, 0.5f);
            sideRt.anchorMax = new Vector2(0.5f, 0.5f);
            sideRt.pivot = new Vector2(0.5f, 0.5f);
            sideRt.anchoredPosition = new Vector2(360, 20);
            sideRt.sizeDelta = new Vector2(460, 680);
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
            gRt.sizeDelta = new Vector2(-30, 95);
            Image guideBg = guideObj.AddComponent<Image>();
            guideBg.color = new Color(0.2f, 0.23f, 0.28f, 0.8f);
            Text txtGuide = CreateText(guideObj.transform, "GuideText", "", 13, TextAnchor.MiddleLeft, Color.white);
            RectTransform gtRt = txtGuide.rectTransform;
            gtRt.anchorMin = Vector2.zero;
            gtRt.anchorMax = Vector2.one;
            gtRt.offsetMin = new Vector2(12, 6);
            gtRt.offsetMax = new Vector2(-12, -6);

            // 전투 토글 버튼 (메인)
            GameObject toggleBtnObj = CreateUIObject("Btn_ToggleBattle", sidePanel.transform);
            RectTransform tbRt = toggleBtnObj.GetComponent<RectTransform>();
            tbRt.anchorMin = new Vector2(0, 1);
            tbRt.anchorMax = new Vector2(1, 1);
            tbRt.pivot = new Vector2(0.5f, 1);
            tbRt.anchoredPosition = new Vector2(0, -170);
            tbRt.sizeDelta = new Vector2(-30, 50);
            Image tbImg = toggleBtnObj.AddComponent<Image>();
            tbImg.color = new Color(0.85f, 0.3f, 0.25f, 1f);
            Button btnToggle = toggleBtnObj.AddComponent<Button>();
            btnToggle.targetGraphic = tbImg;
            Text txtToggle = CreateText(toggleBtnObj.transform, "Label", "전투 시작 (Start Battle)", 17, TextAnchor.MiddleCenter, Color.white);
            SetStretch(txtToggle.rectTransform);

            // 수동 스폰 버튼 그리드
            GameObject btnGroup = CreateUIObject("ButtonGrid", sidePanel.transform);
            RectTransform bgRt = btnGroup.GetComponent<RectTransform>();
            bgRt.anchorMin = new Vector2(0, 1);
            bgRt.anchorMax = new Vector2(1, 1);
            bgRt.pivot = new Vector2(0.5f, 1);
            bgRt.anchoredPosition = new Vector2(0, -235);
            bgRt.sizeDelta = new Vector2(-30, 110);
            GridLayoutGroup gridLayout = btnGroup.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(205, 45);
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
            Text txtLog = CreateText(logBox.transform, "LogContent", "[시스템 준비 완료] 토큰 테스트를 시작합니다...", 13, TextAnchor.UpperLeft, new Color(0.85f, 0.85f, 0.9f));
            RectTransform ltRt = txtLog.rectTransform;
            ltRt.anchorMin = Vector2.zero;
            ltRt.anchorMax = Vector2.one;
            ltRt.offsetMin = new Vector2(10, 8);
            ltRt.offsetMax = new Vector2(-10, -8);

            // 10. TokenTestManager 추가 및 바인딩
            GameObject testManagerObj = new GameObject("TokenTestManager");
            TokenTestManager testManager = testManagerObj.AddComponent<TokenTestManager>();
            var tmSerialized = new SerializedObject(testManager);
            tmSerialized.FindProperty("tokenSpawner").objectReferenceValue = spawner;
            tmSerialized.FindProperty("btnToggleBattle").objectReferenceValue = btnToggle;
            tmSerialized.FindProperty("txtToggleBattle").objectReferenceValue = txtToggle;
            tmSerialized.FindProperty("txtStatus").objectReferenceValue = txtStatus;
            tmSerialized.FindProperty("txtGuide").objectReferenceValue = txtGuide;
            tmSerialized.FindProperty("txtLog").objectReferenceValue = txtLog;
            tmSerialized.ApplyModifiedProperties();

            btnToggle.onClick.AddListener(() => testManager.OnClickToggleBattleState());
            CreateTestButton(btnGroup.transform, "Btn_SpawnAtk", "⚔️ 공격 토큰 스폰", () => testManager.OnClickSpawnAttackToken());
            CreateTestButton(btnGroup.transform, "Btn_SpawnDef", "🛡️ 방어 토큰 스폰", () => testManager.OnClickSpawnDefenseToken());
            CreateTestButton(btnGroup.transform, "Btn_SpawnHeal", "❤️ 회복 토큰 스폰", () => testManager.OnClickSpawnHealToken());
            CreateTestButton(btnGroup.transform, "Btn_Clear", "🗑️ 토큰 전체 비우기", () => testManager.OnClickClearTokens());

            // 11. 프리팹 저장 (Assets/_Project/02_Token/Prefabs/TokenSystem.prefab)
            string prefabPath = "Assets/_Project/02_Token/Prefabs/TokenSystem.prefab";
            PrefabUtility.SaveAsPrefabAsset(tokenSystemRoot, prefabPath);
            Debug.Log($"[Dev2SceneBuilder] TokenSystem 프리팹 저장 완료: {prefabPath}");

            // 12. 씬 저장
            string scenePath = "Assets/_Project/02_Token/Scenes/Dev2_Token_Test.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"[Dev2SceneBuilder] Dev2_Token_Test 씬 저장 완료: {scenePath}");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Dev2SceneBuilder] 빌드 완료!");
        }

        private static GameObject CreateDummyTargetUI(Transform parent, string name, string label, ItemType iType, TokenType tType, int reqCount, float val, Color bgColor)
        {
            GameObject obj = CreateUIObject(name, parent);
            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(340, 110);

            Image bg = obj.AddComponent<Image>();
            bg.color = bgColor;
            bg.raycastTarget = true;

            TokenDummyTarget target = obj.AddComponent<TokenDummyTarget>();

            // 타이틀
            Text title = CreateText(obj.transform, "Title", label, 16, TextAnchor.MiddleLeft, Color.white);
            RectTransform titRt = title.rectTransform;
            titRt.anchorMin = new Vector2(0, 0.5f);
            titRt.anchorMax = new Vector2(1, 1);
            titRt.offsetMin = new Vector2(15, 0);
            titRt.offsetMax = new Vector2(-15, -10);

            // 게이지 배경
            GameObject barBg = CreateUIObject("BarBg", obj.transform);
            RectTransform bbgRt = barBg.GetComponent<RectTransform>();
            bbgRt.anchorMin = new Vector2(0, 0.15f);
            bbgRt.anchorMax = new Vector2(1, 0.45f);
            bbgRt.offsetMin = new Vector2(15, 0);
            bbgRt.offsetMax = new Vector2(-15, 0);
            Image bbgImg = barBg.AddComponent<Image>();
            bbgImg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
            bbgImg.raycastTarget = false;

            // 게이지 채우기
            GameObject barFill = CreateUIObject("BarFill", barBg.transform);
            SetStretch(barFill.GetComponent<RectTransform>());
            Image fillImg = barFill.AddComponent<Image>();
            fillImg.color = new Color(0.3f, 0.85f, 0.4f, 1f);
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillAmount = 0f;
            fillImg.raycastTarget = false;

            // 진행도 텍스트
            Text progTxt = CreateText(barBg.transform, "Progress", $"0 / {reqCount}", 13, TextAnchor.MiddleCenter, Color.white);
            SetStretch(progTxt.rectTransform);

            target.SetupComponents(bg, title, progTxt, fillImg);
            target.Initialize(label, iType, tType, reqCount, val);

            return obj;
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
            text.raycastTarget = false; // 드래그 방해 방지
            return text;
        }

        private static GameObject CreateTokenPrefabObject()
        {
            GameObject tokenObj = new GameObject("TokenItem", typeof(RectTransform), typeof(CanvasGroup), typeof(Image), typeof(TokenInstance));
            RectTransform rt = tokenObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(85, 85);

            Image bg = tokenObj.GetComponent<Image>();
            bg.color = new Color(0.85f, 0.25f, 0.25f, 0.95f);
            bg.raycastTarget = true;

            // 1. 타이머 원형/슬라이스 게이지 (외곽 테두리)
            GameObject fillObj = new GameObject("TimerFill", typeof(RectTransform), typeof(Image));
            fillObj.transform.SetParent(tokenObj.transform, false);
            Image fillImg = fillObj.GetComponent<Image>();
            fillImg.color = new Color(1f, 1f, 1f, 0.4f);
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Radial360;
            fillImg.fillOrigin = (int)Image.Origin360.Top;
            fillImg.fillClockwise = true;
            fillImg.raycastTarget = false;
            RectTransform fillRt = fillObj.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;

            // 2. 토큰 이름/아이콘 텍스트
            GameObject nameObj = new GameObject("Text_Name", typeof(RectTransform), typeof(Text));
            nameObj.transform.SetParent(tokenObj.transform, false);
            Text nameTxt = nameObj.GetComponent<Text>();
            nameTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            nameTxt.alignment = TextAnchor.MiddleCenter;
            nameTxt.color = Color.white;
            nameTxt.fontSize = 13;
            nameTxt.fontStyle = FontStyle.Bold;
            nameTxt.raycastTarget = false;
            nameTxt.text = "⚔️ 공격";
            RectTransform nameRt = nameObj.GetComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0, 0.35f);
            nameRt.anchorMax = Vector2.one;
            nameRt.offsetMin = Vector2.zero;
            nameRt.offsetMax = Vector2.zero;

            // 3. 타이머 남은 초 텍스트
            GameObject timeObj = new GameObject("Text_Timer", typeof(RectTransform), typeof(Text));
            timeObj.transform.SetParent(tokenObj.transform, false);
            Text timeTxt = timeObj.GetComponent<Text>();
            timeTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            timeTxt.alignment = TextAnchor.MiddleCenter;
            timeTxt.color = new Color(1f, 0.9f, 0.2f);
            timeTxt.fontSize = 12;
            timeTxt.raycastTarget = false;
            timeTxt.text = "5.0s";
            RectTransform timeRt = timeObj.GetComponent<RectTransform>();
            timeRt.anchorMin = Vector2.zero;
            timeRt.anchorMax = new Vector2(1, 0.35f);
            timeRt.offsetMin = Vector2.zero;
            timeRt.offsetMax = Vector2.zero;

            var ti = tokenObj.GetComponent<TokenInstance>();
            var tiSerialized = new SerializedObject(ti);
            tiSerialized.FindProperty("bgImage").objectReferenceValue = bg;
            tiSerialized.FindProperty("timerFillImage").objectReferenceValue = fillImg;
            tiSerialized.FindProperty("nameText").objectReferenceValue = nameTxt;
            tiSerialized.FindProperty("timerText").objectReferenceValue = timeTxt;
            tiSerialized.ApplyModifiedProperties();

            return tokenObj;
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
