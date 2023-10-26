using UnityEngine;

public class PlayerAttackController : NetworkBehaviourWithLogger<PlayerAttackController>
{
    private PlayerAnimationController _animationController;
    private PlayerNetworkController _networkController;
    private PlayerParryController _parryController;

    private int _lastAttackId;

    protected override void Awake()
    {
        base.Awake();
        this._animationController = GetComponent<PlayerAnimationController>();
        this._networkController = GetComponent<PlayerNetworkController>();
        this._parryController = GetComponent<PlayerParryController>();
    }

    public int OnTick(AttackInput input)
    {
        if (this._animationController.IsTakingDamage || (this._animationController.IsAttacking && !this._animationController.CanCombo))
            return this._lastAttackId;
        if (input == AttackInput.None)
            return this._lastAttackId;

        if (input == AttackInput.LeftClick)
            this.HandleLightAttack(this._networkController.CurrentWeaponName.Value);
        else if (input == AttackInput.RightClick)
            this.HandleHeavyAttack(this._networkController.CurrentWeaponName.Value);

        return this._lastAttackId;
    }

    public AttackInput GetAttackInput()
    {
        AttackInput input;
        if (Input.GetKeyDown(KeyCode.Mouse0))
            input = AttackInput.LeftClick;
        else if (Input.GetKeyDown(KeyCode.Mouse1))
            input = AttackInput.RightClick;
        else
            input = AttackInput.None;

        return input;
    }

    public void SetAttackState(int lastAttackState) => this._lastAttackId = lastAttackState;

    public void HandleLightAttack(WeaponName weaponName)
    {
        WeaponSO weaponSO = ResourceSystem.GetWeapon(weaponName);
        if (weaponSO == null) { return; }

        int attackId = 0;

        if (!this._animationController.CanCombo)
            attackId = weaponSO.LightAttackOneId;
        else if (this._lastAttackId == weaponSO.LightAttackOneId)
            attackId = weaponSO.LightAttackTwoId;
        else if (this._lastAttackId == weaponSO.LightAttackTwoId)
            attackId = weaponSO.LightAttackThreeId;
        else
            return;

        this._lastAttackId = attackId;
        this._animationController.PlayAttackAnimation(attackId, true);

        if (!this.IsOwner) { return; }
        if (this._animationController.CanCombo)
            this._logger.Log(attackId + " - Light Combo");
        else
            this._logger.Log(attackId);
    }

    public void HandleHeavyAttack(WeaponName weaponName)
    {
        if (!this._animationController.IsAttacking && !this._animationController.IsParrying && this._parryController.CanParry())
        {
            this._parryController.DoParry();
            return;
        }

        WeaponSO weaponSO = ResourceSystem.GetWeapon(weaponName);
        if (weaponSO == null) { return; }

        int attackId = 0;

        if (!this._animationController.CanCombo)
            attackId = weaponSO.HeavyAttackOneId;
        else if (this._lastAttackId == weaponSO.LightAttackTwoId)
            attackId = weaponSO.HeavyAttackOneId;
        else
            return;

        this._lastAttackId = attackId;
        this._animationController.PlayAttackAnimation(attackId, false);

        if (!this.IsOwner) { return; }
        if (this._animationController.CanCombo)
            this._logger.Log(attackId + " - Heavy Combo");
        else
            this._logger.Log(attackId);
    }

    public enum AttackInput
    {
        None,
        LeftClick,
        RightClick
    }
}
