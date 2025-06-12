using UnityEngine;

public class MonsterPatrolNew : MonoBehaviour
{
    [Header("Patrol Parameters")]
    public Transform pointA;
    public Transform pointB;
    public float patrolSpeed = 2f;
    public float chargeSpeed = 5f;
    public float cooldownFollowSpeed = 3f; // Follow speed during cooldown

    [Header("Detection Parameters")]
    public float detectionDistance = 5f;
    public float detectionHeight = 1f;
    public float sideDetectionHeight = 1f;
    public float sideDetectionWidth = 0.5f;
    public float sideDetectionDistance = 50f;
    public LayerMask playerLayer;
    public LayerMask wallLayer;

    [Header("Charge Parameters")]
    public float chargeDuration = 2f;
    public float cooldownDuration = 1f;
    public float chargeWarningTime = 1f;

    [Header("Collision Parameters")]
    public float wallBounceBackSpeed = 2f;
    public float wallBounceUpSpeed = 3f;
    public float wallBounceBackDuration = 0.3f;
    public float stunDuration = 2f;

    [Header("Visual Elements")]
    public GameObject stunSprite;
    public Vector3 stunSpriteOffset;
    public float stunSpriteScale = 1f;
    public GameObject chargeWarningSprite;
    public Vector3 chargeWarningSpriteOffset;
    public float chargeWarningSpriteScale = 1f;

    // State timers
    private float chargeTimer;
    private float cooldownTimer;
    private float bounceTimer;
    private float stunTimer;
    private float chargeWarningTimer;

    // State flags
    private bool isCharging;
    private bool isOnCooldown;
    private bool isBouncingBack;
    private bool isStunned;
    private bool isShowingChargeWarning;
    private bool isFollowingPlayer;
    private bool wasHeadingToA;
    private bool isWaitingForCharge;

    // Component references
    private Rigidbody2D rb;
    private Collider2D col;
    private SpriteRenderer spriteRenderer;
    private Animator animator;

    // Movement direction and target
    private Vector2 chargeDirection;
    private Transform currentTarget;
    private Transform playerTransform;

    // Animation parameter hashes
    private int walkParamHash;
    private int chargeParamHash;
    private int cooldownParamHash;
    private int bounceParamHash;
    private int stunParamHash;
    private int warningParamHash;
    private int followParamHash;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        currentTarget = pointA;

        // Initialize animation parameter hashes
        if (animator != null)
        {
            walkParamHash = Animator.StringToHash("isWalking");
            chargeParamHash = Animator.StringToHash("isCharging");
            cooldownParamHash = Animator.StringToHash("isOnCooldown");
            bounceParamHash = Animator.StringToHash("isBouncing");
            stunParamHash = Animator.StringToHash("isStunned");
            warningParamHash = Animator.StringToHash("isWarning");
            followParamHash = Animator.StringToHash("isFollowing");
        }

        //// Initialize visual elements
        //if (stunSprite != null)
        //{
        //    stunSprite.SetActive(false);
        //    stunSprite.transform.parent = transform;
        //    stunSprite.transform.localPosition = stunSpriteOffset;
        //    stunSprite.transform.localScale = Vector3.one * stunSpriteScale;
        //}

