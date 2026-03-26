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
        if (player == null)
        {
            return;
        }

        float distance = Vector2.Distance(transform.position, player.position);

        if (distance <= pickupRange && Keyboard.current.eKey.wasPressedThisFrame)
        {
            SpellPickupUI.Instance.Show(this);
        }
    }
}
