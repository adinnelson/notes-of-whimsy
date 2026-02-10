using System.Diagnostics;
using UnityEngine;
using UnityEngine.Pool;

public class BeatItem : MonoBehaviour
{
    private const float LATE_OFFSET = 0.5f; 

    // reference to beat handler in in scene
    private BeatHandler beatHandler;

    // end gameobject
    private GameObject endGoal;

    float stepSize = 0.0f;

    void FixedUpdate()
    {
        if(endGoal == null) return;

        if(this.gameObject.transform.position.x >= endGoal.transform.position.x + LATE_OFFSET)
        {
            beatHandler.BeatArrived(this);
            return;
        }

        this.transform.localPosition += Vector3.right * stepSize;
    }

    // Initializes beat item
    public void Init(BeatHandler beatHandler, Color colour, GameObject endGoal, Vector3 position, float stepSize)
    {
        // references used
        this.beatHandler = beatHandler;
        this.endGoal = endGoal;

        // sets initial position
        this.transform.position = position;

        // amount to move after Time.fixedDeltaTime
        this.stepSize = stepSize;

        // set color
        GetComponent<SpriteRenderer>().color = colour;
    }

}
