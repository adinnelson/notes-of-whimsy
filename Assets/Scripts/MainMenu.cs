using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void PlayGame()
    {
        GoldManager.Instance?.ResetGold();
        ProgressFloor.InvokeEndSceneReached();
        SceneManager.LoadScene("Main", LoadSceneMode.Single);
    }

    public void LoadScene(string sceneName)
    {
        GoldManager.Instance?.ResetGold();
        ProgressFloor.InvokeEndSceneReached();
        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }
}
