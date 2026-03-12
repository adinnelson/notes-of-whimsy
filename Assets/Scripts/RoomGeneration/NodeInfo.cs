using Unity.VisualScripting;
using UnityEngine;

public class NodeInfo : MonoBehaviour
{
    private RoomInfo parentRoomInfo;

    void Start()
    {
        parentRoomInfo = this.GetComponentInParent<RoomInfo>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        // tells the room if a node connection has been made as a result of the room choice
        if(collision.CompareTag("Room Node"))
        {
            // print(collision.name);
            
            string oppositeNode = collision.name.ToLower() switch
            {
            "left" => "right",
            "right" => "left",
            "top" => "bottom",
            "bottom" => "top",
            _ => null,
            };
            // collision.GetComponent<NodeInfo>().GetParentInfo().RemoveNode(collision.GameObject());

            // collision.GetComponent<NodeInfo>().GetParentInfo().RemoveNode(collision.gameObject);
            // print(parentRoomInfo.GetNodeList());
            parentRoomInfo.RemoveNode(this.GameObject());
            
            // print(collision.name);
        }

    }

    public RoomInfo GetParentInfo()
    {
        return parentRoomInfo;
    }
}
