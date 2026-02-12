
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

public class RoomGenerator : MonoBehaviour
{
    [SerializeField]
    private List<GameObject> enemyPool ;

    [Header("Object References")]
    private GameObject roomsParent;


    [Header("Layout Controls")]
    [SerializeField]
    private int numberOfRooms;
    // the number of rooms before generation stops not the end number of rooms
    [SerializeField]
    private int desiredRoomNumber;
    [SerializeField]
    private int minRooms;
    [SerializeField]
    private int minEnemies;
    [SerializeField]
    private int maxEnemies;

    [SerializeField]
    private List<GameObject> spawnedRooms;


    [Header("Room Pools")]
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


    public static event System.Action OnDungeonComplete;


    // DEBUG VARIABLE FOR ALLOWING REGENERATION
    public bool allowRegeneration = true;

/*
idea is to make it work for any size and shape room to have creativity and releave staleness of levels
*/
    void Awake()
    {
        FloorInfo.OnFloorOverlap += HandleFloorOverlap;
        CreateFloorLayout();
    }
    // remembird
    // tre'sombre'd

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (Keyboard.current.lKey.wasPressedThisFrame && allowRegeneration
            || spawnedRooms.Count < minRooms)
        {
            Destroy(roomsParent);
            roomsParent = null;
            print("resetting floors");
            numberOfRooms = 0;
            spawnedRooms.Clear();
            CreateFloorLayout();
        }
    }

    void OnDestroy()
    {
        FloorInfo.OnFloorOverlap -= HandleFloorOverlap;
    }

    void HandleFloorOverlap(GameObject overlappingFloor)
    {
        // TODO: Handle floor overlap here
        Debug.LogWarning($"Floor overlap detected with {overlappingFloor.name}", overlappingFloor);
    }
    private void CreateFloorLayout()
    {
        GameObject spawnedRoom = null;
        RoomInfo spawnedRoomInfo = null;
        RoomInfo currentRoomInfo = null;
        int roomSelector = 0;

        if (roomsParent == null)
        {
        roomsParent = new GameObject("Floor Layout");
        roomsParent.transform.SetParent(transform);
        roomsParent.transform.position.Set(0,0,0);
        }
        // pull from the starter rooms list and spawn one at random

        // todo: add randomization
        roomSelector = Random.Range(0, starterRooms.Count);
        GameObject currentRoom = Instantiate(starterRooms[roomSelector], roomsParent.transform);
        spawnedRooms.Add(currentRoom);
        numberOfRooms++;
        // Transform[] nodes = currentRoom.GetComponentsInChildren<Transform>();
        // todo: loop through room spawn points and pick a room to attach
        for (int i = 0; numberOfRooms < desiredRoomNumber; i++)
        {
            currentRoom = spawnedRooms[i];
            currentRoomInfo = currentRoom.GetComponent<RoomInfo>();
            List<GameObject> nodes = currentRoomInfo.nodeList;

            int roomsSpawnedThisIteration = numberOfRooms;

            for(int j = 0; j < nodes.Count; j++)
            {
                if(nodes[j] == null) continue;

                // spawn new rooms on each free node
                switch (nodes[j].name.ToLower())
                {
                    case "leftnode":
                        spawnedRoom = SpawnNextRoom(rightConnections, nodes[j].transform, "right");
                        currentRoomInfo.nodeList.Remove(currentRoomInfo.GetNode("left"));
                    break;
                    case "rightnode":
                        spawnedRoom = SpawnNextRoom(leftConnections, nodes[j].transform, "left");
                        currentRoomInfo.nodeList.Remove(currentRoomInfo.GetNode("right"));
                    break;
                    case "topnode":
                        spawnedRoom = SpawnNextRoom(bottomConnections, nodes[j].transform, "bottom");
                        currentRoomInfo.nodeList.Remove(currentRoomInfo.GetNode("top"));
                    break;
                    case "bottomnode":
                        spawnedRoom = SpawnNextRoom(topConnections, nodes[j].transform, "top");
                        currentRoomInfo.nodeList.Remove(currentRoomInfo.GetNode("bottom"));
                    break;
                    default:
                    spawnedRoom = null;
                    continue;
                }
                if(spawnedRoom == null) continue;
                spawnedRooms.Add(spawnedRoom);
                currentRoomInfo.AddSubRoom(spawnedRoom);
                numberOfRooms++;
                j =0;
            }
            // TODO: find best spot for this
            PopulateEnemies(currentRoomInfo);
            // Break if no rooms were spawned this iteration (dungeon is closed off)
            if (numberOfRooms == roomsSpawnedThisIteration)
            {
                print("Dungeon generation complete: no more available nodes.");
                break;
            }
        }

        CapOffHoles();
        OnDungeonComplete?.Invoke();

    }

    /// <summary>
    /// Handles the creation and placement of the next room to spawn
    /// </summary>
    /// <param name="roomPool">the list of rooms to place</param>
    /// <param name="node">the attachment node of the current room</param>
    /// <param name="direction">The direction of the node to connect to the current room in the spawned room</param>
    /// <returns>The GameObject of the spawned room </returns>
    private GameObject SpawnNextRoom(List<GameObject> roomPool, Transform node, string direction)
    {
        // do check for valid room here so the room can be changed here instead of later
        int selectedRoomNum = Random.Range(0, roomPool.Count);
        // RoomInfo testRoomInfo = roomPool[selectedRoomNum].GetComponent<RoomInfo>();
        // // make sure the dungeon isnt instantly capped off
        // while(testRoomInfo.nodeList.Count == 1)
        // {
        //     print("bad room choice go againe " + roomPool.Count);
        //     selectedRoomNum = Random.Range(0, roomPool.Count);
        //     testRoomInfo = roomPool[selectedRoomNum].GetComponent<RoomInfo>();
        // }
        GameObject spawnedRoom = Instantiate(roomPool[selectedRoomNum],roomsParent.transform);
        RoomInfo spawnedRoomInfo = spawnedRoom.GetComponent<RoomInfo>();

        spawnedRoom.transform.position = node.position - spawnedRoom.GetComponent<RoomInfo>().GetNode(direction).transform.localPosition;
        spawnedRoom.name = node.name + "spawned" + numberOfRooms;
        spawnedRoomInfo.nodeList.Remove(spawnedRoomInfo.GetNode(direction));
        spawnedRoomInfo.SetParentNode(node);
        spawnedRoomInfo.SetSpawnedNode(spawnedRoomInfo.GetNode(direction).transform);

        return spawnedRoom;
    }

    /// <summary>
    /// todo: implement
    /// loop through all created rooms
    /// check to see if the nodes still exist
    /// yes? => add a room with 1 connection to it
    /// no go next
    /// </summary>
    private void CapOffHoles()
    {
        GameObject spawnedRoom = null;
        List<GameObject> intersection;
        for(int i = 0; i < numberOfRooms; i++)
        {
            RoomInfo currentRoomInfo = spawnedRooms[i].GetComponent<RoomInfo>();
            List<GameObject> nodes = currentRoomInfo.nodeList;

            if(currentRoomInfo.nodeList.Count <= 0) continue;

            for(int j = 0; j < nodes.Count; j++)
            {
                if(nodes[j] == null) continue;

                // spawn new rooms on each free node
                switch (nodes[j].name.ToLower())
                {
                    case "leftnode":
                        intersection = rightConnections.Intersect(starterRooms).ToList();
                        spawnedRoom = SpawnNextRoom(intersection, nodes[j].transform, "right");
                        currentRoomInfo.nodeList.Remove(currentRoomInfo.GetNode("left"));
                    break;
                    case "rightnode":
                        intersection = leftConnections.Intersect(starterRooms).ToList();
                        spawnedRoom = SpawnNextRoom(intersection, nodes[j].transform, "left");
                        currentRoomInfo.nodeList.Remove(currentRoomInfo.GetNode("right"));
                    break;
                    case "topnode":
                        intersection = bottomConnections.Intersect(starterRooms).ToList();
                        spawnedRoom = SpawnNextRoom(intersection, nodes[j].transform, "bottom");
                        currentRoomInfo.nodeList.Remove(currentRoomInfo.GetNode("top"));
                    break;
                    case "bottomnode":
                        intersection = topConnections.Intersect(starterRooms).ToList();
                        spawnedRoom = SpawnNextRoom(intersection, nodes[j].transform, "top");
                        currentRoomInfo.nodeList.Remove(currentRoomInfo.GetNode("bottom"));
                    break;
                    default:
                    spawnedRoom = null;
                    continue;
                }
                if(spawnedRoom == null) continue;
                spawnedRooms.Add(spawnedRoom);
                currentRoomInfo.AddSubRoom(spawnedRoom);
                numberOfRooms++;
                j =0;
            }
        }
    }


    //spawns enemies into rooms
    // TODO: re-implement with proper logic
    private void PopulateEnemies(RoomInfo currentRoom)
    {
        int enemyCount = Random.Range(minEnemies,maxEnemies);
        // HACK: choose a random number of enemies to add to the rooms list of enemies
        Tilemap floor = currentRoom.GetFloor();
        // print(floor.cellBounds.center);

        if (currentRoom.GetSafety() == false)
        {
            if(enemyPool.Count > 0)
            {
                // FIXME: change to GetEnemies once implemented also add randomization
                for(int i = 0; i < enemyCount; i++)
                {

                currentRoom.enemies.Add(enemyPool[Random.Range(0, enemyPool.Count)]);
                }

            }
            else
            {
                Debug.LogWarning("Fill out Enemy pool on Generator");
            }
        }
    }

}
