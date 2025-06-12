using UnityEngine;
using UnityEngine.Events;

public class EnemyPatrol : MonoBehaviour
{
    [Header("Patrol Settings")]
    [SerializeField] private Transform[] waypoints; // Patrol path points
    [SerializeField] private float moveSpeed = 2f; // Movement speed
    [SerializeField] private float waitTime = 1f; // Wait time at waypoints
    [SerializeField] private PatrolAxis patrolAxis = PatrolAxis.Horizontal; // Patrol axis (horizontal/vertical)

    [Header("Combat Settings")]
    [SerializeField] private int damage = 1; // Damage to player
    [SerializeField] private Transform headCheck; // Head detection point
    [SerializeField] private float headCheckRadius = 0.2f; // Head detection radius
    [SerializeField] private LayerMask playerLayer; // Player layer
    [SerializeField] private float stompBounce = 5f; // Bounce force when player stomps head

    [Header("Status")]
    [SerializeField] private bool isAlive = true; // Enemy is alive
    [SerializeField] private bool isFacingRight = true; // Enemy is facing right

    private int currentWaypointIndex = 0; // Current waypoint index
    private float waitCounter = 0f; // Wait timer
    private Animator anim;
    private Collider2D col;
    private bool isWaiting = false;

    // Event system
    public UnityEvent OnEnemyKilled; // Triggered when enemy is killed

    // Patrol axis enum
    public enum PatrolAxis
    {
        Horizontal,
        Vertical
    }

    private void Start()
    {
        anim = GetComponent<Animator>();
        col = GetComponent<Collider2D>();

        // Ensure there are at least two waypoints
        if (waypoints.Length < 2)
        {
            Debug.LogError("At least two waypoints are required!", gameObject);
        }

        // Initialize position to first waypoint
        if (waypoints.Length > 0)
        {
            transform.position = waypoints[0].position;
        }

        // Ensure collider is a trigger
        if (col != null)
            col.isTrigger = true;
    }

    private void Update()
    {
        if (!isAlive) return;

        if (isWaiting)
        {
            // Wait time counter
            waitCounter += Time.deltaTime;
            if (waitCounter >= waitTime)
            {
                isWaiting = false;

                // Only flip direction for horizontal patrol
                if (patrolAxis == PatrolAxis.Horizontal)
                {
                    Flip();
                }
            }
        }
        else
        {
            // Move to target waypoint
            MoveToWaypoint();

            // Check if reached waypoint
            if (Vector2.Distance(transform.position, waypoints[currentWaypointIndex].position) < 0.1f)
            {
                // Reached waypoint, start waiting
                waitCounter = 0f;
                isWaiting = true;

                // Update next waypoint index
                currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
            }
        }

        // Update animation state
        if (anim != null)
        {
            anim.SetBool("isAlive", isAlive);
            anim.SetFloat("moveSpeed", moveSpeed);
        }
    }

    private void MoveToWaypoint()
    {
        // Get target position
        Vector3 targetPosition = waypoints[currentWaypointIndex].position;

        // Select movement direction based on patrol axis
        if (patrolAxis == PatrolAxis.Horizontal)
        {
            // Horizontal movement
            transform.position = Vector3.MoveTowards(
                transform.position,
                new Vector3(targetPosition.x, transform.position.y, transform.position.z),
                moveSpeed * Time.deltaTime
            );

            // Auto-flip direction
            if ((targetPosition.x > transform.position.x && !isFacingRight) ||
                (targetPosition.x < transform.position.x && isFacingRight))
            {
                Flip();
            }
        }
        else
        {
            // Vertical movement
            transform.position = Vector3.MoveTowards(
                transform.position,
                new Vector3(transform.position.x, targetPosition.y, transform.position.z),
                moveSpeed * Time.deltaTime
            );
        }
    }

    private void Flip()
    {
        // Flip direction
        isFacingRight = !isFacingRight;
        transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isAlive) return;

        if (headCheck == null)
        {
            ApplyDamageToPlayer(collision.gameObject);
            return;
        }

        // Detect player collision
        // Debug.LogError("准备踩头");
        if (((1 << collision.gameObject.layer) & playerLayer) != 0 && !collision.GetComponent<CatController>().isDead)
        {
            // Debug.LogError("踩中");

            // Check if stomping from above
            if (!collision.GetComponent<CatController>().isDead)
            {
                bool isHeadStomp = CheckHeadStomp(collision);

                if (isHeadStomp)
                {
                    // Player stomped head, kill enemy
                    KillEnemy(collision.gameObject);
                }
                else
                {
                    // Normal collision, damage player
                    ApplyDamageToPlayer(collision.gameObject);
                }
            }
        }
    }

    private bool CheckHeadStomp(Collider2D playerCollider)
    {
        // Detect if player stomped from above
        Vector2 boxSize = new Vector2(col.bounds.size.x * 0.8f, headCheckRadius * 2);
        Collider2D[] colliders = Physics2D.OverlapBoxAll(
            headCheck.position,
            boxSize,
            0f,
            playerLayer
        );

        foreach (Collider2D collider in colliders)
        {
            if (collider == playerCollider)
            {
                // Give player bounce force
                Rigidbody2D playerRb = playerCollider.GetComponent<Rigidbody2D>();
                if (playerRb != null)
                {
                    playerRb.velocity = new Vector2(playerRb.velocity.x, stompBounce);
                }

                return true;
            }
        }

        return false;
    }

    private void ApplyDamageToPlayer(GameObject player)
    {
        // Get player health component and apply damage
        CatController playerHealth = player.GetComponent<CatController>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage();
        }
    }

    private void KillEnemy(GameObject player)
    {
        // Mark enemy as dead
        if (player.GetComponent<CatController>().isDead)
        {
            return;
        }
        isAlive = false;

        // Disable collider
        if (col != null)
        {
            col.enabled = false;
        }

        // Play death animation
        if (anim != null)
        {
            anim.SetBool("isAlive", false);
        }

        // Trigger death event
        // OnEnemyKilled?.Invoke();

        // Destroy enemy after delay
        Destroy(gameObject, 0f);
    }

    // Draw patrol path and head detection area in editor
    private void OnDrawGizmosSelected()
    {
        // Draw patrol path
        if (waypoints != null && waypoints.Length > 1)
        {
            Gizmos.color = Color.red;
            for (int i = 0; i < waypoints.Length - 1; i++)
            {
                Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
            }

            // Close loop
            if (waypoints.Length > 2)
            {
                Gizmos.DrawLine(waypoints[waypoints.Length - 1].position, waypoints[0].position);
            }
        }

        // Draw head detection area
        if (headCheck != null)
        {
            Gizmos.color = Color.green;
            Vector2 boxSize = new Vector2(col.bounds.size.x * 0.8f, headCheckRadius * 2);
            Gizmos.DrawWireCube(headCheck.position, boxSize);
        }
    }
}