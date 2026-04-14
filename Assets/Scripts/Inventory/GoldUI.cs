using UnityEngine;
using TMPro;

public class GoldUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI goldText;
    //[SerializeField] private TMP_SpriteAsset goldSprite;

    private void Awake()
    {
        Debug.Assert(goldText != null, "GoldUI: goldText reference not assigned in inspector.");
        //Debug.Assert(goldSprite != null, "GoldUI: goldSprite reference not assigned in inspector.");
    }

    private void Start()
    {
        //goldText.spriteAsset = goldSprite;
        if (GoldManager.Instance != null)
        {
            GoldManager.Instance.OnGoldChanged += UpdateGold;
            UpdateGold(GoldManager.Instance.CurrentGold);
        }
    }

    private void OnDisable()
    {
        if (GoldManager.Instance != null)
        {
            GoldManager.Instance.OnGoldChanged -= UpdateGold;   
        }
    }

    private void UpdateGold(int newGoldAmount)
    {
        //goldText.text = $"<size=400%><sprite name=\"goldbag-pixilart\"></size> {newGoldAmount}";
        goldText.text = $"{newGoldAmount}";
    }
}
