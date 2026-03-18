using UnityEngine;
using UnityEngine.SceneManagement;

public class ProgressFloor : MonoBehaviour
{
    private static int floorsCompleted = 0;
    private Scene currentScene;

    private Health health;

    public static event System.Action OnFloorProgressed;

    void Awake()
    {
        health = GameObject.FindGameObjectWithTag("Player").GetComponent<Health>();
        if (health == null)
        {
            Debug.LogError($"{name}: No Health component found on player.");
        }
        health.OnDeath += HandleDeath;
    }

    void OnDestroy()
    {
        health.OnDeath -= HandleDeath;
    }
    private void Start()
    {
        currentScene = SceneManager.GetActiveScene();
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            NextScene();
        }
    }

    /// <summary>
    /// TODO: add some kind of transition effect here, maybe a fade out or something?
    /// currently just reloads the same scene. 
    /// Proper functionality should be added here to load different floors  
    /// </summary>
    private void NextScene()
    {
        floorsCompleted++;
        if(floorsCompleted >= 3)
        {
            print("End Scene Reached");
            //make sure the scene is in the build settings for this to work
            floorsCompleted = 0;
            SceneManager.LoadScene("EndScene");
        }
        else
        {
            
            // better option is to have the event pop but some elements do not get destroyed when the parent enemy spawns them since they are not children of the enemy
            // so until that is fixed, reloading the scene is the best option to reset everything.
            // OnFloorProgressed?.Invoke();
            SceneManager.LoadScene(currentScene.name);
            
        }
    }

    private void HandleDeath()
    {
        //reset floors completed on death
        floorsCompleted = 0;
    }
}
