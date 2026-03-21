using System.Collections;
using UnityEngine;

public class LoadScreenControl : MonoBehaviour
{

    private bool playAnimation = false;

    private GameObject loadScreen;

    void Awake()
    {
        loadScreen = transform.GetChild(0).gameObject; // Assuming the load screen is the first child
        loadScreen.SetActive(true);
        RoomGenerator.OnDungeonComplete += () => StartCoroutine(DeactivateLoadScreen());
    }

    private IEnumerator DeactivateLoadScreen()
    {
        yield return new WaitForSeconds(2f); // Adjust the delay as needed
        loadScreen.SetActive(false);
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }


}
