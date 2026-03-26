using System;
using UnityEngine;

public class CameraMovement : MonoBehaviour
{

    [SerializeField]
    private Transform playerTransform;

    private GameObject currentRoom;
    [SerializeField]
    private Transform currentRoomTransform;

    [SerializeField]
    private float sqrDistanceFromPlayerToRoomCenter = 0f;

    [SerializeField]
    [Range(0f, 100f)]
    private float sqrDistanceFromCameraToStartFollowing = 25f;

    [SerializeField]
    [Range(0f,100f)]
    private float defaultMaxSqrHorizontalDistanceFromCameraToRoomCenter = 5f;

    [SerializeField]
    [Range(0f,100f)]
    private float defaultMaxSqrVerticalDistanceFromCameraToRoomCenter = 5f;

    [SerializeField]
    [Range(0.1f, 10f)]
    private float defaultCrossArmHalfThickness = 1f;

    [SerializeField]
    private CameraBoundsShape defaultCameraBoundsShape = CameraBoundsShape.Cross;

    [SerializeField]
    private float currentSqrDistanceFromCameraToRoomCenter = 0f;



    [Header("Smoothing")]
    [Tooltip("How long camera smoothing takes for both follow and recenter. Lower = snappier, higher = smoother.")]
    [Range(0.01f, 2f)]
    public float smoothTime = 0.2f;

    [Header("Inside Bounds Follow")]
    [Tooltip("How strongly the camera follows the player while inside bounds. 0 = room center, 1 = player position.")]
    [Range(0f, 1f)]
    [SerializeField]
    private float insideBoundsFollowAmount = 0.85f;

    [Tooltip("Multiplier applied to smoothTime while inside bounds. Lower values feel more locked-on.")]
    [Range(0.1f, 1f)]
    [SerializeField]
    private float insideBoundsSmoothTimeMultiplier = 0.6f;

    private const float CAMERA_Z_OFFSET = -10f;
    private Vector3 currentVelocity;

    private float activeMaxSqrHorizontalDistanceFromCameraToRoomCenter;
    private float activeMaxSqrVerticalDistanceFromCameraToRoomCenter;
    private float activeCrossArmHalfThickness;
    private CameraBoundsShape activeCameraBoundsShape;
    private float activeCameraBoundsOffsetX;
    private float activeCameraBoundsOffsetY;



    /// <summary>
    /// Ensure the camera Z offset is correct when the camera is placed in the editor.
    /// </summary>
    #if UNITY_EDITOR
    private void OnValidate()
    {
        if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) return;
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this == null) return;
            transform.position = new Vector3(0.0f, 0.0f, CAMERA_Z_OFFSET);
        };
    }

    private void OnDrawGizmos()
    {
        if (currentRoomTransform == null)
        {
            return;
        }

        Gizmos.color = Color.green;
        float radius = Mathf.Sqrt(sqrDistanceFromCameraToStartFollowing);
        Gizmos.DrawWireSphere(transform.position, radius);

        Gizmos.color = Color.red;
        float gizmoMaxSqrHorizontal = defaultMaxSqrHorizontalDistanceFromCameraToRoomCenter;
        float gizmoMaxSqrVertical = defaultMaxSqrVerticalDistanceFromCameraToRoomCenter;
        float gizmoArmHalfThickness = defaultCrossArmHalfThickness;
        CameraBoundsShape gizmoShape = defaultCameraBoundsShape;
        float gizmoOffsetX = 0f;
        float gizmoOffsetY = 0f;

        RoomInfo roomInfo = currentRoomTransform.GetComponent<RoomInfo>();
        if (roomInfo != null && roomInfo.UseCustomCameraBounds())
        {
            gizmoMaxSqrHorizontal = roomInfo.GetCustomMaxSqrHorizontalCameraDistance();
            gizmoMaxSqrVertical = roomInfo.GetCustomMaxSqrVerticalCameraDistance();
            gizmoArmHalfThickness = roomInfo.GetCustomCrossArmHalfThickness();
            gizmoShape = roomInfo.GetCustomCameraBoundsShape();
            gizmoOffsetX = roomInfo.GetCustomCameraBoundsOffsetX();
            gizmoOffsetY = roomInfo.GetCustomCameraBoundsOffsetY();
        }

        float horizontalArmHalfLength = Mathf.Sqrt(gizmoMaxSqrHorizontal);
        float verticalArmHalfLength = Mathf.Sqrt(gizmoMaxSqrVertical);
        Vector3 center = currentRoomTransform.position + new Vector3(gizmoOffsetX, gizmoOffsetY, 0f);

        if (gizmoShape == CameraBoundsShape.Box)
        {
            Gizmos.DrawWireCube(center, new Vector3(horizontalArmHalfLength * 2f, verticalArmHalfLength * 2f, 0f));
            return;
        }

        Gizmos.DrawWireCube(center, new Vector3(horizontalArmHalfLength * 2f, gizmoArmHalfThickness * 2f, 0f));
        Gizmos.DrawWireCube(center, new Vector3(gizmoArmHalfThickness * 2f, verticalArmHalfLength * 2f, 0f));

    }
