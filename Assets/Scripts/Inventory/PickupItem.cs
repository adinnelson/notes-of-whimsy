using UnityEngine;
using UnityEngine.InputSystem;
public class PickupItem : MonoBehaviour
{
    public SpellDataSO spell;

    private void Update()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            Collider2D hit = Physics2D.OverlapPoint(mousePos);

            if (hit != null && hit.gameObject == gameObject)
            {
                Debug.Log($"Clicked on spell: {spell.name}");
                SpellPickupUI.Instance.Show(this);
            }
        }
    }
}
