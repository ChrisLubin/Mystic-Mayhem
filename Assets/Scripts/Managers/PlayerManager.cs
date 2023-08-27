using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerManager : NetworkedStaticInstanceWithLogger<PlayerManager>
{
    [SerializeField] Transform _playerPrefab;
    [SerializeField] Transform _playerSpawnArea;
    [SerializeField] Transform _playerContainer;
    [SerializeField] float _playerSpawnMaxDistance = 9f;

    public static PlayerController LocalPlayer { get; private set; }

    private IDictionary<ulong, PlayerController> _alivePlayersMap = new Dictionary<ulong, PlayerController>();

    public static event Action OnLocalPlayerSpawn;

    protected override void Awake()
    {
        base.Awake();
        GameManager.OnStateChange += this.OnGameStateChange;
        PlayerController.OnSpawn += this.OnSpawn;
        MultiplayerSystem.Instance.PlayerData.OnListChanged += this.OnPlayerDataChanged;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        GameManager.OnStateChange -= this.OnGameStateChange;
        PlayerController.OnSpawn -= this.OnSpawn;
        if (MultiplayerSystem.Instance != null)
        {
            MultiplayerSystem.Instance.PlayerData.OnListChanged -= this.OnPlayerDataChanged;
        }
    }

    private void OnGameStateChange(GameState state)
    {
        switch (state)
        {
            case GameState.GameStarted:
                const int hostId = 0;
                this.SpawnPlayer(hostId);
                break;
            default:
                break;
        }
    }

    private void OnSpawn(ulong clientId, PlayerController player)
    {
        this._alivePlayersMap[clientId] = player;
        if (clientId == MultiplayerSystem.LocalClientId)
        {
            PlayerManager.LocalPlayer = player;
            PlayerManager.OnLocalPlayerSpawn?.Invoke();
        }
    }

    private void OnPlayerDataChanged(NetworkListEvent<PlayerData> _)
    {
        if (GameManager.State != GameState.GameStarted) { return; }
        if (!this.IsHost)
        {
            this._logger.Log($"Player Data list changed. Total players: {MultiplayerSystem.Instance.PlayerData.Count}");
            return;
        }

        foreach (PlayerData playerData in MultiplayerSystem.Instance.PlayerData)
        {
            if (playerData.ClientId == PlayerData.UNREGISTERED_CLIENT_ID || this._alivePlayersMap.ContainsKey(playerData.ClientId)) { continue; }

            this.SpawnPlayer(playerData.ClientId);
        }
    }

    private void SpawnPlayer(ulong clientId)
    {
        if (!this.IsHost) { return; }
        if (this._alivePlayersMap.ContainsKey(clientId))
        {
            this._logger.Log($"This player is still alive. Cannot spawn them again.", Logger.LogLevel.Error);
            return;
        }

        Vector3 randomSpawnPoint = new(UnityEngine.Random.Range(this._playerSpawnArea.position.x - this._playerSpawnMaxDistance, this._playerSpawnArea.position.x + this._playerSpawnMaxDistance), 0, UnityEngine.Random.Range(this._playerSpawnArea.position.z - this._playerSpawnMaxDistance, this._playerSpawnArea.position.z + this._playerSpawnMaxDistance));
        Transform playerTransform = Instantiate(this._playerPrefab, randomSpawnPoint, this._playerSpawnArea.rotation);
        NetworkObject playerNetworkObject = playerTransform.GetComponent<NetworkObject>();
        playerNetworkObject.SpawnWithOwnership(clientId);
        playerNetworkObject.TrySetParent(this._playerContainer);
        this._logger.Log($"Spawned player for {MultiplayerSystem.Instance.GetPlayerUsername(clientId)}");
    }

    public bool TryGetPlayer(ulong clientId, out PlayerController player) => this._alivePlayersMap.TryGetValue(clientId, out player);
}
