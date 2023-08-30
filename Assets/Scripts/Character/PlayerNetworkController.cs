using Unity.Netcode;
using UnityEngine;

public class PlayerNetworkController : NetworkBehaviour
{
    [HideInInspector]
    public NetworkVariable<WeaponName> CurrentWeaponName = new(WeaponName.None, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
}
