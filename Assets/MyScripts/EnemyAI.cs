using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    [Header("巡逻参数")]
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float waitTime = 1f;

    [Header("检测参数")]
    [SerializeField] private float detectionRange = 8f;
    [SerializeField] private float chargeTriggerDistance = 4f;
    [SerializeField] private LayerMask detectionLayers;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private LayerMask obstacleLayer;

    [Header("冲锋参数")]
    [SerializeField] private float chargeSpeed = 8f;
    [SerializeField] private float chargeCooldown = 3f;
    [SerializeField] private float chargeDuration = 1.5f;
    [SerializeField] private float stunDuration = 2f;

    [Header("调试")]
    [SerializeField] private bool enableDetectionLogging = true;
    [SerializeField] private float logUpdateInterval = 0.5f;
    [SerializeField] private bool debugChargeConditions = true;

    [Header("状态")]
    [SerializeField] private EnemyState currentState = EnemyState.Patrol;
    [SerializeField] private bool isFacingRight = true;
    [SerializeField] private bool isStunned = false;

    private int currentPatrolIndex = 0;
    private float waitTimer = 0f;
    private float chargeTimer = 0f;
    private float stunTimer = 0f;
    private float currentChargeTime = 0f;
    private Vector2 chargeDirection;
    private Animator animator;
    private Rigidbody2D rb;
    private GameObject lastSeenPlayer;
    private float detectionLogTimer = 0f;

    // 敌人状态枚举
    public enum EnemyState
    {
        Patrol,
        Chase,
        Charge,
        Stunned,
        HitWall
    }

    private void Start()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();

        if (patrolPoints.Length < 2)
        {
            Debug.LogError("至少需要2个巡逻点!", gameObject);
        }

        detectionLayers = playerLayer | obstacleLayer;

        UpdateFacingDirection();
        Debug.Log("敌人初始化完成", gameObject);
        LogLayerMaskInfo();
    }

    // 打印图层掩码信息
    private void LogLayerMaskInfo()
    {
        string layerNames = "";
        for (int i = 0; i < 32; i++)
        {
            if (((int)detectionLayers & (1 << i)) != 0)
            {
                layerNames += LayerMask.LayerToName(i) + ", ";
            }
        }

        Debug.Log($"[Enemy] 检测图层: {layerNames}", gameObject);
    }

    private void Update()
    {
        DebugDrawState();
        UpdateDetectionLogging();

        switch (currentState)
        {
            case EnemyState.Patrol:
                UpdatePatrolState();
                CheckForPlayer();
                break;

            case EnemyState.Chase:
                UpdateChaseState();
                CheckChargeCondition();
                break;

            case EnemyState.Charge:
                UpdateChargeState();
                CheckChargeCollision();
                break;

            case EnemyState.Stunned:
                UpdateStunState();
                break;

            case EnemyState.HitWall:
                UpdateHitWallState();
                break;
        }

        if (chargeTimer > 0) chargeTimer -= Time.deltaTime;
        if (stunTimer > 0) stunTimer -= Time.deltaTime;

        UpdateAnimation();
    }

    // 绘制状态信息
    private void DebugDrawState()
    {
#if UNITY_EDITOR
        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.white;
        style.fontSize = 16;

        string stateText = $"状态: {currentState}";
        if (currentState == EnemyState.Chase && lastSeenPlayer != null)
        {
            float dist = Vector2.Distance(transform.position, lastSeenPlayer.transform.position);
            stateText += $"\n追逐中 - 距离: {dist:F1}m";
        }
        else if (currentState == EnemyState.Charge)
        {
            stateText += $"\n冲刺中 - 剩余时间: {chargeDuration - currentChargeTime:F1}s";
        }

        Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 1.5f);
        if (screenPos.z > 0 && Camera.main != null)
        {
           // UnityEditor.Handles.Label(transform.position + Vector3.up * 1.5f, stateText, style);
        }
