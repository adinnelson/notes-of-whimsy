using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using NUnit.Framework.Interfaces;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

public class RoomInfo : MonoBehaviour
{
    [Header("Attachment Nodes")]
    public GameObject leftNode;
    public GameObject rightNode;
    public GameObject topNode;
    public GameObject bottomNode;

    [Header("Child Lists")]
    public List<GameObject> nodeList;
    public List<GameObject> subRooms;
    [SerializeField]
    private Transform parentNode;
    [SerializeField]
    private Transform spawnedNode;

    [Header("Enemy logic")]
    public List<GameObject> enemies;
    public bool IS_SAFE = false;

    


    //follows const naming since it will not change
    // controls whether or not enemies should spawn in this room

    public int NumberOfNodes {get; private set;}
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        for(int i =0; i < transform.childCount; i++)
        {
            if(transform.GetChild(i).name.ToLower() == "floor")
            {
                transform.GetChild(i).AddComponent<FloorInfo>();
                break;
            }
        }
        // GameObject floor = transform.GetChild(0).GameObject();

        // TilemapCollider2D floorTiles = floor.AddComponent<TilemapCollider2D>();
        // floorTiles.compositeOperation = Collider2D.CompositeOperation.Merge;
        // Rigidbody2D rb = floor.AddComponent<Rigidbody2D>();
        // rb.bodyType = RigidbodyType2D.Kinematic;
        // CompositeCollider2D compColl = floor.AddComponent<CompositeCollider2D>();
        // compColl.isTrigger = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (Keyboard.current.mKey.wasPressedThisFrame && parentNode != null)
        {
            transform.position = parentNode.position - spawnedNode.localPosition;
        }
    }

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

    public void SetParentNode(Transform parentNode)
    {
        this.parentNode = parentNode;
    }

    public Transform GetParentNode()
    {
        return parentNode;
    }

    public void RemoveNode(GameObject node)
    {
        nodeList.Remove(node);
    }

    public void SetSpawnedNode(Transform spawnedNode)
    {
        this.spawnedNode = spawnedNode;
    }

    public Transform GetSpawnedNode()
    {
        return spawnedNode;
    }

    public void AddSubRoom(GameObject room)
    {
        subRooms.Add(room);
    }

    public void ClearSubRooms()
    {
        for(int i = 0; i < subRooms.Count; i++)
        {
            subRooms[i].GetComponent<RoomInfo>().ClearSubRooms();
            Destroy(subRooms[i]);
        }
        subRooms.Clear();
    }

    public List<GameObject> GetNodeList()
    {
        return nodeList;
    }
}
