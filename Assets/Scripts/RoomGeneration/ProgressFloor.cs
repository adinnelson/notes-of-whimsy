using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ProgressFloor : MonoBehaviour
{
    private static int floorsCompleted = 0;
    private Scene currentScene;
    private void Start()
    {
        currentScene = SceneManager.GetActiveScene();
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            floorsCompleted++;
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
        if(floorsCompleted >= 3)
        {
            print("End Scene Reached");
            //make sure the scene is in the build settings for this to work
            SceneManager.LoadScene("EndScene");
        }
        else
        {
            SceneManager.LoadScene(currentScene.name);
            
        }
    }
}
