using System.Collections.Generic;
using UnityEngine;
using static PlayerMeleeWeaponDamageColliderController;
using DamageState = System.Collections.Generic.IDictionary<ulong, PlayerMeleeWeaponDamageColliderController.CollisionEvent>;

public class PlayerDamageController : MonoBehaviour
{
    private PlayerAnimationController _animationController;
    private PlayerMeleeWeaponDamageColliderController _meleeDamageColliderController;

    private IDictionary<ulong, CollisionEvent> _collisionEventMap = new Dictionary<ulong, CollisionEvent>();

    private void Awake()
    {
        this._animationController = GetComponent<PlayerAnimationController>();
        this._meleeDamageColliderController = GetComponent<PlayerMeleeWeaponDamageColliderController>();
        this._animationController.OnReachedDamageFrame += OnReachedDamageFrame;
        this._animationController.OnAnimationInterrupted += OnAnimationInterrupted;
    }

    private void OnDestroy()
    {
        this._animationController.OnReachedDamageFrame -= OnReachedDamageFrame;
        this._animationController.OnAnimationInterrupted -= OnAnimationInterrupted;
    }

    public DamageState OnTick()
    {
        CollisionEvent[] collisionEvents = this._meleeDamageColliderController.OnTick();
        if (collisionEvents == null) { return this._collisionEventMap; }

        foreach (CollisionEvent collisionEvent in collisionEvents)
        {
            if (this._collisionEventMap.ContainsKey(collisionEvent.PlayerClientId)) { continue; } // Deal damage only once to a player per attack

            this._collisionEventMap.Add(collisionEvent.PlayerClientId, collisionEvent);
        }

        return this._collisionEventMap;
    }

    private void OnReachedDamageFrame()
    {
        foreach (CollisionEvent collisionEvent in this._collisionEventMap.Values)
        {
            if (!PlayerManager.Instance.TryGetPlayer(collisionEvent.PlayerClientId, out PlayerController player)) { continue; }
            player.TakeDamageServer(collisionEvent.Damage);
        }
        this._collisionEventMap.Clear();
    }

    public void SetDamageState(DamageState state) => this._collisionEventMap = state;

    private void OnAnimationInterrupted() => this._collisionEventMap.Clear();
}
