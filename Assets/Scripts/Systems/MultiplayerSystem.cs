using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using static Unity.Services.Lobbies.Models.DataObject;

public class MultiplayerSystem : NetworkedStaticInstanceWithLogger<MultiplayerSystem>
{
    public static event Action<MultiplayerState> OnStateChange;
    public static event Action<PlayerData> OnPlayerDisconnect;
    public static event Action<LobbyExceptionReason> OnLobbyError;
    public static event Action<RelayExceptionReason> OnRelayError;
    public static event Action OnHostDisconnect;
    public static event Action OnError;
    public static bool IsMultiplayer { get; private set; } = false;
    public static MultiplayerState State { get; private set; }
    private const int _MAX_PLAYER_COUNT = 7;
    public static string LocalPlayerName { get; private set; }
    public static ulong LocalClientId { get => NetworkManager.Singleton.LocalClientId; }

    private const string _LOBBY_RELAY_CODE_KEY = "RELAY_CODE";
    private const string _LOBBY_PLAYER_NAME_KEY = "PLAYER_NAME";
    private LobbyEventCallbacks _lobbyEventCallbacks;
    private Lobby _lobby;

    private string _hostUnityId;
    public NetworkList<PlayerData> PlayerData { get; private set; }
    private const float _LOBBY_HEARTBEAT_INTERVAL = 15f;
    private float _timeSinceLastLobbyHeartbeat = 0f;

    protected override void Awake()
    {
        base.Awake();
        this.PlayerData = new();
        MultiplayerSystem.LocalPlayerName = $"Player-{UnityEngine.Random.Range(1, 10)}{UnityEngine.Random.Range(1, 10)}{UnityEngine.Random.Range(1, 10)}";
        RpcSystem.OnClientConnectRpc += this.OnClientConnectRpc;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (this.IsHost) { return; }

        RpcSystem.Instance.ClientConnectServerRpc(AuthenticationService.Instance.PlayerId, MultiplayerSystem.LocalPlayerName);
    }

