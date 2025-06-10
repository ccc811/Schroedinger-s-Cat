using UnityEngine;

public class MonsterPatrol : MonoBehaviour
{
    [Header("Ѳ�߲���")]
    public Transform pointA;
    public Transform pointB;
    public float patrolSpeed = 2f;
    public float chargeSpeed = 5f;
    public float detectionDistance = 5f;
    public LayerMask playerLayer;
    public LayerMask wallLayer;

    [Header("������")]
    public float chargeDuration = 2f; // ������ʱ��
    public float cooldownDuration = 1f; // �����ȴʱ��

    [Header("ײǽ�ص�����")]
    public float wallBounceBackSpeed = 2f; // ײǽ���ˮƽ�ص��ٶ�
    public float wallBounceUpSpeed = 3f; // ײǽ��Ĵ�ֱ�����ٶ�
    public float wallBounceBackDuration = 0.3f; // ײǽ��ص��ĳ���ʱ��

    [Header("ѣ�β���")]
    public float stunDuration = 2f; // ѣ�γ���ʱ��
    public GameObject stunSprite; // ѣ��״̬��ʾ��ͼƬ
    public Vector3 stunSpriteOffset = new Vector3(0, 1f, 0); // ѣ��ͼƬƫ����
    public float stunSpriteScale = 1f; // ѣ��ͼƬ����

    private float chargeTimer = 0f;   // ����ʱ��
    private float cooldownTimer = 0f; // ��ȴ��ʱ��
    private float bounceTimer = 0f;   // �ص���ʱ��
    private float stunTimer = 0f;     // ѣ�μ�ʱ��

    private bool wasHeadingToA = false; // ���ǰ�Ƿ���ǰ��A��
    private bool isOnCooldown = false; // �Ƿ�����ȴ��
    private bool isBouncingBack = false; // �Ƿ��ڻص���
    private bool isStunned = false; // �Ƿ���ѣ����

