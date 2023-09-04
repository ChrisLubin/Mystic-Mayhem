using UnityEngine;

public class PlayerMeleeWeaponDamageColliderController : NetworkBehaviorAutoDisable<PlayerMeleeWeaponDamageColliderController>
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

    protected override void OnOwnerNetworkSpawn()
    {
        this._weapon.OnCollide += this.OnWeaponTriggerEnter;

        foreach (Collider collider in this._playerColliders)
        {
            Physics.IgnoreCollision(collider, this._weapon.GetCollider());
        }
    }

    public override void OnDestroy()
    {
        base.OnDestroy();

        if (this.IsOwner)
            this._weapon.OnCollide -= this.OnWeaponTriggerEnter;
    }

    private void FixedUpdate()
    {
        if (!this.IsOwner) { return; }

        if (this._animationController.CanDealMeleeDamage == this._weapon.IsEnabled) { return; }

        this._weapon.SetEnabled(this._animationController.CanDealMeleeDamage);
    }

    private void OnWeaponTriggerEnter(Collider other)
    {
        if (other.tag != Constants.TagNames.Damagable) { return; }
        other.TryGetComponent<IDamageable>(out IDamageable damageable);
        if (damageable == null) { return; }

        WeaponSO weaponSO = ResourceSystem.GetWeapon(this._networkController.CurrentWeaponName.Value);
        int damage = this._animationController.CurrentAttackId == weaponSO.HeavyAttackOneId ? weaponSO.HeavyAttackDamage : weaponSO.LightAttackDamage;
        damageable.TakeDamageServer(damage);
    }
}