    public async override void OnDestroy()
    {
        base.OnDestroy();
        RpcSystem.OnClientConnectRpc -= this.OnClientConnectRpc;
        if (NetworkManager.Singleton)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= this.OnClientRelayDisconnect;
            NetworkManager.Singleton.Shutdown();
        }
        await this.DisposeLobby();
    }

    private async Task DisposeLobby()
    {
        if (this._lobbyEventCallbacks != null)
        {
            this._lobbyEventCallbacks.PlayerJoined -= this.OnPlayerJoinedLobby;
            this._lobbyEventCallbacks.PlayerLeft -= this.OnPlayerLeftLobby;
        }
        this._lobbyEventCallbacks = null;

        if (this._lobby != null)
        {
            try
            {
                await LobbyService.Instance.RemovePlayerAsync(this._lobby.Id, AuthenticationService.Instance.PlayerId);
                this._logger.Log("Successfully left the lobby");
            }
            catch (Exception) { this._logger.Log("Something went wrong when attempting to leave the lobby", Logger.LogLevel.Error); }
        }
        this._lobby = null;
        this._timeSinceLastLobbyHeartbeat = 0f;
    }

    private async void Update()
    {
        if (!MultiplayerSystem.IsMultiplayer || !this.IsHost || this._lobby == null || MultiplayerSystem.State != MultiplayerState.CreatedLobby) { return; }
        this._timeSinceLastLobbyHeartbeat += Time.deltaTime;

        if (this._timeSinceLastLobbyHeartbeat < _LOBBY_HEARTBEAT_INTERVAL) { return; }
        this._timeSinceLastLobbyHeartbeat = 0f;

        try { await LobbyService.Instance.SendHeartbeatPingAsync(this._lobby.Id); }
        catch (LobbyServiceException e) { this._logger.Log(e.Message, Logger.LogLevel.Error); }
    }

    private void Start() => this.ChangeState(MultiplayerState.NotConnected);
    private void OnRelaySignIn() => this._logger.Log($"Signed in as {AuthenticationService.Instance.PlayerId}");

    public static void QuitMultiplayer()
    {
        NetworkManager.Singleton.Shutdown();
        MultiplayerSystem.Instance.ChangeState(MultiplayerState.Connected);
    }

    public async void ChangeState(MultiplayerState newState)
    {
        if (MultiplayerSystem.State == newState) { return; }
        this._logger.Log($"New state: {newState}");
        MultiplayerSystem.State = newState;
        MultiplayerSystem.OnStateChange?.Invoke(newState);

        RelayServerData relayServerData;

        switch (newState)
        {
            case MultiplayerState.NotConnected:
                NetworkManager.Singleton.OnClientDisconnectCallback += this.OnClientRelayDisconnect;
                string profileName = $"Player-{UnityEngine.Random.Range(1, 9999999)}";
                InitializationOptions initOptions = new();
                initOptions.SetProfile(profileName);
                this._logger.Log($"Set Unity Services profile name to {profileName}");

                await UnityServices.InitializeAsync(initOptions);
                AuthenticationService.Instance.SignedIn += this.OnRelaySignIn;
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                this.ChangeState(MultiplayerState.Connected);
                break;
            case MultiplayerState.Connected:
                AuthenticationService.Instance.SignedIn -= this.OnRelaySignIn;
                await this.DisposeLobby();
                break;
            case MultiplayerState.CreatingLobby:
                MultiplayerSystem.IsMultiplayer = true;
                string lobbyName = $"{UnityEngine.Random.Range(1, 9999999)}";

                try
                {
                    // Create allocation
                    Allocation createAllocation = await RelayService.Instance.CreateAllocationAsync(_MAX_PLAYER_COUNT, Debug.isDebugBuild ? "us-east4" : null);
                    string relayCode = await RelayService.Instance.GetJoinCodeAsync(createAllocation.AllocationId);
                    relayServerData = new(createAllocation, "dtls");
                    NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);
                    this._logger.Log($"Lobby Relay code: {relayCode}");

                    this.StartHost();

                    CreateLobbyOptions createLobbyOptions = new()
                    {
                        Data = new() { { _LOBBY_RELAY_CODE_KEY, new DataObject(VisibilityOptions.Member, relayCode) } },
                        Player = new(null, null, null, createAllocation.AllocationId.ToString())
                        {
                            Data = new() { { _LOBBY_PLAYER_NAME_KEY, new(PlayerDataObject.VisibilityOptions.Member, MultiplayerSystem.LocalPlayerName) } },
                        },
                    };
                    this._lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, _MAX_PLAYER_COUNT, createLobbyOptions);
                    this._hostUnityId = this._lobby.HostId;
                    this._logger.Log($"Created public lobby {lobbyName} as {MultiplayerSystem.LocalPlayerName}");

                    this._lobbyEventCallbacks = new LobbyEventCallbacks();
                    this._lobbyEventCallbacks.PlayerJoined += this.OnPlayerJoinedLobby;
                    this._lobbyEventCallbacks.PlayerLeft += this.OnPlayerLeftLobby;
                    await LobbyService.Instance.SubscribeToLobbyEventsAsync(this._lobby.Id, this._lobbyEventCallbacks);
                    this._logger.Log($"Subscribed to lobby events");
                    this.ChangeState(MultiplayerState.CreatedLobby);
                }
                catch (LobbyServiceException e)
                {
                    MultiplayerSystem.IsMultiplayer = false;
                    this._logger.Log(e.Message, Logger.LogLevel.Error);
                    MultiplayerSystem.OnLobbyError?.Invoke(e.Reason);
                    this.ChangeState(MultiplayerState.Connected);
                }
                catch (RelayServiceException e)
                {
                    MultiplayerSystem.IsMultiplayer = false;
                    this._logger.Log(e.Message, Logger.LogLevel.Error);
                    MultiplayerSystem.OnRelayError?.Invoke(e.Reason);
                    this.ChangeState(MultiplayerState.Connected);
                }
                catch (Exception e)
                {
                    MultiplayerSystem.IsMultiplayer = false;
                    this._logger.Log(e.Message, Logger.LogLevel.Error);
                    MultiplayerSystem.OnError?.Invoke();
                    this.ChangeState(MultiplayerState.Connected);
                }
                break;
            case MultiplayerState.JoiningLobby:
                MultiplayerSystem.IsMultiplayer = true;

                try
                {
                    QuickJoinLobbyOptions joinLobbyOptions = new()
                    {
                        Player = new() { Data = new() { { _LOBBY_PLAYER_NAME_KEY, new(PlayerDataObject.VisibilityOptions.Member, MultiplayerSystem.LocalPlayerName) } } },
                    };
                    this._lobby = await LobbyService.Instance.QuickJoinLobbyAsync(joinLobbyOptions);
                    this._hostUnityId = this._lobby.HostId;
                    this._logger.Log($"Joined lobby {this._lobby.Name} as {MultiplayerSystem.LocalPlayerName}");

                    if (!this._lobby.Data.TryGetValue(_LOBBY_RELAY_CODE_KEY, out DataObject joinedLobbyRelayCodeData))
                    {
                        this._logger.Log("Unable to get Relay code from lobby", Logger.LogLevel.Error);
                        return;
                    }

                    this._logger.Log($"Lobby Relay code: {joinedLobbyRelayCodeData.Value}");
                    JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinedLobbyRelayCodeData.Value);
                    relayServerData = new(joinAllocation, "dtls");
                    NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

                    NetworkManager.Singleton.StartClient();
                    this._logger.Log("Started client");

                    UpdatePlayerOptions updatePlayerOptions = new() { AllocationId = joinAllocation.AllocationId.ToString() };
                    await LobbyService.Instance.UpdatePlayerAsync(this._lobby.Id, AuthenticationService.Instance.PlayerId, updatePlayerOptions);
                    this._logger.Log($"Linked Relay allocation ID to player");

                    this._lobbyEventCallbacks = new LobbyEventCallbacks();
                    this._lobbyEventCallbacks.PlayerLeft += this.OnPlayerLeftLobby;
                    await LobbyService.Instance.SubscribeToLobbyEventsAsync(this._lobby.Id, this._lobbyEventCallbacks);
                    this._logger.Log($"Subscribed to lobby events");
                    this.ChangeState(MultiplayerState.JoinedLobby);
                }
                catch (LobbyServiceException e)
                {
                    MultiplayerSystem.IsMultiplayer = false;
                    this._logger.Log(e.Message, Logger.LogLevel.Error);
                    MultiplayerSystem.OnLobbyError?.Invoke(e.Reason);
                    this.ChangeState(MultiplayerState.Connected);
                }
                catch (RelayServiceException e)
                {
                    MultiplayerSystem.IsMultiplayer = false;
                    this._logger.Log(e.Message, Logger.LogLevel.Error);
                    MultiplayerSystem.OnRelayError?.Invoke(e.Reason);
                    this.ChangeState(MultiplayerState.Connected);
                }
                catch (Exception e)
                {
                    MultiplayerSystem.IsMultiplayer = false;
                    this._logger.Log(e.Message, Logger.LogLevel.Error);
                    MultiplayerSystem.OnError?.Invoke();
                    this.ChangeState(MultiplayerState.Connected);
                }
                break;
            case MultiplayerState.CreatedLobby:
                MultiplayerSystem.IsMultiplayer = true;
                break;
            case MultiplayerState.JoinedLobby:
                MultiplayerSystem.IsMultiplayer = true;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(newState), newState, null);
        }
    }

    public async void HostOrJoinGame()
    {
        QueryResponse response = await LobbyService.Instance.QueryLobbiesAsync();
        List<Lobby> lobbies = response.Results;

        ChangeState(lobbies.Count == 0 ? MultiplayerState.CreatingLobby : MultiplayerState.JoiningLobby);
    }

    public void StartHost()
    {
        NetworkManager.Singleton.StartHost();
        if (this.PlayerData.Count > 0)
        {
            this.PlayerData.Clear();
            this.PlayerData.Initialize(this);
        }
        PlayerData hostPlayerData = new(AuthenticationService.Instance.PlayerId, 0, MultiplayerSystem.LocalPlayerName)
        {
            ClientId = 0
        };
        this.PlayerData.Add(hostPlayerData);
        this._logger.Log("Started host");
    }

    public static void SetLocalPlayerName(string newPlayerName)
    {
        string newPlayerNameTrimmed = newPlayerName.Trim();
        if (newPlayerNameTrimmed == "" || MultiplayerSystem.State != MultiplayerState.Connected) { return; }

        MultiplayerSystem.LocalPlayerName = newPlayerNameTrimmed;
    }

    public string GetPlayerUsername(ulong clientId)
    {
        foreach (PlayerData playerData in this.PlayerData)
        {
            if (playerData.ClientId != clientId) { continue; }

            return playerData.Username.ToString();
        }

        this._logger.Log($"Could not find the player with clientId {clientId}", Logger.LogLevel.Error);
        return "";
    }

    private void OnPlayerJoinedLobby(List<LobbyPlayerJoined> joinedPlayers)
    {
        if (!this.IsHost) { return; }

        foreach (LobbyPlayerJoined joinedPlayer in joinedPlayers)
        {
            string playerName;
            if (!joinedPlayer.Player.Data.TryGetValue(_LOBBY_PLAYER_NAME_KEY, out PlayerDataObject playerNameData))
            {
                this._logger.Log($"Unable to get player name from player data with ID {joinedPlayer.Player.Id}", Logger.LogLevel.Error);
                playerName = "STRANGE";
            }
            else
            {
                this._logger.Log($"{playerNameData.Value} joined the lobby. Total players: {this.PlayerData.Count + 1}");
                playerName = playerNameData.Value;
            }

            this.PlayerData.Add(new(joinedPlayer.Player.Id, joinedPlayer.PlayerIndex, playerName));
        }
    }

    private void OnPlayerLeftLobby(List<int> leftPlayersIndex)
    {
        if (this.IsHost)
        {
            List<PlayerData> playersToRemove = new();

            foreach (PlayerData playerData in this.PlayerData)
            {
                if (!leftPlayersIndex.Contains(playerData.LobbyPlayerIndex)) { continue; }

                playersToRemove.Add(playerData);
            }

            while (playersToRemove.Count > 0)
            {
                int indexToRemove = 0;

                for (int i = 0; i < this.PlayerData.Count; i++)
                {
                    PlayerData tempPlayer = this.PlayerData[i];
                    if (tempPlayer.UnityId != playersToRemove[0].UnityId) { continue; }

                    indexToRemove = i;
                    this._logger.Log($"{tempPlayer.Username} left the lobby. Total players: {this.PlayerData.Count - 1}");
                    MultiplayerSystem.OnPlayerDisconnect?.Invoke(tempPlayer);
                    break;
                }

                this.PlayerData.RemoveAt(indexToRemove);
                playersToRemove.RemoveAt(0);
            }
        }
        else
        {
            bool didHostLeave = false;

            foreach (PlayerData playerData in this.PlayerData)
            {
                if (!leftPlayersIndex.Contains(playerData.LobbyPlayerIndex)) { continue; }
                MultiplayerSystem.OnPlayerDisconnect?.Invoke(playerData);

                if (playerData.UnityId != this._hostUnityId) { continue; }
                didHostLeave = true;
                break;
            }

            if (!didHostLeave) { return; }
            this._logger.Log("The host has disconnected from the lobby");
            MultiplayerSystem.OnHostDisconnect?.Invoke();
        }
    }

    private void OnClientConnectRpc(string joinedPlayerUnityId, string joinedPlayerUsername, ulong joinedPlayerClientId)
    {
        if (!this.IsHost) { return; }
        bool didFindPlayer = false;

        for (int i = 0; i < this.PlayerData.Count; i++)
        {
            PlayerData playerData = this.PlayerData[i];
            if (playerData.UnityId != joinedPlayerUnityId) { continue; }

            didFindPlayer = true;
            playerData.ClientId = joinedPlayerClientId;
            playerData.Username = joinedPlayerUsername;
            this.PlayerData[i] = playerData;
            break;
        }

        if (!didFindPlayer)
        {
            this._logger.Log($"Coudn't find the player {joinedPlayerUsername} in the PlayerData list!", Logger.LogLevel.Error);
        }
    }

    private void OnClientRelayDisconnect(ulong clientId)
    {
        bool didHostLeave = false;

        foreach (PlayerData playerData in this.PlayerData)
        {
            if (playerData.ClientId != clientId) { continue; }
            MultiplayerSystem.OnPlayerDisconnect?.Invoke(playerData);

            if (playerData.UnityId != this._hostUnityId) { continue; }
            didHostLeave = true;
            break;
        }

        if (this.IsHost || !didHostLeave) { return; }
        this._logger.Log("The host has disconnected from the Relay service");
        MultiplayerSystem.OnHostDisconnect?.Invoke();
    }
}

