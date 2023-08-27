using Unity.Netcode;

public class NetworkBehaviorAutoDisable<T> : NetworkBehaviour where T : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!this.IsOwner)
        {
            this.enabled = false;
            return;
        }
        this.OnOwnerNetworkSpawn();
    }

    protected virtual void OnOwnerNetworkSpawn() { }
}