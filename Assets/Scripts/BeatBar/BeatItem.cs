using System.Diagnostics;
using UnityEngine;
using UnityEngine.Pool;

public class BeatItem : MonoBehaviour
{
    // reference to beat handler in in scene
    BeatHandler beatHandler;

    // end gameobject
    GameObject endGoal;

    float stepSize = 0.0f;

    void FixedUpdate()
    {
        if(this.gameObject.transform.position.x >= endGoal.transform.position.x)
        {
            beatHandler.BeatArrived(this);
            return;
        }

        this.transform.position += Vector3.right * stepSize;
    }

    // Initializes beat item
    public void Init(BeatHandler beatHandler, GameObject endGoal, Vector3 position, float stepSize)
    {
        // references used
        this.beatHandler = beatHandler;
        this.endGoal = endGoal;

        // sets initial position
        this.transform.position = position;

        // amount to move after Time.fixedDeltaTime
        this.stepSize = stepSize;
    }

}
