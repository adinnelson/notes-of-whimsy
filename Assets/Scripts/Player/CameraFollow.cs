// ============================================================================
// This script was generated using AI
// ============================================================================

using UnityEngine;

/// <summary>
/// Simple camera follower with smooth damping to reduce motion sickness.
/// Designed for top-down games in Unity 6.
/// Attach this to your main camera and assign the player transform.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("The player or object the camera should follow")]
    public Transform target;

    [Header("Smoothing")]
    [Tooltip("How quickly the camera follows. Lower = more lag (smoother). Range: 0.1 to 20")]
    [Range(0.1f, 20f)]
    public float smoothSpeed = 5f;

    [Header("Optional: Look Ahead")]
    [Tooltip("Enable camera to shift in the direction of movement based on player velocity")]
    public bool enableLookAhead = false;

    [Tooltip("How far ahead to look based on velocity")]
    [Range(0f, 10f)]
    public float lookAheadDistance = 2.5f;

    [Tooltip("Minimum player speed required to activate look-ahead (prevents jitter when idle)")]
    [Range(0f, 2f)]
    public float lookAheadMinSpeed = 0.5f;

    // Private variables
    private Vector3 velocity = Vector3.zero;
    private Rigidbody2D targetRigidbody;

    private void Start()
    {
        if (target == null)
        {
            Debug.LogWarning("CameraFollow: No target assigned! Please assign a target in the inspector.");
            return;
        }

        // Get Rigidbody2D component for look-ahead feature
        targetRigidbody = target.GetComponent<Rigidbody2D>();
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 targetPosition = target.position;
        Vector3 desiredPosition = targetPosition;

        // Calculate look-ahead if enabled
        if (enableLookAhead)
        {
            // Safety check: need Rigidbody2D for look-ahead to work
            if (targetRigidbody == null)
            {
                targetRigidbody = target.GetComponent<Rigidbody2D>();

                if (targetRigidbody == null)
                {
                    // No Rigidbody2D, skip look-ahead this frame
                    Debug.LogWarning("CameraFollow: Look-ahead requires a Rigidbody2D component on the target.");
                }
            }

            // Apply look-ahead if we have rigidbody
            if (targetRigidbody != null)
            {
                Vector2 playerVelocity = targetRigidbody.linearVelocity;

                // Only apply look-ahead if moving faster than minimum speed
                if (playerVelocity.magnitude >= lookAheadMinSpeed)
                {
                    // Calculate look-ahead position instantly (no smoothing here)
                    Vector3 lookAheadOffset = playerVelocity.normalized * lookAheadDistance;
                    desiredPosition += lookAheadOffset;
                }
            }
        }

        // Keep camera's Z position (important for 2D)
        desiredPosition.z = transform.position.z;

        // Smooth follow to desired position (ONLY smoothing operation)
        Vector3 smoothedPosition = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref velocity,
            1f / smoothSpeed
        );

        transform.position = smoothedPosition;
    }

    // Visualize look-ahead in editor
    private void OnDrawGizmosSelected()
    {
        if (target != null)
        {
            // Line from camera to target
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, target.position);

            // Show look ahead if enabled and active
            if (enableLookAhead && Application.isPlaying && targetRigidbody != null)
            {
                Vector2 playerVelocity = targetRigidbody.linearVelocity;

                if (playerVelocity.magnitude >= lookAheadMinSpeed)
                {
                    Vector3 lookAheadOffset = playerVelocity.normalized * lookAheadDistance;
                    Vector3 lookAheadPoint = target.position + lookAheadOffset;

                    Gizmos.color = Color.magenta;
                    Gizmos.DrawLine(target.position, lookAheadPoint);
                    Gizmos.DrawWireSphere(lookAheadPoint, 0.2f);
                }
            }
        }
    }
}
