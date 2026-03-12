using UnityEngine;
using UnityEngine.UIElements;

public class LightningVisualLogic : MonoBehaviour
{

    public Vector2 pointingVector;
    private Transform sprite;

    private void Awake()
    {
        sprite = transform.Find("sprite");
        Debug.Log(sprite);
    }

    private void UpdateBeam()
    {
        float vect_length = pointingVector.magnitude;
        Debug.Log(pointingVector);
        Vector2 midpoint = pointingVector / 2f;

        sprite.localPosition = new Vector3(midpoint.x, midpoint.y, 0);
        sprite.GetComponent<SpriteRenderer>().size = new Vector2(vect_length, 1);

        float angleRadians = Mathf.Atan2(pointingVector.y, pointingVector.x);
        float angleDegrees = angleRadians * Mathf.Rad2Deg;
        sprite.rotation = Quaternion.Euler(0, 0, angleDegrees);
    }




    /// <summary>
    /// Sets the local vector of the beam.
    /// Beam will run from position -> position + vector
    /// </summary>
    /// <param name="newVector">The new local vector of the beam.</param>
    public void SetVector(Vector2 newVector)
    {
        pointingVector = newVector;
        UpdateBeam();
    }

    public void SetWorldPoint(Vector2 point)
    {
        pointingVector = point - (Vector2)transform.position;
        UpdateBeam();
    }
    public void DestroyBeam()
    {
        Destroy(gameObject);
    }

}
