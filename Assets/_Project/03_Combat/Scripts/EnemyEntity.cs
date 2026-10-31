using UnityEngine;
using InventoryAutoBattle.Core;

namespace InventoryAutoBattle.Combat
{
    /// <summary>
    /// 적(몬스터) 캐릭터 엔티티.
    /// 스테이지 데이터에 따른 스탯 초기화 및 GameEvents.OnEnemyHpChanged 발행을 담당합니다.
    /// </summary>
    public class EnemyEntity : CharacterBase
    {
        protected override void Awake()
        {
            characterName = "Enemy (오크)";
            characterType = CharacterType.Enemy;
            base.Awake();
        }

        private void Start()
        {
            NotifyHpChanged();
        }

        public void ApplyStageData(StageData stage)
        {
            if (stage == null) return;

            characterName = stage.enemyName;
            maxHp = stage.enemyMaxHp;
            currentHp = stage.enemyMaxHp;
            attackPower = stage.enemyAttack;
            defense = stage.enemyDefense;
            attackCooldown = stage.enemyAttackCooldown > 0f ? stage.enemyAttackCooldown : 2.0f;
            currentShield = 0f;

            if (stage.enemySprite != null && spriteRenderer != null)
            {
                spriteRenderer.sprite = stage.enemySprite;
            }

            ResetPosition();
            NotifyHpChanged();
        }

        protected override void NotifyHpChanged()
        {
            GameEvents.OnEnemyHpChanged?.Invoke(currentHp, maxHp);
        }

        protected override void OnDeath()
        {
            Debug.Log("<color=green>[EnemyEntity] 🏆 적이 격치되었습니다!</color>");
            isCombatActive = false;
            if (spriteRenderer != null)
                spriteRenderer.color = new Color(0.3f, 0.3f, 0.3f, 0.6f);
        }
    }
}
