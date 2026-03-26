using System.Collections;
using TMPro;
using UnityEngine;

public class LoadScreenControl : MonoBehaviour
{

    private bool playAnimation = true;
    [SerializeField]
    private const int animationSpeed = 120; // Adjust this value to control the speed of the animation
    private int animationIterator = 0;
    private GameObject loadScreen;
    [SerializeField]
    private float loadScreenWaitTime = 3f;
    private TextMeshProUGUI textComponent;

    [SerializeField]
    private static bool turnOffLoadScreen = false;

    void Awake()
    {
        loadScreen = transform.GetChild(0).gameObject; // Assuming the load screen is the first child
        if(loadScreen == null)
        {
            Debug.LogError($"{name}: No child game object found for load screen.");
        }
        loadScreen.SetActive(true);
        // RoomGenerator.OnDungeonComplete += () => StartCoroutine(DeactivateLoadScreen());
        RoomGenerator.OnDungeonReset += ActivateLoadScreen;
        textComponent = loadScreen.GetComponentInChildren<TextMeshProUGUI>();
        if (textComponent == null)
        {
            Debug.LogError($"{name}: No TextMeshProUGUI component found in load screen.");
        }
    }

    void OnDestroy()
    {
        
        RoomGenerator.OnDungeonReset -= ActivateLoadScreen;
    }

    private IEnumerator DeactivateLoadScreen()
    {
        yield return new WaitForSeconds(loadScreenWaitTime); // Adjust the delay as needed
        playAnimation = false;
        animationIterator = 0;
        loadScreen.SetActive(false);
    }

    private void ActivateLoadScreen()
    {
        if(playAnimation) return; // Prevent activating the load screen if it's already active
        playAnimation = true;
        animationIterator = 0;
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
            if(animationIterator % (animationSpeed * 5) == 0)
            {
                textComponent.text = "Loading"; 
            }
        }
        animationIterator++;
        if(turnOffLoadScreen)
        {
            StartCoroutine(DeactivateLoadScreen());
            turnOffLoadScreen = false;
        }
    }

    // Had to be handled this way because event tie ins weren't working 
    // 
    public static void TurnOffLoadScreen()
    {
        turnOffLoadScreen = true;
    }


}
