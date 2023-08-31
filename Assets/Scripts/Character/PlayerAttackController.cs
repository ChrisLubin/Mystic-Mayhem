using Unity.Netcode;
using UnityEngine;

public class PlayerAttackController : NetworkBehaviour
{
    private PlayerAnimationController _animationController;
    private PlayerNetworkController _networkController;

    private void Awake()
    {
        this._animationController = GetComponent<PlayerAnimationController>();
        this._networkController = GetComponent<PlayerNetworkController>();
    }

    private void Update()
    {
        if (!this.IsOwner || this._animationController.IsAttacking || this._animationController.IsTakingDamage) { return; }

        if (Input.GetKeyDown(KeyCode.Mouse0))
            this.HandleLightAttack(this._networkController.CurrentWeaponName.Value);
        else if (Input.GetKeyDown(KeyCode.Mouse1))
            this.HandleHeavyAttack(this._networkController.CurrentWeaponName.Value);
    }

    public void HandleLightAttack(WeaponName weaponName)
    {
        WeaponSO weaponSO = ResourceSystem.GetWeapon(weaponName);
        if (weaponSO == null) { return; }

        this._animationController.PlayAttackAnimation(weaponSO.LightAttackOneId);
    }

    public void HandleHeavyAttack(WeaponName weaponName)
    {
        WeaponSO weaponSO = ResourceSystem.GetWeapon(weaponName);
        if (weaponSO == null) { return; }

        this._animationController.PlayAttackAnimation(weaponSO.HeavyAttackOneId);
    }
}
