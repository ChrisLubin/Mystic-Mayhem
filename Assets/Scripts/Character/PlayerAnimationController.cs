using UnityEngine;

public class PlayerAnimationController : WithLogger<PlayerAnimationController>
{
    private Animator _animator;

    private const int _ATTACK_ANIMATION_COUNT = 2;
    private const int _TAKE_DAMAGE_ANIMATION_COUNT = 1;
    private const string _IS_ATTACKING_PARAMETER = "IsAttacking";
    private const string _ATTACK_ID_PARAMETER = "AttackId";
    private const string _IS_TAKING_DAMAGE_ID_PARAMETER = "IsTakingDamage";
    private const string _TAKE_DAMAGE_ID_PARAMETER = "TakeDamageId";
    private int _isAttackingHash;
    private int _attackIdHash;
    private int _isTakingDamageHash;
    private int _takeDamageIdHash;

    public bool IsAttacking { get => this._animator.GetBool(this._isAttackingHash); }
    public bool IsTakingDamage { get => this._animator.GetBool(this._isTakingDamageHash); }

    protected override void Awake()
    {
        base.Awake();
        this._animator = GetComponent<Animator>();
        this._isAttackingHash = Animator.StringToHash(_IS_ATTACKING_PARAMETER);
        this._attackIdHash = Animator.StringToHash(_ATTACK_ID_PARAMETER);
        this._isTakingDamageHash = Animator.StringToHash(_IS_TAKING_DAMAGE_ID_PARAMETER);
        this._takeDamageIdHash = Animator.StringToHash(_TAKE_DAMAGE_ID_PARAMETER);
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

    public void PlayTakeDamageAnimation(int takeDamageId, bool enableRootMotion = false)
    {
        if (takeDamageId == 0 || takeDamageId > _TAKE_DAMAGE_ANIMATION_COUNT)
        {
            this._logger.Log("Take damage not defined!", Logger.LogLevel.Warning);
            return;
        }

        this._animator.SetBool(this._isTakingDamageHash, true);
        this._animator.SetInteger(this._takeDamageIdHash, takeDamageId);
    }
}

public enum AttackAnimationType
{
    Light,
    Heavy,
}
