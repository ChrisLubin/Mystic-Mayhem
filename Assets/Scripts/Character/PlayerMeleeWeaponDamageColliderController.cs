using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerMeleeWeaponDamageColliderController : NetworkBehaviour
{
    private PlayerAnimationController _animationController;
    private PlayerNetworkController _networkController;
    private Collider[] _playerColliders;

    [SerializeField] private DamagerColliderController _weapon;

    private void Awake()
    {
        this._animationController = GetComponent<PlayerAnimationController>();
        this._networkController = GetComponent<PlayerNetworkController>();
        this._playerColliders = GetComponents<Collider>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        this._weapon.SetIgnoreColliders(this._playerColliders);
    }

    // Call from PlayerDamageController instead of PlayerPredictionController
    public CollisionEvent[] OnTick()
    {
        this._weapon.SetEnabled(this._animationController.CanDealMeleeDamage);
        if (!this._animationController.CanDealMeleeDamage) { return null; }

        Collider[] colliders = this._weapon.CheckCollisions(LayerMask.GetMask(Constants.LayerNames.Player));
        if (colliders == null || colliders.Length == 0) { return null; }

        List<CollisionEvent> collisionEvents = new();

        foreach (Collider collider in colliders)
        {
            if (collider.tag != Constants.TagNames.Damagable) { continue; }
            // collider.TryGetComponent<IDamageable>(out IDamageable damageable); // Refactor later so we have the ability to hit possible NPCs
            if (!collider.TryGetComponent<PlayerNetworkController>(out PlayerNetworkController networkController)) { continue; }

            WeaponSO weaponSO = ResourceSystem.GetWeapon(this._networkController.CurrentWeaponName.Value);
            int damage = this._animationController.CurrentAttackId == weaponSO.HeavyAttackOneId ? weaponSO.HeavyAttackDamage : weaponSO.LightAttackDamage;
            collisionEvents.Add(new(networkController.OwnerClientId, damage));
        }

        return collisionEvents.ToArray();
    }

    public struct CollisionEvent
    {
        public ulong PlayerClientId;
        public int Damage;

        public CollisionEvent(ulong playerClientId, int damage)
        {
            this.PlayerClientId = playerClientId;
            this.Damage = damage;
        }
    }
}
