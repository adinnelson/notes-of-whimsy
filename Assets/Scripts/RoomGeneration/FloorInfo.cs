using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Tilemaps;

public class FloorInfo : MonoBehaviour
{
    public static event System.Action<GameObject> OnFloorOverlap;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        gameObject.tag = "Floor";
        TilemapCollider2D floorTiles = this.AddComponent<TilemapCollider2D>();
        floorTiles.compositeOperation = Collider2D.CompositeOperation.Merge;
        Rigidbody2D rb = this.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        CompositeCollider2D compColl = this.AddComponent<CompositeCollider2D>();
        compColl.isTrigger = true;   
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        print($"collision with {collision.name}");
        if(collision.CompareTag("Floor")) {
            print("good collision");
            OnFloorOverlap?.Invoke(gameObject);
        }
        
    }

    void OnTriggerStay2D(Collider2D collision)
    {
        print("now we lingering");
        if(collision.CompareTag("Floor")) {
            OnFloorOverlap?.Invoke(gameObject);
        }
    }
}

