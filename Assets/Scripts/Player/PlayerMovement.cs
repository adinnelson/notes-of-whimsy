using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float acceleration = 1.25f;
    [SerializeField] private float maxSpeed = 5.0f;
    [SerializeField] private float drag = 1.0f;

    private InputSystem_Actions inputActions;
    private new Rigidbody2D rigidbody;

    void OnDisable()
    {
        inputActions.Player.Disable();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        inputActions = new InputSystem_Actions();
        rigidbody = GetComponent<Rigidbody2D>();
        inputActions.Player.Enable();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        MovePlayer();
        // Apply drag when no input is detected
        if (inputActions.Player.Move.ReadValue<Vector2>() == Vector2.zero)
        {
            ApplyDrag();
        }
        
    }

    private void ApplyDrag()
    {
        Vector2 dragForce = -rigidbody.linearVelocity.normalized * drag;
        if (dragForce.magnitude > rigidbody.linearVelocity.magnitude)
        {
            rigidbody.linearVelocity = Vector2.zero;
        }
        else
        {
            rigidbody.AddForce(dragForce, ForceMode2D.Impulse);
        }
    }

    private void MovePlayer()
    {
        float moveHorizontal = inputActions.Player.Move.ReadValue<Vector2>().x;
        float moveVertical = inputActions.Player.Move.ReadValue<Vector2>().y;

        Vector2 movement = new Vector2(moveHorizontal, moveVertical);
        Vector2 moveForce = movement * acceleration;

        rigidbody.AddForce(moveForce, ForceMode2D.Impulse);

        // Clamp player speed to maxSpeed
        if (rigidbody.linearVelocity.magnitude > maxSpeed)
        {
            rigidbody.linearVelocity = rigidbody.linearVelocity.normalized * maxSpeed;
        }
    }

}
