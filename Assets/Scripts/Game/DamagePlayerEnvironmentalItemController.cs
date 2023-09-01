using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DamagePlayerEnvironmentalItemController : MonoBehaviour
{
    [SerializeField] private int _damage = 10;

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag != Constants.TagNames.Damagable) { return; }
        other.TryGetComponent<NetworkObject>(out NetworkObject networkObject);
        if (networkObject == null || !networkObject.IsOwner) { return; }
        other.TryGetComponent<IDamageable>(out IDamageable damageable);
        if (damageable == null) { return; }
        damageable.TakeDamageLocal(this._damage);
    }
}
