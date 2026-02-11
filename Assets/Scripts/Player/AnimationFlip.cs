using UnityEngine;
using UnityEngine.InputSystem;
public class AnimationFlip : MonoBehaviour
{


    private Animator animator;
    private Rigidbody2D rigidbody;

    private Transform PlayerTransform;

    void Start()
    {
        animator = GetComponent<Animator>();
        rigidbody = GetComponent<Rigidbody2D>();
        PlayerTransform = GetComponent<Transform>();
    }

    void Update()
    {
        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        if(mousePosition.x > PlayerTransform.position.x)
        {
            Debug.Log(mousePosition.x);
            animator.SetBool("CharFaceLeft", true);
        }
        else
        {
            Debug.Log(mousePosition.x);
            animator.SetBool("CharFaceLeft", false);
        }
        rigidbody.linearVelocity = new Vector2(rigidbody.linearVelocity.x, rigidbody.linearVelocity.y);
        animator.SetFloat("Speed", Mathf.Abs(rigidbody.linearVelocity.x));
    }
   
}
