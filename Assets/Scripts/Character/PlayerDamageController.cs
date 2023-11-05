using System.Collections.Generic;
using System.Linq;
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
        this._meleeDamageColliderController.OnCollideWithPlayer += this.OnWeaponCollideWithPlayer;
    }

    private void OnDestroy()
    {
        this._animationController.OnReachedDamageFrame -= OnReachedDamageFrame;
        this._animationController.OnAnimationInterrupted -= OnAnimationInterrupted;
        this._meleeDamageColliderController.OnCollideWithPlayer -= this.OnWeaponCollideWithPlayer;
    }

    private void OnReachedDamageFrame()
    {
        this._damageQueue = this._damageQueue.Distinct().ToList();
        foreach (QueuedDamage queuedDamage in this._damageQueue)
        {
            if (!PlayerManager.Instance.TryGetPlayer(queuedDamage.PlayerClientId, out PlayerController player)) { continue; }
            player.TakeDamageServer(queuedDamage.Damage);
        }
        this._damageQueue.Clear();
    }

    private void OnWeaponCollideWithPlayer(ulong clientId, int damage) => this._damageQueue.Add(new(clientId, damage));
    private void OnAnimationInterrupted() => this._damageQueue.Clear();

    public struct QueuedDamage
    {
        public ulong PlayerClientId;
        public int Damage;

        public QueuedDamage(ulong playerClientId, int damage)
        {
            this.PlayerClientId = playerClientId;
            this.Damage = damage;
        }
    }
}
