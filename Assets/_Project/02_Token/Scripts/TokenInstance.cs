using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using InventoryAutoBattle.Core;

namespace InventoryAutoBattle.Token
{
    /// <summary>
    /// 실시간으로 생성되는 단일 토큰 UI 컴포넌트.
    /// 5초 타이머 게이지, 깜빡임 만료 연출, 마우스 드래그 앤 드롭 주입 기능을 담당합니다.
    /// </summary>
    [RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
    public class TokenInstance : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("토큰 데이터")]
        [SerializeField] private TokenData tokenData;
        public TokenData Data => tokenData;
        public bool IsDragging => isDragging;

        [Header("UI 컴포넌트")]
        [SerializeField] private Image bgImage;
        [SerializeField] private Image iconImage;
        [SerializeField] private Image timerFillImage;
        [SerializeField] private Text timerText;
        [SerializeField] private Text nameText;

        [Header("수명 및 타이머 설정")]
        [SerializeField] private float duration = 5f;
        [SerializeField] private float flickerThreshold = 1.5f; // 만료 1.5초 전부터 깜빡임

        private float remainingTime;
        private bool isExpired = false;
        private bool isDragging = false;
        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private Canvas parentCanvas;
        private Transform originalParent;
        private Vector2 originalAnchoredPosition;
        private TokenSpawner spawner;

        private Canvas ParentCanvas
        {
            get
            {
                if (parentCanvas == null)
                    parentCanvas = GetComponentInParent<Canvas>();
                return parentCanvas;
            }
        }

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();

            if (bgImage == null)
                bgImage = GetComponent<Image>();

            if (bgImage != null)
                bgImage.raycastTarget = true;
        }

        public void SetupComponents(Image bg, Image fill, Text name, Text timer)
        {
            bgImage = bg;
            timerFillImage = fill;
            nameText = name;
            timerText = timer;
        }

        public void Initialize(TokenData data, TokenSpawner tokenSpawner)
        {
            tokenData = data;
            spawner = tokenSpawner;
            duration = data != null ? data.duration : 5f;
            remainingTime = duration;
            isExpired = false;
            isDragging = false;

            UpdateVisuals();
            StartCoroutine(TimerRoutine());
        }

        private void UpdateVisuals()
        {
            if (tokenData == null) return;

            // 타입별 배경 색상
            Color baseColor = GetTokenColor(tokenData.tokenType);
            if (bgImage != null)
                bgImage.color = baseColor;

            if (nameText != null)
                nameText.text = GetTokenTypeName(tokenData.tokenType);

            if (iconImage != null)
            {
                iconImage.sprite = tokenData.icon;
                iconImage.enabled = tokenData.icon != null;
            }
        }

        private Color GetTokenColor(TokenType type)
        {
            return type switch
            {
                TokenType.Attack => new Color(0.85f, 0.25f, 0.25f, 0.95f),   // 공격 (빨강)
                TokenType.Defense => new Color(0.2f, 0.5f, 0.9f, 0.95f),     // 방어 (파랑)
                TokenType.Heal => new Color(0.25f, 0.85f, 0.4f, 0.95f),     // 회복 (초록)
                _ => new Color(0.5f, 0.5f, 0.5f, 0.95f)
            };
        }

        public static string GetTokenTypeName(TokenType type)
        {
            return type switch
            {
                TokenType.Attack => "⚔️ 공격",
                TokenType.Defense => "🛡️ 방어",
                TokenType.Heal => "❤️ 회복",
                _ => "토큰"
            };
        }

        #region 수명 타이머 및 만료 FX
        private IEnumerator TimerRoutine()
        {
            while (remainingTime > 0f)
            {
                // 드래그 중에도 시간은 계속 흐름
                remainingTime -= Time.deltaTime;
                float progress = Mathf.Clamp01(remainingTime / duration);

                if (timerFillImage != null)
                {
                    timerFillImage.fillAmount = progress;
                }

                if (timerText != null)
                {
                    timerText.text = $"{remainingTime:F1}s";
                }

                // 1.5초 이하 깜빡임 연출
                if (remainingTime <= flickerThreshold)
                {
                    float flickerAlpha = Mathf.PingPong(Time.time * 8f, 0.6f) + 0.4f;
                    if (canvasGroup != null)
                        canvasGroup.alpha = flickerAlpha;
                }

                yield return null;
            }

            ExpireToken();
        }

        /// <summary>
        /// 시간 만료로 인한 자동 소멸
        /// </summary>
        public void ExpireToken()
        {
            if (isExpired) return;
            isExpired = true;

            if (isDragging && canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = true;
            }

            Debug.Log($"[TokenInstance] ⌛ 토큰 '{tokenData?.tokenName ?? "Token"}' 시간 만료로 소멸!");
            StartCoroutine(ExpireFadeOutRoutine());
        }

        private IEnumerator ExpireFadeOutRoutine()
        {
            float elapsed = 0f;
            float fadeDuration = 0.2f;
            Vector3 startScale = rectTransform.localScale;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeDuration;
                rectTransform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
                if (canvasGroup != null)
                    canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
                yield return null;
            }

            if (spawner != null)
            {
                spawner.OnTokenDestroyed(this);
            }

            Destroy(gameObject);
        }
        #endregion

        #region 드래그 앤 드롭 구현
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (isExpired) return;

            isDragging = true;
            originalParent = transform.parent;
            originalAnchoredPosition = rectTransform.anchoredPosition;

            Canvas canvas = ParentCanvas;
            if (canvas != null)
            {
                transform.SetParent(canvas.transform, true);
                transform.SetAsLastSibling();
            }

            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = false;
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (isExpired) return;

            Canvas canvas = ParentCanvas;
            float scaleFactor = canvas != null ? canvas.scaleFactor : 1f;
            if (scaleFactor <= 0.001f) scaleFactor = 1f;

            rectTransform.anchoredPosition += eventData.delta / scaleFactor;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (isExpired) return;

            isDragging = false;
            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = true;
            }

            // 마우스 아래에 놓인 대상 레이캐스트 감지
            bool applied = TryApplyTokenAtPointer(eventData);

            if (applied)
            {
                // 성공적으로 주입됨 -> 토큰 소비
                ConsumeToken();
            }
            else
            {
                // 주입 대상이 아니거나 실패 시 원래 슬롯 위치로 복귀
                ReturnToOriginalPosition();
            }
        }

        private bool TryApplyTokenAtPointer(PointerEventData eventData)
        {
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            foreach (var result in results)
            {
                if (result.gameObject == gameObject) continue;

                // 1. ITokenReceiver 인터페이스 구현체 검사
                ITokenReceiver receiver = result.gameObject.GetComponentInParent<ITokenReceiver>();
                if (receiver != null)
                {
                    if (receiver.CanReceiveToken(tokenData))
                    {
                        bool success = receiver.OnReceiveToken(tokenData);
                        if (success)
                        {
                            // 전역 규약 이벤트 발행 (Dev2 -> Dev1)
                            GameEvents.OnTokenApplied?.Invoke(tokenData, receiver);
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private void ConsumeToken()
        {
            isExpired = true;
            if (spawner != null)
            {
                spawner.OnTokenDestroyed(this);
            }
            Destroy(gameObject);
        }

        private void ReturnToOriginalPosition()
        {
            if (originalParent != null)
            {
                transform.SetParent(originalParent, false);
                rectTransform.anchoredPosition = originalAnchoredPosition;
            }
        }
        #endregion
    }
}
