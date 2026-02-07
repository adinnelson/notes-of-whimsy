using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework.Interfaces;
using UnityEngine;
using UnityEngine.InputSystem;

public class RoomInfo : MonoBehaviour
{
    public GameObject leftNode;
    public GameObject rightNode;
    public GameObject topNode;
    public GameObject bottomNode;

    public Transform parentNode;
    public Transform spawnedNode;

    public List<GameObject> nodeList;

    public List<GameObject> Enemies;

    public int NumberOfNodes {get; private set;}
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
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

    public void SetSpawnedNode(Transform spawnedNode)
    {
        this.spawnedNode = spawnedNode;
    }
}
