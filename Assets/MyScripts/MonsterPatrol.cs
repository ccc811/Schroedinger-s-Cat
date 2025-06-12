using UnityEngine;

public class MonsterPatrol : MonoBehaviour
{
    [Header("Patrol Parameters")]
    public Transform pointA;
    public Transform pointB;
    public float patrolSpeed = 2f;
    public float chargeSpeed = 5f;
    public float detectionDistance = 5f;
    public LayerMask playerLayer;
    public LayerMask wallLayer;

    [Header("Charge Parameters")]
    public float chargeDuration = 2f; // Charge duration
    public float cooldownDuration = 1f; // Cooldown duration after charge

    [Header("Wall Bounce Parameters")]
    public float wallBounceBackSpeed = 2f; // Horizontal bounce speed after hitting wall
    public float wallBounceUpSpeed = 3f; // Vertical bounce speed after hitting wall
    public float wallBounceBackDuration = 0.3f; // Bounce duration after hitting wall

    [Header("Stun Parameters")]
    public float stunDuration = 2f; // Stun duration
    public GameObject stunSprite; // Stun state display image
    public Vector3 stunSpriteOffset = new Vector3(0, 1f, 0); // Stun image offset
    public float stunSpriteScale = 1f; // Stun image scale

    private float chargeTimer = 0f;   // Charge timer
    private float cooldownTimer = 0f; // Cooldown timer
    private float bounceTimer = 0f;   // Bounce timer
    private float stunTimer = 0f;     // Stun timer

    private bool wasHeadingToA = false; // Whether heading to point A before charge
    private bool isOnCooldown = false; // Whether in cooldown
    private bool isBouncingBack = false; // Whether bouncing back
    private bool isStunned = false; // Whether stunned

