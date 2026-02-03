using System.Diagnostics;
using UnityEngine;
using UnityEngine.Pool;

public class BeatItem : MonoBehaviour
{
    // reference to beat handler in in scene
    private BeatHandler beatHandler;

    // end gameobject
    private Vector3 endGoal;

    float stepSize = 0.0f;

    void FixedUpdate()
    {
        if(endGoal == null) return;

        if(this.gameObject.transform.position.x >= endGoal.x)
        {
            beatHandler.BeatArrived(this);
            return;
        }

        this.transform.position += Vector3.right * stepSize;
    }

    // Initializes beat item
    public void Init(BeatHandler beatHandler, Color colour, Vector3 endGoal, Vector3 position, float stepSize)
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
