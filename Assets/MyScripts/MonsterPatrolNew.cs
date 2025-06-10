using UnityEngine;

public class MonsterPatrolNew : MonoBehaviour
{
    [Header("Ѳ�߲���")]
    public Transform pointA;
    public Transform pointB;
    public float patrolSpeed = 2f;
    public float chargeSpeed = 5f;
    public float cooldownFollowSpeed = 3f; // ��ȴ�ڼ�����ٶ�

    [Header("������")]
    public float detectionDistance = 5f;
    public float detectionHeight = 1f;
    public float sideDetectionHeight = 1f;
    public float sideDetectionWidth = 0.5f;
    public float sideDetectionDistance = 50f;
    public LayerMask playerLayer;
    public LayerMask wallLayer;

    [Header("������")]
    public float chargeDuration = 2f;
    public float cooldownDuration = 1f;
    public float chargeWarningTime = 1f;

    [Header("��ײ����")]
    public float wallBounceBackSpeed = 2f;
    public float wallBounceUpSpeed = 3f;
    public float wallBounceBackDuration = 0.3f;
    public float stunDuration = 2f;

    [Header("�Ӿ�Ԫ��")]
    public GameObject stunSprite;
    public Vector3 stunSpriteOffset;
    public float stunSpriteScale = 1f;
    public GameObject chargeWarningSprite;
    public Vector3 chargeWarningSpriteOffset;
    public float chargeWarningSpriteScale = 1f;

    // ״̬��ʱ��
    private float chargeTimer;
    private float cooldownTimer;
    private float bounceTimer;
    private float stunTimer;
    private float chargeWarningTimer;

    // ״̬��־
    private bool isCharging;
    private bool isOnCooldown;
    private bool isBouncingBack;
    private bool isStunned;
    private bool isShowingChargeWarning;
    private bool isFollowingPlayer;
    private bool wasHeadingToA;
    private bool isWaitingForCharge;

    // �������
    private Rigidbody2D rb;
    private Collider2D col;
    private SpriteRenderer spriteRenderer;
    private Animator animator;

    // �ƶ������Ŀ��
    private Vector2 chargeDirection;
    private Transform currentTarget;
    private Transform playerTransform;

    // ����������ϣ
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

        // ��ʼ������������ϣ
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

        //// ��ʼ���Ӿ�Ԫ��
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

        // �����ң���ʹ����ȴ�ڼ�Ҳ��Ҫ��⣩
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
        // ��¼Ѳ�߷���
        wasHeadingToA = currentTarget == pointA;

        // ����Ŀ���ʱ�л�
        if (Vector2.Distance(transform.position, currentTarget.position) < 0.1f)
        {
            currentTarget = wasHeadingToA ? pointB : pointA;
        }

        // ��Ŀ����ƶ�
        Vector2 direction = (currentTarget.position - transform.position).normalized;
        rb.velocity = new Vector2(direction.x * patrolSpeed, rb.velocity.y);

        // ��ת����
        spriteRenderer.flipX = direction.x < 0;

        // ���ö���
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
        // ������Ҽ��״̬
        isFollowingPlayer = false;
        playerTransform = null;

        // ���������
        Vector2 leftOrigin = (Vector2)transform.position + Vector2.up * sideDetectionHeight / 2;
        Vector2 leftSize = new Vector2(sideDetectionWidth, sideDetectionHeight);

        RaycastHit2D[] leftHits = Physics2D.BoxCastAll(
            leftOrigin,
            leftSize,
            0,
            Vector2.left,
            sideDetectionDistance,
            playerLayer);

        // �Ҳ�������
        Vector2 rightOrigin = (Vector2)transform.position + Vector2.up * sideDetectionHeight / 2;
        Vector2 rightSize = new Vector2(sideDetectionWidth, sideDetectionHeight);

        RaycastHit2D[] rightHits = Physics2D.BoxCastAll(
            rightOrigin,
            rightSize,
            0,
            Vector2.right,
            sideDetectionDistance,
            playerLayer);

        // �����������
        foreach (RaycastHit2D hit in leftHits)
        {
            if (hit.collider != null && hit.collider.CompareTag("Cat"))
            {
                isFollowingPlayer = true;
                playerTransform = hit.collider.transform;
                Debug.Log("����������⵽���");
                break;
            }
        }

