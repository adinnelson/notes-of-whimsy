using UnityEngine;
using UnityEngine.InputSystem;
public class PickupItem : MonoBehaviour
{
    public SpellDataSO spell;
    public int beatId;

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
            Collider2D[] hits = Physics2D.OverlapPointAll(mousePos);

            foreach (Collider2D hit in hits)
            {
                if (hit.gameObject == gameObject)
                {
                    float distance = Vector2.Distance(transform.position, player.position);
                    if (distance <= pickupRange)
                    {
                        SpellPickupUI.Instance.Show(this);
                    }
                    break;
                }
            }
        }
    }
}
