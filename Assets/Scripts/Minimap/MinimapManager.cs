using UnityEngine;

public class MinimapManager : MonoBehaviour
{
    public static MinimapManager Instance;

    public Transform roomsContainer;
    public GameObject roomPrefab;
    public float roomSpacing = 40.0f;

    public Transform iconsContainer;
    public GameObject itemsPrefab;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        GenerateTestRooms();
        iconsContainer.SetAsLastSibling();
    }

    void GenerateTestRooms()
    {
        Vector2Int[] testRooms = {new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(1, 1), new Vector2Int(1, -1)};
        foreach (Vector2Int gridPos in testRooms)
        {
            CreateRoom(gridPos);
        }
    }

    void CreateRoom(Vector2Int gridPos)
    {
        GameObject room = Instantiate(roomPrefab, roomsContainer);
        RectTransform rect = room.GetComponent<RectTransform>();
        rect.anchoredPosition = new Vector2(gridPos.x * roomSpacing, gridPos.y * roomSpacing);
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
        return worldPos * 2.0f;
    }
}
