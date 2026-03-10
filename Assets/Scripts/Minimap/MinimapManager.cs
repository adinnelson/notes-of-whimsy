using UnityEngine;
using System.Collections.Generic;

public class MinimapManager : MonoBehaviour
{
    public static MinimapManager Instance;

    [Header("Room Layout")]
    public Transform roomsContainer;
    public GameObject roomPrefab;
    public float roomSpacing = 60.0f;

    [Header("Icon Layout")]
    public Transform iconsContainer;
    public GameObject itemsPrefab;
    public float worldToMapScale = 2.0f;

    private RoomGenerator roomGenerator;

    void Awake()
    {
        Instance = this;
        roomGenerator = FindFirstObjectByType<RoomGenerator>();
        RoomGenerator.OnDungeonComplete += BuildMinimapFromDungeon;
    }

    void OnDestroy()
    {
        RoomGenerator.OnDungeonComplete -= BuildMinimapFromDungeon;
    }

    void Start()
    {
        //GenerateTestRooms();
        //iconsContainer.SetAsLastSibling();
        if (iconsContainer != null)
        {
            iconsContainer.SetAsLastSibling();
        }
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

    public RectTransform RegisterIcon(MinimapIcon worldIcon)
    {
        Debug.Log($"Registering minimap icon for {worldIcon.gameObject.name}");

        GameObject icon = Instantiate(itemsPrefab, iconsContainer);
        RectTransform rect = icon.GetComponent<RectTransform>();
        rect.SetAsLastSibling();

        if (rect == null)
        {
            Debug.LogError("Entity icon prefab does not have a RectTransform.");
        }

        return rect;
    }

    public Vector2 WorldToMap(Vector2 worldPos)
    {
        return worldPos * worldToMapScale;
    }




    /// <summary>
    /// below methods:
    /// testing the minimap with set rooms -> unattached to RoomGenerator procedural generation
    /// </summary>

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
