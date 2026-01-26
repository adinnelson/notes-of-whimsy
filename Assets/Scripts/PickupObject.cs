using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class pickupObject : MonoBehaviour
{
    [Header("Inventory UI")]
    [SerializeField] private Transform contentsParent;
    [SerializeField] private GameObject templateIcon;

    private void OnTriggerEnter2D(Collider2D objToPickup)
    {
        Debug.Log("Trigger entered by: " + objToPickup.name);

        if (!objToPickup.CompareTag("puObj"))
        {
            return;
        }

        SpawnIcon();
        //destroy the object from the world
        Destroy(objToPickup.gameObject);
    }

    void SpawnIcon()
    {
        GameObject newIconFromTemplate = Instantiate(templateIcon, contentsParent);
        newIconFromTemplate.SetActive(true);
    }
}
