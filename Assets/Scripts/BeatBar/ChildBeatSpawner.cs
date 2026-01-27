using UnityEngine;

public class ChildBeatSpawner : MonoBehaviour
{
    private float spawnTime = 0.0f;
    public float SpawnTime
    {
        get { return spawnTime; }
    }

    private float timeElapsed = 0.0f;
    private bool waitingToSpawn = false;

    private BeatHandler beatHandler;
    private Color colour;
    public Color Colour 
    {
        get { return colour; }
    }
    
    void FixedUpdate() 
    {
        if(!waitingToSpawn) return;

        if (timeElapsed >= spawnTime)
        {
            beatHandler.SpawnAdditionalBeat(colour);
            timeElapsed = 0.0f;
            waitingToSpawn = false;
        }

        timeElapsed += Time.fixedDeltaTime;
    }

    public void Init(BeatHandler beatHandler, Color colour, Vector3 position, float spawnTime)
    {
        this.beatHandler = beatHandler;
        this.transform.position = position;
        this.spawnTime = spawnTime;
        this.colour = colour;
    }

    // Called when main beat item reaches end
    public void StartSpawnCountdown()
    {
        waitingToSpawn = true;
    }
}
