using System;
using Unity.Netcode;
using UnityEngine;

public class Logger
{
    private string _className;

    public Logger(string className)
    {
        this._className = className;
    }

    public void Log(string message, LogLevel logLevel = LogLevel.Info)
    {
        string log = $"[{this._className}] - {message}";

        switch (logLevel)
        {
            case LogLevel.Info:
                Debug.Log(log);
                break;
            case LogLevel.Warning:
                Debug.LogWarning(log);
                break;
            case LogLevel.Error:
                Debug.LogError(log);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null);
        }
    }

    public void Log(int message, LogLevel logLevel = LogLevel.Info) => this.Log(message.ToString(), logLevel);

    public enum LogLevel
    {
        Info = 0,
        Warning = 1,
        Error = 2,
    }
}

public abstract class WithLogger<T> : MonoBehaviour where T : MonoBehaviour
{
    protected Logger _logger;

    protected virtual void Awake()
    {
        this._logger = new Logger(GetType().Name);
    }
}

public abstract class StaticInstanceWithLogger<T> : StaticInstance<T> where T : MonoBehaviour
{
    protected Logger _logger;

    protected override void Awake()
    {
        base.Awake();
        this._logger = new Logger(GetType().Name);
    }
}

public abstract class NetworkBehaviourWithLogger<T> : NetworkBehaviour where T : NetworkBehaviour
{
    protected Logger _logger;

    protected virtual void Awake()
    {
        this._logger = new Logger(GetType().Name);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        this._logger = new Logger($"{GetType().Name} - {this.OwnerClientId}");
    }
}

public abstract class NetworkedStaticInstanceWithLogger<T> : NetworkedStaticInstance<T> where T : NetworkBehaviour
{
    protected Logger _logger;

    protected override void Awake()
    {
        base.Awake();
        this._logger = new Logger(GetType().Name);
    }
}
