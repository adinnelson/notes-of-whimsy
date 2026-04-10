using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void PlayGame()
    {
        GoldManager.Instance.ResetGold();
        SceneManager.LoadScene("Main", LoadSceneMode.Single);
    }

    public void LoadScene(string sceneName)
    {
        GoldManager.Instance?.ResetGold();
        ProgressFloor.HardReset();
        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }
}
