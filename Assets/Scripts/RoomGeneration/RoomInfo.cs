using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

public class RoomInfo : MonoBehaviour
{
    [Header("TileMaps")]
    [SerializeField]
    private Tilemap floor;
    [SerializeField]
    private Tilemap walls;
    [SerializeField]
    private Tilemap roomLock;


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
    public bool IS_SAFE;

    

    public int NumberOfNodes {get; private set;}
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        RoomGenerator.OnDungeonComplete += HandleDungeonCompletion;
        for(int i =0; i < transform.childCount; i++)
        {
            string childName = transform.GetChild(i).name.ToLower();
            if(childName == "floor")
            {
                floor = transform.GetChild(i).GetComponent<Tilemap>();
                // transform.GetChild(i).AddComponent<FloorInfo>();
                break;
            }else if(childName == "walls")
            {
                walls = transform.GetChild(i).GetComponent<Tilemap>();
            }else if(childName == "roomLock")
            {
                roomLock = transform.GetChild(i).GetComponent<Tilemap>();
            }
        }


    }

    void OnDestroy()
    {
        RoomGenerator.OnDungeonComplete -= HandleDungeonCompletion;
    }

    private void HandleDungeonCompletion()
    {
        PopulateEnemies();
    }


    // Update is called once per frame
    void Update()
    {
        if (Keyboard.current.mKey.wasPressedThisFrame && parentNode != null)
        {
            transform.position = parentNode.position - spawnedNode.localPosition;
        }
    }

    private void PopulateEnemies()
    {
        foreach (GameObject enemy in enemies)
        {
            print("spawning");
            // FIXME: get better logic for finding a spot to spawn enemy
            Instantiate(enemy, floor.CellToWorld(Vector3Int.RoundToInt(floor.localBounds.center) + new Vector3Int(Random.Range(0,2),Random.Range(0,2), 0 )), Quaternion.identity,transform);
        }
    }

    public Tilemap GetFloor()
    {
        return floor;
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

    public bool GetSafety()
    {
        return IS_SAFE;
    }
}
