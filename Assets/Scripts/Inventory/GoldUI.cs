using UnityEngine;
using TMPro;

public class GoldUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI goldText;

    private void OnEnable()
    {
        GoldManager.Instance.OnGoldChanged += UpdateGold;
    }

    private void OnDisable()
    {
        GoldManager.Instance.OnGoldChanged -= UpdateGold;
    }

    private void Start()
    {
        GoldManager.Instance.OnGoldChanged += UpdateGold;
        UpdateGold(GoldManager.Instance.CurrentGold);
    }

    private void UpdateGold(int newGoldAmount)
    {
        goldText.text = $"Gold: {newGoldAmount}";
    }
}
