using UnityEngine;
using UnityEngine.Events;

public class EnemyPatrol : MonoBehaviour
{
    [Header("Ѳ������")]
    [SerializeField] private Transform[] waypoints; // Ѳ��·����
    [SerializeField] private float moveSpeed = 2f; // �ƶ��ٶ�
    [SerializeField] private float waitTime = 1f; // ��·����ȴ���ʱ��
    [SerializeField] private PatrolAxis patrolAxis = PatrolAxis.Horizontal; // Ѳ���ᣨˮƽ/��ֱ��

    [Header("ս������")]
    [SerializeField] private int damage = 1; // �������ɵ��˺�
    [SerializeField] private Transform headCheck; // ͷ������
    [SerializeField] private float headCheckRadius = 0.2f; // ͷ�����뾶
    [SerializeField] private LayerMask playerLayer; // ��Ҳ�
    [SerializeField] private float stompBounce = 5f; // ��Ҳ�ͷ��ĵ�����

    [Header("״̬")]
    [SerializeField] private bool isAlive = true; // �����Ƿ���
    [SerializeField] private bool isFacingRight = true; // �����Ƿ��泯�Ҳ�

    private int currentWaypointIndex = 0; // ��ǰѲ�ߵ�����
    private float waitCounter = 0f; // �ȴ���ʱ��
    private Animator anim;
    private Collider2D col;
    private bool isWaiting = false;

    // �¼�ϵͳ
    public UnityEvent OnEnemyKilled; // ���ﱻ��ɱʱ����

    // Ѳ����ö��
    public enum PatrolAxis
    {
        Horizontal,
        Vertical
    }

    private void Start()
    {
        anim = GetComponent<Animator>();
        col = GetComponent<Collider2D>();

        // ȷ������������Ѳ�ߵ�
        if (waypoints.Length < 2)
        {
            Debug.LogError("������Ҫ����Ѳ�ߵ�!", gameObject);
        }

        // ��ʼ��λ�õ���һ��Ѳ�ߵ�
        if (waypoints.Length > 0)
        {
            transform.position = waypoints[0].position;
        }

        // ȷ����ײ���Ǵ�����
        if (col != null)
            col.isTrigger = true;
    }

    private void Update()
    {
        if (!isAlive) return;

        if (isWaiting)
        {
            // �ȴ�ʱ�����
            waitCounter += Time.deltaTime;
            if (waitCounter >= waitTime)
            {
                isWaiting = false;

                // ֻ��ˮƽѲ��ʱ����Ҫ��ת����
                if (patrolAxis == PatrolAxis.Horizontal)
                {
                    Flip();
                }
            }
        }
        else
        {
            // ��Ŀ��Ѳ�ߵ��ƶ�
            MoveToWaypoint();

            // ����Ƿ񵽴�Ѳ�ߵ�
            if (Vector2.Distance(transform.position, waypoints[currentWaypointIndex].position) < 0.1f)
            {
                // ����Ѳ�ߵ㣬��ʼ�ȴ�
                waitCounter = 0f;
                isWaiting = true;

                // ������һ��Ѳ�ߵ�����
                currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
            }
        }

        // ���¶���״̬
        if (anim != null)
        {
            anim.SetBool("isAlive", isAlive);
            anim.SetFloat("moveSpeed", moveSpeed);
        }
    }

    private void MoveToWaypoint()
    {
        // ��ȡĿ��λ��
        Vector3 targetPosition = waypoints[currentWaypointIndex].position;

        // ����Ѳ����ѡ���ƶ�����
        if (patrolAxis == PatrolAxis.Horizontal)
        {
            // ˮƽ�ƶ�
            transform.position = Vector3.MoveTowards(
                transform.position,
                new Vector3(targetPosition.x, transform.position.y, transform.position.z),
                moveSpeed * Time.deltaTime
            );

            // �Զ���ת����
            if ((targetPosition.x > transform.position.x && !isFacingRight) ||
                (targetPosition.x < transform.position.x && isFacingRight))
            {
                Flip();
            }
        }
        else
        {
            // ��ֱ�ƶ�
            transform.position = Vector3.MoveTowards(
                transform.position,
                new Vector3(transform.position.x, targetPosition.y, transform.position.z),
                moveSpeed * Time.deltaTime
            );
        }
    }

    private void Flip()
    {
        // ��ת����
        isFacingRight = !isFacingRight;
        transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isAlive) return;

        if (headCheck==null)
        {
            ApplyDamageToPlayer(collision.gameObject);
            return;
        }
        // ��������ײ
       // Debug.LogError("׼����ͷ");
        if (((1 << collision.gameObject.layer) & playerLayer) != 0)
        {
           // Debug.LogError("����");

            // ����Ƿ��ͷ������
            bool isHeadStomp = CheckHeadStomp(collision);

            if (isHeadStomp)
            {
                // ��Ҳ�ͷ��ɱ����
                KillEnemy(collision.gameObject);
            }
            else
            {
                // ��ͨ��ײ�����������˺�
                ApplyDamageToPlayer(collision.gameObject);
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
                    playerRb.velocity = new Vector2(playerRb.velocity.x, stompBounce);
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

    private void KillEnemy(GameObject player)
    {
        // ��ǹ���Ϊ����״̬
        if (player.GetComponent<CatController>().isDead)
        {
            return;
        }
        isAlive = false;

        // ������ײ��
        if (col != null)
        {
            col.enabled = false;
        }

        // ������������
        if (anim != null)
        {
            anim.SetBool("isAlive", false);
        }

        // ���������¼�
       // OnEnemyKilled?.Invoke();

        // һ��ʱ������ٹ���
        Destroy(gameObject, 0f);
    }

    // �ڱ༭���л���Ѳ��·����ͷ���������
    private void OnDrawGizmosSelected()
    {
        // ����Ѳ��·��
        if (waypoints != null && waypoints.Length > 1)
        {
            Gizmos.color = Color.red;
            for (int i = 0; i < waypoints.Length - 1; i++)
            {
                Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
            }

            // �պϻ�·
            if (waypoints.Length > 2)
            {
                Gizmos.DrawLine(waypoints[waypoints.Length - 1].position, waypoints[0].position);
            }
        }

        // ����ͷ���������
        if (headCheck != null)
        {
            Gizmos.color = Color.green;
            Vector2 boxSize = new Vector2(col.bounds.size.x * 0.8f, headCheckRadius * 2);
            Gizmos.DrawWireCube(headCheck.position, boxSize);
        }
    }
}