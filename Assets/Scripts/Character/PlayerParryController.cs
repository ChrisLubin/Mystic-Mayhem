using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class PlayerParryController : NetworkBehaviour
{
    private PlayerAnimationController _animationController;
    private PlayerNetworkController _networkController;

    [SerializeField] private float _maxParryDistance = 2.25f;

    public bool CanBeParried => this._animationController.CanBeParried;

    void OnDrawGizmosSelected()
    {
        // Draw parry zone
        Gizmos.color = new Color(255, 0, 0);
        Gizmos.DrawWireSphere(transform.position + Vector3.up, this._maxParryDistance);
    }

    private void Awake()
    {
        this._animationController = GetComponent<PlayerAnimationController>();
        this._networkController = GetComponent<PlayerNetworkController>();
    }

    public bool CanParry()
    {
        RaycastHit[] playerObjectsThatAreNear = Physics.SphereCastAll(transform.position + Vector3.up, this._maxParryDistance, Vector3.forward, 0.01f, LayerMask.GetMask(Constants.LayerNames.Player)).Where(p => p.collider.gameObject != gameObject).ToArray();
        if (playerObjectsThatAreNear.Length == 0) { return false; }

        PlayerAnimationController[] playerAnimatorsThatAreNear = playerObjectsThatAreNear.Select(obj => obj.collider.GetComponent<PlayerAnimationController>()).ToArray();
        return playerAnimatorsThatAreNear.Any(animator => animator.CanBeParried);
    }

    public void DoParry()
    {
        RaycastHit[] playerObjectsThatAreNear = Physics.SphereCastAll(transform.position + Vector3.up, this._maxParryDistance, Vector3.forward, 0.01f, LayerMask.GetMask(Constants.LayerNames.Player)).Where(p => p.collider.gameObject != gameObject).ToArray();
        if (playerObjectsThatAreNear.Length == 0) { return; }

        PlayerParryController[] playerControllersThatAreNear = playerObjectsThatAreNear.Select(obj => obj.collider.GetComponent<PlayerParryController>()).ToArray();
        PlayerParryController[] playerControllersThatCanBeParried = playerControllersThatAreNear.Where(p => p.CanBeParried).ToArray();
        if (playerControllersThatCanBeParried.Length == 0) { return; }

        PlayerParryController[] sortedPlayerControllersByDistance = playerControllersThatCanBeParried.OrderBy(player => Vector3.Distance(transform.position, player.transform.position)).ToArray();
        WeaponSO currentWeaponSO = ResourceSystem.GetWeapon(this._networkController.CurrentWeaponName.Value);
        this._animationController.PlayParryAnimation(currentWeaponSO.DoParryId, false);
        sortedPlayerControllersByDistance[0].GetParriedServer();
    }

    public void GetParriedLocal(bool isFromServer = false)
    {
        WeaponSO currentWeaponSO = ResourceSystem.GetWeapon(this._networkController.CurrentWeaponName.Value);
        this._animationController.PlayParryAnimation(currentWeaponSO.GetParriedId, true);
    }

    public void GetParriedServer()
    {
        if (this.IsOwner) { return; }

        this._networkController.GetParriedServerRpc(this.OwnerClientId);
    }
}