    [Header("Detection Area Parameters")]
    public float detectionHeight = 1f;
    public Color detectionColor = new Color(1, 0, 0, 0.2f);
    public Color detectedColor = new Color(0, 1, 0, 0.2f);
    public Color chargingColor = new Color(1, 0.5f, 0, 0.3f);
    public Color cooldownColor = new Color(0, 0, 1, 0.2f);
    public Color bouncingColor = new Color(0, 1, 1, 0.3f);
    public Color stunColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);

    [Header("Animation Parameters")]
    public Animator animator;
    public string walkParamName = "isWalking";
    public string chargeParamName = "isCharging";
    public string cooldownParamName = "isOnCooldown";
    public string bounceParamName = "isBouncing";
    public string stunParamName = "isStunned";

    [Header("Debug Info")]
    [SerializeField] private bool playerDetected = false;
    [SerializeField] private bool isChargingDebug = false;
    [SerializeField] private bool isOnCooldownDebug = false;
    [SerializeField] private bool isBouncingBackDebug = false;
    [SerializeField] private bool isStunnedDebug = false;
    [SerializeField] private string lastDetectionInfo = "";
    [SerializeField] private Vector2 lastHitPoint = Vector2.zero;
    [SerializeField] private Transform playerTransform = null;

    private Rigidbody2D rb;
    private Collider2D col;
    private Transform currentTarget;
    private bool isCharging = false;
    private Vector2 chargeDirection;
    private SpriteRenderer spriteRenderer;

    // Animation parameter hashes (for performance)
    private int walkParamHash;
    private int chargeParamHash;
    private int cooldownParamHash;
    private int bounceParamHash;
    private int stunParamHash;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        currentTarget = pointA;

        // Initialize animation parameter hashes
        if (animator != null)
        {
            walkParamHash = Animator.StringToHash(walkParamName);
            chargeParamHash = Animator.StringToHash(chargeParamName);
            cooldownParamHash = Animator.StringToHash(cooldownParamName);
            bounceParamHash = Animator.StringToHash(bounceParamName);
            stunParamHash = Animator.StringToHash(stunParamName);
        }


        // Output important parameter info
        Debug.Log($"Monster patrol script initialized: " +
                  $"Detection layer = {LayerMask.LayerToName(playerLayer.value)}, " +
                  $"Detection distance = {detectionDistance}, " +
                  $"Charge duration = {chargeDuration}sec, " +
                  $"Cooldown duration = {cooldownDuration}sec, " +
                  $"Wall bounce back speed = {wallBounceBackSpeed}, " +
                  $"Wall bounce up speed = {wallBounceUpSpeed}, " +
                  $"Stun duration = {stunDuration}sec");
    }

    void Update()
    {
        if (isStunned)
        {
            HandleStun();
            return;
        }

        if (isBouncingBack)
        {
            HandleBounceBack();
            return;
        }

        if (isCharging)
        {
            Charge();

            // Charge timing
            chargeTimer += Time.deltaTime;

            // Check if need to stop charging
            if (chargeTimer >= chargeDuration || HasHitWall())
            {
                StopCharging();

                if (HasHitWall())
                {
                    StartBounceBack();
                    Debug.Log($"Hit wall! Position: {transform.position}, starting bounce back");
                }
                else
                {
                    StartCooldown();
                    Debug.Log("Charge duration ended, entering cooldown");
                }
            }
            return;
        }

        if (isOnCooldown)
        {
            cooldownTimer += Time.deltaTime;
            if (cooldownTimer >= cooldownDuration)
            {
                isOnCooldown = false;
                isOnCooldownDebug = false;
                SetAnimatorBool(cooldownParamHash, false);
                Debug.Log("Cooldown ended, resuming normal patrol");
            }
            else
            {
                // Continue patrol during cooldown but don't respond to player detection
                Patrol();
                return;
            }
        }

        Patrol();
        CheckForPlayer();
    }

    void Patrol()
    {
        // Record patrol direction
        if (currentTarget == pointA)
            wasHeadingToA = true;
        else
            wasHeadingToA = false;

        if (Vector2.Distance(transform.position, currentTarget.position) < 0.1f)
        {
            currentTarget = currentTarget == pointA ? pointB : pointA;
        }

        Vector2 direction = (currentTarget.position - transform.position).normalized;
        rb.velocity = new Vector2(direction.x * patrolSpeed, rb.velocity.y);

        // Flip sprite
        if (direction.x > 0)
            spriteRenderer.flipX = false;
        else if (direction.x < 0)
            spriteRenderer.flipX = true;

        // Set patrol animation
        SetAnimatorBool(walkParamHash, Mathf.Abs(rb.velocity.x) > 0.1f);
        SetAnimatorBool(chargeParamHash, false);
        SetAnimatorBool(cooldownParamHash, false);
        SetAnimatorBool(bounceParamHash, false);
        SetAnimatorBool(stunParamHash, false);
    }

    void CheckForPlayer()
    {
        // Don't detect player during cooldown, bounce or stun
        if (isOnCooldown || isBouncingBack || isStunned)
            return;

        Vector2 raycastDirection = spriteRenderer.flipX ? Vector2.left : Vector2.right;
        Vector2 origin = (Vector2)transform.position + Vector2.up * detectionHeight / 2;

        // Use BoxCast instead of Raycast for better height detection
        RaycastHit2D hit = Physics2D.BoxCast(
            origin,
            new Vector2(0.2f, detectionHeight),
            0,
            raycastDirection,
            detectionDistance,
            playerLayer);

        bool wasDetected = playerDetected;
        playerDetected = hit.collider != null && hit.collider.CompareTag("Cat");

        // Record detection info
        lastDetectionInfo = hit.collider != null
            ? $"Detected object: {hit.collider.name}, tag: {hit.collider.tag}"
            : "No player detected";

        if (hit.collider != null)
        {
            lastHitPoint = hit.point;
            playerTransform = hit.collider.transform;
        }

        // Only output log when state changes
        if (playerDetected != wasDetected)
        {
            Debug.Log(playerDetected ?
                $"Player detected! Position: {lastHitPoint}" :
                "Player left detection range");
        }

        if (playerDetected && !isCharging && !isBouncingBack && !isStunned && !isOnCooldown)
        {
            StartCharging();
        }
    }

    void StartCharging()
    {
        isCharging = true;
        isChargingDebug = true;
        chargeTimer = 0f; // Reset charge timer

        // Calculate direction towards player
        if (playerTransform != null)
        {
            chargeDirection = (playerTransform.position - transform.position).normalized;

            // Ensure charge direction is only on X axis
            chargeDirection = new Vector2(chargeDirection.x, 0);

            // Update sprite facing
            spriteRenderer.flipX = chargeDirection.x < 0;

            Debug.Log($"Starting charge! Direction: {chargeDirection}, Player position: {playerTransform.position}");
        }
        else
        {
            // If no player Transform, use default direction
            chargeDirection = spriteRenderer.flipX ? Vector2.left : Vector2.right;
            Debug.LogWarning("Starting charge but no player Transform found, using default direction");
        }

        // Set charge animation
        SetAnimatorBool(walkParamHash, false);
        SetAnimatorBool(chargeParamHash, true);
        SetAnimatorBool(cooldownParamHash, false);
        SetAnimatorBool(bounceParamHash, false);
        SetAnimatorBool(stunParamHash, false);
    }

    void Charge()
    {
        isChargingDebug = true;

        // Set charge velocity
        rb.velocity = new Vector2(chargeDirection.x * chargeSpeed, rb.velocity.y);
    }

    bool HasHitWall()
    {
        RaycastHit2D wallHit = Physics2D.Raycast(transform.position, chargeDirection, 0.5f, wallLayer);
        return wallHit.collider != null;
    }

    void StartBounceBack()
    {
        isBouncingBack = true;
        isBouncingBackDebug = true;
        bounceTimer = 0f;

        // Set bounce direction (horizontal and vertical)
        Vector2 bounceDirection = new Vector2(-chargeDirection.x * wallBounceBackSpeed, wallBounceUpSpeed);
        rb.velocity = bounceDirection;

        // Set bounce animation
        SetAnimatorBool(chargeParamHash, false);
        SetAnimatorBool(bounceParamHash, true);
        SetAnimatorBool(stunParamHash, false);
    }

    void HandleBounceBack()
    {
        bounceTimer += Time.deltaTime;

        if (bounceTimer >= wallBounceBackDuration)
        {
            isBouncingBack = false;
            isBouncingBackDebug = false;

            // Start stun
            Stun();

            // Set end of bounce animation
            SetAnimatorBool(bounceParamHash, false);
            Debug.Log("Bounce ended, entering stun state");
        }
    }

    void StopCharging()
    {
        isCharging = false;
        isChargingDebug = false;

        // Restore patrol direction
        if (wasHeadingToA)
            currentTarget = pointA;
        else
            currentTarget = pointB;

        Debug.Log($"Stopping charge, resuming patrol, target: {currentTarget.name}");
    }

    void StartCooldown()
    {
        isOnCooldown = true;
        isOnCooldownDebug = true;
        cooldownTimer = 0f;
        SetAnimatorBool(cooldownParamHash, true);
    }

    void Stun()
    {
        isStunned = true;
        isStunnedDebug = true;
        stunTimer = 0f;
        rb.velocity = Vector2.zero;

        // Show stun sprite
        if (stunSprite != null)
            stunSprite.SetActive(true);

        // Set stun animation
        SetAnimatorBool(stunParamHash, true);

        Debug.Log("Entering stun state, duration: " + stunDuration + "sec");
    }

    void HandleStun()
    {
        stunTimer += Time.deltaTime;

        if (stunTimer >= stunDuration)
        {
            isStunned = false;
            isStunnedDebug = false;

            // Hide stun sprite
            if (stunSprite != null)
                stunSprite.SetActive(false);

            // Set end of stun animation
            SetAnimatorBool(stunParamHash, false);

            // Enter cooldown state
            StartCooldown();
            Debug.Log("Stun ended, entering cooldown state");
        }
    }

    // Helper method to safely set animator parameters
    void SetAnimatorBool(int paramHash, bool value)
    {
        if (animator != null && animator.isInitialized)
        {
            animator.SetBool(paramHash, value);
        }
    }

    void OnDrawGizmosSelected()
    {
        // Draw patrol path
        if (pointA != null && pointB != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(pointA.position, pointB.position);

            // Show current patrol target
            //Gizmos.color = currentTarget == pointA ? Color.green : Color.red;
            //Gizmos.DrawSphere(currentTarget.position, 0.2f);
        }

        // Draw detection area
        if (spriteRenderer != null)
        {
            // Use different colors based on state
            Color gizmoColor;
            if (isCharging)
                gizmoColor = chargingColor;
            else if (isOnCooldown)
                gizmoColor = cooldownColor;
            else if (isBouncingBack)
                gizmoColor = bouncingColor;
            else if (isStunned)
                gizmoColor = stunColor;
            else if (playerDetected)
                gizmoColor = detectedColor;
            else
                gizmoColor = detectionColor;

            Vector2 direction = spriteRenderer.flipX ? Vector2.left : Vector2.right;
            Vector2 size = new Vector2(detectionDistance, detectionHeight);
            Vector2 center = (Vector2)transform.position + direction * detectionDistance / 2 + Vector2.up * detectionHeight / 2;

            Gizmos.color = gizmoColor;
            Gizmos.DrawCube(center, size);

            // Draw border
            if (isCharging)
                Gizmos.color = Color.magenta;
            else if (isOnCooldown)
                Gizmos.color = Color.cyan;
            else if (isBouncingBack)
                Gizmos.color = Color.cyan;
            else if (isStunned)
                Gizmos.color = Color.gray;
            else if (playerDetected)
                Gizmos.color = Color.green;
            else
                Gizmos.color = Color.red;

            Gizmos.DrawWireCube(center, size);

            // Draw last detection point
            if (playerDetected)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawSphere(lastHitPoint, 0.1f);
            }

            // Draw remaining charge time
            if (isCharging)
            {
                Gizmos.color = Color.white;
                float remainingTime = Mathf.Max(0, chargeDuration - chargeTimer);
                Vector3 timePos = transform.position + Vector3.up * 1.5f;
               // UnityEditor.Handles.Label(timePos, $"Charge remaining: {remainingTime:F1}s");
            }

            // Draw remaining cooldown time
            if (isOnCooldown)
            {
                Gizmos.color = Color.white;
                float remainingTime = Mathf.Max(0, cooldownDuration - cooldownTimer);
                Vector3 timePos = transform.position + Vector3.up * 1.2f;
             //   UnityEditor.Handles.Label(timePos, $"Cooldown remaining: {remainingTime:F1}s");
            }

            // Draw remaining bounce time
            if (isBouncingBack)
            {
                Gizmos.color = Color.white;
                float remainingTime = Mathf.Max(0, wallBounceBackDuration - bounceTimer);
                Vector3 timePos = transform.position + Vector3.up * 0.9f;
             //   UnityEditor.Handles.Label(timePos, $"Bounce remaining: {remainingTime:F1}s");
            }

            // Draw remaining stun time
            if (isStunned)
            {
                Gizmos.color = Color.white;
                float remainingTime = Mathf.Max(0, stunDuration - stunTimer);
                Vector3 timePos = transform.position + Vector3.up * 0.6f;
              //  UnityEditor.Handles.Label(timePos, $"Stun remaining: {remainingTime:F1}s");
            }
        }
    }


    [SerializeField] private Transform headCheck; // Head detection point
    [SerializeField] private float headCheckRadius = 0.2f; // Head detection radius

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Detect player collision
        // Debug.LogError("Ready for head stomp");
        if (((1 << collision.gameObject.layer) & playerLayer) != 0)
        {
            // Check if stomped from above

            if (!collision.GetComponent<CatController>().isDead)
            {
                bool isHeadStomp = CheckHeadStomp(collision);

                if (isHeadStomp)
                {
                    // Player stomps monster's head
                    Debug.LogError("Head stomp");
                    Stun();
                    animator.Play("Idle");
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
        // Check if player stomped from above
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
                // Give player a bounce force
                Rigidbody2D playerRb = playerCollider.GetComponent<Rigidbody2D>();
                if (playerRb != null)
                {
                    playerRb.velocity = new Vector2(playerRb.velocity.x, 8);
                }

                return true;
            }
        }

        return false;
    }
    private void ApplyDamageToPlayer(GameObject player)
    {
        // Get player health component and deal damage
        CatController playerHealth = player.GetComponent<CatController>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage();
        }
    }
}
