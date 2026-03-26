using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HealthBarUpdater : MonoBehaviour
{
    [SerializeField] private Slider slider;

    public void UpdateHealthBar(float currentHealth, float maxHealth)
    {
        slider.value = currentHealth / maxHealth;
    }
}
