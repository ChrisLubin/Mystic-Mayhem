using System;

public class PlayerHealthController : NetworkBehaviorAutoDisable<PlayerHealthController>
{
    private PlayerNetworkController _networkController;

    public const int PLAYER_MAX_HEALTH = 100;
    private const int _PLAYER_MIN_HEALTH = 0;

    public static event Action<int> OnLocalPlayerHealthChange;

    private void Awake()
    {
        this._networkController = GetComponent<PlayerNetworkController>();
    }

    protected override void OnOwnerNetworkSpawn()
    {
        OnLocalPlayerHealthChange?.Invoke(this._networkController.CurrentHealth.Value);
    }

    public void TakeDamage(int damage)
    {
        if (!this.IsOwner) { return; }

        this._networkController.CurrentHealth.Value = Math.Clamp(this._networkController.CurrentHealth.Value - damage, _PLAYER_MIN_HEALTH, PLAYER_MAX_HEALTH);
        OnLocalPlayerHealthChange?.Invoke(this._networkController.CurrentHealth.Value);
    }
}
