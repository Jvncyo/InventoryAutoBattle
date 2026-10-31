using UnityEngine;
using InventoryAutoBattle.Core;

namespace InventoryAutoBattle.Combat
{
    /// <summary>
    /// 플레이어 캐릭터 엔티티.
    /// 체력 변화 시 GameEvents.OnPlayerHpChanged 발행 및 인벤토리 아이템 효과 수혜를 받습니다.
    /// </summary>
    public class PlayerEntity : CharacterBase
    {
        protected override void Awake()
        {
            characterName = "Player (용사)";
            characterType = CharacterType.Player;
            base.Awake();
        }

        private void Start()
        {
            NotifyHpChanged();
        }

        protected override void NotifyHpChanged()
        {
            GameEvents.OnPlayerHpChanged?.Invoke(currentHp, maxHp);
        }

        protected override void OnDeath()
        {
            Debug.Log("<color=red>[PlayerEntity] 💀 플레이어가 쓰러졌습니다!</color>");
            isCombatActive = false;
            if (spriteRenderer != null)
                spriteRenderer.color = new Color(0.4f, 0.4f, 0.4f, 0.8f);
        }
    }
}
