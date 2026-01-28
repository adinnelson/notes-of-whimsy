using System;
using System.Collections;
using System.Collections.Generic;
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
        StartCoroutine(CreateFloorLayout());
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
            // Destroy(roomsParent);
            print("resetting floors");
            CreateFloorLayout();
        }
    }

    /// <summary>
    /// todo: implement
    /// step 1: spawn starter room
    /// step 2: outer loop 
    /// </summary>
    private IEnumerator CreateFloorLayout()
    {
        GameObject spawnedRoom = null;

        if (roomsParent == null)
        {
        roomsParent = new GameObject("Floor Layout");
        roomsParent.transform.SetParent(grid.transform);
        roomsParent.transform.position.Set(0,0,0);
        }
        // pull from the starter rooms list and spawn one at random

        // todo: add randomization
        GameObject currentRoom = Instantiate(starterRooms[0], roomsParent.transform);
        spawnedRooms.Add(currentRoom);
        numberOfRooms++;
        Transform[] nodes = currentRoom.GetComponentsInChildren<Transform>();
        // todo: loop through room spawn points and pick a room to attach
        for (; numberOfRooms < desiredRoomNumber;)
        {
            // todo: add randomization
            currentRoom = spawnedRooms[numberOfRooms-1];
            // spawnedRooms.Add(currentRoom);
            nodes = currentRoom.GetComponentsInChildren<Transform>();
            foreach (var node in nodes)
            {
                if(node == null) continue;
                print("starting wait");
                yield return new WaitForSeconds(0.5f);
                
                // spawn new rooms on each free node 
                switch (node.name.ToLower())
                {
                    case "leftnode":
                    
                        print(node.name);
                        // todo: add randomness and expand                      from here to set data separately so that we can use node data from the room
                        spawnedRoom = Instantiate(rightConnections[0],node.position - rightConnections[0].GetComponent<RoomInfo>().GetNode("right").transform.position,
                                                    Quaternion.identity,roomsParent.transform);
                        spawnedRoom.name = node.name + "spawned" + numberOfRooms;
                        Destroy(spawnedRoom.GetComponent<RoomInfo>().GetNode("right"));
                        Destroy(currentRoom.GetComponent<RoomInfo>().GetNode("left"));
                        numberOfRooms++;
                    break;
                    case "rightnode":
                    print(node.name);
                        // todo: add randomness and expand                      from here to set data separately so that we can use node data from the room
                        spawnedRoom = Instantiate(leftConnections[0],node.position - leftConnections[0].GetComponent<RoomInfo>().GetNode("left").transform.position,
                                                    Quaternion.identity,roomsParent.transform);
                        spawnedRoom.name = node.name + "spawned" + numberOfRooms;
                        Destroy(spawnedRoom.GetComponent<RoomInfo>().GetNode("left")); 
                        Destroy(currentRoom.GetComponent<RoomInfo>().GetNode("right"));
                        numberOfRooms++;
                    break;
                    case "topnode":
                    print(node.name);
                        // todo: add randomness and expand                      from here to set data separately so that we can use node data from the room
                        spawnedRoom = Instantiate(bottomConnections[0],node.position - bottomConnections[0].GetComponent<RoomInfo>().GetNode("bottom").transform.position,
                                                    Quaternion.identity,roomsParent.transform);
                        spawnedRoom.name = node.name + "spawned" + numberOfRooms;
                        Destroy(spawnedRoom.GetComponent<RoomInfo>().GetNode("bottom")); 
                        Destroy(currentRoom.GetComponent<RoomInfo>().GetNode("top"));
                        numberOfRooms++;
                    break;
                    case "bottomnode":
                    print(node.name);
                        // todo: add randomness and expand                      from here to set data separately so that we can use node data from the room
                        spawnedRoom = Instantiate(topConnections[0],node.position - topConnections[0].GetComponent<RoomInfo>().GetNode("top").transform.position,
                                                    Quaternion.identity,roomsParent.transform);
                        spawnedRoom.name = node.name + "spawned" + numberOfRooms;
                        Destroy(spawnedRoom.GetComponent<RoomInfo>().GetNode("top")); 
                        Destroy(currentRoom.GetComponent<RoomInfo>().GetNode("bottom"));
                        numberOfRooms++;
                    break;
                    default:
                    spawnedRoom = null;
                    continue;
                }
            
                spawnedRooms.Add(spawnedRoom);
            }


        }



    }


}
