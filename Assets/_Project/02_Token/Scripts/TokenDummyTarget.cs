using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using InventoryAutoBattle.Core;

namespace InventoryAutoBattle.Token
{
    /// <summary>
    /// [Mock 테스트용] 토큰 드래그 앤 드롭 주입을 검증하기 위한 더미 장비 타깃.
    /// ITokenReceiver를 구현하여 실제 InventoryItem과 동일한 인터페이스 규약으로 동작합니다.
    /// </summary>
    public class TokenDummyTarget : MonoBehaviour, ITokenReceiver
    {
        [Header("타깃 장비 설정")]
        [SerializeField] private string targetName = "초보자의 검";
        [SerializeField] private ItemType itemType = ItemType.Weapon;
        [SerializeField] private TokenType requiredTokenType = TokenType.Attack;
        [SerializeField] private int requiredTokenCount = 2;
        [SerializeField] private float effectValue = 20f;

        [Header("UI 컴포넌트")]
        [SerializeField] private Image bgImage;
        [SerializeField] private Text titleText;
        [SerializeField] private Text progressText;
        [SerializeField] private Image fillBarImage;

        private int currentTokenCount = 0;
        private Color originalBgColor;

        private void Awake()
        {
            if (bgImage == null) bgImage = GetComponent<Image>();
            if (bgImage != null) originalBgColor = bgImage.color;
            UpdateUI();
        }

        public void Initialize(string name, ItemType iType, TokenType tType, int reqCount, float val)
        {
            targetName = name;
            itemType = iType;
            requiredTokenType = tType;
            requiredTokenCount = reqCount;
            effectValue = val;
            currentTokenCount = 0;

            if (bgImage == null) bgImage = GetComponent<Image>();
            if (bgImage != null) originalBgColor = bgImage.color;

            UpdateUI();
        }

        public void SetupComponents(Image bg, Text title, Text progress, Image fillBar)
        {
            bgImage = bg;
            titleText = title;
            progressText = progress;
            fillBarImage = fillBar;
            if (bgImage != null) originalBgColor = bgImage.color;
            UpdateUI();
        }

        public bool CanReceiveToken(TokenData token)
        {
            if (token == null) return false;
            return token.tokenType == requiredTokenType;
        }

        public bool OnReceiveToken(TokenData token)
        {
            if (!CanReceiveToken(token)) return false;

            currentTokenCount++;
            Debug.Log($"<color=cyan>[TokenDummyTarget] '{targetName}'에 {token.tokenType} 토큰 주입 성공! ({currentTokenCount}/{requiredTokenCount})</color>");
            UpdateUI();

            if (currentTokenCount >= requiredTokenCount)
            {
                TriggerEffect();
            }

            return true;
        }

        private void TriggerEffect()
        {
            currentTokenCount = 0;
            UpdateUI();

            Debug.Log($"<color=yellow>[TokenDummyTarget] 💥 '{targetName}' 발동! ({itemType}, 수치: {effectValue})</color>");
            GameEvents.OnItemTriggered?.Invoke(itemType, effectValue);

            StartCoroutine(FlashRoutine());
        }

        private IEnumerator FlashRoutine()
        {
            if (bgImage != null)
                bgImage.color = Color.yellow;

            yield return new WaitForSeconds(0.2f);

            if (bgImage != null)
                bgImage.color = originalBgColor;
        }

        private void UpdateUI()
        {
            if (titleText != null)
            {
                string typeIcon = requiredTokenType switch
                {
                    TokenType.Attack => "⚔️",
                    TokenType.Defense => "🛡️",
                    TokenType.Heal => "❤️",
                    _ => "●"
                };
                titleText.text = $"{typeIcon} {targetName}";
            }

            if (progressText != null)
            {
                progressText.text = $"{currentTokenCount} / {requiredTokenCount}";
            }

            if (fillBarImage != null)
            {
                fillBarImage.fillAmount = (float)currentTokenCount / requiredTokenCount;
            }
        }
    }
}
