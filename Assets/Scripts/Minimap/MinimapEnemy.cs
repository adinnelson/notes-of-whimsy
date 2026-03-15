using UnityEngine;

//enemy icons will look different than items icon
public class MinimapEnemy : MonoBehaviour
{
    private RectTransform icon;

    void Start()
    {
        if (MinimapManager.Instance == null)
        {
            Debug.LogWarning($"MinimapEnemy on {gameObject.name}: No MinimapManager instance found.");
            return;
        }

        icon = MinimapManager.Instance.RegisterEnemyIcon();
    }

    void Update()
    {
        if (icon == null || MinimapManager.Instance == null)
            return;

        icon.anchoredPosition = MinimapManager.Instance.WorldToMap(transform.position);
    }

    void OnDisable()
    {
        if (icon != null)
        {
            Destroy(icon.gameObject);
            icon = null;
        }
    }

    void OnDestroy()
    {
        if (icon != null)
        {
            Destroy(icon.gameObject);
            icon = null;
        }
    }
}
