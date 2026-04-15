using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Tilemaps;
using UnityEngine.SceneManagement;

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

    [Header("Scene Settings")]
    [SerializeField] private string bossSceneKeyword = "Boss";

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

        /*if (enemyIconPrefab == null)
        {
            Debug.LogWarning("MinimapManager: enemyIconPrefab is not assigned in the Inspector.");
        }

        if (itemIconPrefab == null)
        {
            Debug.LogWarning("MinimapManager: itemIconPrefab is not assigned in the Inspector.");
        }*/
    }

    void Start()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        Debug.Log("MinimapManager Start | Scene: " + currentScene);

        if (currentScene.Contains(bossSceneKeyword))
        {
            StartCoroutine(BuildBossMinimapDelayed());
        }
        else if (roomGenerator != null) //if the dungeon is already generated
        {
            if (roomGenerator.GetSpawnedRooms().Count > 0)
            {
                BuildMinimapFromDungeon();
            }
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

    public void BuildMinimapFromBossScene()
    {
        Debug.Log("BuildMinimapFromBossScene called");

        ClearRoomIcons();

        //RoomInfo[] rooms = FindObjectsByType<RoomInfo>(FindObjectsSortMode.None);
        RoomInfo[] rooms = new RoomInfo[0];
        Debug.Log($"Found {rooms.Length} RoomInfo objects");

        if (rooms.Length == 0)
        {
            Debug.LogWarning("No RoomInfo components found in Boss scene.");
            return;
        }

        foreach (RoomInfo roomInfo in rooms)
        {
            MinimapRoomOverride overrideData = roomInfo.GetComponent<MinimapRoomOverride>();

            if (overrideData != null && overrideData.minimapPrefab != null)
            {
                Debug.Log($"Instantiating OVERRIDE minimap for {roomInfo.name}");

                GameObject icon = Instantiate(overrideData.minimapPrefab, roomsContainer);
                RectTransform rect = icon.GetComponent<RectTransform>();

                if (rect == null)
                {
                    Debug.LogError("Override prefab missing RectTransform!");
                    continue;
                }

                rect.anchoredPosition = WorldToMap(roomInfo.transform.position);
            }
            else
            {
                Debug.Log($"Instantiating DEFAULT minimap for {roomInfo.name}");
                CreateRoomFromWorld(roomInfo.transform.position, roomInfo);
            }
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

        if (rect == null)
        {
            Debug.LogError("roomPrefab missing RectTransform!");
            return;
        }

        rect.anchoredPosition = WorldToMap(worldPosition);

        if (roomInfo != null)
        {
            Tilemap floor = roomInfo.GetFloor();
            if (floor != null)
            {
                Vector3 worldCentre = worldPosition + floor.localBounds.center;

                rect.anchoredPosition = WorldToMap(worldCentre);

                Vector2 worldSize = floor.localBounds.size;
                rect.sizeDelta = (worldSize * worldToMapScale);

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

    IEnumerator BuildBossMinimapDelayed()
    {
        yield return null;
        Debug.Log("Building Boss Minimap (delayed)");
        BuildMinimapFromBossScene();
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
