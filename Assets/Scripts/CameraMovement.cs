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
    private float maxSqrDistanceFromCameraToRoomCenter = 5f;

    [SerializeField]
    private float currentSqrDistanceFromCameraToRoomCenter = 0f;



    [Header("Smoothing")]
    [Tooltip("How long camera smoothing takes for both follow and recenter. Lower = snappier, higher = smoother.")]
    [Range(0.01f, 2f)]
    public float smoothTime = 0.2f;

    private const float CAMERA_Z_OFFSET = -10f;
    private Vector3 currentVelocity;

    

    /// <summary>
    /// Ensure the camera Z offset is correct when the camera is placed in the editor.
    /// </summary>
    #if UNITY_EDITOR
    private void OnValidate()
    {
        if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) return;
        UnityEditor.EditorApplication.delayCall += () =>
        {
            transform.position = new Vector3(0.0f, 0.0f, CAMERA_Z_OFFSET);
        };
    }
    
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        float radius = Mathf.Sqrt(sqrDistanceFromCameraToStartFollowing);
        Gizmos.DrawWireSphere(transform.position, radius);
        Gizmos.color = Color.red;
        float maxRadius = Mathf.Sqrt(maxSqrDistanceFromCameraToRoomCenter);
        Gizmos.DrawWireCube(currentRoomTransform.position, Vector3.one * 2f * maxRadius);

    }
#endif

    void Awake()
    {
        playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
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

        Vector3 playerFromRoomCenter = playerPosition - roomCenter;
        bool isPlayerInsideRoomCameraBounds = playerFromRoomCenter.sqrMagnitude <= maxSqrDistanceFromCameraToRoomCenter;

        Vector3 desiredCameraPosition = roomCenter;
        if (!isPlayerInsideRoomCameraBounds)
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

            Vector3 offsetFromRoomCenter = desiredCameraPosition - roomCenter;
            if (offsetFromRoomCenter.sqrMagnitude > maxSqrDistanceFromCameraToRoomCenter)
            {
                float maxDistance = Mathf.Sqrt(maxSqrDistanceFromCameraToRoomCenter);
                desiredCameraPosition = roomCenter + offsetFromRoomCenter.normalized * maxDistance;
            }
        }

        currentSqrDistanceFromCameraToRoomCenter = (desiredCameraPosition - roomCenter).sqrMagnitude;
        transform.position = Vector3.SmoothDamp(
            cameraPosition,
            desiredCameraPosition,
            ref currentVelocity,
            smoothTime
        );
        transform.position = new Vector3(transform.position.x, transform.position.y, CAMERA_Z_OFFSET);
    }

    private void OnDestroy()
    {
        RoomInfo.OnEnterRoom -= HandleEnterRoom;
    }
}
