using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    [Header("Ѳ�߲���")]
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float waitTime = 1f;

    [Header("������")]
    [SerializeField] private float detectionRange = 8f;
    [SerializeField] private float chargeTriggerDistance = 4f;
    [SerializeField] private LayerMask detectionLayers;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private LayerMask obstacleLayer;

    [Header("������")]
    [SerializeField] private float chargeSpeed = 8f;
    [SerializeField] private float chargeCooldown = 3f;
    [SerializeField] private float chargeDuration = 1.5f;
    [SerializeField] private float stunDuration = 2f;

    [Header("����")]
    [SerializeField] private bool enableDetectionLogging = true;
    [SerializeField] private float logUpdateInterval = 0.5f;
    [SerializeField] private bool debugChargeConditions = true;

    [Header("״̬")]
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

    // ����״̬ö��
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
            Debug.LogError("������Ҫ2��Ѳ�ߵ�!", gameObject);
        }

        detectionLayers = playerLayer | obstacleLayer;

        UpdateFacingDirection();
        Debug.Log("���˳�ʼ�����", gameObject);
        LogLayerMaskInfo();
    }

    // ��ӡͼ��������Ϣ
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

        Debug.Log($"[Enemy] ���ͼ��: {layerNames}", gameObject);
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

    // ����״̬��Ϣ
    private void DebugDrawState()
    {
#if UNITY_EDITOR
        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.white;
        style.fontSize = 16;

        string stateText = $"״̬: {currentState}";
        if (currentState == EnemyState.Chase && lastSeenPlayer != null)
        {
            float dist = Vector2.Distance(transform.position, lastSeenPlayer.transform.position);
            stateText += $"\n׷���� - ����: {dist:F1}m";
        }
        else if (currentState == EnemyState.Charge)
        {
            stateText += $"\n����� - ʣ��ʱ��: {chargeDuration - currentChargeTime:F1}s";
        }

        Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 1.5f);
        if (screenPos.z > 0 && Camera.main != null)
        {
           // UnityEditor.Handles.Label(transform.position + Vector3.up * 1.5f, stateText, style);
        }
