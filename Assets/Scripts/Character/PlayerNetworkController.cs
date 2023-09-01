using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class PlayerNetworkController : NetworkBehaviour
{
    private PlayerHealthController _healthController;

    private void Awake()
    {
        this._healthController = GetComponent<PlayerHealthController>();
    }

    [HideInInspector]
    public NetworkVariable<WeaponName> CurrentWeaponName = new(WeaponName.None, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<int> CurrentHealth = new(PlayerHealthController.PLAYER_MAX_HEALTH, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(ulong targetPlayerClientId, int damage)
    {
        ulong[] allClientIds = Helpers.ToArray(NetworkManager.Singleton.ConnectedClientsIds);

        // Send only to player taking damage
        ClientRpcParams rpcParams = new()
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = allClientIds.Where((ulong clientId) => clientId == targetPlayerClientId).ToArray()
            }
        };

        this.TakeDamageClientRpc(damage, rpcParams);
    }

    [ClientRpc]
    public void TakeDamageClientRpc(int damage, ClientRpcParams _) => this._healthController.TakeDamageLocal(damage, true);
}
