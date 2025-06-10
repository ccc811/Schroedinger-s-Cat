using UnityEngine;

public class MonsterPatrolNew : MonoBehaviour
{
    [Header("巡逻参数")]
    public Transform pointA;
    public Transform pointB;
    public float patrolSpeed = 2f;
    public float chargeSpeed = 5f;
    public float cooldownFollowSpeed = 3f; // 冷却期间跟随速度

    [Header("检测参数")]
    public float detectionDistance = 5f;
    public float detectionHeight = 1f;
    public float sideDetectionHeight = 1f;
    public float sideDetectionWidth = 0.5f;
    public float sideDetectionDistance = 50f;
    public LayerMask playerLayer;
    public LayerMask wallLayer;

    [Header("冲锋参数")]
    public float chargeDuration = 2f;
    public float cooldownDuration = 1f;
    public float chargeWarningTime = 1f;

    [Header("碰撞参数")]
    public float wallBounceBackSpeed = 2f;
    public float wallBounceUpSpeed = 3f;
    public float wallBounceBackDuration = 0.3f;
    public float stunDuration = 2f;

    [Header("视觉元素")]
    public GameObject stunSprite;
    public Vector3 stunSpriteOffset;
    public float stunSpriteScale = 1f;
    public GameObject chargeWarningSprite;
    public Vector3 chargeWarningSpriteOffset;
    public float chargeWarningSpriteScale = 1f;

    // 状态计时器
    private float chargeTimer;
    private float cooldownTimer;
    private float bounceTimer;
    private float stunTimer;
    private float chargeWarningTimer;

    // 状态标志
    private bool isCharging;
    private bool isOnCooldown;
    private bool isBouncingBack;
    private bool isStunned;
    private bool isShowingChargeWarning;
    private bool isFollowingPlayer;
    private bool wasHeadingToA;
    private bool isWaitingForCharge;

    // 组件引用
    private Rigidbody2D rb;
    private Collider2D col;
    private SpriteRenderer spriteRenderer;
    private Animator animator;

    // 移动方向和目标
    private Vector2 chargeDirection;
    private Transform currentTarget;
    private Transform playerTransform;

    // 动画参数哈希
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

        // 初始化动画参数哈希
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

        //// 初始化视觉元素
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

        // 检测玩家（即使在冷却期间也需要检测）
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
        // 记录巡逻方向
        wasHeadingToA = currentTarget == pointA;

        // 到达目标点时切换
        if (Vector2.Distance(transform.position, currentTarget.position) < 0.1f)
        {
            currentTarget = wasHeadingToA ? pointB : pointA;
        }

        // 向目标点移动
        Vector2 direction = (currentTarget.position - transform.position).normalized;
        rb.velocity = new Vector2(direction.x * patrolSpeed, rb.velocity.y);

        // 翻转精灵
        spriteRenderer.flipX = direction.x < 0;

        // 设置动画
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
        // 重置玩家检测状态
        isFollowingPlayer = false;
        playerTransform = null;

        // 左侧检测区域
        Vector2 leftOrigin = (Vector2)transform.position + Vector2.up * sideDetectionHeight / 2;
        Vector2 leftSize = new Vector2(sideDetectionWidth, sideDetectionHeight);

        RaycastHit2D[] leftHits = Physics2D.BoxCastAll(
            leftOrigin,
            leftSize,
            0,
            Vector2.left,
            sideDetectionDistance,
            playerLayer);

        // 右侧检测区域
        Vector2 rightOrigin = (Vector2)transform.position + Vector2.up * sideDetectionHeight / 2;
        Vector2 rightSize = new Vector2(sideDetectionWidth, sideDetectionHeight);

        RaycastHit2D[] rightHits = Physics2D.BoxCastAll(
            rightOrigin,
            rightSize,
            0,
            Vector2.right,
            sideDetectionDistance,
            playerLayer);

        // 处理左侧检测结果
        foreach (RaycastHit2D hit in leftHits)
        {
            if (hit.collider != null && hit.collider.CompareTag("Cat"))
            {
                isFollowingPlayer = true;
                playerTransform = hit.collider.transform;
                Debug.Log("左侧检测区域检测到玩家");
                break;
            }
        }

