using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

public class RoomGenerator : MonoBehaviour
{
    // SERIALIZED FIELDS
    [Header("Enemy Pool")]
    [SerializeField]
    private List<GameObject> enemyPool;

    [Header("Layout Controls")]
    [SerializeField]
    private int desiredRoomNumber;
    [SerializeField]
    private int minRooms;
    [SerializeField]
    private int minEnemies;
    [SerializeField]
    private int maxEnemies;

    [Header("Room Pools")]
    [SerializeField]
    private List<GameObject> mapPool;
    [SerializeField]
    private List<GameObject> starterRooms;
    [SerializeField]
    private List<GameObject> leftConnections;
    [SerializeField]
    private List<GameObject> rightConnections;
    [SerializeField]
    private List<GameObject> topConnections;
    [SerializeField]
    private List<GameObject> bottomConnections;
    [SerializeField]
    private List<GameObject> shopRooms;


    // CONSTANTS
    private const int ROOMS_SPAWNED_RESET_THRESHOLD = 0;

    // PRIVATE FIELDS
    [SerializeField]
    private List<GameObject> spawnedRooms;
    private GameObject roomsParent;
    private int numberOfRooms;

    // EVENTS
    public static event System.Action OnDungeonComplete;

    // DEBUG
    [SerializeField]
    private bool allowRegeneration = true;

    private void Awake()
    {
        RoomInfo.OnFloorOverlap += HandleFloorOverlap;
        CreateRoomPools();
        // CreateFloorLayout();
    }

    private void Update()
    {
        //TODO: uncomment for testing purposes, maybe add some conditions to prevent accidental resets during gameplay
        if ((Keyboard.current.lKey.wasPressedThisFrame && allowRegeneration) /*|| spawnedRooms.Count < minRooms*/)
        {
            ResetGeneration();
        }
    }

    private void OnDestroy()
    {
        RoomInfo.OnFloorOverlap -= HandleFloorOverlap;
    }

    // PUBLIC METHODS

    /// <summary>
    /// Resets the dungeon generation by destroying the current layout and generating a new one
    /// </summary>
    public void ResetGeneration()
    {
        Destroy(roomsParent);
        roomsParent = null;
        print("Resetting floors");
        numberOfRooms = 0;
        spawnedRooms.Clear();
        // CreateFloorLayout();
    }

    /// <summary>
    /// Handles floor overlap events by resetting generation when rooms overlap
    /// </summary>
    /// <param name="overlappingFloor">The floor GameObject that overlapped</param>
    private void HandleFloorOverlap(GameObject overlappingFloor)
    {
        // ResetGeneration();
        // Debug.LogWarning($"Floor overlap detected with {overlappingFloor.name}", overlappingFloor);
    }

