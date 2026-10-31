using System.Collections;
using UnityEngine;
using InventoryAutoBattle.Core;

namespace InventoryAutoBattle.Combat
{
    /// <summary>
    /// 전투 참가 엔티티(플레이어/적)의 공통 기본 클래스.
    /// 체력, 공격력, 방어력, 보호막, 이동 및 기본 공격 쿨타임을 관리합니다.
    /// </summary>
    public abstract class CharacterBase : MonoBehaviour
    {
        [Header("기본 정보")]
        [SerializeField] protected string characterName = "Unit";
        [SerializeField] protected CharacterType characterType;

        [Header("스탯")]
        [SerializeField] protected float maxHp = 100f;
        [SerializeField] protected float currentHp = 100f;
        [SerializeField] protected float attackPower = 10f;
        [SerializeField] protected float defense = 0f;
        [SerializeField] protected float currentShield = 0f;
        [SerializeField] protected float attackCooldown = 1.5f;
        [SerializeField] protected float attackRange = 1.6f;
        [SerializeField] protected float moveSpeed = 2.0f;

        [Header("시각 연출")]
        [SerializeField] protected SpriteRenderer spriteRenderer;

        public string CharacterName => characterName;
        public CharacterType Type => characterType;
        public float MaxHp => maxHp;
        public float CurrentHp => currentHp;
        public float CurrentShield => currentShield;
        public float AttackPower => attackPower;
        public float Defense => defense;
        public bool IsDead => currentHp <= 0f;

        protected CharacterBase target;
        protected float attackTimer = 0f;
        protected bool isCombatActive = false;
        protected Vector3 initialPosition;
        protected Color originalColor = Color.white;

        protected virtual void Awake()
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            if (spriteRenderer != null)
                originalColor = spriteRenderer.color;

            initialPosition = transform.position;
            currentHp = maxHp;
        }

        public virtual void Initialize(float hp, float atk, float def, float atkCooldown)
        {
            maxHp = hp;
            currentHp = hp;
            attackPower = atk;
            defense = def;
            currentShield = 0f;
            attackCooldown = atkCooldown;
            attackTimer = 0.5f; // 첫 타격까지 약간의 딜레이
            transform.position = initialPosition;
            if (spriteRenderer != null)
                spriteRenderer.color = originalColor;
            NotifyHpChanged();
        }

        public virtual void SetTarget(CharacterBase newTarget)
        {
            target = newTarget;
        }

        public virtual void SetCombatActive(bool active)
        {
            isCombatActive = active;
        }

        public virtual void ResetPosition()
        {
            transform.position = initialPosition;
            currentShield = 0f;
            attackTimer = 0f;
            if (spriteRenderer != null)
                spriteRenderer.color = originalColor;
        }

        protected virtual void Update()
        {
            if (!isCombatActive || IsDead || target == null || target.IsDead)
                return;

            float distance = Vector2.Distance(transform.position, target.transform.position);

            if (distance > attackRange)
            {
                // 사거리 밖: 타깃 방향으로 전진
                Vector3 direction = (target.transform.position - transform.position).normalized;
                transform.position += direction * (moveSpeed * Time.deltaTime);
            }
            else
            {
                // 사거리 도달: 공격 쿨다운 계산
                attackTimer += Time.deltaTime;
                if (attackTimer >= attackCooldown)
                {
                    attackTimer = 0f;
                    PerformBasicAttack();
                }
            }
        }

        /// <summary>
        /// 기본 자동 공격 실행
        /// </summary>
        protected virtual void PerformBasicAttack()
        {
            if (target == null || target.IsDead) return;

            Debug.Log($"[{characterName}] 기본 공격! -> {target.CharacterName} ({attackPower} 피해)");
            StartCoroutine(AttackVisualFeedback());
            target.TakeDamage(attackPower);
        }

        /// <summary>
        /// 데미지 계산 및 피격 (방어력 및 쉴드 우선 차감)
        /// </summary>
        public virtual void TakeDamage(float damage)
        {
            if (IsDead) return;

            // 방어력 감소율 적용 (예: defense 10 -> 데미지 10 감소 또는 비율)
            float finalDamage = Mathf.Max(1f, damage - defense);

            // 1. 쉴드가 있다면 쉴드 우선 소모
            if (currentShield > 0f)
            {
                if (currentShield >= finalDamage)
                {
                    currentShield -= finalDamage;
                    finalDamage = 0f;
                }
                else
                {
                    finalDamage -= currentShield;
                    currentShield = 0f;
                }
            }

            // 2. 남은 데미지 체력에 적용
            if (finalDamage > 0f)
            {
                currentHp = Mathf.Max(0f, currentHp - finalDamage);
            }

            NotifyHpChanged();
            StartCoroutine(DamageVisualFeedback());

            if (currentHp <= 0f)
            {
                OnDeath();
            }
        }

        /// <summary>
        /// 체력 회복
        /// </summary>
        public virtual void Heal(float amount)
        {
            if (IsDead) return;

            currentHp = Mathf.Min(maxHp, currentHp + amount);
            NotifyHpChanged();
            StartCoroutine(HealVisualFeedback());
        }

        /// <summary>
        /// 보호막 획득
        /// </summary>
        public virtual void AddShield(float amount)
        {
            if (IsDead) return;

            currentShield += amount;
            NotifyHpChanged();
            StartCoroutine(ShieldVisualFeedback());
        }

        protected abstract void NotifyHpChanged();
        protected abstract void OnDeath();

        #region 피드백 시각 연출
        protected virtual IEnumerator DamageVisualFeedback()
        {
            if (spriteRenderer == null) yield break;

            spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(0.12f);
            spriteRenderer.color = originalColor;
        }

        protected virtual IEnumerator AttackVisualFeedback()
        {
            // 전방으로 살짝 찔렀다가 복귀하는 연출
            Vector3 startPos = transform.position;
            Vector3 lungeDir = target != null ? (target.transform.position - transform.position).normalized * 0.25f : Vector3.zero;

            transform.position = startPos + lungeDir;
            yield return new WaitForSeconds(0.1f);
            transform.position = startPos;
        }

        protected virtual IEnumerator HealVisualFeedback()
        {
            if (spriteRenderer == null) yield break;

            spriteRenderer.color = Color.green;
            yield return new WaitForSeconds(0.2f);
            spriteRenderer.color = originalColor;
        }

        protected virtual IEnumerator ShieldVisualFeedback()
        {
            if (spriteRenderer == null) yield break;

            spriteRenderer.color = new Color(0.4f, 0.7f, 1f);
            yield return new WaitForSeconds(0.2f);
            spriteRenderer.color = originalColor;
        }
        #endregion
    }
}
