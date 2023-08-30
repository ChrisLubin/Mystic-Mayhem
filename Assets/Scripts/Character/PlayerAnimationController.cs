using UnityEngine;

public class PlayerAnimationController : WithLogger<PlayerAnimationController>
{
    private Animator _animator;

    private const int _ATTACK_ANIMATION_COUNT = 2;
    private const string _IS_ATTACKING_PARAMETER = "IsAttacking";
    private const string _ATTACK_ID_PARAMETER = "AttackId";
    private int _isAttackingHash;
    private int _attackIdHash;

    public bool IsAttacking { get => this._animator.GetBool(this._isAttackingHash); }

    protected override void Awake()
    {
        base.Awake();
        this._animator = GetComponent<Animator>();
        this._isAttackingHash = Animator.StringToHash(_IS_ATTACKING_PARAMETER);
        this._attackIdHash = Animator.StringToHash(_ATTACK_ID_PARAMETER);
    }

    public void PlayAttackAnimation(int attackId, bool enableRootMotion = false)
    {
        if (attackId == 0 || attackId > _ATTACK_ANIMATION_COUNT)
        {
            this._logger.Log("Attack not defined!", Logger.LogLevel.Warning);
            return;
        }

        this._animator.SetBool(this._isAttackingHash, true);
        this._animator.SetInteger(this._attackIdHash, attackId);
    }
}

public enum AttackAnimationType
{
    Light,
    Heavy,
}
