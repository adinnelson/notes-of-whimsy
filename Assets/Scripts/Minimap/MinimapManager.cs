using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Tilemaps;

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

    //for debugging the overlapping room icons - it is the room boundaries themselves that are overlapping, not the room icons. 
    private List<Bounds> debugRoomBounds = new List<Bounds>();

    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        foreach (Bounds b in debugRoomBounds)
        {
            Gizmos.DrawWireCube(b.center, b.size); 
        }
    }

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
        //RoomGenerator.OnDungeonComplete += BuildMinimapFromDungeon;
    }

    void Start()
    {
        if (roomGenerator != null && roomGenerator.GetSpawnedRooms().Count > 0)
        {
            BuildMinimapFromDungeon();
        }
    }

    void OnEnable()
    {
        RoomGenerator.OnDungeonComplete += BuildMinimapFromDungeon;
    }

    void OnDisable()
    {
        RoomGenerator.OnDungeonComplete -= BuildMinimapFromDungeon;
    }

    void OnDestroy()
    {
        RoomGenerator.OnDungeonComplete -= BuildMinimapFromDungeon;
    }

    public void BuildMinimapFromDungeon()
    {
        debugRoomBounds.Clear();
        ClearRoomIcons();

        if (roomGenerator == null)
        {
            Debug.LogWarning("MinimapManager could not find RoomGenerator.");
            return;
        }

        List<GameObject> rooms = roomGenerator.GetSpawnedRooms();

        foreach (GameObject room in rooms)
        {
            //adding a pull from RoomInfo.cs so that the minimap rooms can accurately show the scale of actual rooms on the Minimap
            RoomInfo roomInfo = room.GetComponent<RoomInfo>();
            CreateRoomFromWorld(room.transform.position, roomInfo);
        }
    }

    void ClearRoomIcons()
    {
        for (int i = roomsContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(roomsContainer.GetChild(i).gameObject);
        }
    }

    void CreateRoomFromWorld(Vector3 worldPosition, RoomInfo roomInfo)
    {
        GameObject room = Instantiate(roomPrefab, roomsContainer);
        RectTransform rect = room.GetComponent<RectTransform>();
        rect.anchoredPosition = WorldToMap(worldPosition);

        //added to size the minimap room icons to match actual room boundaries
        if (roomInfo != null)
        {
            Tilemap floor = roomInfo.GetFloor();
            if (floor != null)
            {
                //actual centre of tiled area, not centre of room origin
                Vector3 worldCentre = worldPosition + floor.localBounds.center;

                //this shows the boundaries of the actual rooms being generated, and how they overlap in many cases, causing the icons to also overlap
                //debugRoomBounds.Add(new Bounds(worldCentre, floor.localBounds.size));

                rect.anchoredPosition = WorldToMap(worldCentre);

                Vector2 worldSize = floor.localBounds.size;
                rect.sizeDelta = (worldSize * worldToMapScale);
                //this line allows for space between the room icons to attempt to balance out that the actual rooms overlap, replacing the above line 
                //rect.sizeDelta = (worldSize * worldToMapScale) * 0.85f;

                return;
            }
        }
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
