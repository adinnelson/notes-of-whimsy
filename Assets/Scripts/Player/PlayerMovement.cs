using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerStats))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float acceleration = 1.25f;
    //[SerializeField] private float maxSpeed = 5.0f;

    private SpriteRenderer sprite;

    private InputSystem_Actions inputActions;
    private new Rigidbody2D rigidbody;
    private PlayerStats stats;

    private void Awake()
    {
        sprite = GetComponent<SpriteRenderer>();
    }

    void OnDisable()
    {
        inputActions.Player.Disable();
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

        rigidbody.linearVelocity = Vector2.Lerp(rigidbody.linearVelocity, targetVelocity, acceleration * Time.fixedDeltaTime);
    }
}
