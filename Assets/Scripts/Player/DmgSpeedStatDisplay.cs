using UnityEngine;
using TMPro;

public class DmgSpeedStatDisplay : MonoBehaviour
{
    public enum StatType
    {
        Damage,
        Speed
    }

    [SerializeField] private StatType statType;
    [SerializeField] private TextMeshProUGUI amountText;

    private PlayerStats playerStats;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerStats = PlayerStats.Instance;
        if (playerStats == null)
        {
            Debug.LogWarning("DmgSpeedStatDisplay. Player Stats not found.");
            return;
        }

        playerStats.OnDamageChanged += HandleDamageChanged;
        playerStats.OnSpeedChanged += HandleSpeedChanged;
        playerStats.OnStatsReset += RefreshDisplay;

        RefreshDisplay();
    }

    void OnDestroy()
    {
        if (playerStats == null)
        {
            return;
        }

        playerStats.OnDamageChanged -= HandleDamageChanged;
        playerStats.OnSpeedChanged -= HandleSpeedChanged;
        playerStats.OnStatsReset -= RefreshDisplay;
    }

    private void HandleDamageChanged(int newDamage)
    {
        if (statType == StatType.Damage && amountText != null)
        {
            amountText.text = newDamage.ToString();
        }
    }

    private void HandleSpeedChanged(float newSpeed)
    {
        if (statType == StatType.Speed && amountText != null)
        {
            amountText.text = newSpeed.ToString("F0");
        }
    }

    private void RefreshDisplay()
    {
        if (playerStats == null || amountText == null)
        {
            return;
        }

        switch (statType)
        {
            case StatType.Damage:
                amountText.text = playerStats.Damage.ToString();
                break;
            case StatType.Speed:
                amountText.text = playerStats.Speed.ToString("F0");
                break;
        }
    }

}
