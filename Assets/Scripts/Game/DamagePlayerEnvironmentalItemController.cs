using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DamagePlayerEnvironmentalItemController : MonoBehaviour
{
    [SerializeField] private int _damage = 10;

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetType().ToString() == "UnityEngine.CharacterController") { return; } // Hacky workaround for avoiding doing damage twice
        if (other.tag != Constants.TagNames.Damagable) { return; }
        other.TryGetComponent<IDamageable>(out IDamageable damageable);
        if (damageable == null) { return; }

        if (MultiplayerSystem.IsGameHost)
            damageable.TakeDamageServer(this._damage);
        else
            damageable.TakeDamageLocal(this._damage);
    }
}
