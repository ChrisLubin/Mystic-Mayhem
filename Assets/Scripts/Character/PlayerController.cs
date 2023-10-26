using System;

public class PlayerController : NetworkBehaviorAutoDisable<PlayerController>
{
    private PlayerCameraController _cameraController;
    private UnityEngine.InputSystem.PlayerInput _input;

    public static event Action<ulong, PlayerController> OnSpawn;

    private void Awake()
    {
        this._cameraController = GetComponent<PlayerCameraController>();
        this._input = GetComponent<UnityEngine.InputSystem.PlayerInput>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        PlayerController.OnSpawn?.Invoke(this.OwnerClientId, this);
    }

    protected override void OnOwnerNetworkSpawn()
    {
        this._cameraController.OnHoverCameraReached += this.OnHoverCameraReached;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();

        if (!this.IsOwner) { return; }

        this._cameraController.OnHoverCameraReached -= this.OnHoverCameraReached;
    }

    private void OnHoverCameraReached()
    {
        this._input.enabled = true;
    }
}
