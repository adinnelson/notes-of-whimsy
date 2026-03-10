using UnityEngine;

public class MinimapIcon : MonoBehaviour
{
    RectTransform icon;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Debug.Log($"MinimapIcon starting on {gameObject.name}");
        icon = MinimapManager.Instance.RegisterIcon(this);

        if (icon == null)
        {
            Debug.LogWarning($"No minimap icon was created for {gameObject.name}");
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (icon != null)
        {
            Vector2 mapPos = MinimapManager.Instance.WorldToMap(transform.position);
            icon.anchoredPosition = mapPos;
        }
    }

    void OnDisable()
    {
        if (icon != null)
        {
            OnDestroy(icon.gameObject);
            icon = null;
        }
    }

    void OnDestroy()
    {
        if ( icon != null )
        {
            Destroy(icon.gameObject);
            icon = null;
        }
    }
}
