using System.Collections;
using System.Collections.Generic;
using NUnit.Framework.Internal;
using UnityEngine;
using UnityEngine.InputSystem;

public class RoomGenerator : MonoBehaviour
{

    [Header("Object References")]
    [SerializeField]
    private GameObject roomsParent;
    
    [SerializeField]
    private GameObject grid;

    [Header("Layout Controls")]
    [SerializeField]
    private int numberOfRooms;

    // the number of rooms before generation stops not the end number of rooms
    [SerializeField]
    private int desiredRoomNumber;

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



    private InputSystem_Actions inputs;



/*
idea is to make it work for any size and shape room to have creativity and releave staleness of levels
*/
    void Awake()
    {
        inputs = new();
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
        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            Destroy(roomsParent);
            roomsParent = null;
            print("resetting floors");
            numberOfRooms = 0;
            spawnedRooms.Clear();
            CreateFloorLayout();
        }
    }

    /// <summary>
    /// todo: implement
    /// step 1: spawn starter room
    /// step 2: outer loop 
    /// </summary>
    private void CreateFloorLayout()
    {
        GameObject spawnedRoom = null;
        RoomInfo spawnedRoomInfo = null;
        RoomInfo currentRoomInfo = null;
        int roomSelector = 0;

        if (roomsParent == null)
        {
        roomsParent = new GameObject("Floor Layout");
        roomsParent.transform.SetParent(grid.transform);
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
            // fixme: find a way to have this section not hang
            print(i);
            // todo: add randomization
            currentRoom = spawnedRooms[i];
            currentRoomInfo = currentRoom.GetComponent<RoomInfo>();
            // spawnedRooms.Add(currentRoom);
            List<GameObject> nodes = currentRoomInfo.nodeList;
            
            int roomsSpawnedThisIteration = numberOfRooms;
            
            for(int j = 0; j < nodes.Count; j++)
            {
                if(nodes[j] == null) continue;
                print("starting wait");
                
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
                spawnedRooms.Add(spawnedRoom);
                numberOfRooms++;
            }
            
            // Break if no rooms were spawned this iteration (dungeon is closed off)
            if (numberOfRooms == roomsSpawnedThisIteration)
            {
                print("Dungeon generation complete: no more available nodes.");
                break;
            }
        }
        
        // CapOffHoles();
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
        print(node.name);

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
        for(int i = 0; i < numberOfRooms; i++)
        {
            RoomInfo currentRoomInfo = spawnedRooms[i].GetComponent<RoomInfo>();
            if(currentRoomInfo.nodeList.Count <= 0) continue;

            foreach (var node in currentRoomInfo.nodeList)
            {
                switch (node.name.ToLower())
                {
                    case "leftnode":
                        // Add logic for left node
                        List<GameObject> intersection = new();
                        foreach (var room in starterRooms)
                        {
                            if (rightConnections.Contains(room))
                            {
                                intersection.Add(room);
                            }
                        }
                        if (intersection.Count > 0)
                        {
                            int intersectionSelector = (int)Random.Range(0f, intersection.Count);
                            GameObject spawnedRoom = Instantiate(intersection[intersectionSelector], roomsParent.transform);
                            RoomInfo spawnedRoomInfo = spawnedRoom.GetComponent<RoomInfo>();
                            spawnedRoom.transform.position = node.transform.position - spawnedRoomInfo.GetNode("right").transform.localPosition;
                            spawnedRoom.name = node.name + "spawned" + numberOfRooms;
                            spawnedRoomInfo.nodeList.Remove(spawnedRoomInfo.GetNode("right"));
                            spawnedRooms.Add(spawnedRoom);
                            // numberOfRooms++;
                        }

                        break;
                    case "rightnode":
                        // Add logic for right node
                        
                        break;
                    case "topnode":
                        // Add logic for top node
                        break;
                    case "bottomnode":
                        // Add logic for bottom node
                        break;
                    default:
                        break;
                } }
        }
    }


}
