using UnityEngine;
using TMPro;

public class HealthText : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI healthText;

    public void UpdateHPText(float currentHealth, float maxHealth)
    {
        healthText.text = $"{currentHealth:F0} / {maxHealth:F0}";
    }
}
