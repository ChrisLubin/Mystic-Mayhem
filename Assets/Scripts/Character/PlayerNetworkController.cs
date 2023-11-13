using Unity.Netcode;
using UnityEngine;

public class PlayerNetworkController : NetworkBehaviour
{
    [HideInInspector]
    public NetworkVariable<WeaponName> CurrentWeaponName = new(WeaponName.None, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<int> CurrentHealth = new(PlayerHealthController.PLAYER_MAX_HEALTH, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
}
