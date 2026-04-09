using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseScreenManager : MonoBehaviour
{
    GameObject pauseScreen;

    void Awake()
    {
        pauseScreen = transform.GetChild(0).gameObject; // Assuming the pause screen is the first child
        pauseScreen.SetActive(false);
        
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if(pauseScreen.activeSelf)
            {
                Close();
            }
            else
            {
                Open();
            }
        }
    }

    public void Close()
    {
        Time.timeScale = 1f; // Resume the game
        pauseScreen.SetActive(false);
    }

    void Open()
    {
        Time.timeScale = 0f; // Pause the game
        pauseScreen.SetActive(true);
    }

    public void ReturnToMainMenu()
    {
        GoldManager.Instance.ResetGold();
        Close();
        ProgressFloor.InvokeEndSceneReached();
        SceneManager.LoadScene(0);
    }
}