#endif
    }

    // 更新检测日志
    private void UpdateDetectionLogging()
    {
        if (!enableDetectionLogging) return;

        detectionLogTimer += Time.deltaTime;
        if (detectionLogTimer >= logUpdateInterval)
        {
            detectionLogTimer = 0f;
            LogDetectedObjects();
        }
    }

    // 记录检测到的所有物体
    private void LogDetectedObjects()
    {
        string logMessage = $"[{name}] 检测到的物体:";
        bool foundAnything = false;

        GameObject player = FindPlayerInSight();
        if (player != null)
        {
            foundAnything = true;
            logMessage += $"\n  玩家: {player.name} - 距离: {Vector2.Distance(transform.position, player.transform.position):F1}m";
        }

        LogRaycastResults();

        if (!foundAnything)
        {
            logMessage += " 无";
        }

        Debug.Log(logMessage, gameObject);
    }

    // 记录射线检测结果
    private void LogRaycastResults()
    {
        // 右侧检测
        RaycastHit2D rightHit = Physics2D.Raycast(
            transform.position,
            Vector2.right,
            detectionRange,
            detectionLayers
        );

        if (rightHit.collider != null)
        {
            string layerName = LayerMask.LayerToName(rightHit.collider.gameObject.layer);
            Debug.Log($"[Enemy] 右侧射线检测到: {rightHit.collider.name} ({layerName}) - 距离: {rightHit.distance:F1}m", gameObject);
        }

        // 左侧检测
        RaycastHit2D leftHit = Physics2D.Raycast(
            transform.position,
            Vector2.left,
            detectionRange,
            detectionLayers
        );

        if (leftHit.collider != null)
        {
            string layerName = LayerMask.LayerToName(leftHit.collider.gameObject.layer);
            Debug.Log($"[Enemy] 左侧射线检测到: {leftHit.collider.name} ({layerName}) - 距离: {leftHit.distance:F1}m", gameObject);
        }
    }

    // 更新巡逻状态
    private void UpdatePatrolState()
    {
        if (waitTimer > 0)
        {
            waitTimer -= Time.deltaTime;
            rb.velocity = Vector2.zero;
            return;
        }

        Vector2 targetPosition = patrolPoints[currentPatrolIndex].position;
        Vector2 direction = (targetPosition - (Vector2)transform.position).normalized;

        rb.velocity = new Vector2(direction.x * patrolSpeed, rb.velocity.y);

        if (Vector2.Distance(transform.position, targetPosition) < 0.1f)
        {
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
            waitTimer = waitTime;
            UpdateFacingDirection();
        }
    }

    // 更新追逐状态 - 修改：强制转向玩家
    private void UpdateChaseState()
    {
        if (lastSeenPlayer == null || !lastSeenPlayer.activeSelf)
        {
            TransitionToState(EnemyState.Patrol, "丢失玩家目标");
            return;
        }

        Vector2 direction = (lastSeenPlayer.transform.position - transform.position).normalized;
        rb.velocity = new Vector2(direction.x * patrolSpeed, rb.velocity.y);

        // 强制转向玩家
        if ((direction.x > 0 && !isFacingRight) || (direction.x < 0 && isFacingRight))
        {
            Flip();
        }

        Debug.DrawLine(transform.position, lastSeenPlayer.transform.position, Color.yellow);
    }

    // 更新冲锋状态
    private void UpdateChargeState()
    {
        currentChargeTime += Time.deltaTime;
        rb.velocity = new Vector2(chargeDirection.x * chargeSpeed, rb.velocity.y);

        if (currentChargeTime >= chargeDuration)
        {
            TransitionToState(EnemyState.Patrol, "冲锋时间结束");
        }
    }

    // 更新眩晕状态
    private void UpdateStunState()
    {
        if (stunTimer <= 0)
        {
            TransitionToState(EnemyState.Patrol, "眩晕结束");
        }
    }

    // 更新撞墙状态
    private void UpdateHitWallState()
    {
        rb.velocity = Vector2.zero;

        if (waitTimer > 0)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0)
            {
                Flip();
                TransitionToState(EnemyState.Patrol, "撞墙后恢复");
            }
        }
    }

    // 状态转换方法
    private void TransitionToState(EnemyState newState, string reason)
    {
        Debug.Log($"状态转换: {currentState} → {newState} | 原因: {reason}", gameObject);
        currentState = newState;

        switch (newState)
        {
            case EnemyState.Charge:
                currentChargeTime = 0f;
                break;

            case EnemyState.HitWall:
                waitTimer = 1f;
                break;

            case EnemyState.Stunned:
                stunTimer = stunDuration;
                break;
        }
    }

    // 检查是否可以冲锋 - 修改：移除方向限制
    private void CheckChargeCondition()
    {
        if (debugChargeConditions)
        {
            Debug.Log($"[Enemy] 检查冲锋条件 - 冷却剩余: {chargeTimer:F1}s", gameObject);
        }

        // 1. 检查冷却时间
        if (chargeTimer > 0)
        {
            if (debugChargeConditions) Debug.Log("[Enemy] 冲锋冷却中，无法冲锋", gameObject);
            return;
        }

        // 2. 检查玩家是否存在
        if (lastSeenPlayer == null)
        {
            if (debugChargeConditions) Debug.Log("[Enemy] 玩家不存在，无法冲锋", gameObject);
            TransitionToState(EnemyState.Patrol, "追逐中丢失玩家");
            return;
        }

        // 3. 计算与玩家的距离
        float distanceToPlayer = Vector2.Distance(transform.position, lastSeenPlayer.transform.position);

        if (debugChargeConditions)
        {
            Debug.Log($"[Enemy] 与玩家距离: {distanceToPlayer:F1}m | 冲锋触发距离: {chargeTriggerDistance}m", gameObject);
        }

        // 4. 检查玩家是否在冲锋触发距离内
        if (distanceToPlayer > chargeTriggerDistance)
        {
            if (debugChargeConditions) Debug.Log("[Enemy] 玩家太远，无法冲锋", gameObject);
            return;
        }

        // 5. 检查视线是否被阻挡（修改：使用面向玩家的方向）
        Vector2 directionToPlayer = (lastSeenPlayer.transform.position - transform.position).normalized;

        RaycastHit2D hit = Physics2D.Raycast(
            transform.position,
            directionToPlayer,
            distanceToPlayer,
            obstacleLayer
        );

        if (hit.collider != null)
        {
            if (debugChargeConditions)
            {
                Debug.Log($"[Enemy] 视线被阻挡: {hit.collider.name} - 距离: {hit.distance:F1}m", gameObject);
                Debug.DrawLine(transform.position, hit.point, Color.magenta, 2f);
            }
            return;
        }

        // 所有条件满足，发起冲锋
        chargeDirection = directionToPlayer;

        if (debugChargeConditions)
        {
            Debug.Log($"[Enemy] 所有条件满足! 发起冲锋，方向: {directionToPlayer}", gameObject);
            Debug.DrawLine(transform.position, transform.position + (Vector3)directionToPlayer * 3f, Color.red, 2f);
        }

        TransitionToState(EnemyState.Charge, $"玩家距离 {distanceToPlayer:F1}m，发起冲锋");
        chargeTimer = chargeCooldown;
    }

    // 检查冲锋碰撞
    private void CheckChargeCollision()
    {
        float rayDistance = 0.5f;
        Vector2 rayDirection = chargeDirection; // 使用实际冲锋方向
        RaycastHit2D hit = Physics2D.Raycast(
            transform.position,
            rayDirection,
            rayDistance,
            obstacleLayer
        );

        if (hit.collider != null)
        {
            TransitionToState(EnemyState.HitWall, $"冲锋撞到 {hit.collider.name}");
        }
    }

    // 检查是否能看到玩家
    private void CheckForPlayer()
    {
        if (isStunned) return;

        GameObject player = FindPlayerInSight();
        if (player != null && currentState != EnemyState.Charge && currentState != EnemyState.HitWall)
        {
            lastSeenPlayer = player;
            TransitionToState(EnemyState.Chase, $"发现玩家: {player.name}");
        }
    }

    // 寻找视野内的玩家
    private GameObject FindPlayerInSight()
    {
        // 检测左右两侧的玩家
        Vector2 rightDirection = Vector2.right;
        Vector2 leftDirection = Vector2.left;

        // 右侧射线检测
        RaycastHit2D rightHit = Physics2D.Raycast(
            transform.position,
            rightDirection,
            detectionRange,
            detectionLayers
        );

        // 左侧射线检测
        RaycastHit2D leftHit = Physics2D.Raycast(
            transform.position,
            leftDirection,
            detectionRange,
            detectionLayers
        );

        // 绘制检测射线
        Debug.DrawLine(transform.position, transform.position + (Vector3)rightDirection * detectionRange, Color.blue);
        Debug.DrawLine(transform.position, transform.position + (Vector3)leftDirection * detectionRange, Color.blue);

        // 处理右侧检测结果
        if (rightHit.collider != null)
        {
            Debug.DrawLine(transform.position, rightHit.point, Color.red);

            if ((1 << rightHit.collider.gameObject.layer) == (1 << LayerMask.NameToLayer("Player")))
            {
                return rightHit.collider.gameObject;
            }
        }

        // 处理左侧检测结果
        if (leftHit.collider != null)
        {
            Debug.DrawLine(transform.position, leftHit.point, Color.red);

            if ((1 << leftHit.collider.gameObject.layer) == (1 << LayerMask.NameToLayer("Player")))
            {
                return leftHit.collider.gameObject;
            }
        }

        return null;
    }

    // 被踩头处理
    public void StompHead()
    {
        if (isStunned) return;

        TransitionToState(EnemyState.Stunned, "被踩头");
        isStunned = true;

        if (animator != null)
        {
            animator.SetBool("isStunned", true);
        }

        Collider2D[] colliders = GetComponents<Collider2D>();
        foreach (Collider2D collider in colliders)
        {
            collider.enabled = false;
        }

        Invoke("EnableColliders", stunDuration);
    }

    // 启用碰撞器
    private void EnableColliders()
    {
        Collider2D[] colliders = GetComponents<Collider2D>();
        foreach (Collider2D collider in colliders)
        {
            collider.enabled = true;
        }

        if (animator != null)
        {
            animator.SetBool("isStunned", false);
        }
    }

    // 更新面朝方向
    private void UpdateFacingDirection()
    {
        if (patrolPoints.Length < 2) return;

        Vector2 direction = patrolPoints[currentPatrolIndex].position - transform.position;
        if ((direction.x > 0 && !isFacingRight) || (direction.x < 0 && isFacingRight))
        {
            Flip();
        }
    }

    // 翻转方向
    private void Flip()
    {
        isFacingRight = !isFacingRight;
        transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);
        Debug.Log("翻转方向: " + (isFacingRight ? "向右" : "向左"), gameObject);
    }

    // 更新动画
    private void UpdateAnimation()
    {
        if (animator == null) return;

        animator.SetFloat("moveSpeed", Mathf.Abs(rb.velocity.x));
        animator.SetInteger("state", (int)currentState);
    }

    // 在编辑器中绘制辅助线
    private void OnDrawGizmosSelected()
    {
        // 绘制巡逻路径
        if (patrolPoints != null && patrolPoints.Length > 1)
        {
            Gizmos.color = Color.yellow;
            for (int i = 0; i < patrolPoints.Length - 1; i++)
            {
                Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[i + 1].position);
            }
            Gizmos.DrawLine(patrolPoints[patrolPoints.Length - 1].position, patrolPoints[0].position);
        }

        // 绘制检测范围和冲刺触发范围
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, chargeTriggerDistance);

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.right * detectionRange);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.left * detectionRange);

        // 绘制冲锋方向
        if (currentState == EnemyState.Charge)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, transform.position + (Vector3)chargeDirection * 2f);

            Gizmos.color = Color.magenta;
            Vector3 chargeEndPos = transform.position + (Vector3)chargeDirection * chargeSpeed * chargeDuration;
            Gizmos.DrawWireSphere(chargeEndPos, 0.3f);
        }
    }
}