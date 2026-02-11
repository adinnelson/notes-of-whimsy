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
    [Tooltip("Enable camera to shift in the direction of movement based on player input")]
    public bool enableLookAhead = false;

    [Tooltip("How far ahead to look based on input direction")]
    [Range(0f, 10f)]
    public float lookAheadDistance = 2.5f;

    [Tooltip("Minimum input magnitude required to activate look-ahead (prevents jitter when idle)")]
    [Range(0f, 1f)]
    public float lookAheadMinSpeed = 0.1f;

    [Tooltip("How quickly look-ahead offset adjusts (should be faster than smoothSpeed)")]
    [Range(1f, 20f)]
    public float lookAheadResponsiveness = 10f;

    // Private variables
    private Vector3 velocity = Vector3.zero;
    private Vector3 currentLookAheadOffset = Vector3.zero;
    private Vector3 lookAheadVelocity = Vector3.zero;
    private InputSystem_Actions inputActions;

    private const float CAMERA_Z_OFFSET = -10f;

    /// <summary>
    /// Ensure the camera Z offset is correct when the camera is placed in the editor.
    /// </summary>
#if UNITY_EDITOR
    private void OnValidate()
    {
        UnityEditor.EditorApplication.delayCall += () =>
        {
            transform.position = new Vector3(0.0f, 0.0f, CAMERA_Z_OFFSET);
        };
    }
#endif

    private void Start()
    {
        if (target == null)
        {
            Debug.LogWarning("CameraFollow: No target assigned! Please assign a target in the inspector.");
            return;
        }

        // Initialize input actions for lookahead
        inputActions = new InputSystem_Actions();
        inputActions.Player.Enable();
    }

    private void OnDisable()
    {
        if (inputActions != null)
        {
            inputActions.Player.Disable();
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 targetPosition = target.position;
        Vector3 desiredPosition = targetPosition;

        // Calculate look-ahead if enabled
        if (enableLookAhead)
        {
            // Get player input direction
            Vector2 moveInput = inputActions.Player.Move.ReadValue<Vector2>();

            // Calculate desired lookahead offset based on input, not velocity
            Vector3 desiredLookAheadOffset = Vector3.zero;
            if (moveInput.magnitude >= lookAheadMinSpeed)
            {
                // Normalize diagonal input
                if (moveInput.magnitude > 1f)
                    moveInput.Normalize();

                desiredLookAheadOffset = moveInput.normalized * lookAheadDistance;
            }

            // Smooth the offset itself (prevents overshoot when stopping)
            currentLookAheadOffset = Vector3.SmoothDamp(
                currentLookAheadOffset,
                desiredLookAheadOffset,
                ref lookAheadVelocity,
                1f / lookAheadResponsiveness
            );

            desiredPosition += currentLookAheadOffset;
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
            if (enableLookAhead && Application.isPlaying && currentLookAheadOffset.magnitude > 0.01f)
            {
                Vector3 lookAheadPoint = target.position + currentLookAheadOffset;

                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(target.position, lookAheadPoint);
                Gizmos.DrawWireSphere(lookAheadPoint, 0.2f);
            }
        }
    }
}