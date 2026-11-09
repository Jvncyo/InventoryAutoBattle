using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerCombatActor : CombatActor
{
    [SerializeField] private float startingMaxHP = 100f;
    [SerializeField] private ItemContainer playerInventory;

    private void Awake()
    {
        Initialize(startingMaxHP, startingMaxHP);
    }

    public void Initialize(float maxHp, float currentHp)
    {
        MaxHP = maxHp;
        CurrentHP = currentHp;
        Shield = 0f;
    }

    /// <summary>
    /// Restore full HP for a new wave. Called when transitioning to Preparation.
    /// </summary>
    public void ResetForNewWave()
    {
        if (MaxHP <= 0f) MaxHP = 100f;
        CurrentHP = MaxHP;
        Shield = 0f;
        
        // 전투 비주얼 리셋 (애니메이션 상태 초기화)
        ResetCombatVisuals();
    }

    /// <summary>
    /// 플레이어 데미지 처리 - 방어구(BaseDamageReduction) 적용
    /// </summary>
    public override void TakeDamage(float amount)
    {
        // 1. 방어구 데미지 감소율 계산 (인벤토리의 모든 장비 합산)
        float totalDamageReduction = 0f;
        if (playerInventory != null)
        {
            var equipments = playerInventory.Items
                .Where(i => i.Data.ItemType == ItemType.Equipment && i.Data.BaseDamageReduction > 0f);
            
            foreach (var eq in equipments)
            {
                totalDamageReduction += eq.Data.BaseDamageReduction;
            }
        }
        
        // 데미지 감소율 캡 (최대 90%)
        totalDamageReduction = Mathf.Clamp01(totalDamageReduction);
        
        // 2. 실데미지 계산 (감소율 적용)
        float reducedDamage = amount * (1f - totalDamageReduction);
        
        // 3. 쉴드 우선 소모
        if (Shield > 0)
        {
            float shieldDmg = Mathf.Min(Shield, reducedDamage);
            Shield -= shieldDmg;
            reducedDamage -= shieldDmg;
        }

        CurrentHP = Mathf.Max(0, CurrentHP - reducedDamage);
        
        // 4. OnDamaged 트리거 발동 (장비의 OnDamaged 트리거 처리)
        if (playerInventory != null && reducedDamage > 0f)
        {
            var equipments = playerInventory.Items
                .Where(i => i.Data.ItemType == ItemType.Equipment);
            
            foreach (var eq in equipments)
            {
                var onDamagedTriggers = eq.Data.Triggers
                    .Where(t => t.Type == TriggerType.OnDamaged);
                
                foreach (var trigger in onDamagedTriggers)
                {
                    // 트리거 쿨다운 체크는 ItemInstance에서 관리
                    if (eq.CooldownTimer <= 0f)
                    {
                        // 트리거 효과 발동 로직은 CombatManager에서 처리
                        // 여기서는 이벤트만 발행
                        EventBus.Publish(new EquipmentDamagedTriggerEvent(eq, trigger.Value));
                    }
                }
            }
        }
        
        // 피격 애니메이션 트리거
        EventBus.Publish(new CombatActionEvent(gameObject, CombatActionType.Hit));
    }
}