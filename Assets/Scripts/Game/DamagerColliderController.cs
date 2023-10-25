using System;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DamagerColliderController : MonoBehaviour
{
    private Collider _collider;

    public event Action<Collider> OnCollide;
    public bool IsEnabled => this._collider.enabled;

    private void Awake()
    {
        this._collider = GetComponent<Collider>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetType().ToString() == "UnityEngine.CharacterController") { return; } // Hacky workaround for avoiding doing damage twice

        this.OnCollide?.Invoke(other);
    }

    public void SetEnabled(bool isEnabled) => this._collider.enabled = isEnabled;
    public Collider GetCollider() => this._collider;
}