        //if (chargeWarningSprite != null)
        //{
        //    chargeWarningSprite.SetActive(false);
        //    chargeWarningSprite.transform.parent = transform;
        //    chargeWarningSprite.transform.localPosition = chargeWarningSpriteOffset;
        //    chargeWarningSprite.transform.localScale = Vector3.one * chargeWarningSpriteScale;
        //}
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
            HandleCharging();
            return;
        }

        // Detect player (even during cooldown)
        DetectPlayer();

        if (isOnCooldown)
        {
            HandleCooldown();
            return;
        }

        if (isShowingChargeWarning)
        {
            HandleChargeWarning();
            return;
        }

        if (isFollowingPlayer)
        {
            FollowPlayer();
        }
        else
        {
            Patrol();
        }
    }

    void Patrol()
    {
        // Record patrol direction
        wasHeadingToA = currentTarget == pointA;

        // Switch target when reaching the destination
        if (Vector2.Distance(transform.position, currentTarget.position) < 0.1f)
        {
            currentTarget = wasHeadingToA ? pointB : pointA;
        }

        // Move towards target
        Vector2 direction = (currentTarget.position - transform.position).normalized;
        rb.velocity = new Vector2(direction.x * patrolSpeed, rb.velocity.y);

        // Flip sprite
        spriteRenderer.flipX = direction.x < 0;

        // Set animation
        SetAnimatorBool(walkParamHash, true);
        SetAnimatorBool(chargeParamHash, false);
        SetAnimatorBool(cooldownParamHash, false);
        SetAnimatorBool(bounceParamHash, false);
        SetAnimatorBool(stunParamHash, false);
        SetAnimatorBool(warningParamHash, false);
        SetAnimatorBool(followParamHash, false);
    }

    void DetectPlayer()
    {
        // Reset player detection state
        isFollowingPlayer = false;
        playerTransform = null;

        // Left detection area
        Vector2 leftOrigin = (Vector2)transform.position + Vector2.up * sideDetectionHeight / 2;
        Vector2 leftSize = new Vector2(sideDetectionWidth, sideDetectionHeight);

        RaycastHit2D[] leftHits = Physics2D.BoxCastAll(
            leftOrigin,
            leftSize,
            0,
            Vector2.left,
            sideDetectionDistance,
            playerLayer);

        // Right detection area
        Vector2 rightOrigin = (Vector2)transform.position + Vector2.up * sideDetectionHeight / 2;
        Vector2 rightSize = new Vector2(sideDetectionWidth, sideDetectionHeight);

        RaycastHit2D[] rightHits = Physics2D.BoxCastAll(
            rightOrigin,
            rightSize,
            0,
            Vector2.right,
            sideDetectionDistance,
            playerLayer);

        // Process left detection results
        foreach (RaycastHit2D hit in leftHits)
        {
            if (hit.collider != null && hit.collider.CompareTag("Cat"))
            {
                isFollowingPlayer = true;
                playerTransform = hit.collider.transform;
                Debug.Log("Player detected in left detection area");
                break;
            }
        }

        // Process right detection results (if not detected on left)
        if (!isFollowingPlayer)
        {
            foreach (RaycastHit2D hit in rightHits)
            {
                if (hit.collider != null && hit.collider.CompareTag("Cat"))
                {
                    isFollowingPlayer = true;
                    playerTransform = hit.collider.transform;
                    Debug.Log("Player detected in right detection area");
                    break;
                }
            }
        }
    }

    void FollowPlayer()
    {
        if (playerTransform == null)
        {
            isFollowingPlayer = false;
            return;
        }

        // Follow player with appropriate speed
        float followSpeed = isOnCooldown ? cooldownFollowSpeed : patrolSpeed;

        // Calculate direction towards player
        Vector2 direction = (playerTransform.position - transform.position).normalized;
        rb.velocity = new Vector2(direction.x * followSpeed, rb.velocity.y);

        // Flip sprite
        spriteRenderer.flipX = direction.x < 0;

        // Set animation
        SetAnimatorBool(walkParamHash, true);
        SetAnimatorBool(chargeParamHash, false);
        SetAnimatorBool(cooldownParamHash, isOnCooldown);
        SetAnimatorBool(bounceParamHash, false);
        SetAnimatorBool(stunParamHash, false);
        SetAnimatorBool(warningParamHash, false);
        SetAnimatorBool(followParamHash, true);

        // If player is detected and close enough, prepare to charge (only when not on cooldown)
        if (!isOnCooldown && playerTransform != null)
        {
            float distance = Vector2.Distance(transform.position, playerTransform.position);
            if (distance < detectionDistance)
            {
                StartChargeWarning();
            }
        }
    }

    void StartChargeWarning()
    {
        isShowingChargeWarning = true;
        isWaitingForCharge = true;
        chargeWarningTimer = 0f;

        // Calculate charge direction
        if (playerTransform != null)
        {
            chargeDirection = (playerTransform.position - transform.position).normalized;
            chargeDirection = new Vector2(chargeDirection.x, 0);
            spriteRenderer.flipX = chargeDirection.x < 0;
        }
        else
        {
            chargeDirection = spriteRenderer.flipX ? Vector2.left : Vector2.right;
        }

        // Show warning visual
        if (chargeWarningSprite != null)
            chargeWarningSprite.SetActive(true);

        // Stop movement
        rb.velocity = Vector2.zero;

        // Set animation
        SetAnimatorBool(walkParamHash, false);
        SetAnimatorBool(chargeParamHash, false);
        SetAnimatorBool(cooldownParamHash, false);
        SetAnimatorBool(bounceParamHash, false);
        SetAnimatorBool(stunParamHash, false);
        SetAnimatorBool(warningParamHash, true);
        SetAnimatorBool(followParamHash, false);

        Debug.Log("Charge warning started");
    }

    void HandleChargeWarning()
    {
        chargeWarningTimer += Time.deltaTime;

        if (chargeWarningTimer >= chargeWarningTime)
        {
            isShowingChargeWarning = false;
            isWaitingForCharge = false;

            // Hide warning visual
            if (chargeWarningSprite != null)
                chargeWarningSprite.SetActive(false);

            // Start charging
            StartCharging();
        }
    }

    void StartCharging()
    {
        isCharging = true;
        chargeTimer = 0f;

        // Set animation
        SetAnimatorBool(chargeParamHash, true);
        SetAnimatorBool(warningParamHash, false);

        Debug.Log("Charging started");
    }

    void HandleCharging()
    {
        // Set charge velocity
        rb.velocity = new Vector2(chargeDirection.x * chargeSpeed, rb.velocity.y);

        // Update timer
        chargeTimer += Time.deltaTime;

        // Check if hit a wall or charge time ended
        if (HasHitWall() || chargeTimer >= chargeDuration)
        {
            StopCharging();

            if (HasHitWall())
            {
                StartBounceBack();
                Debug.Log("Hit a wall, starting bounce back");
            }
            else
            {
                StartCooldown();
                Debug.Log("Charge time ended, entering cooldown");
            }
        }
    }

    bool HasHitWall()
    {
        Vector2 origin = transform.position;
        Vector2 direction = chargeDirection.normalized;
        float distance = 0.5f;

        RaycastHit2D hit = Physics2D.Raycast(
            origin,
            direction,
            distance,
            wallLayer);

        return hit.collider != null;
    }

    void StartBounceBack()
    {
        isBouncingBack = true;
        bounceTimer = 0f;

        // Set bounce velocity
        Vector2 bounceDirection = new Vector2(-chargeDirection.x * wallBounceBackSpeed, wallBounceUpSpeed);
        rb.velocity = bounceDirection;

        // Set animation
        SetAnimatorBool(bounceParamHash, true);
        SetAnimatorBool(chargeParamHash, false);
    }

    void HandleBounceBack()
    {
        bounceTimer += Time.deltaTime;

        if (bounceTimer >= wallBounceBackDuration)
        {
            isBouncingBack = false;
            StartStun();
            Debug.Log("Bounce back ended, entering stun");
        }
    }

    void StartStun()
    {
        isStunned = true;
        stunTimer = 0f;
        rb.velocity = Vector2.zero;

        // Show stun visual
        if (stunSprite != null)
            stunSprite.SetActive(true);

        // Set animation
        animator.Play("Idle");
        SetAnimatorBool(stunParamHash, true);
        SetAnimatorBool(bounceParamHash, false);
    }

    void HandleStun()
    {
        stunTimer += Time.deltaTime;

        if (stunTimer >= stunDuration)
        {
            isStunned = false;

            // Hide stun visual
            if (stunSprite != null)
                stunSprite.SetActive(false);

            StartCooldown();
            Debug.Log("Stun ended, entering cooldown");
        }
    }

    void StopCharging()
    {
        isCharging = false;
        rb.velocity = Vector2.zero;
    }

    void StartCooldown()
    {
        isOnCooldown = true;
        cooldownTimer = 0f;

        // Set animation
        SetAnimatorBool(cooldownParamHash, true);
    }

    void HandleCooldown()
    {
        cooldownTimer += Time.deltaTime;

        // During cooldown, follow player if detected, otherwise patrol
        if (isFollowingPlayer)
        {
            FollowPlayer();
        }
        else
        {
            Patrol();
        }

        if (cooldownTimer >= cooldownDuration)
        {
            isOnCooldown = false;
            Debug.Log("Cooldown ended, resuming normal patrol");
        }
    }

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
        }

        // Draw left and right detection areas
        if (spriteRenderer != null)
        {
            // Left detection area
            Vector2 leftOrigin = (Vector2)transform.position + Vector2.up * sideDetectionHeight / 2;
            Vector2 leftSize = new Vector2(sideDetectionWidth, sideDetectionHeight);

            Gizmos.color = isFollowingPlayer ? Color.green : Color.red;
            Gizmos.DrawCube(
                leftOrigin + Vector2.left * sideDetectionDistance / 2,
                new Vector3(sideDetectionDistance, sideDetectionHeight, 0.1f));

            // Right detection area
            Vector2 rightOrigin = (Vector2)transform.position + Vector2.up * sideDetectionHeight / 2;
            Vector2 rightSize = new Vector2(sideDetectionWidth, sideDetectionHeight);

            Gizmos.DrawCube(
                rightOrigin + Vector2.right * sideDetectionDistance / 2,
                new Vector3(sideDetectionDistance, sideDetectionHeight, 0.1f));
        }

        // Draw charge detection area
        if (spriteRenderer != null)
        {
            Color gizmoColor;
            if (isCharging)
                gizmoColor = Color.magenta;
            else if (isOnCooldown)
                gizmoColor = Color.blue;
            else if (isBouncingBack)
                gizmoColor = Color.cyan;
            else if (isStunned)
                gizmoColor = Color.gray;
            else if (isShowingChargeWarning)
                gizmoColor = Color.yellow;
            else if (isFollowingPlayer)
                gizmoColor = Color.green;
            else
                gizmoColor = Color.red;

            Vector2 direction = spriteRenderer.flipX ? Vector2.left : Vector2.right;
            Vector2 size = new Vector2(detectionDistance, detectionHeight);
            Vector2 center = (Vector2)transform.position + direction * detectionDistance / 2 + Vector2.up * detectionHeight / 2;

            Gizmos.color = gizmoColor;
            Gizmos.DrawCube(center, size);

            // Draw wireframe
            Gizmos.color = Color.black;
            Gizmos.DrawWireCube(center, size);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Handle player stomping from above
        if ((1 << collision.gameObject.layer & playerLayer) != 0 && !collision.GetComponent<CatController>().isDead)
        {
            if (IsHeadStomp(collision))
            {
                StartStun();
                Debug.Log("Stomped by player, entering stun");
            }
            else
            {
                // Logic to deal damage to player
                ApplyDamageToPlayer(collision.gameObject);
            }
        }
    }

    private bool IsHeadStomp(Collider2D playerCollider)
    {
        // Logic to detect stomp from above
        Vector2 boxSize = new Vector2(col.bounds.size.x * 0.8f, 0.2f);
        Vector2 boxCenter = (Vector2)transform.position + Vector2.up * (col.bounds.size.y * 0.5f + 0.1f);

        Collider2D[] colliders = Physics2D.OverlapBoxAll(boxCenter, boxSize, 0, playerLayer);
        foreach (Collider2D collider in colliders)
        {
            if (collider == playerCollider)
            {
                // Give player bounce force
                Rigidbody2D playerRb = playerCollider.GetComponent<Rigidbody2D>();
                if (playerRb != null)
                {
                    playerRb.velocity = new Vector2(playerRb.velocity.x, 8f);
                }
                return true;
            }
        }
        return false;
    }

    private void ApplyDamageToPlayer(GameObject player)
    {
        // Logic to deal damage to player
        CatController playerHealth = player.GetComponent<CatController>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage();
        }
    }
}