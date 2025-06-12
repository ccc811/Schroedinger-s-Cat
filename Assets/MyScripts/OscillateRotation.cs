using UnityEngine;

public class OscillateRotation : MonoBehaviour
{
    [Header("Rotation Parameters")]
    [SerializeField] private float minAngle = -70f; // Minimum rotation angle
    [SerializeField] private float maxAngle = 70f; // Maximum rotation angle
    [SerializeField] private float rotationSpeed = 45f; // Rotation speed (degrees/second)
    [SerializeField] private bool startAtMin = true; // Whether to start at the minimum angle
    [SerializeField] private bool rotateInstantly = false; // Whether to rotate instantly

    [Header("Wait Times")]
    [SerializeField] private float waitAtMin = 0.5f; // Wait time at minimum angle
    [SerializeField] private float waitAtMax = 0.5f; // Wait time at maximum angle

    private float currentAngle; // Current angle
    private float direction = 1f; // Rotation direction (1: forward, -1: backward)
    private float waitTimer = 0f; // Wait timer
    private bool isWaiting = false; // Whether currently waiting

    private void Start()
    {
        // Initialize angle
        currentAngle = startAtMin ? minAngle : maxAngle;
        direction = startAtMin ? 1f : -1f;

        // Set initial rotation
        transform.rotation = Quaternion.Euler(0, 0, currentAngle);
    }

    private void Update()
    {
        if (isWaiting)
        {
            // Wait timer
            waitTimer += Time.deltaTime;

            if ((direction > 0 && waitTimer >= waitAtMax) ||
                (direction < 0 && waitTimer >= waitAtMin))
            {
                // Wait finished, switch direction
                isWaiting = false;
                waitTimer = 0f;

                if (rotateInstantly)
                {
                    // Instant rotation mode
                    direction *= -1;
                }
            }
            else
            {
                // Still waiting, do not rotate
                return;
            }
        }

        // Calculate rotation amount for this frame
        float rotationAmount = rotationSpeed * Time.deltaTime * direction;

        // Predict next angle
        float nextAngle = currentAngle + rotationAmount;

        // Check if reaching or exceeding boundaries
        if ((direction > 0 && nextAngle >= maxAngle) ||
            (direction < 0 && nextAngle <= minAngle))
        {
            // Reached or exceeded boundary, adjust to boundary angle
            currentAngle = direction > 0 ? maxAngle : minAngle;
            transform.rotation = Quaternion.Euler(0, 0, currentAngle);

            // Start waiting
            isWaiting = true;

            if (!rotateInstantly)
            {
                // Smooth rotation mode
                direction *= -1;
            }
        }
        else
        {
            // Not reached boundary, continue rotating
            currentAngle = nextAngle;
            transform.rotation = Quaternion.Euler(0, 0, currentAngle);
        }
    }

    // Draw rotation range in editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        // Draw current rotation angle
        Vector3 currentDirection = Quaternion.Euler(0, 0, currentAngle) * Vector3.right;
        Gizmos.DrawLine(transform.position, transform.position + currentDirection * 2f);

        // Draw minimum and maximum rotation angles
        Vector3 minDirection = Quaternion.Euler(0, 0, minAngle) * Vector3.right;
        Vector3 maxDirection = Quaternion.Euler(0, 0, maxAngle) * Vector3.right;

        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + minDirection * 2f);

        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + maxDirection * 2f);

        // Draw rotation range arc
        DrawArc(transform.position, minAngle, maxAngle, 2f, 20);

        // Draw rotation direction
        if (isWaiting)
        {
            Gizmos.color = Color.white;
        }
        else
        {
            Gizmos.color = direction > 0 ? Color.cyan : Color.magenta;
        }

        float radius = 1.2f;
        Vector3 center = transform.position;
        Vector3 arrowDir = Quaternion.Euler(0, 0, currentAngle) * Vector3.right * radius;
        DrawArrow(center, arrowDir, 0.3f);
    }

    // Helper method to draw arc
    private void DrawArc(Vector3 center, float startAngle, float endAngle, float radius, int segments)
    {
        Gizmos.color = Color.yellow;

        float angleStep = (endAngle - startAngle) / segments;
        Vector3 prevPoint = center + Quaternion.Euler(0, 0, startAngle) * Vector3.right * radius;

        for (int i = 1; i <= segments; i++)
        {
            float angle = startAngle + angleStep * i;
            Vector3 currentPoint = center + Quaternion.Euler(0, 0, angle) * Vector3.right * radius;

            Gizmos.DrawLine(prevPoint, currentPoint);
            prevPoint = currentPoint;
        }
    }

    // Helper method to draw arrow
    private void DrawArrow(Vector3 position, Vector3 direction, float size)
    {
        Gizmos.DrawLine(position, position + direction);

        // Draw arrowhead
        Vector3 arrowHead = position + direction;
        Vector3 arrowSide1 = Quaternion.Euler(0, 0, 150) * direction.normalized * size;
        Vector3 arrowSide2 = Quaternion.Euler(0, 0, -150) * direction.normalized * size;

        Gizmos.DrawLine(arrowHead, arrowHead + arrowSide1);
        Gizmos.DrawLine(arrowHead, arrowHead + arrowSide2);
    }
}