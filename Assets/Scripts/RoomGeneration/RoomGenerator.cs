using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework.Internal;
using Unity.VisualScripting;
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
            
            // Break if no rooms were spawned this iteration (dungeon is closed off)
            if (numberOfRooms == roomsSpawnedThisIteration)
            {
                print("Dungeon generation complete: no more available nodes.");
                break;
            }
        }
        
        CapOffHoles();
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


}
