using System.Collections.Generic;
using UnityEngine;

public class PlayerDamageController : MonoBehaviour
{
    private PlayerAnimationController _animationController;
    private PlayerMeleeWeaponDamageColliderController _meleeDamageColliderController;

    private List<QueuedDamage> _damageQueue = new();

    private void Awake()
    {
        this._animationController = GetComponent<PlayerAnimationController>();
        this._meleeDamageColliderController = GetComponent<PlayerMeleeWeaponDamageColliderController>();
        this._animationController.OnReachedDamageFrame += OnReachedDamageFrame;
        this._animationController.OnAnimationInterrupted += OnAnimationInterrupted;
        this._meleeDamageColliderController.OnCollideWithDamageable += this.OnWeaponCollideWithDamageable;
    }

    private void OnDestroy()
    {
        this._animationController.OnReachedDamageFrame -= OnReachedDamageFrame;
        this._animationController.OnAnimationInterrupted -= OnAnimationInterrupted;
        this._meleeDamageColliderController.OnCollideWithDamageable -= this.OnWeaponCollideWithDamageable;
    }

    private void OnReachedDamageFrame()
    {
        foreach (QueuedDamage queuedDamage in this._damageQueue)
        {
            queuedDamage.Damageable.TakeDamageServer(queuedDamage.Damage);
        }
        this._damageQueue.Clear();
    }

    private void OnWeaponCollideWithDamageable(IDamageable damageable, int damage) => this._damageQueue.Add(new(damageable, damage));
    private void OnAnimationInterrupted() => this._damageQueue.Clear();

    public struct QueuedDamage
    {
        public IDamageable Damageable;
        public int Damage;

        public QueuedDamage(IDamageable damageable, int damage)
        {
            this.Damageable = damageable;
            this.Damage = damage;
        }
    }
}
