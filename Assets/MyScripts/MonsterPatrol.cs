using UnityEngine;

public class MonsterPatrol : MonoBehaviour
{
    [Header("巡逻参数")]
    public Transform pointA;
    public Transform pointB;
    public float patrolSpeed = 2f;
    public float chargeSpeed = 5f;
    public float detectionDistance = 5f;
    public LayerMask playerLayer;
    public LayerMask wallLayer;

    [Header("冲锋参数")]
    public float chargeDuration = 2f; // 冲锋持续时间
    public float cooldownDuration = 1f; // 冲锋冷却时间

    [Header("撞墙回弹参数")]
    public float wallBounceBackSpeed = 2f; // 撞墙后的水平回弹速度
    public float wallBounceUpSpeed = 3f; // 撞墙后的垂直弹跳速度
    public float wallBounceBackDuration = 0.3f; // 撞墙后回弹的持续时间

    [Header("眩晕参数")]
    public float stunDuration = 2f; // 眩晕持续时间
    public GameObject stunSprite; // 眩晕状态显示的图片
    public Vector3 stunSpriteOffset = new Vector3(0, 1f, 0); // 眩晕图片偏移量
    public float stunSpriteScale = 1f; // 眩晕图片缩放

    private float chargeTimer = 0f;   // 冲锋计时器
    private float cooldownTimer = 0f; // 冷却计时器
    private float bounceTimer = 0f;   // 回弹计时器
    private float stunTimer = 0f;     // 眩晕计时器

    private bool wasHeadingToA = false; // 冲锋前是否正前往A点
    private bool isOnCooldown = false; // 是否在冷却中
    private bool isBouncingBack = false; // 是否在回弹中
    private bool isStunned = false; // 是否在眩晕中

