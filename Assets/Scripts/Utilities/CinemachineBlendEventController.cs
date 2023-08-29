using Cinemachine;
using System.Collections;
using UnityEngine;
using System;

[RequireComponent(typeof(CinemachineBrain))]
public class CinemachineBlendEventController : WithLogger<CinemachineBlendEventController>
{
    public static event Action<ICinemachineCamera> OnBlendStarted;
    public static event Action<ICinemachineCamera> OnBlendFinished;

    CinemachineBrain _cmBrain;
    Coroutine _trackingBlend;

    protected override void Awake()
    {
        base.Awake();
        _cmBrain = GetComponent<CinemachineBrain>();
        _cmBrain.m_CameraActivatedEvent.AddListener(OnCameraActivated);
    }

    /// <summary>
    /// Called by the <see cref="CinemachineBrain"/> when a camera blend is started.
    /// </summary>
    /// <param name="nextCamera">The Cinemachine camera the brain is blending to.</param>
    /// <param name="previousCamera">The Cinemachine camera the brain started blending from.</param>
    void OnCameraActivated(ICinemachineCamera nextCamera, ICinemachineCamera previousCamera)
    {
        if (previousCamera == null || nextCamera == null) { return; }

        this._logger.Log($"Blending from {previousCamera.Name} to {nextCamera.Name}");

        if (_trackingBlend != null)
            StopCoroutine(_trackingBlend);

        OnBlendStarted?.Invoke(previousCamera);
        _trackingBlend = StartCoroutine(WaitForBlendCompletion());

        IEnumerator WaitForBlendCompletion()
        {
            while (_cmBrain.IsBlending)
            {
                yield return null;
            }

            OnBlendFinished?.Invoke(nextCamera);
            _trackingBlend = null;
        }
    }
}
