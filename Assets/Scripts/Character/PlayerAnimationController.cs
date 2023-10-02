using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerAnimationController : NetworkBehaviourWithLogger<PlayerAnimationController>
{
    private Animator _animator;
    private PlayerAttackController _attackController;

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
        this._attackController = GetComponent<PlayerAttackController>();
        this._animator.speed = 0f;
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

    public struct AnimatorState
    {
        public int AA;
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

        public AnimatorState(int aa, int animationHash, float animationNormalizedTime, int parryId, bool isParrying, bool canBeParried, bool canCombo, bool isCanComboWindowOver, bool CanDealMeleeDamage, bool IsAttacking, int attackId, bool isTakingDamage, int takeDamageId)
        {
            this.AA = aa;
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
    }

    private void SetAnimatorState(AnimatorState state)
    {
        this._animator.speed = 1f;
        this._animator.Play(state.AnimationHash, -1, state.AnimationNormalizedTime);
        // this._animator.Update(0f); // Could also work to make more deteministic
        this._animator.Update(0.000000000001f);
        this.SetInteger(this._parryIdHash, state.ParryId, false);
        this.SetBool(this._isParryingHash, state.IsParrying, false);
        this.SetBool(this._canBeParriedHash, state.CanBeParried, false);
        this.SetBool(this._canComboHash, state.CanCombo, false);
        this.SetBool(this._isCanComboWindowOverHash, state.IsCanComboWindowOver, false);
        this.SetBool(this._canDealMeleeDamageHash, state.CanDealMeleeDamage, false);
        this.SetBool(this._isAttackingHash, state.IsAttacking, false);
        this.SetInteger(this._attackIdHash, state.AttackId, false);
        this.SetBool(this._isTakingDamageHash, state.IsTakingDamage, false);
        this.SetInteger(this._takeDamageIdHash, state.TakeDamageId, false);
        this._animator.speed = 0f;
    }

    private void LogState(int frameCount)
    {
        var layerIndex = this._animator.GetCurrentAnimatorClipInfo(1).Count() == 0 ? 0 : 1;
        var clipState = this._animator.GetCurrentAnimatorStateInfo(layerIndex);
        var ok = new AnimatorState(frameCount, clipState.fullPathHash, clipState.normalizedTime, this.GetInteger(this._parryIdHash), this.GetBool(this._isParryingHash), this.GetBool(this._canBeParriedHash), this.GetBool(this._canComboHash), this.GetBool(this._isCanComboWindowOverHash), this.GetBool(this._canDealMeleeDamageHash), this.GetBool(this._isAttackingHash), this.GetInteger(this._attackIdHash), this.GetBool(this._isTakingDamageHash), this.GetInteger(this._takeDamageIdHash));
        Debug.Log(JsonUtility.ToJson(ok, true));
    }

    private List<AnimatorState> _animatorStates = new();
    private bool _isSimulating = false;
    private bool _isRecording = false;
    private int _currentTick = -1;

    private void Update()
    {
        if (this._isSimulating)
        {
            bool isFirstTickInSimulationWindow = this._currentTick == -1;
            if (isFirstTickInSimulationWindow)
                this._currentTick = 1;

            if (isFirstTickInSimulationWindow)
            {
                this.SetAnimatorState(this._animatorStates[0]);
                this._attackController.SetAttackState();
            }

            this.Run(this._isRecording);

            this._currentTick++;

            if (this._currentTick == this._animatorStates.Count)
            {
                // Stop simulating
                this._currentTick = -1;
                this._isSimulating = false;
                this._isRecording = false;
                this._animatorStates.Clear();
                return;
            }
        }
        else
            this.Run(this._isRecording);

        // this._attackController.OnUpdate(this._isRecording, this._isSimulating, this._isRecording || this._isSimulating, this._isSimulating ? this._animatorStates[this._currentTick - 1].AA : Time.frameCount, this._currentTick);
        this._attackController.OnUpdate(this._isRecording, this._isSimulating, false, this._isSimulating ? this._animatorStates[this._currentTick - 1].AA : Time.frameCount, this._currentTick);
        // if (this._isRecording || this._isSimulating)
        //     this.LogState(this._isSimulating ? this._animatorStates[this._currentTick - 1].AA : Time.frameCount);

        if (Input.GetKeyDown(KeyCode.Q))
            this._isRecording = true;
        if (Input.GetKeyDown(KeyCode.T))
        {
            this._isSimulating = true;
            this._isRecording = false;
        }
    }

    private void Run(bool isRecording)
    {
        this._animator.speed = 1f;
        this._animator.Update(Time.deltaTime);
        this._animator.speed = 0f;

        if (isRecording)
        {
            var layerIndex = this._animator.GetCurrentAnimatorClipInfo(1).Count() == 0 ? 0 : 1;
            var clipState = this._animator.GetCurrentAnimatorStateInfo(layerIndex);
            this._animatorStates.Add(new AnimatorState(Time.frameCount, clipState.fullPathHash, clipState.normalizedTime, this.GetInteger(this._parryIdHash), this.GetBool(this._isParryingHash), this.GetBool(this._canBeParriedHash), this.GetBool(this._canComboHash), this.GetBool(this._isCanComboWindowOverHash), this.GetBool(this._canDealMeleeDamageHash), this.GetBool(this._isAttackingHash), this.GetInteger(this._attackIdHash), this.GetBool(this._isTakingDamageHash), this.GetInteger(this._takeDamageIdHash)));
        }
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

    public void PlayParryAnimation(int parryId, bool isBeingParried, bool enableRootMotion = false)
    {
        if (!this.IsOwner) { return; }
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

    private void SetBool(int hash, bool newValue, bool hasToBeOwner = true)
    {
        if (!this.IsOwner && hasToBeOwner) { return; }
        bool currentValue = this.GetBool(hash);
        if (currentValue == newValue) { return; }
        this._animator.SetBool(hash, newValue);
    }

    private void SetInteger(int hash, int newValue, bool hasToBeOwner = true)
    {
        if (!this.IsOwner && hasToBeOwner) { return; }
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
}
