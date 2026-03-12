using UnityEngine;
using UnityEngine.SceneManagement;

public class EndScreenManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    public void ReturnToMainMenu()
    {
        // change this to load the actual main menu scene when it is created
        // the scene must be added to the build settings for this to work
        SceneManager.LoadScene("StartMenu");
    }
}
