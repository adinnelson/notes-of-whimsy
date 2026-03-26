using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void PlayGame()
    {
        SceneManager.LoadScene("Main", LoadSceneMode.Single);
    }

    public void LoadScene(string sceneName)
    {
        ProgressFloor.HardReset();
        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }
}
