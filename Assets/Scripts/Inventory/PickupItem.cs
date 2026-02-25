using UnityEngine;
using UnityEngine.InputSystem;
public class PickupItem : MonoBehaviour
{
    public SpellDataSO spell;

    private float pickupRange = 2.0f;

    private Transform player;

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    private void Update()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            Collider2D hit = Physics2D.OverlapPoint(mousePos);

            if (hit != null && hit.gameObject == gameObject)
            {
                float distance = Vector2.Distance(transform.position, player.position);
                if (distance <= pickupRange)
                {
                    SpellPickupUI.Instance.Show(this);
                }
            }
            else
            {
                return;
            }
        }
    }
}
