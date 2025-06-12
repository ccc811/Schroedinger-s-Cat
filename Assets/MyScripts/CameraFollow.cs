using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target; // The target the camera will follow
    [SerializeField] private float smoothSpeed = 0.125f; // The speed at which the camera moves smoothly
    [SerializeField] private Vector3 offset; // The offset between the camera and the target

    [Header("Boundary Limits")]
    [SerializeField] private bool limitX = true; // Whether to limit movement on the X axis
    [SerializeField] private bool limitY = true; // Whether to limit movement on the Y axis
    [SerializeField] private float minX; // Minimum boundary for X axis
    [SerializeField] private float maxX; // Maximum boundary for X axis
    [SerializeField] private float minY; // Minimum boundary for Y axis
    [SerializeField] private float maxY; // Maximum boundary for Y axis

    private void LateUpdate()
    {
        print(123123123);
        if (target == null) return;

        // Calculate the desired position for the camera
        Vector3 desiredPosition = target.position + offset;

        // Apply boundary limits
        if (limitX)
        {
            desiredPosition.x = Mathf.Clamp(desiredPosition.x, minX, maxX);
        }

        if (limitY)
        {
            desiredPosition.y = Mathf.Clamp(desiredPosition.y, minY, maxY);
        }

        // Keep the camera's Z axis unchanged
        desiredPosition.z = transform.position.z;

        // Smoothly move the camera
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.position = smoothedPosition;
    }

    // Draw boundary visualization in the editor
    private void OnDrawGizmosSelected()
    {
        if (!limitX && !limitY) return;

        Gizmos.color = Color.red;

        // Calculate the camera's half width and height
        float cameraHeight = Camera.main.orthographicSize * 2;
        float cameraWidth = cameraHeight * Camera.main.aspect;

        // Draw X axis boundaries
        if (limitX)
        {
            Vector3 topLeft = new Vector3(minX, transform.position.y + cameraHeight / 2, 0);
            Vector3 bottomLeft = new Vector3(minX, transform.position.y - cameraHeight / 2, 0);
            Vector3 topRight = new Vector3(maxX, transform.position.y + cameraHeight / 2, 0);
            Vector3 bottomRight = new Vector3(maxX, transform.position.y - cameraHeight / 2, 0);

            Gizmos.DrawLine(topLeft, bottomLeft);
            Gizmos.DrawLine(topRight, bottomRight);
        }

        // Draw Y axis boundaries
        if (limitY)
        {
            Vector3 topLeft = new Vector3(transform.position.x - cameraWidth / 2, maxY, 0);
            Vector3 topRight = new Vector3(transform.position.x + cameraWidth / 2, maxY, 0);
            Vector3 bottomLeft = new Vector3(transform.position.x - cameraWidth / 2, minY, 0);
            Vector3 bottomRight = new Vector3(transform.position.x + cameraWidth / 2, minY, 0);

            Gizmos.DrawLine(topLeft, topRight);
            Gizmos.DrawLine(bottomLeft, bottomRight);
        }
    }
}