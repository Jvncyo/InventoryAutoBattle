using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using InventoryAutoBattle.Core;

namespace InventoryAutoBattle.Token
{
    /// <summary>
    /// 실시간 토큰 생성기 및 관리 시스템.
    /// 전투 진행(GameState.Battle) 중 주기적으로 무작위 토큰을 생성하고, 만료 및 UI 배치를 관리합니다.
    /// </summary>
    public class TokenSpawner : MonoBehaviour
    {
        [Header("토큰 보관 트레이")]
        [SerializeField] private RectTransform tokenTray;
        [SerializeField] private GameObject tokenPrefab;

        [Header("스폰 설정")]
        [SerializeField] private float spawnInterval = 1.8f;
        [SerializeField] private int maxTokens = 5;
        [SerializeField] private bool autoStartInBattle = true;

        [Header("토큰 데이터 목록 (Attack, Defense, Heal)")]
        [SerializeField] private TokenData[] tokenDataPool;

        public float SpawnInterval => spawnInterval;
        public int MaxTokens => maxTokens;
        public int ActiveTokenCount => activeTokens.Count;

        private List<TokenInstance> activeTokens = new List<TokenInstance>();
        private Coroutine spawnCoroutine;
        private bool isSpawning = false;

        public bool IsSpawning => isSpawning;

        private void Awake()
        {
            if (tokenTray == null)
                tokenTray = transform as RectTransform;

            EnsureTokenDataPool();
        }

        private void OnEnable()
        {
            GameEvents.OnGameStateChanged += HandleGameStateChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnGameStateChanged -= HandleGameStateChanged;
        }

        private void EnsureTokenDataPool()
        {
            if (tokenDataPool == null || tokenDataPool.Length == 0)
            {
                TokenData atk = ScriptableObject.CreateInstance<TokenData>();
                atk.tokenId = "token_attack";
                atk.tokenName = "공격 토큰";
                atk.tokenType = TokenType.Attack;
                atk.duration = 5f;

                TokenData def = ScriptableObject.CreateInstance<TokenData>();
                def.tokenId = "token_defense";
                def.tokenName = "방어 토큰";
                def.tokenType = TokenType.Defense;
                def.duration = 5f;

                TokenData heal = ScriptableObject.CreateInstance<TokenData>();
                heal.tokenId = "token_heal";
                heal.tokenName = "회복 토큰";
                heal.tokenType = TokenType.Heal;
                heal.duration = 5f;

                tokenDataPool = new TokenData[] { atk, def, heal };
            }
        }

        private void HandleGameStateChanged(GameState state)
        {
            if (state == GameState.Battle)
            {
                if (autoStartInBattle)
                {
                    StartSpawning();
                }
            }
            else
            {
                StopSpawning();
                ClearAllTokens();
            }
        }

        public void StartSpawning()
        {
            if (isSpawning) return;
            isSpawning = true;
            spawnCoroutine = StartCoroutine(SpawnRoutine());
            Debug.Log("[TokenSpawner] ▶ 토큰 자동 생성 루프 개시!");
        }

        public void StopSpawning()
        {
            if (!isSpawning) return;
            isSpawning = false;
            if (spawnCoroutine != null)
            {
                StopCoroutine(spawnCoroutine);
                spawnCoroutine = null;
            }
            Debug.Log("[TokenSpawner] ⏹ 토큰 자동 생성 중지!");
        }

        private IEnumerator SpawnRoutine()
        {
            // 전투 시작 즉시 첫 번째 토큰 하나 스폰
            if (activeTokens.Count < maxTokens)
            {
                SpawnRandomToken();
            }

            while (isSpawning)
            {
                yield return new WaitForSeconds(spawnInterval);

                if (activeTokens.Count < maxTokens)
                {
                    SpawnRandomToken();
                }
            }
        }

        /// <summary>
        /// 무작위 토큰(공격, 방어, 회복 중 하나) 생성
        /// </summary>
        public TokenInstance SpawnRandomToken()
        {
            if (activeTokens.Count >= maxTokens) return null;
            if (tokenDataPool == null || tokenDataPool.Length == 0) return null;

            int randomIndex = Random.Range(0, tokenDataPool.Length);
            return SpawnToken(tokenDataPool[randomIndex]);
        }

        /// <summary>
        /// 지정한 타입의 토큰 생성
        /// </summary>
        public TokenInstance SpawnToken(TokenType type)
        {
            if (activeTokens.Count >= maxTokens) return null;

            TokenData targetData = null;
            foreach (var data in tokenDataPool)
            {
                if (data.tokenType == type)
                {
                    targetData = data;
                    break;
                }
            }

            if (targetData == null)
            {
                targetData = ScriptableObject.CreateInstance<TokenData>();
                targetData.tokenType = type;
                targetData.tokenName = TokenInstance.GetTokenTypeName(type);
                targetData.duration = 5f;
            }

            return SpawnToken(targetData);
        }

        public TokenInstance SpawnToken(TokenData data)
        {
            GameObject tokenObj;
            if (tokenPrefab != null)
            {
                tokenObj = Instantiate(tokenPrefab, tokenTray);
            }
            else
            {
                tokenObj = CreateDefaultTokenObject(data);
            }

            TokenInstance tokenInstance = tokenObj.GetComponent<TokenInstance>();
            if (tokenInstance == null)
            {
                tokenInstance = tokenObj.AddComponent<TokenInstance>();
            }

            tokenInstance.Initialize(data, this);
            activeTokens.Add(tokenInstance);

            return tokenInstance;
        }

        private GameObject CreateDefaultTokenObject(TokenData data)
        {
            GameObject tokenObj = new GameObject($"Token_{data.tokenType}", typeof(RectTransform), typeof(CanvasGroup), typeof(Image), typeof(TokenInstance));
            tokenObj.transform.SetParent(tokenTray, false);

            RectTransform rt = tokenObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(85, 85);

            Image bg = tokenObj.GetComponent<Image>();
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
            nameTxt.text = TokenInstance.GetTokenTypeName(data.tokenType);
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
            timeTxt.text = $"{data.duration:F1}s";
            RectTransform timeRt = timeObj.GetComponent<RectTransform>();
            timeRt.anchorMin = Vector2.zero;
            timeRt.anchorMax = new Vector2(1, 0.35f);
            timeRt.offsetMin = Vector2.zero;
            timeRt.offsetMax = Vector2.zero;

            var ti = tokenObj.GetComponent<TokenInstance>();
            ti.SetupComponents(bg, fillImg, nameTxt, timeTxt);

            return tokenObj;
        }

        public void OnTokenDestroyed(TokenInstance token)
        {
            activeTokens.Remove(token);
        }

        public void ClearAllTokens()
        {
            for (int i = activeTokens.Count - 1; i >= 0; i--)
            {
                if (activeTokens[i] != null)
                {
                    Destroy(activeTokens[i].gameObject);
                }
            }
            activeTokens.Clear();
        }
    }
}
