using System;

public class GameManager : NetworkedStaticInstanceWithLogger<GameManager>
{
    public static event Action<GameState> OnStateChange;
    public static GameState State { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        MultiplayerSystem.OnStateChange += this.OnMultiplayerStateChange;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        MultiplayerSystem.OnStateChange -= this.OnMultiplayerStateChange;
    }

    public void ChangeState(GameState newState)
    {
        if (GameManager.State == newState) { return; }

        this._logger.Log($"New state: {newState}");
        GameManager.State = newState;
        GameManager.OnStateChange?.Invoke(newState);

        switch (newState)
        {
            case GameState.None:
                break;
            case GameState.GameStarted:
                this.HandleGameStarted();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(newState), newState, null);
        }
    }

    private void HandleGameStarted()
    {
    }

    private void OnMultiplayerStateChange(MultiplayerState state)
    {
        switch (state)
        {
            case MultiplayerState.CreatedLobby:
                this.ChangeState(GameState.GameStarted);
                break;
            case MultiplayerState.JoinedLobby:
                this.ChangeState(GameState.GameStarted);
                break;
        }
    }
}

[Serializable]
public enum GameState
{
    None,
    GameStarted,
}