    /// <summary>
    /// Creates the floor layout of the dungeon by spawning rooms from the respective room pools and connecting them together until
    /// the desired number of rooms is reached or there are no more available nodes to spawn on.
    /// </summary>
    public void CreateFloorLayout()
    {
        GameObject spawnedRoom = null;
        RoomInfo currentRoomInfo = null;
        int roomSelector = 0;

        if (roomsParent == null)
        {
            roomsParent = new GameObject("Floor Layout");
            roomsParent.transform.SetParent(transform);
            roomsParent.transform.position = Vector3.zero;
        }
        // Spawn a random starter room and add it to the list of spawned rooms
        roomSelector = Random.Range(0, starterRooms.Count);
        GameObject currentRoom = Instantiate(starterRooms[roomSelector], roomsParent.transform);
        spawnedRooms.Add(currentRoom);
        numberOfRooms++;

        // Loop through the list of spawned rooms and spawn new rooms on each available node until the desired number of rooms is reached or there are no more available nodes
        for (int i = 0; numberOfRooms < desiredRoomNumber; i++)
        {
            currentRoom = spawnedRooms[i];
            currentRoomInfo = currentRoom.GetComponent<RoomInfo>();
            List<GameObject> nodes = currentRoomInfo.GetNodeList();

            int roomsSpawnedThisIteration = numberOfRooms;

            for (int j = 0; j < nodes.Count; j++)
            {
                if (nodes[j] == null)
                {
                    continue;
                }

                // spawn new rooms on each free node
                switch (nodes[j].name.ToLower())
                {
                    case "leftnode":
                        spawnedRoom = SpawnNextRoom(rightConnections, nodes[j].transform, "right");
                        currentRoomInfo.GetNodeList().Remove(currentRoomInfo.GetNode("left"));
                        break;
                    case "rightnode":
                        spawnedRoom = SpawnNextRoom(leftConnections, nodes[j].transform, "left");
                        currentRoomInfo.GetNodeList().Remove(currentRoomInfo.GetNode("right"));
                        break;
                    case "topnode":
                        spawnedRoom = SpawnNextRoom(bottomConnections, nodes[j].transform, "bottom");
                        currentRoomInfo.GetNodeList().Remove(currentRoomInfo.GetNode("top"));
                        break;
                    case "bottomnode":
                        spawnedRoom = SpawnNextRoom(topConnections, nodes[j].transform, "top");
                        currentRoomInfo.GetNodeList().Remove(currentRoomInfo.GetNode("bottom"));
                        break;
                    default:
                        spawnedRoom = null;
                        continue;
                }
                if (spawnedRoom == null)
                {
                    continue;
                }
                spawnedRooms.Add(spawnedRoom);
                currentRoomInfo.AddSubRoom(spawnedRoom);
                numberOfRooms++;
                j =0;
            }
            // Exit if dungeon is fully closed off and no new rooms can be spawned
            if (numberOfRooms == roomsSpawnedThisIteration)
            {
                print("Dungeon generation complete: no more available nodes.");
                break;
            }
        }

        CapOffHoles();

    }

    // PRIVATE METHODS

    /// <summary>
    /// Spawns a room from the given pool and connects it to the given node in the current room
    /// </summary>
    /// <param name="roomPool">The list of rooms to place</param>
    /// <param name="node">The attachment node of the current room</param>
    /// <param name="direction">The direction of the node to connect to the current room in the spawned room</param>
    /// <returns>The GameObject of the spawned room</returns>
    private GameObject SpawnNextRoom(List<GameObject> roomPool, Transform node, string direction)
    {
        // TODO: update logic to check for special rooms and maybe add some weighted randomness to the room selection

        // room selection logic
        int selectedRoomNum = Random.Range(0, roomPool.Count);
        GameObject spawnedRoom = Instantiate(roomPool[selectedRoomNum], roomsParent.transform);
        RoomInfo spawnedRoomInfo = spawnedRoom.GetComponent<RoomInfo>();

        // Position the spawned room so its matching node aligns with the parent node
        spawnedRoom.transform.position = node.position - spawnedRoomInfo.GetNode(direction).transform.localPosition;

        // Prevent spawning new rooms from the opposite side to avoid recursion
        spawnedRoomInfo.GetNodeList().Remove(spawnedRoomInfo.GetNode(direction));

        spawnedRoomInfo.SetParentNode(node);
        spawnedRoomInfo.SetSpawnedNode(spawnedRoomInfo.GetNode(direction).transform);

        return spawnedRoom;
    }

