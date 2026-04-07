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
    private float sqrDistanceFromPlayerToRoomCenter = 0.0f;

    [SerializeField]
    [Range(0.0f, 100.0f)]
    private float sqrDistanceFromCameraToStartFollowing = 25.0f;

    [SerializeField]
    [Range(0.0f,100.0f)]
    private float defaultMaxSqrHorizontalDistanceFromCameraToRoomCenter = 5.0f;

    [SerializeField]
    [Range(0.0f,100.0f)]
    private float defaultMaxSqrVerticalDistanceFromCameraToRoomCenter = 5.0f;

    [SerializeField]
    [Range(0.1f, 10.0f)]
    private float defaultCrossArmHalfThickness = 1.0f;

    [SerializeField]
    private CameraBoundsShape defaultCameraBoundsShape = CameraBoundsShape.Cross;

    [SerializeField]
    private float currentSqrDistanceFromCameraToRoomCenter = 0.0f;



    [Header("Smoothing")]
    [Tooltip("How long camera smoothing takes for both follow and recenter. Lower = snappier, higher = smoother.")]
    [Range(0.01f, 2.0f)]
    public float smoothTime = 0.2f;

    [Header("Inside Bounds Follow")]
    [Tooltip("How strongly the camera follows the player while inside bounds. 0 = room center, 1 = player position.")]
    [Range(0.0f, 1.0f)]
    [SerializeField]
    private float insideBoundsFollowAmount = 0.85f;

    [Tooltip("Multiplier applied to smoothTime while inside bounds. Lower values feel more locked-on.")]
    [Range(0.1f, 1.0f)]
    [SerializeField]
    private float insideBoundsSmoothTimeMultiplier = 0.6f;

    private const float CAMERA_Z_OFFSET = -10.0f;
    private Vector3 currentVelocity;

    private float activeMaxSqrHorizontalDistanceFromCameraToRoomCenter;
    private float activeMaxSqrVerticalDistanceFromCameraToRoomCenter;
    private float activeCrossArmHalfThickness;
    private CameraBoundsShape activeCameraBoundsShape;
    private float activeCameraBoundsOffsetX;
    private float activeCameraBoundsOffsetY;
    private float activeHorizontalArmOffsetX;
    private float activeHorizontalArmOffsetY;
    private float activeVerticalArmOffsetX;
    private float activeVerticalArmOffsetY;



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
        float gizmoOffsetX = 0.0f;
        float gizmoOffsetY = 0.0f;
        float gizmoHorizontalArmOffsetX = 0.0f;
        float gizmoHorizontalArmOffsetY = 0.0f;
        float gizmoVerticalArmOffsetX = 0.0f;
        float gizmoVerticalArmOffsetY = 0.0f;

        RoomInfo roomInfo = currentRoomTransform.GetComponent<RoomInfo>();
        if (roomInfo != null && roomInfo.UseCustomCameraBounds())
        {
            gizmoMaxSqrHorizontal = roomInfo.GetCustomMaxSqrHorizontalCameraDistance();
            gizmoMaxSqrVertical = roomInfo.GetCustomMaxSqrVerticalCameraDistance();
            gizmoArmHalfThickness = roomInfo.GetCustomCrossArmHalfThickness();
            gizmoShape = roomInfo.GetCustomCameraBoundsShape();
            gizmoOffsetX = roomInfo.GetCustomCameraBoundsOffsetX();
            gizmoOffsetY = roomInfo.GetCustomCameraBoundsOffsetY();
            gizmoHorizontalArmOffsetX = roomInfo.GetCustomHorizontalArmOffsetX();
            gizmoHorizontalArmOffsetY = roomInfo.GetCustomHorizontalArmOffsetY();
            gizmoVerticalArmOffsetX = roomInfo.GetCustomVerticalArmOffsetX();
            gizmoVerticalArmOffsetY = roomInfo.GetCustomVerticalArmOffsetY();
        }

        float horizontalArmHalfLength = Mathf.Sqrt(gizmoMaxSqrHorizontal);
        float verticalArmHalfLength = Mathf.Sqrt(gizmoMaxSqrVertical);
        Vector3 baseCenter = currentRoomTransform.position + new Vector3(gizmoOffsetX, gizmoOffsetY, 0f);

        if (gizmoShape == CameraBoundsShape.Box)
        {
            Gizmos.DrawWireCube(baseCenter, new Vector3(horizontalArmHalfLength * 2f, verticalArmHalfLength * 2f, 0f));
            return;
        }

        Vector3 horizontalCenter = baseCenter + new Vector3(gizmoHorizontalArmOffsetX, gizmoHorizontalArmOffsetY, 0f);
        Vector3 verticalCenter = baseCenter + new Vector3(gizmoVerticalArmOffsetX, gizmoVerticalArmOffsetY, 0f);

        Gizmos.DrawWireCube(horizontalCenter, new Vector3(horizontalArmHalfLength * 2f, gizmoArmHalfThickness * 2f, 0f));
        Gizmos.DrawWireCube(verticalCenter, new Vector3(gizmoArmHalfThickness * 2f, verticalArmHalfLength * 2f, 0f));

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
            activeHorizontalArmOffsetX = roomInfo.GetCustomHorizontalArmOffsetX();
            activeHorizontalArmOffsetY = roomInfo.GetCustomHorizontalArmOffsetY();
            activeVerticalArmOffsetX = roomInfo.GetCustomVerticalArmOffsetX();
            activeVerticalArmOffsetY = roomInfo.GetCustomVerticalArmOffsetY();
            return;
        }

        activeMaxSqrHorizontalDistanceFromCameraToRoomCenter = defaultMaxSqrHorizontalDistanceFromCameraToRoomCenter;
        activeMaxSqrVerticalDistanceFromCameraToRoomCenter = defaultMaxSqrVerticalDistanceFromCameraToRoomCenter;
        activeCrossArmHalfThickness = defaultCrossArmHalfThickness;
        activeCameraBoundsShape = defaultCameraBoundsShape;
        activeCameraBoundsOffsetX = 0.0f;
        activeCameraBoundsOffsetY = 0.0f;
        activeHorizontalArmOffsetX = 0.0f;
        activeHorizontalArmOffsetY = 0.0f;
        activeVerticalArmOffsetX = 0.0f;
        activeVerticalArmOffsetY = 0.0f;
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

        Vector3 boundsCenterOffset = roomCenter + new Vector3(activeCameraBoundsOffsetX, activeCameraBoundsOffsetY, 0f);
        Vector2 playerPosition2D = new Vector2(playerPosition.x, playerPosition.y);
        Vector2 boxCenter = new Vector2(boundsCenterOffset.x, boundsCenterOffset.y);
        Vector2 horizontalArmCenter = boxCenter + new Vector2(activeHorizontalArmOffsetX, activeHorizontalArmOffsetY);
        Vector2 verticalArmCenter = boxCenter + new Vector2(activeVerticalArmOffsetX, activeVerticalArmOffsetY);

        float horizontalArmHalfLength = Mathf.Sqrt(activeMaxSqrHorizontalDistanceFromCameraToRoomCenter);
        float verticalArmHalfLength = Mathf.Sqrt(activeMaxSqrVerticalDistanceFromCameraToRoomCenter);
        bool isPlayerInsideRoomCameraBounds = IsInsideCameraBounds(
            playerPosition2D,
            boxCenter,
            horizontalArmCenter,
            verticalArmCenter,
            horizontalArmHalfLength,
            verticalArmHalfLength,
            activeCrossArmHalfThickness,
            activeCameraBoundsShape
        );

        Vector3 desiredCameraPosition;
        if (isPlayerInsideRoomCameraBounds)
        {
            Vector2 followAnchor = activeCameraBoundsShape == CameraBoundsShape.Box
                ? boxCenter
                : GetCrossFollowAnchor(playerPosition2D, horizontalArmCenter, verticalArmCenter, horizontalArmHalfLength, verticalArmHalfLength, activeCrossArmHalfThickness);

            Vector2 desiredPosition2D = Vector2.Lerp(
                followAnchor,
                playerPosition2D,
                insideBoundsFollowAmount
            );
            Vector2 clampedPosition = ClampPositionToCameraBounds(
                desiredPosition2D,
                boxCenter,
                horizontalArmCenter,
                verticalArmCenter,
                horizontalArmHalfLength,
                verticalArmHalfLength,
                activeCrossArmHalfThickness,
                activeCameraBoundsShape
            );
            desiredCameraPosition = new Vector3(clampedPosition.x, clampedPosition.y, CAMERA_Z_OFFSET);
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

            Vector2 desiredPosition2D = new Vector2(
                desiredCameraPosition.x,
                desiredCameraPosition.y
            );
            Vector2 clampedPosition = ClampPositionToCameraBounds(
                desiredPosition2D,
                boxCenter,
                horizontalArmCenter,
                verticalArmCenter,
                horizontalArmHalfLength,
                verticalArmHalfLength,
                activeCrossArmHalfThickness,
                activeCameraBoundsShape
            );
            desiredCameraPosition = new Vector3(clampedPosition.x, clampedPosition.y, CAMERA_Z_OFFSET);
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

    private bool IsInsideCameraBounds(
        Vector2 position,
        Vector2 boxCenter,
        Vector2 horizontalArmCenter,
        Vector2 verticalArmCenter,
        float horizontalArmHalfLength,
        float verticalArmHalfLength,
        float armHalfThickness,
        CameraBoundsShape boundsShape
    )
    {
        if (boundsShape == CameraBoundsShape.Box)
        {
            return Mathf.Abs(position.x - boxCenter.x) <= horizontalArmHalfLength
                && Mathf.Abs(position.y - boxCenter.y) <= verticalArmHalfLength;
        }

        return IsInsideCrossBounds(position, horizontalArmCenter, verticalArmCenter, horizontalArmHalfLength, verticalArmHalfLength, armHalfThickness);
    }

    private Vector2 ClampPositionToCameraBounds(
        Vector2 position,
        Vector2 boxCenter,
        Vector2 horizontalArmCenter,
        Vector2 verticalArmCenter,
        float horizontalArmHalfLength,
        float verticalArmHalfLength,
        float armHalfThickness,
        CameraBoundsShape boundsShape
    )
    {
        if (boundsShape == CameraBoundsShape.Box)
        {
            return new Vector2(
                Mathf.Clamp(position.x, boxCenter.x - horizontalArmHalfLength, boxCenter.x + horizontalArmHalfLength),
                Mathf.Clamp(position.y, boxCenter.y - verticalArmHalfLength, boxCenter.y + verticalArmHalfLength)
            );
        }

        return ClampPositionToCrossBounds(position, horizontalArmCenter, verticalArmCenter, horizontalArmHalfLength, verticalArmHalfLength, armHalfThickness);
    }

    private bool IsInsideCrossBounds(
        Vector2 position,
        Vector2 horizontalArmCenter,
        Vector2 verticalArmCenter,
        float horizontalArmHalfLength,
        float verticalArmHalfLength,
        float armHalfThickness
    )
    {
        bool insideHorizontalArm = IsInsideHorizontalArm(position, horizontalArmCenter, horizontalArmHalfLength, armHalfThickness);
        bool insideVerticalArm = IsInsideVerticalArm(position, verticalArmCenter, verticalArmHalfLength, armHalfThickness);
        return insideHorizontalArm || insideVerticalArm;
    }

    private Vector2 ClampPositionToCrossBounds(
        Vector2 position,
        Vector2 horizontalArmCenter,
        Vector2 verticalArmCenter,
        float horizontalArmHalfLength,
        float verticalArmHalfLength,
        float armHalfThickness
    )
    {
        Vector2 clampedToHorizontal = ClampPositionToRect(position, horizontalArmCenter, horizontalArmHalfLength, armHalfThickness);
        Vector2 clampedToVertical = ClampPositionToRect(position, verticalArmCenter, armHalfThickness, verticalArmHalfLength);

        float horizontalDistance = (position - clampedToHorizontal).sqrMagnitude;
        float verticalDistance = (position - clampedToVertical).sqrMagnitude;
        return horizontalDistance <= verticalDistance ? clampedToHorizontal : clampedToVertical;
    }

    private Vector2 GetCrossFollowAnchor(
        Vector2 playerPosition,
        Vector2 horizontalArmCenter,
        Vector2 verticalArmCenter,
        float horizontalArmHalfLength,
        float verticalArmHalfLength,
        float armHalfThickness
    )
    {
        bool insideHorizontalArm = IsInsideHorizontalArm(playerPosition, horizontalArmCenter, horizontalArmHalfLength, armHalfThickness);
        bool insideVerticalArm = IsInsideVerticalArm(playerPosition, verticalArmCenter, verticalArmHalfLength, armHalfThickness);

        if (insideHorizontalArm && insideVerticalArm)
        {
            float horizontalCenterDistance = (playerPosition - horizontalArmCenter).sqrMagnitude;
            float verticalCenterDistance = (playerPosition - verticalArmCenter).sqrMagnitude;
            return horizontalCenterDistance <= verticalCenterDistance ? horizontalArmCenter : verticalArmCenter;
        }

        if (insideHorizontalArm)
        {
            return horizontalArmCenter;
        }

        if (insideVerticalArm)
        {
            return verticalArmCenter;
        }

        return (horizontalArmCenter + verticalArmCenter) * 0.5f;
    }

    private bool IsInsideHorizontalArm(Vector2 position, Vector2 horizontalArmCenter, float horizontalArmHalfLength, float armHalfThickness)
    {
        return Mathf.Abs(position.x - horizontalArmCenter.x) <= horizontalArmHalfLength
            && Mathf.Abs(position.y - horizontalArmCenter.y) <= armHalfThickness;
    }

    private bool IsInsideVerticalArm(Vector2 position, Vector2 verticalArmCenter, float verticalArmHalfLength, float armHalfThickness)
    {
        return Mathf.Abs(position.x - verticalArmCenter.x) <= armHalfThickness
            && Mathf.Abs(position.y - verticalArmCenter.y) <= verticalArmHalfLength;
    }

    private Vector2 ClampPositionToRect(Vector2 position, Vector2 rectCenter, float halfWidth, float halfHeight)
    {
        return new Vector2(
            Mathf.Clamp(position.x, rectCenter.x - halfWidth, rectCenter.x + halfWidth),
            Mathf.Clamp(position.y, rectCenter.y - halfHeight, rectCenter.y + halfHeight)
        );
    }

    private void OnDestroy()
    {
        RoomInfo.OnEnterRoom -= HandleEnterRoom;
    }
}
