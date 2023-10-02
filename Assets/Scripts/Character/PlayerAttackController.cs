using UnityEngine;

public class PlayerAttackController : NetworkBehaviourWithLogger<PlayerAttackController>
{
    private PlayerAnimationController _animationController;
    private PlayerNetworkController _networkController;
    private PlayerParryController _parryController;

    private int _lastAttackId;
    private int _lastAttackInputFrame; // Used so only 1 attack can be sent to animator per Update frame
    // WILL HAVE TO REFACTOR LAST ATTACK INPUT FRAME WHEN IMPLEMENTING TICKETS BETWEEN FRAMES

    protected override void Awake()
    {
        base.Awake();
        this._animationController = GetComponent<PlayerAnimationController>();
        this._networkController = GetComponent<PlayerNetworkController>();
        this._parryController = GetComponent<PlayerParryController>();
    }

    public void SetAttackState(int lastAttackState) => this._lastAttackId = lastAttackState;

    public struct AttackInput
    {
        public bool Sprint;
        public Vector2 Move;

        public AttackInput(bool sprint, Vector2 move)
        {
            this.Sprint = sprint;
            this.Move = move;
        }
    }

    public enum MouseClick
    {
        None,
        Left,
        Right
    }

    public MouseClick GetAttackInput()
    {
        MouseClick mouseClick;
        if (Input.GetKeyDown(KeyCode.Mouse0))
            mouseClick = MouseClick.Left;
        else if (Input.GetKeyDown(KeyCode.Mouse1))
            mouseClick = MouseClick.Right;
        else
            mouseClick = MouseClick.None;

        return mouseClick;
    }

    public int OnTick(MouseClick mouseClick)
    {
        if (!this.IsOwner)
        {
            Debug.Log("WAS NOT THE OWNER!!!!!!!");
            return 0;
        }

        if (this._animationController.IsTakingDamage || this._lastAttackInputFrame == Time.frameCount || (this._animationController.IsAttacking && !this._animationController.CanCombo))
            return this._lastAttackId;
        if (mouseClick == MouseClick.None)
            return this._lastAttackId;
        this._lastAttackInputFrame = Time.frameCount;

        if (mouseClick == MouseClick.Left)
            this.HandleLightAttack(this._networkController.CurrentWeaponName.Value);
        else if (mouseClick == MouseClick.Right)
            this.HandleHeavyAttack(this._networkController.CurrentWeaponName.Value);

        return this._lastAttackId;
    }

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

        if (this._animationController.CanCombo)
            this._logger.Log(attackId + " - Heavy Combo");
        else
            this._logger.Log(attackId);
    }
}