    /// <summary>
    /// Fills any remaining open nodes with single-connection end rooms to close off dead ends
    /// </summary>
    private void CapOffHoles()
    {
        GameObject spawnedRoom = null;
        List<GameObject> singleConnectionRooms;
        for (int i = 0; i < numberOfRooms; i++)
        {
            RoomInfo currentRoomInfo = spawnedRooms[i].GetComponent<RoomInfo>();
            List<GameObject> nodes = currentRoomInfo.GetNodeList();

            if (currentRoomInfo.GetNodeList().Count <= 0)
            {
                continue;
            }

            for (int j = nodes.Count - 1; j >= 0; j--)
            {
                if (nodes[j] == null)
                {
                    continue;
                }

                switch (nodes[j].name.ToLower())
                {
                    case "leftnode":
                        // Filter for single-connection rooms to ensure dead-end rooms are placed, not mid-corridor rooms
                        singleConnectionRooms = rightConnections.Where(room => room.GetComponent<RoomInfo>().GetNodeList().Count == 1).ToList();
                        spawnedRoom = SpawnNextRoom(singleConnectionRooms, nodes[j].transform, "right");
                        currentRoomInfo.GetNodeList().Remove(currentRoomInfo.GetNode("left"));
                        break;
                    case "rightnode":
                        singleConnectionRooms = leftConnections.Where(room => room.GetComponent<RoomInfo>().GetNodeList().Count == 1).ToList();
                        spawnedRoom = SpawnNextRoom(singleConnectionRooms, nodes[j].transform, "left");
                        currentRoomInfo.GetNodeList().Remove(currentRoomInfo.GetNode("right"));
                        break;
                    case "topnode":
                        singleConnectionRooms = bottomConnections.Where(room => room.GetComponent<RoomInfo>().GetNodeList().Count == 1).ToList();
                        spawnedRoom = SpawnNextRoom(singleConnectionRooms, nodes[j].transform, "bottom");
                        currentRoomInfo.GetNodeList().Remove(currentRoomInfo.GetNode("top"));
                        break;
                    case "bottomnode":
                        singleConnectionRooms = topConnections.Where(room => room.GetComponent<RoomInfo>().GetNodeList().Count == 1).ToList();
                        spawnedRoom = SpawnNextRoom(singleConnectionRooms, nodes[j].transform, "top");
                        currentRoomInfo.GetNodeList().Remove(currentRoomInfo.GetNode("bottom"));
                        break;
                    default:
                        spawnedRoom = null;
                        continue;
                }
                if (spawnedRoom == null)
                {
                    continue;
                }

                spawnedRooms.Add(spawnedRoom);
                currentRoomInfo.AddSubRoom(spawnedRoom);
                numberOfRooms++;
            }
        }
        // Populate enemies for all spawned rooms
        for (int i = 0; i < spawnedRooms.Count; i++)
        {
            RoomInfo roomInfo = spawnedRooms[i].GetComponent<RoomInfo>();
            PopulateEnemies(roomInfo);
        }

        OnDungeonComplete?.Invoke();
    }


    /// <summary>
    /// Populates the given room with random enemies from the enemy pool if it is not a safe room
    /// </summary>
    /// <param name="currentRoom">The room to populate with enemies</param>
    private void PopulateEnemies(RoomInfo currentRoom)
    {
        if (currentRoom.GetSafety())
            return;

        if (enemyPool.Count == 0)
        {
            Debug.LogWarning("Enemy pool is empty on Generator Prefab");
            return;
        }

        int enemyCount = Random.Range(minEnemies, maxEnemies);
        for (int i = 0; i < enemyCount; i++)
        {
            currentRoom.GetEnemies().Add(enemyPool[Random.Range(0, enemyPool.Count)]);
        }
    }

    private void CreateRoomPools()
    {
        // mapPool is assigned via the Inspector

        starterRooms = mapPool.Where(room => room.GetComponent<RoomInfo>().GetRoomType() == RoomTypes.Starter).ToList();
        leftConnections = mapPool.Where(room => room.GetComponent<RoomInfo>().GetNode("left") != null 
                                        && room.GetComponent<RoomInfo>().GetRoomType() != RoomTypes.Starter).ToList();
        rightConnections = mapPool.Where(room => room.GetComponent<RoomInfo>().GetNode("right") != null 
                                        && room.GetComponent<RoomInfo>().GetRoomType() != RoomTypes.Starter).ToList();
        topConnections = mapPool.Where(room => room.GetComponent<RoomInfo>().GetNode("top") != null 
                                        && room.GetComponent<RoomInfo>().GetRoomType() != RoomTypes.Starter).ToList();
        bottomConnections = mapPool.Where(room => room.GetComponent<RoomInfo>().GetNode("bottom") != null 
                                        && room.GetComponent<RoomInfo>().GetRoomType() != RoomTypes.Starter).ToList();
        shopRooms = mapPool.Where(room => room.GetComponent<RoomInfo>().GetRoomType() == RoomTypes.Shop).ToList();

    }

}
