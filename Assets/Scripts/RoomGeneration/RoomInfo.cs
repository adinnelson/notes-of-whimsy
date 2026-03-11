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
    private Tilemap roomLock;

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
        enemyWaveController = GetComponentInChildren<EnemyWaveController>();
    }

    private void Update()
    {
        if (Keyboard.current.mKey.wasPressedThisFrame && parentNode != null)
        {
            transform.position = parentNode.position - spawnedNode.localPosition;
        }
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
    public Tilemap GetRoomLock()
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
        print("Locking room");

        if (roomLock != null)
        {
            roomLock.gameObject.SetActive(true);
        }
        {
            print("No room lock tilemap found on " + gameObject.name);
        }
    }

    public void UnlockRoom()
    {
        print("Unlocking room");
        if (roomLock != null)
        {
            roomLock.gameObject.SetActive(false);
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
                // transform.GetChild(i).AddComponent<FloorInfo>();
                break;
            }
            else if (childName == "walls")
            {
                walls = transform.GetChild(i).GetComponent<Tilemap>();
            }
            else if (childName == "roomLock")
            {
                roomLock = transform.GetChild(i).GetComponent<Tilemap>();
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
            OnEnterRoom?.Invoke(gameObject);
        }
    }

}

public enum RoomTypes
{
    Starter,
    Generic,
    Combat,
    Shop,
    Reward,
    End

}