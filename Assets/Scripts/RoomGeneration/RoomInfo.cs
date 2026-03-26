using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

public class RoomInfo : MonoBehaviour
{
    // SERIALIZED FIELDS
    [Header("TileMaps")]
    [SerializeField]
    private Tilemap floor;
    [SerializeField]
    private Tilemap walls;
    [SerializeField]
    private GameObject roomLock;
    // private Tilemap roomLock;

    [Header("Attachment Nodes")]
    [SerializeField]
    private GameObject leftNode;
    [SerializeField]
    private GameObject rightNode;
    [SerializeField]
    private GameObject topNode;
    [SerializeField]
    private GameObject bottomNode;

    [Header("Child Lists")]
    [SerializeField]
    private List<GameObject> nodeList;
    [SerializeField]
    private List<GameObject> subRooms;
    [SerializeField]
    private Transform parentNode;
    [SerializeField]
    private Transform spawnedNode;

    [Header("Enemy Spawning")]
    [SerializeField]
    private bool isCompleted;
    [SerializeField]
    private List<GameObject> enemyPrefabs;
    [SerializeField]
    private int numberOfWaves;
    [SerializeField]
    private int extraCombatWaves;

    [Header("Room Type")]
    [SerializeField]
    private bool isSafe;
    [SerializeField]
    private RoomTypes roomType;

    [Header("Shop Attributes")]
    [SerializeField]
    private bool isShop;
    
    [SerializeField]
    private List<GameObject> itemSpawnLocations;

    [Header("Camera Bounds")]
    [SerializeField]
    private bool useCustomCameraBounds;

    [SerializeField]
    [Range(0.0f, 100.0f)]
    private float customMaxSqrHorizontalCameraDistance = 5.0f;

    [SerializeField]
    [Range(0.0f, 100.0f)]
    private float customMaxSqrVerticalCameraDistance = 5.0f;

    [SerializeField]
    private CameraBoundsShape customCameraBoundsShape = CameraBoundsShape.Cross;

    [SerializeField]
    [Range(0.1f, 10.0f)]
    private float customCrossArmHalfThickness = 1.0f;

    [SerializeField]
    private float customCameraBoundsOffsetX = 0.0f;

    [SerializeField]
    private float customCameraBoundsOffsetY = 0.0f;

    private ShopManager shopManager;

    private RewardManager rewardManager;

    private bool hasGivenRewards;

    // PROPERTIES
    public int NumberOfNodes { get; private set; }

    // EVENTS
    public static event System.Action<GameObject> OnFloorOverlap;
    public static event System.Action<GameObject> OnEnterRoom;
    // UNITY LIFECYCLE METHODS

    private EnemyWaveController enemyWaveController;

    private void Awake()
    {
        CacheChildTilemaps();
        if(roomType == RoomTypes.Shop)
        {
            isShop = true;
            shopManager = GetComponent<ShopManager>();
            if(shopManager == null)
            {
                Debug.LogWarning("RoomInfo: ShopManager component missing from shop room");
            }
        }
        rewardManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<RewardManager>();
        if(rewardManager == null)
        {
            Debug.LogWarning("RoomInfo: RewardManager reference missing from GameManager");
        }

        enemyWaveController = GetComponentInChildren<EnemyWaveController>();
        UnlockRoom();
    }

    private void Update()
    {
        if (Keyboard.current.mKey.wasPressedThisFrame && parentNode != null)
        {
            transform.position = parentNode.position - spawnedNode.localPosition;
        }
        isCompleted = CheckIsRoomCompleted();
        if (isCompleted)
        {
            // unlock room            roomLock.gameObject.SetActive(false);
            GiveRewards();
        }
    }

    private void GiveRewards()
    {
        // possible edge case to talk about 
        // both given rewards here and checking for starter room
        if (isShop && !hasGivenRewards)
        {
            // Give rewards for shop room
            shopManager.GenerateShopRewards();
        }
        else if(!hasGivenRewards && roomType != RoomTypes.Starter && roomType != RoomTypes.End)
        {
            // TODO: maybe add odds to drop an item in general
            // Give rewards for non-shop room
            
            foreach (GameObject location in itemSpawnLocations)
            {
                rewardManager.SpawnReward(location.transform);
            }
        }
        hasGivenRewards = true;

    }

    // PUBLIC METHODS

    /// <summary>
    /// Gets the floor tilemap for this room
    /// </summary>
    /// <returns>The floor tilemap component</returns>
    public Tilemap GetFloor()
    {
        return floor;
    }

    /// <summary>
    /// Gets a node by direction name
    /// </summary>
    /// <param name="name">The direction name: "left", "right", "top", or "bottom"</param>
    /// <returns>The node GameObject in the specified direction, or null if not found</returns>
    public GameObject GetNode(string name)
    {
        return name.ToLower() switch
        {
            "left" => leftNode,
            "right" => rightNode,
            "top" => topNode,
            "bottom" => bottomNode,
            _ => null,
        };
    }

