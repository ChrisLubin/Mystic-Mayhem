using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DamagePlayerItemController : MonoBehaviour
{
    [SerializeField] private int _damage = 10;

    private void OnTriggerEnter(Collider other)
    {
        other.TryGetComponent<PlayerHealthController>(out PlayerHealthController playerHealthController);
        if (playerHealthController == null) { return; }
        playerHealthController.TryGetComponent<NetworkObject>(out NetworkObject networkObject);
        if (networkObject == null || !networkObject.IsOwner) { return; }
        playerHealthController.TakeDamage(_damage);
    }
}
