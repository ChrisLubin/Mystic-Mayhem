using System;
using Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : NetworkBehaviorAutoDisable<PlayerController>
{
    private PlayerInput _input;

    [SerializeField] private Camera _mainCamera;
    [SerializeField] private CinemachineVirtualCamera _followCamera;
    [SerializeField] private AudioListener _audioListener;

    public static event Action<ulong, PlayerController> OnSpawn;

    private void Awake()
    {
        this._input = GetComponent<PlayerInput>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        PlayerController.OnSpawn?.Invoke(this.OwnerClientId, this);
    }

    protected override void OnOwnerNetworkSpawn()
    {
        this._input.enabled = true;
        this._mainCamera.enabled = true;
        this._followCamera.enabled = true;
        this._audioListener.enabled = true;
    }
}