    /// <summary>
    /// Sets the parent node for this room
    /// </summary>
    /// <param name="newParentNode">The transform of the parent node</param>
    public void SetParentNode(Transform newParentNode)
    {
        parentNode = newParentNode;
    }

    /// <summary>
    /// Gets the parent node of this room
    /// </summary>
    /// <returns>The parent node transform</returns>
    public Transform GetParentNode()
    {
        return parentNode;
    }

    /// <summary>
    /// Removes a node from the node list
    /// </summary>
    /// <param name="node">The node to remove</param>
    public void RemoveNode(GameObject node)
    {
        nodeList.Remove(node);
    }

    /// <summary>
    /// Sets the spawned node for this room
    /// </summary>
    /// <param name="newSpawnedNode">The transform of the spawned node</param>
    public void SetSpawnedNode(Transform newSpawnedNode)
    {
        spawnedNode = newSpawnedNode;
    }

    /// <summary>
    /// Gets the spawned node of this room
    /// </summary>
    /// <returns>The spawned node transform</returns>
    public Transform GetSpawnedNode()
    {
        return spawnedNode;
    }

    /// <summary>
    /// Adds a sub-room (child room) to this room
    /// </summary>
    /// <param name="room">The room to add as a sub-room</param>
    public void AddSubRoom(GameObject room)
    {
        subRooms.Add(room);
    }

    /// <summary>
    /// Clears all sub-rooms and destroys them
    /// </summary>
    public void ClearSubRooms()
    {
        for (int i = 0; i < subRooms.Count; i++)
        {
            subRooms[i].GetComponent<RoomInfo>().ClearSubRooms();
            Destroy(subRooms[i]);
        }
        subRooms.Clear();
    }

    /// <summary>
    /// Gets the list of available nodes for this room
    /// </summary>
    /// <returns>The node list</returns>
    public List<GameObject> GetNodeList()
    {
        return nodeList;
    }

    /// <summary>
    /// Gets whether this room is safe from enemies
    /// </summary>
    /// <returns>True if the room is safe, false otherwise</returns>
    public bool GetSafety()
    {
        return isSafe;
    }
    public void SetSafety(bool safe)
    {
        isSafe = safe;
    }

    /// <summary>
    /// Gets the walls tilemap for this room
    /// </summary>
    /// <returns>The walls tilemap component</returns>
    public Tilemap GetWalls()
    {
        return walls;
    }

    /// <summary>
    /// Gets the room lock tilemap for this room
    /// </summary>
    /// <returns>The room lock tilemap component</returns>
    // public Tilemap GetRoomLock()
    // {
    //     return roomLock;
    // }
    public GameObject GetRoomLock()
    {
        return roomLock;
    }

    /// <summary>
    /// Gets the list of sub-rooms for this room
    /// </summary>
    /// <returns>The sub-rooms list</returns>
    public List<GameObject> GetSubRooms()
    {
        return subRooms;
    }

    /// <summary>
    /// Gets the room type
    /// </summary>
    /// <returns>The room type enum value</returns>
    public RoomTypes GetRoomType()
    {
        return roomType;
    }

    // sets room type
    public void SetRoomType(RoomTypes newType)
    {
        roomType = newType;
    }

    /// <summary>
    /// Gets whether this room is a shop
    /// </summary>
    /// <returns>True if this is a shop room, false otherwise</returns>
    public bool GetIsShop()
    {
        return isShop;
    }

    /// <summary>
    /// Gets the list of item spawn locations in this room
    /// </summary>
    /// <returns>The item spawn locations list</returns>
    public List<GameObject> GetItemSpawnLocations()
    {
        return itemSpawnLocations;
    }

    public bool UseCustomCameraBounds()
    {
        return useCustomCameraBounds;
    }

    public float GetCustomMaxSqrHorizontalCameraDistance()
    {
        return customMaxSqrHorizontalCameraDistance;
    }

    public float GetCustomMaxSqrVerticalCameraDistance()
    {
        return customMaxSqrVerticalCameraDistance;
    }

    public CameraBoundsShape GetCustomCameraBoundsShape()
    {
        return customCameraBoundsShape;
    }

    public float GetCustomCrossArmHalfThickness()
    {
        return customCrossArmHalfThickness;
    }

    public float GetCustomCameraBoundsOffsetX()
    {
        return customCameraBoundsOffsetX;
    }

    public float GetCustomCameraBoundsOffsetY()
    {
        return customCameraBoundsOffsetY;
    }

    public bool IsCompleted()
    {
        return isCompleted;
    }

    public void SetEnemyPrefabs(List<GameObject> enemyPrefabs)
    {
        this.enemyPrefabs = enemyPrefabs;
    }

    /// <summary>
    /// Gets the list of enemy prefabs assigned to this room
    /// </summary>
    /// <returns>The enemy prefabs list</returns>
    public List<GameObject> GetEnemyPrefabs()
    {
        return enemyPrefabs;
    }

