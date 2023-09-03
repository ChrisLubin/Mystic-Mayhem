using System;

public class PlayerHealthController : NetworkBehaviorAutoDisable<PlayerHealthController>, IDamageable
{
    private PlayerNetworkController _networkController;
    private PlayerAnimationController _animationController;

    public const int PLAYER_MAX_HEALTH = 100;
    private const int _PLAYER_MIN_HEALTH = 0;

    public static event Action<int> OnLocalPlayerHealthChange;

    private void Awake()
    {
        this._networkController = GetComponent<PlayerNetworkController>();
        this._animationController = GetComponent<PlayerAnimationController>();
    }

    protected override void OnOwnerNetworkSpawn()
    {
        OnLocalPlayerHealthChange?.Invoke(this._networkController.CurrentHealth.Value);
    }

    public void TakeDamageLocal(int damage, bool isFromServer = false)
    {
        if (!this.IsOwner || this._animationController.IsParrying) { return; }

        this._networkController.CurrentHealth.Value = Math.Clamp(this._networkController.CurrentHealth.Value - damage, _PLAYER_MIN_HEALTH, PLAYER_MAX_HEALTH);
        OnLocalPlayerHealthChange?.Invoke(this._networkController.CurrentHealth.Value);

        WeaponSO weaponSO = ResourceSystem.GetWeapon(this._networkController.CurrentWeaponName.Value);
        if (weaponSO == null) { return; }
        this._animationController.PlayTakeDamageAnimation(weaponSO.TakeDamageFrontId);
    }

    public void TakeDamageServer(int damage)
    {
        if (this.IsOwner) { return; }

        this._networkController.TakeDamageServerRpc(this.OwnerClientId, damage);
    }
}
