using System;

public class PlayerController : NetworkBehaviorAutoDisable<PlayerController>
{
    public static event Action<ulong, PlayerController> OnSpawn;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        PlayerController.OnSpawn?.Invoke(this.OwnerClientId, this);
    }
}
