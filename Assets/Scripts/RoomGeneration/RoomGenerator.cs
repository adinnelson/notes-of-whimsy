using System;
using System.Collections.Generic;
using UnityEngine;

public class RoomGenerator : MonoBehaviour
{

    [SerializeField]
    private int numberOfRooms = 0;

    [SerializeField]
    private int desiredRoomNumber = 10;

    [SerializeField]
    private List<GameObject> enemies;

    [SerializeField]
    private List<GameObject> rooms = new List<GameObject>();

    public Dictionary<System.Object, int> enemyValues;

    public struct enemy{
        string name;
        int spawnValue;
    }

     

/*
idea is to make it work for any size and shape room to have creativity and releave staleness of levels
*/
    void Awake()
    {

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
        
    }


}
