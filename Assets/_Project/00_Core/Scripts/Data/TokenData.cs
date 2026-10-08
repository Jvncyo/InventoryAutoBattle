using UnityEngine;

namespace InventoryAutoBattle.Core
{
    [CreateAssetMenu(fileName = "NewTokenData", menuName = "InventoryAutoBattle/Token Data")]
    public class TokenData : ScriptableObject
    {
        [Header("토큰 정보")]
        public string tokenId;
        public string tokenName;
        public TokenType tokenType;
        public Sprite icon;

        [Header("수명 (기본 5초)")]
        [Tooltip("토큰이 소멸하기 전까지의 유효 시간 (초)")]
        public float duration = 5f;
    }
}
