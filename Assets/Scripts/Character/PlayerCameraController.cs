using UnityEngine;
using Cinemachine;
using System;

public class PlayerCameraController : NetworkBehaviorAutoDisable<PlayerCameraController>
{
    [SerializeField] private CinemachineVirtualCamera _firstPersonCamera;
    [SerializeField] private CinemachineVirtualCamera _hoverBehindCamera;

    public event Action OnHoverCameraReached;

    protected override void OnOwnerNetworkSpawn()
    {
        CinemachineBlendEventController.OnBlendFinished += this.OnBlendFinished;
        this._hoverBehindCamera.enabled = true;
        Cursor.lockState = CursorLockMode.Locked;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();

        if (!this.IsOwner) { return; }

        CinemachineBlendEventController.OnBlendFinished -= this.OnBlendFinished;
    }

    private void OnBlendFinished(ICinemachineCamera blendedCamera)
    {
        if (blendedCamera.Name != this._hoverBehindCamera.Name) { return; }

        this._firstPersonCamera.enabled = true;
        this.OnHoverCameraReached?.Invoke();
    }
}
