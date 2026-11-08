using UnityEngine;

/// <summary>
/// 보스 전용 전투 액터. HP 50% 이하 시 2페이즈(격노)로 진입하며 공격 주기가 빨라집니다.
/// </summary>
public class BossCombatActor : EnemyCombatActor
{
    private bool _isEnraged = false;

    protected override void Update()
    {
        base.Update();

        if (!_isEnraged && CurrentHP <= MaxHP * 0.5f)
        {
            _isEnraged = true;
            float enragedCooldown = Mathf.Max(0.5f, _baseCooldown * 0.7f);
            SetCooldownOverride(enragedCooldown);
            
            // 격노 진입 시 공격 애니메이션 트리거 (연출용)
            EventBus.Publish(new CombatActionEvent(gameObject, CombatActionType.Attack));
        }
    }
}