    /// <summary>
    /// Gets the number of enemy waves assigned to this room, including extra waves if this is a combat room
    /// </summary>
    /// <returns>The number of waves</returns>
    public int GetNumberOfWaves()
    {
        // Return base number of waves plus extra combat waves if this is a combat room
        return numberOfWaves + (roomType == RoomTypes.Combat ? extraCombatWaves : 0);
    }

    public void LockRoom()
    {
        // todo: add visual 
        print("Locking room");

        if (roomLock != null && isSafe == false)
        {
            roomLock.SetActive(true);
        }
        else
        {
            print("No room lock tilemap found on " + gameObject.name);
        }
    }

    public void UnlockRoom()
    {
        // todo: add visual 
        if (roomLock != null)
        {
            isCompleted = true;
            roomLock.SetActive(false);
        }
    }


    // PRIVATE METHODS

    /// <summary>
    /// Caches references to child tilemaps by their names
    /// </summary>
    private void CacheChildTilemaps()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            string childName = transform.GetChild(i).name.ToLower();
            if (childName == "floor")
            {
                floor = transform.GetChild(i).GetComponent<Tilemap>();
                continue;
                // transform.GetChild(i).AddComponent<FloorInfo>();
            }
            else if (childName == "walls")
            {
                walls = transform.GetChild(i).GetComponent<Tilemap>();
                continue;
            }
            else if (childName == "roomlock 2")
            {
                // for gameobjects
                roomLock = transform.GetChild(i).gameObject;
                CullRoomLockObjects();
                UnlockRoom();

                continue;
            }
        }
    }

    /// <summary>
    /// Handles collision detection for floor overlaps
    /// </summary>
    /// <param name="collision">The collider that entered the trigger</param>
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // print($"collision with {collision.name}");
        if (collision.name == "Floor" && collision.gameObject != floor.gameObject)
        {
            OnFloorOverlap?.Invoke(gameObject);
        }
        if (collision.CompareTag("Player"))
        {
            // print(gameObject.name + " has been entered by the player!" + roomType );
            OnEnterRoom?.Invoke(gameObject);
            if(roomType != RoomTypes.Shop && roomType != RoomTypes.Reward && roomType != RoomTypes.Starter)
            {
                // LockRoom();
            }
            
        }
    }

    private bool CheckIsRoomCompleted()
    {
       return enemyWaveController == null || enemyWaveController.AreWavesComplete();
    }

    private void OnDrawGizmosSelected()
    {
        if (!useCustomCameraBounds)
        {
            return;
        }

        float horizontalHalfLength = Mathf.Sqrt(customMaxSqrHorizontalCameraDistance);
        float verticalHalfLength = Mathf.Sqrt(customMaxSqrVerticalCameraDistance);
        Vector3 center = transform.position;

        Gizmos.color = Color.cyan;

        if (customCameraBoundsShape == CameraBoundsShape.Box)
        {
            Gizmos.DrawWireCube(center, new Vector3(horizontalHalfLength * 2f, verticalHalfLength * 2f, 0f));
            return;
        }

        Gizmos.DrawWireCube(center, new Vector3(horizontalHalfLength * 2f, customCrossArmHalfThickness * 2f, 0f));
        Gizmos.DrawWireCube(center, new Vector3(customCrossArmHalfThickness * 2f, verticalHalfLength * 2f, 0f));
    }

    private void CullRoomLockObjects()
    {
        int numberOfChildren = roomLock.transform.childCount;
        GameObject nodeAttachment;
        for (int i = numberOfChildren - 1; i >= 0; i--)
        {
            GameObject child = roomLock.transform.GetChild(i).gameObject;
            switch (child.name.ToLower())
            {
                case "leftroomlock":
                    nodeAttachment = GetNode("left");
                    if(nodeAttachment == null)
                    {
                        Destroy(child);
                        continue;
                    }
                    child.transform.localPosition = nodeAttachment.transform.localPosition;
                    break;
                case "rightroomlock":
                    nodeAttachment = GetNode("right");
                    if(nodeAttachment == null)
                    {
                        Destroy(child);
                        continue;


                    }
                    child.transform.localPosition = nodeAttachment.transform.localPosition;
                    break;
                case "toproomlock":
                    nodeAttachment = GetNode("top");
                    if(nodeAttachment == null)
                    {
                        Destroy(child);
                        continue;


                    }
                    child.transform.localPosition = nodeAttachment.transform.localPosition;
                    break;
                case "bottomroomlock":
                    nodeAttachment = GetNode("bottom");
                    if(nodeAttachment == null)
                    {
                        Destroy(child);
                        continue;

                    }
                    child.transform.localPosition = nodeAttachment.transform.localPosition;
                    break;
                default:
                    Destroy(child);
                    break;
            }
        }
    }

}

public enum RoomTypes
{
    Starter,
    Generic,
    Combat,
    Shop,
    Boss,
    Reward,
    End

}

public enum CameraBoundsShape
{
    Cross,
    Box
}