    [Header("����������")]
    public float detectionHeight = 1f;
    public Color detectionColor = new Color(1, 0, 0, 0.2f);
    public Color detectedColor = new Color(0, 1, 0, 0.2f);
    public Color chargingColor = new Color(1, 0.5f, 0, 0.3f);
    public Color cooldownColor = new Color(0, 0, 1, 0.2f);
    public Color bouncingColor = new Color(0, 1, 1, 0.3f);
    public Color stunColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);

    [Header("��������")]
    public Animator animator;
    public string walkParamName = "isWalking";
    public string chargeParamName = "isCharging";
    public string cooldownParamName = "isOnCooldown";
    public string bounceParamName = "isBouncing";
    public string stunParamName = "isStunned";

    [Header("������Ϣ")]
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

    // ����������ϣֵ��������ܣ�
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

        // ��ʼ������������ϣֵ
        if (animator != null)
        {
            walkParamHash = Animator.StringToHash(walkParamName);
            chargeParamHash = Animator.StringToHash(chargeParamName);
            cooldownParamHash = Animator.StringToHash(cooldownParamName);
            bounceParamHash = Animator.StringToHash(bounceParamName);
            stunParamHash = Animator.StringToHash(stunParamName);
        }


        // �����Ҫ������Ϣ
        Debug.Log($"����Ѳ�߽ű���ʼ��: " +
                  $"���㼶 = {LayerMask.LayerToName(playerLayer.value)}, " +
                  $"������ = {detectionDistance}, " +
                  $"������ʱ�� = {chargeDuration}��, " +
                  $"�����ȴʱ�� = {cooldownDuration}��, " +
                  $"ײǽˮƽ�ص��ٶ� = {wallBounceBackSpeed}, " +
                  $"ײǽ��ֱ�����ٶ� = {wallBounceUpSpeed}, " +
                  $"ѣ�γ���ʱ�� = {stunDuration}��");
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

            // ����ʱ
            chargeTimer += Time.deltaTime;

            // ����Ƿ���Ҫֹͣ���
            if (chargeTimer >= chargeDuration || HasHitWall())
            {
                StopCharging();

                if (HasHitWall())
                {
                    StartBounceBack();
                    Debug.Log($"ײ��ǽ��λ��: {transform.position}����ʼ�����ص�");
                }
                else
                {
                    StartCooldown();
                    Debug.Log("���ʱ�������������ȴ״̬");
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
                Debug.Log("��ȴ�������ָ�����Ѳ��");
            }
            else
            {
                // ����ȴ�ڼ����Ѳ�ߣ�������Ӧ��Ҽ��
                Patrol();
                return;
            }
        }

        Patrol();
        CheckForPlayer();
    }

    void Patrol()
    {
        // ��¼Ѳ�߷���
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

        // ��ת����
        if (direction.x > 0)
            spriteRenderer.flipX = false;
        else if (direction.x < 0)
            spriteRenderer.flipX = true;

        // ����Ѳ�߶���
        SetAnimatorBool(walkParamHash, Mathf.Abs(rb.velocity.x) > 0.1f);
        SetAnimatorBool(chargeParamHash, false);
        SetAnimatorBool(cooldownParamHash, false);
        SetAnimatorBool(bounceParamHash, false);
        SetAnimatorBool(stunParamHash, false);
    }

    void CheckForPlayer()
    {
        // ����ȴ���ص���ѣ���ڼ䲻������
        if (isOnCooldown || isBouncingBack || isStunned)
            return;

        Vector2 raycastDirection = spriteRenderer.flipX ? Vector2.left : Vector2.right;
        Vector2 origin = (Vector2)transform.position + Vector2.up * detectionHeight / 2;

        // ʹ��BoxCast���Raycast�����Ӽ��߶�
        RaycastHit2D hit = Physics2D.BoxCast(
            origin,
            new Vector2(0.2f, detectionHeight),
            0,
            raycastDirection,
            detectionDistance,
            playerLayer);

        bool wasDetected = playerDetected;
        playerDetected = hit.collider != null && hit.collider.CompareTag("Cat");

        // ��¼�����Ϣ
        lastDetectionInfo = hit.collider != null
            ? $"��⵽����: {hit.collider.name}, ��ǩ: {hit.collider.tag}"
            : "δ��⵽���";

        if (hit.collider != null)
        {
            lastHitPoint = hit.point;
            playerTransform = hit.collider.transform;
        }

        // ����״̬�仯ʱ�����־
        if (playerDetected != wasDetected)
        {
            Debug.Log(playerDetected ?
                $"��ұ���⵽��λ��: {lastHitPoint}" :
                "����������ⷶΧ");
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
        chargeTimer = 0f; // ���ó���ʱ��

        // ���㳯����ҵķ���
        if (playerTransform != null)
        {
            chargeDirection = (playerTransform.position - transform.position).normalized;

            // ȷ����淽��ֻ��X����
            chargeDirection = new Vector2(chargeDirection.x, 0);

            // ���¾��鳯��
            spriteRenderer.flipX = chargeDirection.x < 0;

            Debug.Log($"��ʼ��棡����: {chargeDirection}, ���λ��: {playerTransform.position}");
        }
        else
        {
            // ���û�л�ȡ�����Transform��ʹ��Ĭ�Ϸ���
            chargeDirection = spriteRenderer.flipX ? Vector2.left : Vector2.right;
            Debug.LogWarning("��ʼ��棬��û�л�ȡ�����Transform��ʹ��Ĭ�Ϸ���");
        }

        // ���ó�涯��
        SetAnimatorBool(walkParamHash, false);
        SetAnimatorBool(chargeParamHash, true);
        SetAnimatorBool(cooldownParamHash, false);
        SetAnimatorBool(bounceParamHash, false);
        SetAnimatorBool(stunParamHash, false);
    }

    void Charge()
    {
        isChargingDebug = true;

        // ���ó���ٶ�
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

        // ���ûص����� (ˮƽ�ʹ�ֱ)
        Vector2 bounceDirection = new Vector2(-chargeDirection.x * wallBounceBackSpeed, wallBounceUpSpeed);
        rb.velocity = bounceDirection;

        // ���ûص�����
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

            // ��ʼѣ��
            Stun();

            // ���ûص���������
            SetAnimatorBool(bounceParamHash, false);
            Debug.Log("�ص�����������ѣ��״̬");
        }
    }

    void StopCharging()
    {
        isCharging = false;
        isChargingDebug = false;

        // �ָ�Ѳ�߷���
        if (wasHeadingToA)
            currentTarget = pointA;
        else
            currentTarget = pointB;

        Debug.Log($"ֹͣ��棬�ָ�Ѳ�ߣ�Ŀ���: {currentTarget.name}");
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

        // ��ʾѣ��ͼƬ
        if (stunSprite != null)
            stunSprite.SetActive(true);

        // ����ѣ�ζ���
        SetAnimatorBool(stunParamHash, true);

        Debug.Log("����ѣ��״̬������ʱ��: " + stunDuration + "��");
    }

    void HandleStun()
    {
        stunTimer += Time.deltaTime;

        if (stunTimer >= stunDuration)
        {
            isStunned = false;
            isStunnedDebug = false;

            // ����ѣ��ͼƬ
            if (stunSprite != null)
                stunSprite.SetActive(false);

            // ����ѣ�ν�������
            SetAnimatorBool(stunParamHash, false);

            // ������ȴ״̬
            StartCooldown();
            Debug.Log("ѣ�ν�����������ȴ״̬");
        }
    }

    // ��ȫ���ö��������ĸ�������
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

            // ��ʾ��ǰѲ��Ŀ��
            //Gizmos.color = currentTarget == pointA ? Color.green : Color.red;
            //Gizmos.DrawSphere(currentTarget.position, 0.2f);
        }

        // ���Ƽ������
        if (spriteRenderer != null)
        {
            // ����״̬ʹ�ò�ͬ��ɫ
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

            // ���Ʊ߿�
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

            // �������һ�μ���
            if (playerDetected)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawSphere(lastHitPoint, 0.1f);
            }

            // ���Ƴ��ʣ��ʱ��
            if (isCharging)
            {
                Gizmos.color = Color.white;
                float remainingTime = Mathf.Max(0, chargeDuration - chargeTimer);
                Vector3 timePos = transform.position + Vector3.up * 1.5f;
               // UnityEditor.Handles.Label(timePos, $"���ʣ��: {remainingTime:F1}s");
            }

            // ������ȴʣ��ʱ��
            if (isOnCooldown)
            {
                Gizmos.color = Color.white;
                float remainingTime = Mathf.Max(0, cooldownDuration - cooldownTimer);
                Vector3 timePos = transform.position + Vector3.up * 1.2f;
             //   UnityEditor.Handles.Label(timePos, $"��ȴʣ��: {remainingTime:F1}s");
            }

            // ���ƻص�ʣ��ʱ��
            if (isBouncingBack)
            {
                Gizmos.color = Color.white;
                float remainingTime = Mathf.Max(0, wallBounceBackDuration - bounceTimer);
                Vector3 timePos = transform.position + Vector3.up * 0.9f;
             //   UnityEditor.Handles.Label(timePos, $"�ص�ʣ��: {remainingTime:F1}s");
            }

            // ����ѣ��ʣ��ʱ��
            if (isStunned)
            {
                Gizmos.color = Color.white;
                float remainingTime = Mathf.Max(0, stunDuration - stunTimer);
                Vector3 timePos = transform.position + Vector3.up * 0.6f;
              //  UnityEditor.Handles.Label(timePos, $"ѣ��ʣ��: {remainingTime:F1}s");
            }
        }
    }


    [SerializeField] private Transform headCheck; // ͷ������
    [SerializeField] private float headCheckRadius = 0.2f; // ͷ�����뾶

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // ��������ײ
        // Debug.LogError("׼����ͷ");
        if (((1 << collision.gameObject.layer) & playerLayer) != 0)
        {
            // ����Ƿ��ͷ������

            if (!collision.GetComponent<CatController>().isDead)
            {
                bool isHeadStomp = CheckHeadStomp(collision);

                if (isHeadStomp)
                {
                    // ��Ҳ�ͷ��ɱ����
                    Debug.LogError("�˵�ͷ��");
                    Stun();
                    animator.Play("Idle");
                }
                else
                {
                    // ��ͨ��ײ�����������˺�
                    ApplyDamageToPlayer(collision.gameObject);
                }
            }
          
        }
    }
    private bool CheckHeadStomp(Collider2D playerCollider)
    {
        // �������Ƿ��ͷ������
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
                // �����һ��������
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
        // ��ȡ��ҵ�����ֵ���������˺�
        CatController playerHealth = player.GetComponent<CatController>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage();
        }
    }


}