public enum MultiplayerState
{
    None,
    NotConnected,
    Connected,
    CreatingLobby,
    CreatedLobby,
    JoiningLobby,
    JoinedLobby,
}

[Serializable]
public struct PlayerData : INetworkSerializable, System.IEquatable<PlayerData>
{
    public FixedString64Bytes UnityId;
    public ulong ClientId;
    public int LobbyPlayerIndex;
    public FixedString64Bytes Username;
    public const ulong UNREGISTERED_CLIENT_ID = 98277;

    public PlayerData(FixedString64Bytes unityId, int lobbyPlayerIndex, FixedString64Bytes username)
    {
        this.UnityId = unityId;
        this.ClientId = UNREGISTERED_CLIENT_ID;
        this.LobbyPlayerIndex = lobbyPlayerIndex;
        this.Username = username;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        if (serializer.IsReader)
        {
            var reader = serializer.GetFastBufferReader();
            reader.ReadValueSafe(out UnityId);
            reader.ReadValueSafe(out ClientId);
            reader.ReadValueSafe(out LobbyPlayerIndex);
            reader.ReadValueSafe(out Username);
        }
        else
        {
            var writer = serializer.GetFastBufferWriter();
            writer.WriteValueSafe(UnityId);
            writer.WriteValueSafe(ClientId);
            writer.WriteValueSafe(LobbyPlayerIndex);
            writer.WriteValueSafe(Username);
        }
    }

    public readonly bool Equals(PlayerData other) => other.Equals(this) && UnityId == other.UnityId && ClientId == other.ClientId && LobbyPlayerIndex == other.LobbyPlayerIndex && Username == other.Username;
}
