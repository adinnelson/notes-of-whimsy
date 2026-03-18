using UnityEngine;
using System.Collections.Generic;

public class MinimapManager : MonoBehaviour
{
    public static MinimapManager Instance;

    [Header("Room Layout")]
    public Transform roomsContainer;
    public GameObject roomPrefab;

    [Header("Icon Layout")]
    public Transform iconContainer;
    public GameObject enemyIconPrefab;
    public GameObject itemIconPrefab;

    [Header("Map Scale")]
    public float worldToMapScale = 2.0f;

    private RoomGenerator roomGenerator;

    void Awake()
    {
        Instance = this;
        roomGenerator = FindFirstObjectByType<RoomGenerator>();

        if (roomGenerator == null)
        {
            Debug.LogWarning("MinimapManager: Could not find a RoomGenerator in the scene.");
        }

        if (roomsContainer == null)
        {
            Debug.LogError("MinimapManager: roomsContainer is not assigned in the Inspector.");
        }

        if (roomPrefab == null)
        {
            Debug.LogError("MinimapManager: roomPrefab is not assigned in the Inspector.");
        }

        if (iconContainer == null)
        {
            Debug.LogError("MinimapManager: iconContainer is not assigned in the Inspector.");
        }

        if (enemyIconPrefab == null)
        {
            Debug.LogWarning("MinimapManager: enemyIconPrefab is not assigned in the Inspector.");
        }

        if (itemIconPrefab == null)
        {
            Debug.LogWarning("MinimapManager: itemIconPrefab is not assigned in the Inspector.");
        }

        RoomGenerator.OnDungeonComplete += BuildMinimapFromDungeon;
    }

    void OnDestroy()
    {
        RoomGenerator.OnDungeonComplete -= BuildMinimapFromDungeon;
    }

    void Start()
    {
        BuildMinimapFromDungeon();
    }

    public void BuildMinimapFromDungeon()
    {
        ClearRoomIcons();

        if (roomGenerator == null)
        {
            Debug.LogWarning("MinimapManager could not find RoomGenerator.");
            return;
        }

        List<GameObject> rooms = roomGenerator.GetSpawnedRooms();

        foreach (GameObject room in rooms)
        {
            CreateRoomFromWorld(room.transform.position);
        }
    }

    void ClearRoomIcons()
    {
        for (int i = roomsContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(roomsContainer.GetChild(i).gameObject);
        }
    }

    void CreateRoomFromWorld(Vector3 worldPosition)
    {
        GameObject room = Instantiate(roomPrefab, roomsContainer);
        RectTransform rect = room.GetComponent<RectTransform>();
        rect.anchoredPosition = WorldToMap(worldPosition);
    }

    public RectTransform RegisterEnemyIcon()
    {
        if (iconContainer == null || enemyIconPrefab == null)
        {
            Debug.LogWarning("MinimapManager: Cannot register enemy icon because iconsContainer or enemyIconPrefab is missing.");
            return null;
        }

        GameObject icon = Instantiate(enemyIconPrefab, iconContainer);
        RectTransform rect = icon.GetComponent<RectTransform>();

        if (rect == null)
        {
            Debug.LogWarning("MinimapManager: enemyIconPrefab is missing a RectTransform.");
            return null;
        }

        rect.SetAsLastSibling();
        return rect;
    }

    public RectTransform RegisterItemIcon()
    {
        if (iconContainer == null || itemIconPrefab == null)
        {
            Debug.LogWarning("MinimapManager: Cannot register item icon because iconsContainer or itemIconPrefab is missing.");
            return null;
        }

        GameObject icon = Instantiate(itemIconPrefab, iconContainer);
        RectTransform rect = icon.GetComponent<RectTransform>();

        if (rect == null)
        {
            Debug.LogWarning("MinimapManager: itemIconPrefab is missing a RectTransform.");
            return null;
        }

        rect.SetAsLastSibling();
        return rect;
    }




    public Vector2 WorldToMap(Vector2 worldPos)
    {
        return worldPos * worldToMapScale;
    }




    /// <summary>
    /// below methods are for testing:
    /// testing the minimap with set rooms -> unattached to RoomGenerator procedural generation
    /// </summary>

    private float roomSpacing = 60.0f;

    // Generate minimap layout of test rooms
    void GenerateTestRooms()
    {
        Vector2Int[] testRooms = {new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(1, 1), new Vector2Int(1, -1)};
        foreach (Vector2Int gridPos in testRooms)
        {
            CreateRoom(gridPos);
        }
    }

    // Create the rooms to test in the minimap
    void CreateRoom(Vector2Int gridPos)
    {
        GameObject room = Instantiate(roomPrefab, roomsContainer);
        RectTransform rect = room.GetComponent<RectTransform>();
        rect.anchoredPosition = new Vector2(gridPos.x * roomSpacing, gridPos.y * roomSpacing);
    }
}
