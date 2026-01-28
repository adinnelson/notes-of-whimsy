using System.Collections.Generic;
using UnityEngine;

public class RoomInfo : MonoBehaviour
{
    public GameObject leftNode;
    public GameObject rightNode;
    public GameObject topNode;
    public GameObject bottomNode;

    public List<GameObject> Enemies;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
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
}
