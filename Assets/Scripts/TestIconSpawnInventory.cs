using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

//script is temporary to test out dynamic addition of icons to the inventory UI
//script is completely disconnected from any game play, and is solely to test inventory UI behaviour
public class TestIconSpawnInventory : MonoBehaviour
{
    [SerializeField] private Transform contentsParent;
    [SerializeField] private GameObject templateIcon;

    void Update ()
    {
        if (Keyboard.current.iKey.wasPressedThisFrame)
        {
            SpawnIcon();
        }
    }

    void SpawnIcon()
    {
        GameObject newIconFromTemplate = Instantiate(templateIcon, contentsParent);
        newIconFromTemplate.SetActive(true);
    }
}
