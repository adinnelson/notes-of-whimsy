using UnityEngine;

public class MinimapPlayer : MonoBehaviour
{
    public Transform player;
    public RectTransform mapContainer;

    void Update()
    {
        if (player == null || mapContainer == null || MinimapManager.Instance == null)
        {
            return;
        }

        Vector2 mapPos = MinimapManager.Instance.WorldToMap(player.position);
        mapContainer.anchoredPosition = -mapPos;
    }
}
