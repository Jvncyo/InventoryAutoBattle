using UnityEngine;

[RequireComponent(typeof(Animator))]
public class CombatAnimatorController : MonoBehaviour
{
    private Animator _animator;

    private static readonly int TriggerAttack = Animator.StringToHash("Attack");
    private static readonly int TriggerHit = Animator.StringToHash("Hit");
    private static readonly int TriggerDie = Animator.StringToHash("Die");

    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    public void PlayAttack() => _animator.SetTrigger(TriggerAttack);
    public void PlayHit() => _animator.SetTrigger(TriggerHit);
    public void PlayDie() => _animator.SetTrigger(TriggerDie);

    public void ResetToIdle()
    {
        if (_animator == null) return;
        
        // 모든 트리거 초기화
        _animator.ResetTrigger(TriggerAttack);
        _animator.ResetTrigger(TriggerHit);
        _animator.ResetTrigger(TriggerDie);
        
        // Idle 상태로 강제 전환 (레이어 0, 정규화 시간 0)
        _animator.Play("Idle", 0, 0f);
    }
}
