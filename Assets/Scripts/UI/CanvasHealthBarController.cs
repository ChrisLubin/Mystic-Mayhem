using UnityEngine;
using UnityEngine.UI;

public class CanvasHealthBarController : MonoBehaviour
{
    private Slider _slider;

    private void Awake()
    {
        this._slider = GetComponent<Slider>();
        PlayerHealthController.OnLocalPlayerHealthChange += this.SetCurrentHealth;
        PlayerManager.OnLocalPlayerSpawn += this.Show;
    }

    private void OnDestroy()
    {
        PlayerHealthController.OnLocalPlayerHealthChange -= this.SetCurrentHealth;
        PlayerManager.OnLocalPlayerSpawn -= this.Show;
    }

    private void Start()
    {
        this.Hide();
        this._slider.maxValue = PlayerHealthController.PLAYER_MAX_HEALTH;
    }

    private void SetCurrentHealth(int currentHealth)
    {
        this._slider.value = currentHealth;
    }

    private void Show()
    {
        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(true);
        }
    }

    private void Hide()
    {
        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(false);
        }
    }
}
