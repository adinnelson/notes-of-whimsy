using UnityEngine;
using UnityEngine.SceneManagement;

public class ProgressFloor : MonoBehaviour
{
    private Scene currentScene;
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
        SceneManager.LoadScene(currentScene.name);
    }
}
