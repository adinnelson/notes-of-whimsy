using UnityEngine;

public class MinimapPlayer : MonoBehaviour
{
    public Transform player;
    public RectTransform mapContainer;

    void Start()
    {
        //should auto find the player if reference is missing -> essential for when player dies and everything resets
        if (player == null)
        {
            GameObject foundPlayer = GameObject.FindWithTag("Player");
            if (foundPlayer != null)
            {
                player = foundPlayer.transform;
            }
            else
            {
                Debug.LogWarning("MinimapPlayer: Could not find a GameObject tagged as 'Player'.");
            }
        }
    }

    void Update()
    {
        //should auto find the player if reference is missing -> essential for when player dies and everything resets
        if (player == null)
        {
            GameObject foundPlayer = GameObject.FindWithTag("Player");
            if (foundPlayer != null)
            {
                player = foundPlayer.transform;
            }
            else
            {
                Debug.LogWarning("MinimapPlayer: Could not find a GameObject tagged as 'Player'.");
            }
        }

        if (player == null || mapContainer == null || MinimapManager.Instance == null)
        {
            return;
        }

        Vector2 mapPos = MinimapManager.Instance.WorldToMap(player.position);
        mapContainer.anchoredPosition = -mapPos;
    }
}
