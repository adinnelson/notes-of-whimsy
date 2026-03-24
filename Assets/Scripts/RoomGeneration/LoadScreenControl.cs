using System.Collections;
using TMPro;
using UnityEngine;

public class LoadScreenControl : MonoBehaviour
{

    private bool playAnimation = true;
    [SerializeField]
    private const int animationSpeed = 120; // Adjust this value to control the speed of the animation
    private int animationIterator = 0;
    private TextMeshProUGUI textComponent;

    private GameObject loadScreen;

    void Awake()
    {
        loadScreen = transform.GetChild(0).gameObject; // Assuming the load screen is the first child
        loadScreen.SetActive(true);
        RoomGenerator.OnDungeonComplete += () => StartCoroutine(DeactivateLoadScreen());
        RoomGenerator.OnDungeonReset += ActivateLoadScreen;
        textComponent = loadScreen.GetComponentInChildren<TextMeshProUGUI>();
        if (textComponent == null)
        {
            Debug.LogError($"{name}: No TextMeshProUGUI component found in load screen.");
        }
    }

    void OnDestroy()
    {
        RoomGenerator.OnDungeonComplete -= () => StartCoroutine(DeactivateLoadScreen());
        RoomGenerator.OnDungeonReset -= ActivateLoadScreen;
    }

    private IEnumerator DeactivateLoadScreen()
    {
        yield return new WaitForSeconds(5f); // Adjust the delay as needed
        playAnimation = false;
        animationIterator = 0;
        loadScreen.SetActive(false);
    }
// todo: fix the fact that the load screen flickers when resetting the dungeon after completion. This is because the load screen is activated before the new floor is generated, so it is active for a brief moment before being deactivated by the DeactivateLoadScreen coroutine. One solution could be to add a delay before activating the load screen, or to only activate it after the new floor has been generated.
    private void ActivateLoadScreen()
    {
        if(playAnimation) return; // Prevent activating the load screen if it's already active
        playAnimation = true;
        animationIterator = 0;
        print("Activating load screen");
        loadScreen.SetActive(true);
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }
    // Update is called once per frame
    void Update()
    {
   
       
        if(textComponent != null && playAnimation)
        {
            if(animationIterator % animationSpeed == 0)
            {
                textComponent.text += '.'; 
            }
            if(animationIterator % (animationSpeed * 4) == 0)
            {
                textComponent.text = "Loading"; 
            }
        }
        animationIterator++;
    }


}
