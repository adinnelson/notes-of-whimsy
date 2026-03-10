using UnityEngine;

public class MinimapPlayer : MonoBehaviour
{
    public Transform player;
    public RectTransform mapContainer;
    public float worldToMapScale = 2.0f;
    RectTransform rect;

    void Start()
    {
        rect = GetComponent<RectTransform>();
    }

    void Update()
    {
        if (player == null || mapContainer == null || MinimapManager.Instance == null)
        {
            return;
        }

        Vector2 worldPos = player.position;
        Vector2 mapPos = worldPos * worldToMapScale;
        rect.anchoredPosition = mapPos;
        mapContainer.anchoredPosition = -mapPos;
    }
}
