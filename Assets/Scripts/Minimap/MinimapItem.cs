using UnityEngine;

//items that will show up: spell-notes, beat-ticks, any boostables (max health, max speed, max damage, health pot)
public class MinimapItem : MonoBehaviour
{
    private RectTransform icon;

    void Start()
    {
        if (MinimapManager.Instance == null)
        {
            Debug.LogWarning($"MinimapItem on {gameObject.name}: No MinimapManager instance found.");
            return;
        }

        icon = MinimapManager.Instance.RegisterItemIcon();
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
