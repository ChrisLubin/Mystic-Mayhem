using System;
using Unity.Netcode;
using UnityEngine;

public class TickSystem : NetworkBehaviour
{
    private static float _timer = 0f;
    private static int _currentTick = 1;
    private const float _SERVER_TICK_RATE = 60f;
    public const float MIN_TIME_BETWEEN_TICKS = 1f / _SERVER_TICK_RATE;
    public static event Action<int> OnTick;

    private void Update()
    {
        if (MultiplayerSystem.State != MultiplayerState.CreatedLobby && MultiplayerSystem.State != MultiplayerState.JoinedLobby) { return; }
        _timer += Time.deltaTime;

        while (_timer >= MIN_TIME_BETWEEN_TICKS)
        {
            _timer -= MIN_TIME_BETWEEN_TICKS;
            OnTick?.Invoke(_currentTick);
            _currentTick++;
        }
    }
}
