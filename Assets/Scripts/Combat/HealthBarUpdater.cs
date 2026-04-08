using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HealthBarUpdater : MonoBehaviour
{
    [SerializeField] private Slider slider;
    [SerializeField] private TMP_Text healthText;

    public void UpdateHealthBar(float currentHealth, float maxHealth)
    {
        if (slider != null)
        {
            slider.minValue = 0f;
            slider.maxValue = maxHealth;
            slider.value = currentHealth;
        }

        if (healthText != null)
        {
            healthText.text = $"{Mathf.CeilToInt(currentHealth)}/{Mathf.CeilToInt(maxHealth)}";
        }
    }
}
