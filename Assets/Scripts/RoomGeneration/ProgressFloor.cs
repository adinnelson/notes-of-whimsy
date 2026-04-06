using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

public class ProgressFloor : MonoBehaviour
{
    [SerializeField] private bool loadCustomScene = false;
    [SerializeField] private string customSceneName = "StartMenu";
    // NOTE: Progress is currently not kept through portals so this cannot be implemented yet
    // [SerializeField] bool resetProgressOnDeath = true;

    private static int floorsCompleted = 0;
    private Scene currentScene;

    private Health health;

    public static event System.Action OnFloorProgressed;
    public static event System.Action OnEndSceneReached;

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
            // NOTE: Loading custom scene does not reset progress or increment floors completed
            if (loadCustomScene)
            {
                HardReset();

                // BeatHandler.ClearUnlockedBeats();
                OnEndSceneReached?.Invoke();
                SceneManager.LoadScene(customSceneName, LoadSceneMode.Single);
                return;
            }
            else
            {
                // floorsCompleted++;
                NextScene();
            }
        }
    }

    /// <summary>
    /// Hard reset the game by destroying the player, game manager, music manager, and beat handler.
    /// </summary>
    public static void HardReset()
    {
        GameObject player = GameObject.FindWithTag("Player");
        GameObject gm = GameObject.FindWithTag("GameManager");
        GameObject mm = FindFirstObjectByType<MusicManager>()?.gameObject;
        BeatHandler beatHandler = FindFirstObjectByType<BeatHandler>();

        Destroy(player);
        Destroy(gm);
        Destroy(mm);
    }

    /// <summary>
    /// Invoked on demo end screen, calls HardReset to reset progress.
    /// </summary>
    public static void InvokeEndSceneReached()
    {
        HardReset();
        OnEndSceneReached?.Invoke();
    }

    /// <summary>
    /// Returns the number of portals the player has passed through.
    /// </summary>
    public static int GetFloorsCompleted()
    {
        return floorsCompleted;
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
            OnEndSceneReached?.Invoke();
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