        // 处理右侧检测结果（如果左侧未检测到）
        if (!isFollowingPlayer)
        {
            foreach (RaycastHit2D hit in rightHits)
            {
                if (hit.collider != null && hit.collider.CompareTag("Cat"))
                {
                    isFollowingPlayer = true;
                    playerTransform = hit.collider.transform;
                    Debug.Log("右侧检测区域检测到玩家");
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

        // 使用适当的速度跟随玩家
        float followSpeed = isOnCooldown ? cooldownFollowSpeed : patrolSpeed;

        // 计算朝向玩家的方向
        Vector2 direction = (playerTransform.position - transform.position).normalized;
        rb.velocity = new Vector2(direction.x * followSpeed, rb.velocity.y);

        // 翻转精灵
        spriteRenderer.flipX = direction.x < 0;

        // 设置动画
        SetAnimatorBool(walkParamHash, true);
        SetAnimatorBool(chargeParamHash, false);
        SetAnimatorBool(cooldownParamHash, isOnCooldown);
        SetAnimatorBool(bounceParamHash, false);
        SetAnimatorBool(stunParamHash, false);
        SetAnimatorBool(warningParamHash, false);
        SetAnimatorBool(followParamHash, true);

        // 如果检测到玩家且距离足够近，准备冲锋（仅在非冷却状态下）
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

        // 计算冲锋方向
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

        // 显示预警图片
        if (chargeWarningSprite != null)
            chargeWarningSprite.SetActive(true);

        // 停止移动
        rb.velocity = Vector2.zero;

        // 设置动画
        SetAnimatorBool(walkParamHash, false);
        SetAnimatorBool(chargeParamHash, false);
        SetAnimatorBool(cooldownParamHash, false);
        SetAnimatorBool(bounceParamHash, false);
        SetAnimatorBool(stunParamHash, false);
        SetAnimatorBool(warningParamHash, true);
        SetAnimatorBool(followParamHash, false);

        Debug.Log("开始冲锋预警");
    }

    void HandleChargeWarning()
    {
        chargeWarningTimer += Time.deltaTime;

        if (chargeWarningTimer >= chargeWarningTime)
        {
            isShowingChargeWarning = false;
            isWaitingForCharge = false;

            // 隐藏预警图片
            if (chargeWarningSprite != null)
                chargeWarningSprite.SetActive(false);

            // 开始冲锋
            StartCharging();
        }
    }

    void StartCharging()
    {
        isCharging = true;
        chargeTimer = 0f;

        // 设置动画
        SetAnimatorBool(chargeParamHash, true);
        SetAnimatorBool(warningParamHash, false);

        Debug.Log("开始冲锋");
    }

    void HandleCharging()
    {
        // 设置冲锋速度
        rb.velocity = new Vector2(chargeDirection.x * chargeSpeed, rb.velocity.y);

        // 更新计时器
        chargeTimer += Time.deltaTime;

        // 检查是否撞到墙或冲锋时间结束
        if (HasHitWall() || chargeTimer >= chargeDuration)
        {
            StopCharging();

            if (HasHitWall())
            {
                StartBounceBack();
                Debug.Log("撞到墙，开始回弹");
            }
            else
            {
                StartCooldown();
                Debug.Log("冲锋时间结束，进入冷却");
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

        // 设置回弹速度
        Vector2 bounceDirection = new Vector2(-chargeDirection.x * wallBounceBackSpeed, wallBounceUpSpeed);
        rb.velocity = bounceDirection;

        // 设置动画
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
            Debug.Log("回弹结束，进入眩晕");
        }
    }

    void StartStun()
    {
        isStunned = true;
        stunTimer = 0f;
        rb.velocity = Vector2.zero;

        // 显示眩晕图片
        if (stunSprite != null)
            stunSprite.SetActive(true);

        // 设置动画
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

            // 隐藏眩晕图片
            if (stunSprite != null)
                stunSprite.SetActive(false);

            StartCooldown();
            Debug.Log("眩晕结束，进入冷却");
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

        // 设置动画
        SetAnimatorBool(cooldownParamHash, true);
    }

    void HandleCooldown()
    {
        cooldownTimer += Time.deltaTime;

        // 在冷却期间，如果检测到玩家则跟随，否则继续巡逻
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
            Debug.Log("冷却结束，恢复正常巡逻");
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
        // 绘制巡逻路径
        if (pointA != null && pointB != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(pointA.position, pointB.position);
        }

        // 绘制左右检测区域
        if (spriteRenderer != null)
        {
            // 左侧检测区域
            Vector2 leftOrigin = (Vector2)transform.position + Vector2.up * sideDetectionHeight / 2;
            Vector2 leftSize = new Vector2(sideDetectionWidth, sideDetectionHeight);

            Gizmos.color = isFollowingPlayer ? Color.green : Color.red;
            Gizmos.DrawCube(
                leftOrigin + Vector2.left * sideDetectionDistance / 2,
                new Vector3(sideDetectionDistance, sideDetectionHeight, 0.1f));

            // 右侧检测区域
            Vector2 rightOrigin = (Vector2)transform.position + Vector2.up * sideDetectionHeight / 2;
            Vector2 rightSize = new Vector2(sideDetectionWidth, sideDetectionHeight);

            Gizmos.DrawCube(
                rightOrigin + Vector2.right * sideDetectionDistance / 2,
                new Vector3(sideDetectionDistance, sideDetectionHeight, 0.1f));
        }

        // 绘制冲锋检测区域
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

            // 绘制边框
            Gizmos.color = Color.black;
            Gizmos.DrawWireCube(center, size);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 玩家从头顶踩下的处理
        if ((1 << collision.gameObject.layer & playerLayer) != 0 && !collision.GetComponent<CatController>().isDead)
        {
            if (IsHeadStomp(collision))
            {
                StartStun();
                Debug.Log("被玩家踩头，进入眩晕");

            }
            else
            {
                // 对玩家造成伤害的逻辑
                ApplyDamageToPlayer(collision.gameObject);
            }
        }
    }

    private bool IsHeadStomp(Collider2D playerCollider)
    {
        // 检测是否从头顶踩下的逻辑
        Vector2 boxSize = new Vector2(col.bounds.size.x * 0.8f, 0.2f);
        Vector2 boxCenter = (Vector2)transform.position + Vector2.up * (col.bounds.size.y * 0.5f + 0.1f);

        Collider2D[] colliders = Physics2D.OverlapBoxAll(boxCenter, boxSize, 0, playerLayer);
        foreach (Collider2D collider in colliders)
        {
            if (collider == playerCollider)
            {
                // 给玩家一个弹跳力
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
        // 对玩家造成伤害的逻辑
        CatController playerHealth = player.GetComponent<CatController>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage();
        }
    }
}