using UnityEngine;
using TMPro;

public class HealthUI : MonoBehaviour
{
    [SerializeField] private TextMeshPro healthText;  // assign this in the HealthDisplay prefab

    private Health target;

    public void Initialize(Health health)
    {
        target = health;
        UpdateText();
    }

    // Update display
    public void UpdateText()
    {
        if (target != null)
        {
            //healthText.text = $"{target.name} {target.CurrentHealth}/{target.MaxHealth}";
            healthText.text = $"{target.CurrentHealth}/{target.MaxHealth}";
        }
    }
}
