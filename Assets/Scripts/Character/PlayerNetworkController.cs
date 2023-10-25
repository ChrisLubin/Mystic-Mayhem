using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class PlayerNetworkController : NetworkBehaviour
{
    private PlayerHealthController _healthController;
    private PlayerParryController _parryController;

    private void Awake()
    {
        this._healthController = GetComponent<PlayerHealthController>();
        this._parryController = GetComponent<PlayerParryController>();
    }

    [HideInInspector]
    public NetworkVariable<WeaponName> CurrentWeaponName = new(WeaponName.None, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<int> CurrentHealth = new(PlayerHealthController.PLAYER_MAX_HEALTH, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [ServerRpc(RequireOwnership = false)]
    public void GetParriedServerRpc(ulong targetPlayerClientId)
    {
        ulong[] allClientIds = Helpers.ToArray(NetworkManager.Singleton.ConnectedClientsIds);

        // Send only to player that was parried
        ClientRpcParams rpcParams = new()
        {
            Send = new ClientRpcSendParams { TargetClientIds = allClientIds.Where((ulong clientId) => clientId == targetPlayerClientId).ToArray() }
        };

        this.GetParriedClientRpc(rpcParams);
    }

    [ClientRpc]
    public void GetParriedClientRpc(ClientRpcParams _) => this._parryController.GetParriedLocal(true);
}
