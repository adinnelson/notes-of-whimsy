using UnityEngine;
using TMPro;

public class GoldUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI goldText;

    private void Awake()
    {
        if (goldText == null)
        {
            Transform goldCount = transform.Find("GoldCanvas/GoldCount");
            if (goldCount != null )
            {
                goldText = goldCount.GetComponent<TextMeshProUGUI>();
            }
            else
            {
                Debug.LogWarning("GoldUI could not find child object named GoldCount. Add GoldCanvas to scene hierarchy, and confirm GoldCount TMP is its child.");
            }
        }
    }

    private void Start()
    {
        GoldManager.Instance.OnGoldChanged += UpdateGold;
        UpdateGold(GoldManager.Instance.CurrentGold);
    }

    private void OnDisable()
    {
        GoldManager.Instance.OnGoldChanged -= UpdateGold;
    }

    private void UpdateGold(int newGoldAmount)
    {
        goldText.text = $"Gold: {newGoldAmount}";
    }
}
