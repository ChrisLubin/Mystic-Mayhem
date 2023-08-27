using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private Image _background;
    [SerializeField] private TMP_InputField _playerNameInput;
    [SerializeField] private Button _startGameButton;
    [SerializeField] private TextMeshProUGUI _loadingText;

    private void Awake()
    {
        PlayerManager.OnLocalPlayerSpawn += this.HideAllGameObjects;
        MultiplayerSystem.OnStateChange += this.OnMultiplayerStateChange;
        this._playerNameInput.onValueChanged.AddListener(this.OnPlayerNameInputValueChange);
        this._startGameButton.onClick.AddListener(this.OnStartButtonClick);
        this._background.gameObject.SetActive(true);
    }

    private void OnDestroy()
    {
        PlayerManager.OnLocalPlayerSpawn -= this.HideAllGameObjects;
        MultiplayerSystem.OnStateChange -= this.OnMultiplayerStateChange;
        this._playerNameInput.onValueChanged.RemoveAllListeners();
        this._startGameButton.onClick.RemoveAllListeners();
    }

    private void HideAllGameObjects()
    {
        this._background.gameObject.SetActive(false);
        this._playerNameInput.gameObject.SetActive(false);
        this._startGameButton.gameObject.SetActive(false);
        this._loadingText.gameObject.SetActive(false);
    }

    private void OnPlayerNameInputValueChange(string newValue)
    {
        string newValueTrimmed = newValue.Trim();
        if (newValueTrimmed == "" || newValue == MultiplayerSystem.LocalPlayerName) { return; }

        MultiplayerSystem.SetLocalPlayerName(newValueTrimmed);
    }

    private void OnStartButtonClick()
    {
        this._playerNameInput.gameObject.SetActive(false);
        this._startGameButton.gameObject.SetActive(false);
        this._loadingText.gameObject.SetActive(true);
        this._loadingText.text = "Processing...";
        MultiplayerSystem.Instance.HostOrJoinGame();
    }

    private void OnMultiplayerStateChange(MultiplayerState state)
    {
        switch (state)
        {
            case MultiplayerState.Connected:
                this._playerNameInput.gameObject.SetActive(true);
                this._startGameButton.gameObject.SetActive(true);
                break;
            case MultiplayerState.CreatingLobby:
                this._loadingText.text = "Creating game...";
                break;
            case MultiplayerState.JoiningLobby:
                this._loadingText.text = "Joining game...";
                break;
        }
    }
}