        // �����Ҳ�������������δ��⵽��
        if (!isFollowingPlayer)
        {
            foreach (RaycastHit2D hit in rightHits)
            {
                if (hit.collider != null && hit.collider.CompareTag("Cat"))
                {
                    isFollowingPlayer = true;
                    playerTransform = hit.collider.transform;
                    Debug.Log("�Ҳ��������⵽���");
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

        // ʹ���ʵ����ٶȸ������
        float followSpeed = isOnCooldown ? cooldownFollowSpeed : patrolSpeed;

        // ���㳯����ҵķ���
        Vector2 direction = (playerTransform.position - transform.position).normalized;
        rb.velocity = new Vector2(direction.x * followSpeed, rb.velocity.y);

        // ��ת����
        spriteRenderer.flipX = direction.x < 0;

        // ���ö���
        SetAnimatorBool(walkParamHash, true);
        SetAnimatorBool(chargeParamHash, false);
        SetAnimatorBool(cooldownParamHash, isOnCooldown);
        SetAnimatorBool(bounceParamHash, false);
        SetAnimatorBool(stunParamHash, false);
        SetAnimatorBool(warningParamHash, false);
        SetAnimatorBool(followParamHash, true);

        // �����⵽����Ҿ����㹻����׼����棨���ڷ���ȴ״̬�£�
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

        // �����淽��
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

        // ��ʾԤ��ͼƬ
        if (chargeWarningSprite != null)
            chargeWarningSprite.SetActive(true);

        // ֹͣ�ƶ�
        rb.velocity = Vector2.zero;

        // ���ö���
        SetAnimatorBool(walkParamHash, false);
        SetAnimatorBool(chargeParamHash, false);
        SetAnimatorBool(cooldownParamHash, false);
        SetAnimatorBool(bounceParamHash, false);
        SetAnimatorBool(stunParamHash, false);
        SetAnimatorBool(warningParamHash, true);
        SetAnimatorBool(followParamHash, false);

        Debug.Log("��ʼ���Ԥ��");
    }

    void HandleChargeWarning()
    {
        chargeWarningTimer += Time.deltaTime;

        if (chargeWarningTimer >= chargeWarningTime)
        {
            isShowingChargeWarning = false;
            isWaitingForCharge = false;

            // ����Ԥ��ͼƬ
            if (chargeWarningSprite != null)
                chargeWarningSprite.SetActive(false);

            // ��ʼ���
            StartCharging();
        }
    }

    void StartCharging()
    {
        isCharging = true;
        chargeTimer = 0f;

        // ���ö���
        SetAnimatorBool(chargeParamHash, true);
        SetAnimatorBool(warningParamHash, false);

        Debug.Log("��ʼ���");
    }

    void HandleCharging()
    {
        // ���ó���ٶ�
        rb.velocity = new Vector2(chargeDirection.x * chargeSpeed, rb.velocity.y);

        // ���¼�ʱ��
        chargeTimer += Time.deltaTime;

        // ����Ƿ�ײ��ǽ����ʱ�����
        if (HasHitWall() || chargeTimer >= chargeDuration)
        {
            StopCharging();

            if (HasHitWall())
            {
                StartBounceBack();
                Debug.Log("ײ��ǽ����ʼ�ص�");
            }
            else
            {
                StartCooldown();
                Debug.Log("���ʱ�������������ȴ");
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

        // ���ûص��ٶ�
        Vector2 bounceDirection = new Vector2(-chargeDirection.x * wallBounceBackSpeed, wallBounceUpSpeed);
        rb.velocity = bounceDirection;

        // ���ö���
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
            Debug.Log("�ص�����������ѣ��");
        }
    }

    void StartStun()
    {
        isStunned = true;
        stunTimer = 0f;
        rb.velocity = Vector2.zero;

        // ��ʾѣ��ͼƬ
        if (stunSprite != null)
            stunSprite.SetActive(true);

        // ���ö���
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

            // ����ѣ��ͼƬ
            if (stunSprite != null)
                stunSprite.SetActive(false);

            StartCooldown();
            Debug.Log("ѣ�ν�����������ȴ");
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

        // ���ö���
        SetAnimatorBool(cooldownParamHash, true);
    }

    void HandleCooldown()
    {
        cooldownTimer += Time.deltaTime;

        // ����ȴ�ڼ䣬�����⵽�������棬�������Ѳ��
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
            Debug.Log("��ȴ�������ָ�����Ѳ��");
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
        // ����Ѳ��·��
        if (pointA != null && pointB != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(pointA.position, pointB.position);
        }

        // �������Ҽ������
        if (spriteRenderer != null)
        {
            // ���������
            Vector2 leftOrigin = (Vector2)transform.position + Vector2.up * sideDetectionHeight / 2;
            Vector2 leftSize = new Vector2(sideDetectionWidth, sideDetectionHeight);

            Gizmos.color = isFollowingPlayer ? Color.green : Color.red;
            Gizmos.DrawCube(
                leftOrigin + Vector2.left * sideDetectionDistance / 2,
                new Vector3(sideDetectionDistance, sideDetectionHeight, 0.1f));

            // �Ҳ�������
            Vector2 rightOrigin = (Vector2)transform.position + Vector2.up * sideDetectionHeight / 2;
            Vector2 rightSize = new Vector2(sideDetectionWidth, sideDetectionHeight);

            Gizmos.DrawCube(
                rightOrigin + Vector2.right * sideDetectionDistance / 2,
                new Vector3(sideDetectionDistance, sideDetectionHeight, 0.1f));
        }

        // ���Ƴ��������
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

            // ���Ʊ߿�
            Gizmos.color = Color.black;
            Gizmos.DrawWireCube(center, size);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // ��Ҵ�ͷ�����µĴ���
        if ((1 << collision.gameObject.layer & playerLayer) != 0 && !collision.GetComponent<CatController>().isDead)
        {
            if (IsHeadStomp(collision))
            {
                StartStun();
                Debug.Log("����Ҳ�ͷ������ѣ��");

            }
            else
            {
                // ���������˺����߼�
                ApplyDamageToPlayer(collision.gameObject);
            }
        }
    }

    private bool IsHeadStomp(Collider2D playerCollider)
    {
        // ����Ƿ��ͷ�����µ��߼�
        Vector2 boxSize = new Vector2(col.bounds.size.x * 0.8f, 0.2f);
        Vector2 boxCenter = (Vector2)transform.position + Vector2.up * (col.bounds.size.y * 0.5f + 0.1f);

        Collider2D[] colliders = Physics2D.OverlapBoxAll(boxCenter, boxSize, 0, playerLayer);
        foreach (Collider2D collider in colliders)
        {
            if (collider == playerCollider)
            {
                // �����һ��������
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
        // ���������˺����߼�
        CatController playerHealth = player.GetComponent<CatController>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage();
        }
    }
}