using UnityEngine;
using TMPro;

public class GoldUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private TMP_SpriteAsset goldSprite;

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

        if (goldSprite == null)
        {
            TMP_SpriteAsset[] sprites = Resources.FindObjectsOfTypeAll<TMP_SpriteAsset>();
            foreach (var sprite in sprites)
            {
                if (sprite.name.ToLower().Contains("goldbag"))
                {
                    goldSprite = sprite;
                    break;
                }
            }

            if (goldSprite == null)
            {
                Debug.LogWarning("GoldUI: No TMP Sprite Asset assigned and none found containing 'goldbag'. Assign in PickupUIManager>GoldCount");
            }
        }
    }

    private void Start()
    {
        goldText.spriteAsset = goldSprite;
        GoldManager.Instance.OnGoldChanged += UpdateGold;
        UpdateGold(GoldManager.Instance.CurrentGold);
    }

    private void OnDisable()
    {
        GoldManager.Instance.OnGoldChanged -= UpdateGold;
    }

    private void UpdateGold(int newGoldAmount)
    {
        goldText.text = $"<sprite name=\"goldbag-pixilart\"> {newGoldAmount}";
        //goldText.text = $"Gold: {newGoldAmount}";
    }
}