#endif

    void Awake()
    {
        playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
        activeMaxSqrHorizontalDistanceFromCameraToRoomCenter = defaultMaxSqrHorizontalDistanceFromCameraToRoomCenter;
        activeMaxSqrVerticalDistanceFromCameraToRoomCenter = defaultMaxSqrVerticalDistanceFromCameraToRoomCenter;
        activeCrossArmHalfThickness = defaultCrossArmHalfThickness;
        activeCameraBoundsShape = defaultCameraBoundsShape;
        RoomInfo.OnEnterRoom += HandleEnterRoom;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        GetSqrDistanceToPlayer();
        UpdateCameraPosition();
    }

    // set room transform
    private void HandleEnterRoom(GameObject room)
    {
        currentRoomTransform = room.transform;
        currentRoom = room;

        RoomInfo roomInfo = room.GetComponent<RoomInfo>();
        if (roomInfo != null && roomInfo.UseCustomCameraBounds())
        {
            activeMaxSqrHorizontalDistanceFromCameraToRoomCenter = roomInfo.GetCustomMaxSqrHorizontalCameraDistance();
            activeMaxSqrVerticalDistanceFromCameraToRoomCenter = roomInfo.GetCustomMaxSqrVerticalCameraDistance();
            activeCrossArmHalfThickness = roomInfo.GetCustomCrossArmHalfThickness();
            activeCameraBoundsShape = roomInfo.GetCustomCameraBoundsShape();
            activeCameraBoundsOffsetX = roomInfo.GetCustomCameraBoundsOffsetX();
            activeCameraBoundsOffsetY = roomInfo.GetCustomCameraBoundsOffsetY();
            return;
        }

        activeMaxSqrHorizontalDistanceFromCameraToRoomCenter = defaultMaxSqrHorizontalDistanceFromCameraToRoomCenter;
        activeMaxSqrVerticalDistanceFromCameraToRoomCenter = defaultMaxSqrVerticalDistanceFromCameraToRoomCenter;
        activeCrossArmHalfThickness = defaultCrossArmHalfThickness;
        activeCameraBoundsShape = defaultCameraBoundsShape;
        activeCameraBoundsOffsetX = 0f;
        activeCameraBoundsOffsetY = 0f;
    }

    private void GetSqrDistanceToPlayer()
    {
        if (playerTransform == null || currentRoomTransform == null)
        {
            return;
        }

        Vector3 offset = playerTransform.position - currentRoomTransform.position;
        float sqrDistance = offset.sqrMagnitude;
        sqrDistanceFromPlayerToRoomCenter = sqrDistance;
    }

    private void UpdateCameraPosition()
    {
        if (playerTransform == null || currentRoomTransform == null)
        {
            return;
        }

        Vector3 cameraPosition = transform.position;
        Vector3 roomCenter = currentRoomTransform.position;
        roomCenter.z = CAMERA_Z_OFFSET;

        Vector3 playerPosition = playerTransform.position;
        playerPosition.z = CAMERA_Z_OFFSET;

        // Apply bounds offset to get the center of the camera bounds region
        Vector3 boundsCenterOffset = roomCenter + new Vector3(activeCameraBoundsOffsetX, activeCameraBoundsOffsetY, 0f);

        Vector2 playerOffsetFromRoomCenter = new Vector2(
            playerPosition.x - boundsCenterOffset.x,
            playerPosition.y - boundsCenterOffset.y
        );
        float horizontalArmHalfLength = Mathf.Sqrt(activeMaxSqrHorizontalDistanceFromCameraToRoomCenter);
        float verticalArmHalfLength = Mathf.Sqrt(activeMaxSqrVerticalDistanceFromCameraToRoomCenter);
        bool isPlayerInsideRoomCameraBounds = IsInsideCameraBounds(
            playerOffsetFromRoomCenter,
            horizontalArmHalfLength,
            verticalArmHalfLength,
            activeCrossArmHalfThickness,
            activeCameraBoundsShape
        );

        Vector3 desiredCameraPosition;
        if (isPlayerInsideRoomCameraBounds)
        {
            desiredCameraPosition = Vector3.Lerp(boundsCenterOffset, playerPosition, insideBoundsFollowAmount);

            Vector2 desiredOffsetFromRoomCenter = new Vector2(
                desiredCameraPosition.x - boundsCenterOffset.x,
                desiredCameraPosition.y - boundsCenterOffset.y
            );
            Vector2 clampedOffset = ClampOffsetToCameraBounds(
                desiredOffsetFromRoomCenter,
                horizontalArmHalfLength,
                verticalArmHalfLength,
                activeCrossArmHalfThickness,
                activeCameraBoundsShape
            );
            desiredCameraPosition = new Vector3(boundsCenterOffset.x + clampedOffset.x, boundsCenterOffset.y + clampedOffset.y, CAMERA_Z_OFFSET);
        }
        else
        {
            Vector3 playerFromCamera = playerPosition - cameraPosition;
            float sqrDistanceFromPlayerToCamera = playerFromCamera.sqrMagnitude;

            desiredCameraPosition = cameraPosition;
            if (sqrDistanceFromPlayerToCamera > sqrDistanceFromCameraToStartFollowing)
            {
                float followRadius = Mathf.Sqrt(sqrDistanceFromCameraToStartFollowing);
                Vector3 directionToPlayer = playerFromCamera.normalized;
                desiredCameraPosition = playerPosition - directionToPlayer * followRadius;
            }

            Vector2 desiredOffsetFromRoomCenter = new Vector2(
                desiredCameraPosition.x - boundsCenterOffset.x,
                desiredCameraPosition.y - boundsCenterOffset.y
            );
            Vector2 clampedOffset = ClampOffsetToCameraBounds(
                desiredOffsetFromRoomCenter,
                horizontalArmHalfLength,
                verticalArmHalfLength,
                activeCrossArmHalfThickness,
                activeCameraBoundsShape
            );
            desiredCameraPosition = new Vector3(boundsCenterOffset.x + clampedOffset.x, boundsCenterOffset.y + clampedOffset.y, CAMERA_Z_OFFSET);
        }

        float appliedSmoothTime = isPlayerInsideRoomCameraBounds
            ? smoothTime * insideBoundsSmoothTimeMultiplier
            : smoothTime;

        currentSqrDistanceFromCameraToRoomCenter = (desiredCameraPosition - boundsCenterOffset).sqrMagnitude;
        transform.position = Vector3.SmoothDamp(
            cameraPosition,
            desiredCameraPosition,
            ref currentVelocity,
            appliedSmoothTime
        );
        transform.position = new Vector3(transform.position.x, transform.position.y, CAMERA_Z_OFFSET);
    }

    private bool IsInsideCameraBounds(Vector2 offset, float horizontalArmHalfLength, float verticalArmHalfLength, float armHalfThickness, CameraBoundsShape boundsShape)
    {
        if (boundsShape == CameraBoundsShape.Box)
        {
            return Mathf.Abs(offset.x) <= horizontalArmHalfLength && Mathf.Abs(offset.y) <= verticalArmHalfLength;
        }

        return IsInsideCrossBounds(offset, horizontalArmHalfLength, verticalArmHalfLength, armHalfThickness);
    }

    private Vector2 ClampOffsetToCameraBounds(Vector2 offset, float horizontalArmHalfLength, float verticalArmHalfLength, float armHalfThickness, CameraBoundsShape boundsShape)
    {
        if (boundsShape == CameraBoundsShape.Box)
        {
            return new Vector2(
                Mathf.Clamp(offset.x, -horizontalArmHalfLength, horizontalArmHalfLength),
                Mathf.Clamp(offset.y, -verticalArmHalfLength, verticalArmHalfLength)
            );
        }

        return ClampOffsetToCrossBounds(offset, horizontalArmHalfLength, verticalArmHalfLength, armHalfThickness);
    }

    private bool IsInsideCrossBounds(Vector2 offset, float horizontalArmHalfLength, float verticalArmHalfLength, float armHalfThickness)
    {
        bool insideHorizontalArm = Mathf.Abs(offset.x) <= horizontalArmHalfLength && Mathf.Abs(offset.y) <= armHalfThickness;
        bool insideVerticalArm = Mathf.Abs(offset.x) <= armHalfThickness && Mathf.Abs(offset.y) <= verticalArmHalfLength;
        return insideHorizontalArm || insideVerticalArm;
    }

    private Vector2 ClampOffsetToCrossBounds(Vector2 offset, float horizontalArmHalfLength, float verticalArmHalfLength, float armHalfThickness)
    {
        Vector2 clampedToHorizontal = new Vector2(
            Mathf.Clamp(offset.x, -horizontalArmHalfLength, horizontalArmHalfLength),
            Mathf.Clamp(offset.y, -armHalfThickness, armHalfThickness)
        );

        Vector2 clampedToVertical = new Vector2(
            Mathf.Clamp(offset.x, -armHalfThickness, armHalfThickness),
            Mathf.Clamp(offset.y, -verticalArmHalfLength, verticalArmHalfLength)
        );

        float horizontalDistance = (offset - clampedToHorizontal).sqrMagnitude;
        float verticalDistance = (offset - clampedToVertical).sqrMagnitude;
        return horizontalDistance <= verticalDistance ? clampedToHorizontal : clampedToVertical;
    }

    private void OnDestroy()
    {
        RoomInfo.OnEnterRoom -= HandleEnterRoom;
    }
}
