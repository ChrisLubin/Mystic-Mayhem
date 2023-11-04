using System;

public class PlayerController : NetworkBehaviorAutoDisable<PlayerController>
{
    private PlayerCameraController _cameraController;
    private PlayerHealthController _healthController;
    private UnityEngine.InputSystem.PlayerInput _input;

    public static event Action<ulong, PlayerController> OnSpawn;

    private void Awake()
    {
        this._cameraController = GetComponent<PlayerCameraController>();
        this._healthController = GetComponent<PlayerHealthController>();
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

    public void TakeDamageServer(int damage) => this._healthController.TakeDamageServer(damage);
}
