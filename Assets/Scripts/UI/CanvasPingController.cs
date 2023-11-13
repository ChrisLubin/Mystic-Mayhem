using TMPro;
using UnityEngine;

public class CanvasPingController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _pingText;
    private int _tickDiff = 0;
    private const float _UPDATE_PING_INTERVAL_SECCONDS = 2f;
    private float _timeSinceLastPingUpdate = 0f;

    private void Awake() => PlayerPredictionController.OnTickDiffBetweenLocalClientAndServer += this.UpdateTickDiff;
    private void Start() => this.gameObject.SetActive(false);
    private void OnDestroy() => PlayerPredictionController.OnTickDiffBetweenLocalClientAndServer -= this.UpdateTickDiff;

    private void Update()
    {
        this._timeSinceLastPingUpdate += Time.deltaTime;
        if (this._timeSinceLastPingUpdate < _UPDATE_PING_INTERVAL_SECCONDS || (!MultiplayerSystem.IsGameHost && this._tickDiff == 0)) { return; }

        this._pingText.text = $"{Mathf.RoundToInt(this._tickDiff * TickSystem.MIN_TIME_BETWEEN_TICKS * 1000)} ms";
        this._timeSinceLastPingUpdate = 0f;
    }

    private void UpdateTickDiff(int tickDiff)
    {
        this._tickDiff = tickDiff;
        if (!this.gameObject.activeSelf)
        {
            this.gameObject.SetActive(true);
            this._pingText.text = $"{Mathf.RoundToInt(this._tickDiff * TickSystem.MIN_TIME_BETWEEN_TICKS * 1000)} ms";
        }
    }
}
