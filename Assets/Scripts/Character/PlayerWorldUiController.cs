using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class PlayerWorldUiController : NetworkBehaviour
{
    private PlayerNetworkController _networkController;

    [SerializeField] private Slider _healthSlider;

    private void Awake()
    {
        this._networkController = GetComponent<PlayerNetworkController>();
        this._networkController.CurrentHealth.OnValueChanged += OnHealthChange;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        this._networkController.CurrentHealth.OnValueChanged -= OnHealthChange;
    }

    private void Start()
    {
        this._healthSlider.maxValue = PlayerHealthController.PLAYER_MAX_HEALTH;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (this.IsOwner)
        {
            this._healthSlider.gameObject.SetActive(false);
            return;
        }
    }

    private void OnHealthChange(int _, int currentHealth)
    {
        if (this.IsOwner) { return; }
        this._healthSlider.value = currentHealth;
    }
}
