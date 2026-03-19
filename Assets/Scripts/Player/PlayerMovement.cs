using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerStats))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float acceleration = 1.25f;
    //[SerializeField] private float maxSpeed = 5.0f;
    private float scaleAccelerationWithSpeedBoost = 1.0f;

    private SpriteRenderer sprite;

    private InputSystem_Actions inputActions;
    private new Rigidbody2D rigidbody;
    private PlayerStats stats;



    private void Awake()
    {
        sprite = GetComponent<SpriteRenderer>();
        RoomGenerator.OnDungeonComplete += OnDungeonComplete;
    }

    void OnDisable()
    {
        inputActions.Player.Disable();

    }

    void OnDestroy()
    {
        RoomGenerator.OnDungeonComplete -= OnDungeonComplete;        
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        inputActions = new InputSystem_Actions();
        rigidbody = GetComponent<Rigidbody2D>();
        stats = GetComponent<PlayerStats>();

        if (stats == null)
        {
            Debug.LogError("PlayerStats component missing!");
        }

        inputActions.Player.Enable();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        MovePlayer();
    }

    private void OnDungeonComplete()
    {
        transform.position = Vector3.zero;
    }

    private void MovePlayer()
    {
        float moveHorizontal = inputActions.Player.Move.ReadValue<Vector2>().x;
        float moveVertical = inputActions.Player.Move.ReadValue<Vector2>().y;

        Vector2 movement = new Vector2(moveHorizontal, moveVertical);
        if (movement.magnitude > 1)
        {
            movement.Normalize();
        }

        if (moveHorizontal > 0)
        {
            sprite.flipX = false;
        }
        else if (moveHorizontal < 0)
        {
            sprite.flipX = true;
        }

        Vector2 targetVelocity = movement * stats.Speed;

        scaleAccelerationWithSpeedBoost = acceleration * (stats.Speed / stats.BaseSpeed); //scale the acceleration with a speed boost
        //Debug.Log($"BaseSpeed: {stats.BaseSpeed}, TotalSpeed: {stats.Speed}");
        rigidbody.linearVelocity = Vector2.Lerp(rigidbody.linearVelocity, targetVelocity, scaleAccelerationWithSpeedBoost * Time.fixedDeltaTime);
        //rigidbody.linearVelocity = Vector2.Lerp(rigidbody.linearVelocity, targetVelocity, acceleration * Time.fixedDeltaTime);
    }
}
