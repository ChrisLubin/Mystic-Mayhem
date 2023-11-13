using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DamagerColliderController : MonoBehaviour
{
    [SerializeField] private float _collisionStartOffset = 0f;
    [SerializeField] private float _distance = 1f;
    [SerializeField] private int _lerpSteps = 5;
    [SerializeField] private float _debugLineDuration = 0.5f;

    private Vector3 _lastPosition;
    private Quaternion _lastRotation;
    private bool _isEnabled = false;
    private Collider[] _ignoreColliders;

    public Collider[] CheckCollisions(int layerMask)
    {
        if (!this._isEnabled || transform.position == this._lastPosition) { return null; }

        if (this._lastPosition == Vector3.zero)
        {
            // First frame of collider being enabled
            this._lastPosition = transform.position;
            this._lastRotation = transform.rotation;
            return this.CheckLineCollision(transform.position, transform.rotation, layerMask);
        }

        float lerpStepInterval = 1 / (float)this._lerpSteps;
        List<Collider> colliders = new();

        for (int i = 1; i <= this._lerpSteps; i++)
        {
            Vector3 lerpedPosition = Vector3.Lerp(this._lastPosition, transform.position, lerpStepInterval * (float)i);
            Quaternion lerpedRotation = Quaternion.Lerp(this._lastRotation, transform.rotation, lerpStepInterval * (float)i);
            colliders.AddRange(this.CheckLineCollision(lerpedPosition, lerpedRotation, layerMask));
        }

        this._lastPosition = transform.position;
        this._lastRotation = transform.rotation;

        return colliders.Distinct().ToArray();
    }

    private Collider[] CheckLineCollision(Vector3 position, Quaternion rotation, int layerMask)
    {
        Vector3 startingPosition = position + rotation * Vector3.down * this._collisionStartOffset;
        Vector3 maxInteractDistancePoint = startingPosition + rotation * Vector3.up * (this._distance + this._collisionStartOffset);
        Debug.DrawLine(startingPosition, maxInteractDistancePoint, Color.black, this._debugLineDuration);
        IEnumerable<Collider> colliders = Physics.RaycastAll(startingPosition, rotation * Vector3.up, this._distance + this._collisionStartOffset, layerMask).Select(hit => hit.collider).Where(collider => collider.tag == Constants.TagNames.Damagable).Except(this._ignoreColliders);
        return colliders.Where(collider => collider.GetType().ToString() != "UnityEngine.CharacterController").ToArray(); // Hacky workaround for avoiding doing damage twice
    }

    public void SetEnabled(bool isEnabled)
    {
        bool isGettingDisabled = this._isEnabled && !isEnabled;
        if (isGettingDisabled)
        {
            this._lastPosition = Vector3.zero;
            this._lastRotation = default(Quaternion);
        }

        this._isEnabled = isEnabled;
    }

    public void SetIgnoreColliders(Collider[] ignoreColliders) => this._ignoreColliders = ignoreColliders;
}
