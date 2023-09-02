using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimationController : NetworkBehaviourWithLogger<PlayerAnimationController>
{
    private Animator _animator;

    private HashSet<int> _validAttackIds = new() { 101, 102, 103, 104 };
    private HashSet<int> _validTakeDamageIds = new() { 1 };

    private const string _CAN_COMBO_PARAMETER = "CanCombo";
    private const string _IS_CAN_COMBO_WINDOW_OVER_PARAMETER = "IsCanComboWindowOver";
    private const string _CAN_DEAL_MELEE_DAMAGE_PARAMETER = "CanDealMeleeDamage";
    private const string _IS_ATTACKING_PARAMETER = "IsAttacking";
    private const string _ATTACK_ID_PARAMETER = "AttackId";
    private const string _IS_TAKING_DAMAGE_ID_PARAMETER = "IsTakingDamage";
    private const string _TAKE_DAMAGE_ID_PARAMETER = "TakeDamageId";
    private int _canComboHash;
    private int _isCanComboWindowOverHash;
    private int _canDealMeleeDamageHash;
    private int _isAttackingHash;
    private int _attackIdHash;
    private int _isTakingDamageHash;
    private int _takeDamageIdHash;

    public bool CanCombo { get => this._animator.GetBool(this._canComboHash); }
    public bool CanDealMeleeDamage { get => this._animator.GetBool(this._canDealMeleeDamageHash); }
    public bool IsAttacking { get => this._animator.GetBool(this._isAttackingHash); }
    public bool IsTakingDamage { get => this._animator.GetBool(this._isTakingDamageHash); }
    public int CurrentAttackId { get => this._animator.GetInteger(this._attackIdHash); }

    protected override void Awake()
    {
        base.Awake();
        this._animator = GetComponent<Animator>();
        this._canComboHash = Animator.StringToHash(_CAN_COMBO_PARAMETER);
        this._isCanComboWindowOverHash = Animator.StringToHash(_IS_CAN_COMBO_WINDOW_OVER_PARAMETER);
        this._canDealMeleeDamageHash = Animator.StringToHash(_CAN_DEAL_MELEE_DAMAGE_PARAMETER);
        this._isAttackingHash = Animator.StringToHash(_IS_ATTACKING_PARAMETER);
        this._attackIdHash = Animator.StringToHash(_ATTACK_ID_PARAMETER);
        this._isTakingDamageHash = Animator.StringToHash(_IS_TAKING_DAMAGE_ID_PARAMETER);
        this._takeDamageIdHash = Animator.StringToHash(_TAKE_DAMAGE_ID_PARAMETER);
    }

    public void PlayAttackAnimation(int attackId, bool enableRootMotion = false)
    {
        if (!this.IsOwner) { return; }
        if (!this._validAttackIds.TryGetValue(attackId, out _))
        {
            this._logger.Log("Attack not defined!", Logger.LogLevel.Warning);
            return;
        }

        this.SetBool(this._isAttackingHash, true);
        this.SetInteger(this._attackIdHash, attackId);
    }

    public void PlayTakeDamageAnimation(int takeDamageId, bool enableRootMotion = false)
    {
        if (!this.IsOwner) { return; }
        if (!this._validTakeDamageIds.TryGetValue(takeDamageId, out _))
        {
            this._logger.Log("Take damage not defined!", Logger.LogLevel.Warning);
            return;
        }

        this.SetBool(this._isTakingDamageHash, true);
        this.SetInteger(this._takeDamageIdHash, takeDamageId);
    }

    private void SetBool(int hash, bool newValue, bool hasToBeOwner = true)
    {
        if (!this.IsOwner && hasToBeOwner) { return; }
        bool currentValue = this._animator.GetBool(hash);
        if (currentValue == newValue) { return; }
        this._animator.SetBool(hash, newValue);
    }

    private void SetInteger(int hash, int newValue, bool hasToBeOwner = true)
    {
        if (!this.IsOwner && hasToBeOwner) { return; }
        int currentValue = this._animator.GetInteger(hash);
        if (currentValue == newValue) { return; }
        this._animator.SetInteger(hash, newValue);
    }

    // Called from animation events
    private void EnableCanDealMeleeDamage() => this.SetBool(this._canDealMeleeDamageHash, true);
    private void DisableCanDealMeleeDamage() => this.SetBool(this._canDealMeleeDamageHash, false);
    private void EnableCanCombo() => this.SetBool(this._canComboHash, true);
    private void DisableCanCombo()
    {
        this.SetBool(this._canComboHash, false);
        this.SetBool(this._isCanComboWindowOverHash, true);
    }
}
