using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class PlayerAnimationController : NetworkBehaviourWithLogger<PlayerAnimationController>
{
    private Animator _animator;

    // These IDs cannot overlap
    private HashSet<int> _validAttackIds = new() { 101, 102, 103, 104 };
    private HashSet<int> _validTakeDamageIds = new() { 1 };
    private HashSet<int> _validDoParryIds = new() { 51, 52 };

    private const string _IS_PARRYING_PARAMETER = "IsParrying";
    private const string _CAN_BE_PARRIED_PARAMETER = "CanBeParried";
    private const string _PARRY_ID_PARAMETER = "ParryId";
    private const string _CAN_COMBO_PARAMETER = "CanCombo";
    private const string _IS_CAN_COMBO_WINDOW_OVER_PARAMETER = "IsCanComboWindowOver";
    private const string _CAN_DEAL_MELEE_DAMAGE_PARAMETER = "CanDealMeleeDamage";
    private const string _IS_ATTACKING_PARAMETER = "IsAttacking";
    private const string _ATTACK_ID_PARAMETER = "AttackId";
    private const string _IS_TAKING_DAMAGE_ID_PARAMETER = "IsTakingDamage";
    private const string _TAKE_DAMAGE_ID_PARAMETER = "TakeDamageId";
    private int _isParryingHash;
    private int _canBeParriedHash;
    private int _parryIdHash;
    private int _canComboHash;
    private int _isCanComboWindowOverHash;
    private int _canDealMeleeDamageHash;
    private int _isAttackingHash;
    private int _attackIdHash;
    private int _isTakingDamageHash;
    private int _takeDamageIdHash;

    public bool IsParrying { get => this.GetBool(this._isParryingHash); }
    public bool CanBeParried { get => this.GetBool(this._canBeParriedHash); }
    public bool CanCombo { get => this.GetBool(this._canComboHash); }
    public bool CanDealMeleeDamage { get => this.GetBool(this._canDealMeleeDamageHash); }
    public bool IsAttacking { get => this.GetBool(this._isAttackingHash); }
    public bool IsTakingDamage { get => this.GetBool(this._isTakingDamageHash); }
    public int CurrentAttackId { get => this.GetInteger(this._attackIdHash); }

    protected override void Awake()
    {
        base.Awake();
        this._animator = GetComponent<Animator>();
        this._isParryingHash = Animator.StringToHash(_IS_PARRYING_PARAMETER);
        this._canBeParriedHash = Animator.StringToHash(_CAN_BE_PARRIED_PARAMETER);
        this._parryIdHash = Animator.StringToHash(_PARRY_ID_PARAMETER);
        this._canComboHash = Animator.StringToHash(_CAN_COMBO_PARAMETER);
        this._isCanComboWindowOverHash = Animator.StringToHash(_IS_CAN_COMBO_WINDOW_OVER_PARAMETER);
        this._canDealMeleeDamageHash = Animator.StringToHash(_CAN_DEAL_MELEE_DAMAGE_PARAMETER);
        this._isAttackingHash = Animator.StringToHash(_IS_ATTACKING_PARAMETER);
        this._attackIdHash = Animator.StringToHash(_ATTACK_ID_PARAMETER);
        this._isTakingDamageHash = Animator.StringToHash(_IS_TAKING_DAMAGE_ID_PARAMETER);
        this._takeDamageIdHash = Animator.StringToHash(_TAKE_DAMAGE_ID_PARAMETER);
    }

    public AnimatorState OnTick()
    {
        this._animator.speed = 1f;
        this._animator.Update(TickSystem.MIN_TIME_BETWEEN_TICKS);
        this._animator.speed = 0f;

        int layerIndex = this._animator.GetCurrentAnimatorClipInfo(1).Count() == 0 ? 0 : 1;
        AnimatorStateInfo clipState = this._animator.GetCurrentAnimatorStateInfo(layerIndex);
        return new AnimatorState(clipState.fullPathHash, clipState.normalizedTime, this.GetInteger(this._parryIdHash), this.GetBool(this._isParryingHash), this.GetBool(this._canBeParriedHash), this.GetBool(this._canComboHash), this.GetBool(this._isCanComboWindowOverHash), this.GetBool(this._canDealMeleeDamageHash), this.GetBool(this._isAttackingHash), this.GetInteger(this._attackIdHash), this.GetBool(this._isTakingDamageHash), this.GetInteger(this._takeDamageIdHash));
    }

    public void SetAnimatorState(AnimatorState state)
    {
        this._animator.speed = 1f;
        this._animator.Play(state.AnimationHash, -1, state.AnimationNormalizedTime);
        // this._animator.Update(0f); // Could also work to make more deteministic
        this._animator.Update(0.000000000001f);
        this.SetInteger(this._parryIdHash, state.ParryId);
        this.SetBool(this._isParryingHash, state.IsParrying);
        this.SetBool(this._canBeParriedHash, state.CanBeParried);
        this.SetBool(this._canComboHash, state.CanCombo);
        this.SetBool(this._isCanComboWindowOverHash, state.IsCanComboWindowOver);
        this.SetBool(this._canDealMeleeDamageHash, state.CanDealMeleeDamage);
        this.SetBool(this._isAttackingHash, state.IsAttacking);
        this.SetInteger(this._attackIdHash, state.AttackId);
        this.SetBool(this._isTakingDamageHash, state.IsTakingDamage);
        this.SetInteger(this._takeDamageIdHash, state.TakeDamageId);
        this._animator.speed = 0f;
    }

    public void PlayAttackAnimation(int attackId, bool enableRootMotion = false)
    {
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
        if (!this._validTakeDamageIds.TryGetValue(takeDamageId, out _))
        {
            this._logger.Log("Take damage not defined!", Logger.LogLevel.Warning);
            return;
        }

        this.SetBool(this._isTakingDamageHash, true);
        this.SetInteger(this._takeDamageIdHash, takeDamageId);
    }

    public void PlayParryAnimation(int parryId, bool isBeingParried, bool enableRootMotion = false)
    {
        if (!this._validDoParryIds.TryGetValue(parryId, out _))
        {
            this._logger.Log("Parry not defined!", Logger.LogLevel.Warning);
            return;
        }

        if (!isBeingParried)
            this.SetBool(this._isParryingHash, true);
        this.SetInteger(this._parryIdHash, parryId);
    }

    private bool GetBool(int hash) => this._animator.GetBool(hash);
    private int GetInteger(int hash) => this._animator.GetInteger(hash);

    private void SetBool(int hash, bool newValue)
    {
        bool currentValue = this.GetBool(hash);
        if (currentValue == newValue) { return; }
        this._animator.SetBool(hash, newValue);
    }

    private void SetInteger(int hash, int newValue)
    {
        int currentValue = this.GetInteger(hash);
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
    private void EnableCanBeParried() => this.SetBool(this._canBeParriedHash, true);
    private void DisableCanBeParried() => this.SetBool(this._canBeParriedHash, false);

    [Serializable]
    public struct AnimatorState : INetworkSerializable
    {
        public int AnimationHash;
        public float AnimationNormalizedTime;
        public int ParryId;
        public bool IsParrying;
        public bool CanBeParried;
        public bool CanCombo;
        public bool IsCanComboWindowOver;
        public bool CanDealMeleeDamage;
        public bool IsAttacking;
        public int AttackId;
        public bool IsTakingDamage;
        public int TakeDamageId;

        public AnimatorState(int animationHash, float animationNormalizedTime, int parryId, bool isParrying, bool canBeParried, bool canCombo, bool isCanComboWindowOver, bool CanDealMeleeDamage, bool IsAttacking, int attackId, bool isTakingDamage, int takeDamageId)
        {
            this.AnimationHash = animationHash;
            this.AnimationNormalizedTime = animationNormalizedTime;
            this.ParryId = parryId;
            this.IsParrying = isParrying;
            this.CanBeParried = canBeParried;
            this.CanCombo = canCombo;
            this.IsCanComboWindowOver = isCanComboWindowOver;
            this.CanDealMeleeDamage = CanDealMeleeDamage;
            this.IsAttacking = IsAttacking;
            this.AttackId = attackId;
            this.IsTakingDamage = isTakingDamage;
            this.TakeDamageId = takeDamageId;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            if (serializer.IsReader)
            {
                var reader = serializer.GetFastBufferReader();
                reader.ReadValueSafe(out AnimationHash);
                reader.ReadValueSafe(out AnimationNormalizedTime);
                reader.ReadValueSafe(out ParryId);
                reader.ReadValueSafe(out IsParrying);
                reader.ReadValueSafe(out CanBeParried);
                reader.ReadValueSafe(out CanCombo);
                reader.ReadValueSafe(out IsCanComboWindowOver);
                reader.ReadValueSafe(out CanDealMeleeDamage);
                reader.ReadValueSafe(out IsAttacking);
                reader.ReadValueSafe(out AttackId);
                reader.ReadValueSafe(out IsTakingDamage);
                reader.ReadValueSafe(out TakeDamageId);
            }
            else
            {
                var writer = serializer.GetFastBufferWriter();
                writer.WriteValueSafe(AnimationHash);
                writer.WriteValueSafe(AnimationNormalizedTime);
                writer.WriteValueSafe(ParryId);
                writer.WriteValueSafe(IsParrying);
                writer.WriteValueSafe(CanBeParried);
                writer.WriteValueSafe(CanCombo);
                writer.WriteValueSafe(IsCanComboWindowOver);
                writer.WriteValueSafe(CanDealMeleeDamage);
                writer.WriteValueSafe(IsAttacking);
                writer.WriteValueSafe(AttackId);
                writer.WriteValueSafe(IsTakingDamage);
                writer.WriteValueSafe(TakeDamageId);
            }
        }
    }
}
