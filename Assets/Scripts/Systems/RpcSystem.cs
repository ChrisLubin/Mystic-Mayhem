using System;
using Unity.Netcode;

public class RpcSystem : NetworkedStaticInstanceWithLogger<RpcSystem>
{
    public static event Action<string, string, ulong> OnClientConnectRpc;

    [ServerRpc(RequireOwnership = false)]
    public void ClientConnectServerRpc(string playerUnityId, string playerUsername, ServerRpcParams serverRpcParams = default)
    {
        this._logger.Log($"{playerUsername} sent connect RPC");
        RpcSystem.OnClientConnectRpc?.Invoke(playerUnityId, playerUsername, serverRpcParams.Receive.SenderClientId);
    }
}