#endif
    }

    // ���¼����־
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

    // ��¼��⵽����������
    private void LogDetectedObjects()
    {
        string logMessage = $"[{name}] ��⵽������:";
        bool foundAnything = false;

        GameObject player = FindPlayerInSight();
        if (player != null)
        {
            foundAnything = true;
            logMessage += $"\n  ���: {player.name} - ����: {Vector2.Distance(transform.position, player.transform.position):F1}m";
        }

        LogRaycastResults();

        if (!foundAnything)
        {
            logMessage += " ��";
        }

        Debug.Log(logMessage, gameObject);
    }

    // ��¼���߼����
    private void LogRaycastResults()
    {
        // �Ҳ���
        RaycastHit2D rightHit = Physics2D.Raycast(
            transform.position,
            Vector2.right,
            detectionRange,
            detectionLayers
        );

        if (rightHit.collider != null)
        {
            string layerName = LayerMask.LayerToName(rightHit.collider.gameObject.layer);
            Debug.Log($"[Enemy] �Ҳ����߼�⵽: {rightHit.collider.name} ({layerName}) - ����: {rightHit.distance:F1}m", gameObject);
        }

        // �����
        RaycastHit2D leftHit = Physics2D.Raycast(
            transform.position,
            Vector2.left,
            detectionRange,
            detectionLayers
        );

        if (leftHit.collider != null)
        {
            string layerName = LayerMask.LayerToName(leftHit.collider.gameObject.layer);
            Debug.Log($"[Enemy] ������߼�⵽: {leftHit.collider.name} ({layerName}) - ����: {leftHit.distance:F1}m", gameObject);
        }
    }

    // ����Ѳ��״̬
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

    // ����׷��״̬ - �޸ģ�ǿ��ת�����
    private void UpdateChaseState()
    {
        if (lastSeenPlayer == null || !lastSeenPlayer.activeSelf)
        {
            TransitionToState(EnemyState.Patrol, "��ʧ���Ŀ��");
            return;
        }

        Vector2 direction = (lastSeenPlayer.transform.position - transform.position).normalized;
        rb.velocity = new Vector2(direction.x * patrolSpeed, rb.velocity.y);

        // ǿ��ת�����
        if ((direction.x > 0 && !isFacingRight) || (direction.x < 0 && isFacingRight))
        {
            Flip();
        }

        Debug.DrawLine(transform.position, lastSeenPlayer.transform.position, Color.yellow);
    }

    // ���³��״̬
    private void UpdateChargeState()
    {
        currentChargeTime += Time.deltaTime;
        rb.velocity = new Vector2(chargeDirection.x * chargeSpeed, rb.velocity.y);

        if (currentChargeTime >= chargeDuration)
        {
            TransitionToState(EnemyState.Patrol, "���ʱ�����");
        }
    }

    // ����ѣ��״̬
    private void UpdateStunState()
    {
        if (stunTimer <= 0)
        {
            TransitionToState(EnemyState.Patrol, "ѣ�ν���");
        }
    }

    // ����ײǽ״̬
    private void UpdateHitWallState()
    {
        rb.velocity = Vector2.zero;

        if (waitTimer > 0)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0)
            {
                Flip();
                TransitionToState(EnemyState.Patrol, "ײǽ��ָ�");
            }
        }
    }

    // ״̬ת������
    private void TransitionToState(EnemyState newState, string reason)
    {
        Debug.Log($"״̬ת��: {currentState} �� {newState} | ԭ��: {reason}", gameObject);
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

    // ����Ƿ���Գ�� - �޸ģ��Ƴ���������
    private void CheckChargeCondition()
    {
        if (debugChargeConditions)
        {
            Debug.Log($"[Enemy] ��������� - ��ȴʣ��: {chargeTimer:F1}s", gameObject);
        }

        // 1. �����ȴʱ��
        if (chargeTimer > 0)
        {
            if (debugChargeConditions) Debug.Log("[Enemy] �����ȴ�У��޷����", gameObject);
            return;
        }

        // 2. �������Ƿ����
        if (lastSeenPlayer == null)
        {
            if (debugChargeConditions) Debug.Log("[Enemy] ��Ҳ����ڣ��޷����", gameObject);
            TransitionToState(EnemyState.Patrol, "׷���ж�ʧ���");
            return;
        }

        // 3. ��������ҵľ���
        float distanceToPlayer = Vector2.Distance(transform.position, lastSeenPlayer.transform.position);

        if (debugChargeConditions)
        {
            Debug.Log($"[Enemy] ����Ҿ���: {distanceToPlayer:F1}m | ��津������: {chargeTriggerDistance}m", gameObject);
        }

        // 4. �������Ƿ��ڳ�津��������
        if (distanceToPlayer > chargeTriggerDistance)
        {
            if (debugChargeConditions) Debug.Log("[Enemy] ���̫Զ���޷����", gameObject);
            return;
        }

        // 5. ��������Ƿ��赲���޸ģ�ʹ��������ҵķ���
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
                Debug.Log($"[Enemy] ���߱��赲: {hit.collider.name} - ����: {hit.distance:F1}m", gameObject);
                Debug.DrawLine(transform.position, hit.point, Color.magenta, 2f);
            }
            return;
        }

        // �����������㣬������
        chargeDirection = directionToPlayer;

        if (debugChargeConditions)
        {
            Debug.Log($"[Enemy] ������������! �����棬����: {directionToPlayer}", gameObject);
            Debug.DrawLine(transform.position, transform.position + (Vector3)directionToPlayer * 3f, Color.red, 2f);
        }

        TransitionToState(EnemyState.Charge, $"��Ҿ��� {distanceToPlayer:F1}m��������");
        chargeTimer = chargeCooldown;
    }

    // �������ײ
    private void CheckChargeCollision()
    {
        float rayDistance = 0.5f;
        Vector2 rayDirection = chargeDirection; // ʹ��ʵ�ʳ�淽��
        RaycastHit2D hit = Physics2D.Raycast(
            transform.position,
            rayDirection,
            rayDistance,
            obstacleLayer
        );

        if (hit.collider != null)
        {
            TransitionToState(EnemyState.HitWall, $"���ײ�� {hit.collider.name}");
        }
    }

    // ����Ƿ��ܿ������
    private void CheckForPlayer()
    {
        if (isStunned) return;

        GameObject player = FindPlayerInSight();
        if (player != null && currentState != EnemyState.Charge && currentState != EnemyState.HitWall)
        {
            lastSeenPlayer = player;
            TransitionToState(EnemyState.Chase, $"�������: {player.name}");
        }
    }

    // Ѱ����Ұ�ڵ����
    private GameObject FindPlayerInSight()
    {
        // ���������������
        Vector2 rightDirection = Vector2.right;
        Vector2 leftDirection = Vector2.left;

        // �Ҳ����߼��
        RaycastHit2D rightHit = Physics2D.Raycast(
            transform.position,
            rightDirection,
            detectionRange,
            detectionLayers
        );

        // ������߼��
        RaycastHit2D leftHit = Physics2D.Raycast(
            transform.position,
            leftDirection,
            detectionRange,
            detectionLayers
        );

        // ���Ƽ������
        Debug.DrawLine(transform.position, transform.position + (Vector3)rightDirection * detectionRange, Color.blue);
        Debug.DrawLine(transform.position, transform.position + (Vector3)leftDirection * detectionRange, Color.blue);

        // �����Ҳ�����
        if (rightHit.collider != null)
        {
            Debug.DrawLine(transform.position, rightHit.point, Color.red);

            if ((1 << rightHit.collider.gameObject.layer) == (1 << LayerMask.NameToLayer("Player")))
            {
                return rightHit.collider.gameObject;
            }
        }

        // �����������
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

    // ����ͷ����
    public void StompHead()
    {
        if (isStunned) return;

        TransitionToState(EnemyState.Stunned, "����ͷ");
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

    // ������ײ��
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

    // �����泯����
    private void UpdateFacingDirection()
    {
        if (patrolPoints.Length < 2) return;

        Vector2 direction = patrolPoints[currentPatrolIndex].position - transform.position;
        if ((direction.x > 0 && !isFacingRight) || (direction.x < 0 && isFacingRight))
        {
            Flip();
        }
    }

    // ��ת����
    private void Flip()
    {
        isFacingRight = !isFacingRight;
        transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);
        Debug.Log("��ת����: " + (isFacingRight ? "����" : "����"), gameObject);
    }

    // ���¶���
    private void UpdateAnimation()
    {
        if (animator == null) return;

        animator.SetFloat("moveSpeed", Mathf.Abs(rb.velocity.x));
        animator.SetInteger("state", (int)currentState);
    }

    // �ڱ༭���л��Ƹ�����
    private void OnDrawGizmosSelected()
    {
        // ����Ѳ��·��
        if (patrolPoints != null && patrolPoints.Length > 1)
        {
            Gizmos.color = Color.yellow;
            for (int i = 0; i < patrolPoints.Length - 1; i++)
            {
                Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[i + 1].position);
            }
            Gizmos.DrawLine(patrolPoints[patrolPoints.Length - 1].position, patrolPoints[0].position);
        }

        // ���Ƽ�ⷶΧ�ͳ�̴�����Χ
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, chargeTriggerDistance);

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.right * detectionRange);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.left * detectionRange);

        // ���Ƴ�淽��
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