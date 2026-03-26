using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
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

    [SerializeField]
    private GameObject endPrefab;

    // CONSTANTS
    private const int ROOMS_SPAWNED_RESET_THRESHOLD = 0;

    // PRIVATE FIELDS
    [SerializeField]
    private List<GameObject> spawnedRooms;
    private GameObject roomsParent;
    private int numberOfRooms;

    [SerializeField]
    private int maxNumberOfCombatRooms = 1;
    private int combatRoomsSpawned = 0;
    private bool isGenerating = false;
    private bool hasSpawnedShop = false;
    private bool hasSpawnedCombatRoom = false;
    private bool needToReset = false;
    private bool hasCompletedDungeon = false;

    // EVENTS
    public static event System.Action OnDungeonComplete;
    public static event System.Action OnDungeonReset;

    // DEBUG
    [SerializeField]
    private bool allowRegeneration = true;

    private void Awake()
    {
        RoomInfo.OnFloorOverlap += HandleFloorOverlap;
        ProgressFloor.OnFloorProgressed += ResetGeneration;
    }

    private void Start()
    {
        
        CreateRoomPools();
        CreateFloorLayout();
    }

    private void Update()
    {
        if (Keyboard.current.lKey.wasPressedThisFrame && allowRegeneration)
        {
            ResetGeneration("reset from update due to key press or room count");
        }
        if(needToReset){
            // print("reset in update");
            needToReset = false;
            ResetGeneration("reset from update");
        }
    }

    private void OnDestroy()
    {
        RoomInfo.OnFloorOverlap -= HandleFloorOverlap;
        ProgressFloor.OnFloorProgressed -= ResetGeneration;
    }

    // PUBLIC METHODS

    /// <summary>
    /// Resets the dungeon generation by destroying the current layout and generating a new one
    /// </summary>
    public void ResetGeneration()
    {
        Destroy(roomsParent);
        roomsParent = null;
        print("Resetting floors generally");
        numberOfRooms = 0;
        spawnedRooms.Clear();
        combatRoomsSpawned = 0;
        hasSpawnedShop = false;
        hasSpawnedCombatRoom = false;
        RemoveRoomTypeFromRoomPools(RoomTypes.Shop);
        RemoveRoomTypeFromRoomPools(RoomTypes.Combat);
        needToReset = false;
        if(hasCompletedDungeon) OnDungeonReset?.Invoke();
        hasCompletedDungeon = false;
        CreateFloorLayout();
    }

    private void ResetGeneration(string specialString){
        print(specialString);
        ResetGeneration();

    }

    //get Minimap access to the generated rooms
    public List<GameObject> GetSpawnedRooms()
    {
        return spawnedRooms;
    }

    /// <summary>
    /// Handles floor overlap events by resetting generation when rooms overlap
    /// </summary>
    /// <param name="overlappingFloor">The floor GameObject that overlapped</param>
    private void HandleFloorOverlap(GameObject overlappingFloor)
    {
        needToReset = true;
        // ResetGeneration();
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
            if(numberOfRooms == 2)
            {
                AddRoomTypeToRoomPools(RoomTypes.Shop);
                AddRoomTypeToRoomPools(RoomTypes.Combat);
            }
            
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
        GameObject spawnedRoom = null;
        RoomInfo spawnedRoomInfo = null;
        // TODO: update logic to check for special rooms and maybe add some weighted randomness to the room selection
        // room selection logic
        int selectedRoomNum = Random.Range(0, roomPool.Count);
        spawnedRoom = Instantiate(roomPool[selectedRoomNum], roomsParent.transform);
        spawnedRoomInfo = spawnedRoom.GetComponent<RoomInfo>();

        if(spawnedRoomInfo.GetRoomType() == RoomTypes.Shop && !hasSpawnedShop)
        {
            hasSpawnedShop = true;
            RemoveRoomTypeFromRoomPools(RoomTypes.Shop);
        }
        else if(spawnedRoomInfo.GetRoomType() == RoomTypes.Combat && combatRoomsSpawned < maxNumberOfCombatRooms)
        {
            combatRoomsSpawned++;
        }
        else if(spawnedRoomInfo.GetRoomType() == RoomTypes.Combat && combatRoomsSpawned >= maxNumberOfCombatRooms)
        {
            hasSpawnedCombatRoom = true;
            RemoveRoomTypeFromRoomPools(RoomTypes.Combat);
        }
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
        
        GameObject endRoom = spawnedRooms[spawnedRooms.Count - 1];
        RoomInfo endRoomInfo = endRoom.GetComponent<RoomInfo>();
        endRoomInfo.SetRoomType(RoomTypes.End);
        endRoomInfo.SetSafety(true);
        Instantiate(endPrefab, endRoom.transform.position, Quaternion.identity, endRoom.transform);
        // Populate enemies for all spawned rooms
        for (int i = 0; i < spawnedRooms.Count; i++)
        {
            RoomInfo roomInfo = spawnedRooms[i].GetComponent<RoomInfo>();

            if(roomInfo.GetSafety()) continue;

            roomInfo.SetEnemyPrefabs(enemyPool);
        }

        if(spawnedRooms.Count < minRooms) needToReset = true;

        if(needToReset)
        {
            return;
        }

        OnDungeonComplete?.Invoke();
        LoadScreenControl.TurnOffLoadScreen();
        hasCompletedDungeon = true;
    }

    private void CreateRoomPools()
    {
        // mapPool is assigned via the Inspector
        for(int i = 0; i < mapPool.Count; i++)
        {
            GameObject roomToMove = mapPool[i];
            RoomInfo roomToMoveInfo = roomToMove.GetComponent<RoomInfo>();
            
            RoomTypes roomType = roomToMoveInfo.GetRoomType();
            bool isNotStarterOrShop = roomType != RoomTypes.Starter && roomType != RoomTypes.Shop && roomType != RoomTypes.Combat;
            bool hasLeftNode = roomToMoveInfo.GetNode("left") != null;
            bool hasRightNode = roomToMoveInfo.GetNode("right") != null;
            bool hasTopNode = roomToMoveInfo.GetNode("top") != null;
            bool hasBottomNode = roomToMoveInfo.GetNode("bottom") != null;
            
            if (roomType == RoomTypes.Starter)
            starterRooms.Add(roomToMove);
            
            if (isNotStarterOrShop && hasLeftNode)
            leftConnections.Add(roomToMove);
            
            if (isNotStarterOrShop && hasRightNode)
            rightConnections.Add(roomToMove);
            
            if (isNotStarterOrShop && hasTopNode)
            topConnections.Add(roomToMove);
            
            if (isNotStarterOrShop && hasBottomNode)
            bottomConnections.Add(roomToMove);
            
            if (roomType == RoomTypes.Shop)
            shopRooms.Add(roomToMove);
        }

    }

    private void AddRoomTypeToRoomPools(RoomTypes roomType)
    {
        for(int i = 0; i < mapPool.Count; i++)
        {
            GameObject roomToMove = mapPool[i];
            RoomInfo roomToMoveInfo = roomToMove.GetComponent<RoomInfo>();

            bool hasLeftNode = roomToMoveInfo.GetNode("left") != null;
            bool hasRightNode = roomToMoveInfo.GetNode("right") != null;
            bool hasTopNode = roomToMoveInfo.GetNode("top") != null;
            bool hasBottomNode = roomToMoveInfo.GetNode("bottom") != null;

            if (roomToMoveInfo.GetRoomType() == roomType)
            {
                if (hasBottomNode)
                {
                    bottomConnections.Add(roomToMove);
                }
                if (hasTopNode)
                {
                    topConnections.Add(roomToMove);
                }
                if (hasRightNode)
                {
                    rightConnections.Add(roomToMove);
                }
                if (hasLeftNode)
                {
                    leftConnections.Add(roomToMove);
                }
            }
        }
    }

    private void RemoveRoomTypeFromRoomPools(RoomTypes roomType)
    {
        for(int i = 0; i < mapPool.Count; i++)
        {
            GameObject roomToMove = mapPool[i];
            RoomInfo roomToMoveInfo = roomToMove.GetComponent<RoomInfo>();

            bool hasLeftNode = roomToMoveInfo.GetNode("left") != null;
            bool hasRightNode = roomToMoveInfo.GetNode("right") != null;
            bool hasTopNode = roomToMoveInfo.GetNode("top") != null;
            bool hasBottomNode = roomToMoveInfo.GetNode("bottom") != null;

            if (roomToMoveInfo.GetRoomType() == roomType)
            {
                if (hasBottomNode)
                {
                    bottomConnections.Remove(roomToMove);
                }
                if (hasTopNode)
                {
                    topConnections.Remove(roomToMove);
                }
                if (hasRightNode)
                {
                    rightConnections.Remove(roomToMove);
                }
                if (hasLeftNode)
                {
                    leftConnections.Remove(roomToMove);
                }
            }
        }
    }

    
}