    [Header("检测区域参数")]
    public float detectionHeight = 1f;
    public Color detectionColor = new Color(1, 0, 0, 0.2f);
    public Color detectedColor = new Color(0, 1, 0, 0.2f);
    public Color chargingColor = new Color(1, 0.5f, 0, 0.3f);
    public Color cooldownColor = new Color(0, 0, 1, 0.2f);
    public Color bouncingColor = new Color(0, 1, 1, 0.3f);
    public Color stunColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);

    [Header("动画参数")]
    public Animator animator;
    public string walkParamName = "isWalking";
    public string chargeParamName = "isCharging";
    public string cooldownParamName = "isOnCooldown";
    public string bounceParamName = "isBouncing";
    public string stunParamName = "isStunned";

    [Header("调试信息")]
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

    // 动画参数哈希值（提高性能）
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

        // 初始化动画参数哈希值
        if (animator != null)
        {
            walkParamHash = Animator.StringToHash(walkParamName);
            chargeParamHash = Animator.StringToHash(chargeParamName);
            cooldownParamHash = Animator.StringToHash(cooldownParamName);
            bounceParamHash = Animator.StringToHash(bounceParamName);
            stunParamHash = Animator.StringToHash(stunParamName);
        }


        // 输出重要参数信息
        Debug.Log($"怪物巡逻脚本初始化: " +
                  $"检测层级 = {LayerMask.LayerToName(playerLayer.value)}, " +
                  $"检测距离 = {detectionDistance}, " +
                  $"冲锋持续时间 = {chargeDuration}秒, " +
                  $"冲锋冷却时间 = {cooldownDuration}秒, " +
                  $"撞墙水平回弹速度 = {wallBounceBackSpeed}, " +
                  $"撞墙垂直弹跳速度 = {wallBounceUpSpeed}, " +
                  $"眩晕持续时间 = {stunDuration}秒");
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

            // 冲锋计时
            chargeTimer += Time.deltaTime;

            // 检查是否需要停止冲锋
            if (chargeTimer >= chargeDuration || HasHitWall())
            {
                StopCharging();

                if (HasHitWall())
                {
                    StartBounceBack();
                    Debug.Log($"撞到墙！位置: {transform.position}，开始弹跳回弹");
                }
                else
                {
                    StartCooldown();
                    Debug.Log("冲锋时间结束，进入冷却状态");
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
                Debug.Log("冷却结束，恢复正常巡逻");
            }
            else
            {
                // 在冷却期间继续巡逻，但不响应玩家检测
                Patrol();
                return;
            }
        }

        Patrol();
        CheckForPlayer();
    }

    void Patrol()
    {
        // 记录巡逻方向
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

        // 翻转精灵
        if (direction.x > 0)
            spriteRenderer.flipX = false;
        else if (direction.x < 0)
            spriteRenderer.flipX = true;

        // 设置巡逻动画
        SetAnimatorBool(walkParamHash, Mathf.Abs(rb.velocity.x) > 0.1f);
        SetAnimatorBool(chargeParamHash, false);
        SetAnimatorBool(cooldownParamHash, false);
        SetAnimatorBool(bounceParamHash, false);
        SetAnimatorBool(stunParamHash, false);
    }

    void CheckForPlayer()
    {
        // 在冷却、回弹或眩晕期间不检测玩家
        if (isOnCooldown || isBouncingBack || isStunned)
            return;

        Vector2 raycastDirection = spriteRenderer.flipX ? Vector2.left : Vector2.right;
        Vector2 origin = (Vector2)transform.position + Vector2.up * detectionHeight / 2;

        // 使用BoxCast替代Raycast，增加检测高度
        RaycastHit2D hit = Physics2D.BoxCast(
            origin,
            new Vector2(0.2f, detectionHeight),
            0,
            raycastDirection,
            detectionDistance,
            playerLayer);

        bool wasDetected = playerDetected;
        playerDetected = hit.collider != null && hit.collider.CompareTag("Cat");

        // 记录检测信息
        lastDetectionInfo = hit.collider != null
            ? $"检测到对象: {hit.collider.name}, 标签: {hit.collider.tag}"
            : "未检测到玩家";

        if (hit.collider != null)
        {
            lastHitPoint = hit.point;
            playerTransform = hit.collider.transform;
        }

        // 仅在状态变化时输出日志
        if (playerDetected != wasDetected)
        {
            Debug.Log(playerDetected ?
                $"玩家被检测到！位置: {lastHitPoint}" :
                "玩家已脱离检测范围");
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
        chargeTimer = 0f; // 重置冲锋计时器

        // 计算朝向玩家的方向
        if (playerTransform != null)
        {
            chargeDirection = (playerTransform.position - transform.position).normalized;

            // 确保冲锋方向只在X轴上
            chargeDirection = new Vector2(chargeDirection.x, 0);

            // 更新精灵朝向
            spriteRenderer.flipX = chargeDirection.x < 0;

            Debug.Log($"开始冲锋！方向: {chargeDirection}, 玩家位置: {playerTransform.position}");
        }
        else
        {
            // 如果没有获取到玩家Transform，使用默认方向
            chargeDirection = spriteRenderer.flipX ? Vector2.left : Vector2.right;
            Debug.LogWarning("开始冲锋，但没有获取到玩家Transform，使用默认方向");
        }

        // 设置冲锋动画
        SetAnimatorBool(walkParamHash, false);
        SetAnimatorBool(chargeParamHash, true);
        SetAnimatorBool(cooldownParamHash, false);
        SetAnimatorBool(bounceParamHash, false);
        SetAnimatorBool(stunParamHash, false);
    }

    void Charge()
    {
        isChargingDebug = true;

        // 设置冲锋速度
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

        // 设置回弹方向 (水平和垂直)
        Vector2 bounceDirection = new Vector2(-chargeDirection.x * wallBounceBackSpeed, wallBounceUpSpeed);
        rb.velocity = bounceDirection;

        // 设置回弹动画
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

            // 开始眩晕
            Stun();

            // 设置回弹结束动画
            SetAnimatorBool(bounceParamHash, false);
            Debug.Log("回弹结束，进入眩晕状态");
        }
    }

    void StopCharging()
    {
        isCharging = false;
        isChargingDebug = false;

        // 恢复巡逻方向
        if (wasHeadingToA)
            currentTarget = pointA;
        else
            currentTarget = pointB;

        Debug.Log($"停止冲锋，恢复巡逻，目标点: {currentTarget.name}");
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

        // 显示眩晕图片
        if (stunSprite != null)
            stunSprite.SetActive(true);

        // 设置眩晕动画
        SetAnimatorBool(stunParamHash, true);

        Debug.Log("进入眩晕状态，持续时间: " + stunDuration + "秒");
    }

    void HandleStun()
    {
        stunTimer += Time.deltaTime;

        if (stunTimer >= stunDuration)
        {
            isStunned = false;
            isStunnedDebug = false;

            // 隐藏眩晕图片
            if (stunSprite != null)
                stunSprite.SetActive(false);

            // 设置眩晕结束动画
            SetAnimatorBool(stunParamHash, false);

            // 进入冷却状态
            StartCooldown();
            Debug.Log("眩晕结束，进入冷却状态");
        }
    }

    // 安全设置动画参数的辅助方法
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

            // 显示当前巡逻目标
            //Gizmos.color = currentTarget == pointA ? Color.green : Color.red;
            //Gizmos.DrawSphere(currentTarget.position, 0.2f);
        }

        // 绘制检测区域
        if (spriteRenderer != null)
        {
            // 根据状态使用不同颜色
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

            // 绘制边框
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

            // 绘制最后一次检测点
            if (playerDetected)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawSphere(lastHitPoint, 0.1f);
            }

            // 绘制冲锋剩余时间
            if (isCharging)
            {
                Gizmos.color = Color.white;
                float remainingTime = Mathf.Max(0, chargeDuration - chargeTimer);
                Vector3 timePos = transform.position + Vector3.up * 1.5f;
               // UnityEditor.Handles.Label(timePos, $"冲锋剩余: {remainingTime:F1}s");
            }

            // 绘制冷却剩余时间
            if (isOnCooldown)
            {
                Gizmos.color = Color.white;
                float remainingTime = Mathf.Max(0, cooldownDuration - cooldownTimer);
                Vector3 timePos = transform.position + Vector3.up * 1.2f;
             //   UnityEditor.Handles.Label(timePos, $"冷却剩余: {remainingTime:F1}s");
            }

            // 绘制回弹剩余时间
            if (isBouncingBack)
            {
                Gizmos.color = Color.white;
                float remainingTime = Mathf.Max(0, wallBounceBackDuration - bounceTimer);
                Vector3 timePos = transform.position + Vector3.up * 0.9f;
             //   UnityEditor.Handles.Label(timePos, $"回弹剩余: {remainingTime:F1}s");
            }

            // 绘制眩晕剩余时间
            if (isStunned)
            {
                Gizmos.color = Color.white;
                float remainingTime = Mathf.Max(0, stunDuration - stunTimer);
                Vector3 timePos = transform.position + Vector3.up * 0.6f;
              //  UnityEditor.Handles.Label(timePos, $"眩晕剩余: {remainingTime:F1}s");
            }
        }
    }


    [SerializeField] private Transform headCheck; // 头顶检测点
    [SerializeField] private float headCheckRadius = 0.2f; // 头顶检测半径

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 检测玩家碰撞
        // Debug.LogError("准备踩头");
        if (((1 << collision.gameObject.layer) & playerLayer) != 0)
        {
            // 检查是否从头顶踩下

            if (!collision.GetComponent<CatController>().isDead)
            {
                bool isHeadStomp = CheckHeadStomp(collision);

                if (isHeadStomp)
                {
                    // 玩家踩头击杀怪物
                    Debug.LogError("菜刀头部");
                    Stun();
                    animator.Play("Idle");
                }
                else
                {
                    // 普通碰撞，对玩家造成伤害
                    ApplyDamageToPlayer(collision.gameObject);
                }
            }
          
        }
    }
    private bool CheckHeadStomp(Collider2D playerCollider)
    {
        // 检测玩家是否从头顶踩下
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
                // 给玩家一个弹跳力
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
        // 获取玩家的生命值组件并造成伤害
        CatController playerHealth = player.GetComponent<CatController>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage();
        }
    }


}