using UnityEngine;
using UnityEngine.Events;

public class EnemyPatrol : MonoBehaviour
{
    [Header("巡逻设置")]
    [SerializeField] private Transform[] waypoints; // 巡逻路径点
    [SerializeField] private float moveSpeed = 2f; // 移动速度
    [SerializeField] private float waitTime = 1f; // 在路径点等待的时间
    [SerializeField] private PatrolAxis patrolAxis = PatrolAxis.Horizontal; // 巡逻轴（水平/垂直）

    [Header("战斗设置")]
    [SerializeField] private int damage = 1; // 对玩家造成的伤害
    [SerializeField] private Transform headCheck; // 头顶检测点
    [SerializeField] private float headCheckRadius = 0.2f; // 头顶检测半径
    [SerializeField] private LayerMask playerLayer; // 玩家层
    [SerializeField] private float stompBounce = 5f; // 玩家踩头后的弹跳力

    [Header("状态")]
    [SerializeField] private bool isAlive = true; // 怪物是否存活
    [SerializeField] private bool isFacingRight = true; // 怪物是否面朝右侧

    private int currentWaypointIndex = 0; // 当前巡逻点索引
    private float waitCounter = 0f; // 等待计时器
    private Animator anim;
    private Collider2D col;
    private bool isWaiting = false;

    // 事件系统
    public UnityEvent OnEnemyKilled; // 怪物被击杀时触发

    // 巡逻轴枚举
    public enum PatrolAxis
    {
        Horizontal,
        Vertical
    }

    private void Start()
    {
        anim = GetComponent<Animator>();
        col = GetComponent<Collider2D>();

        // 确保至少有两个巡逻点
        if (waypoints.Length < 2)
        {
            Debug.LogError("至少需要两个巡逻点!", gameObject);
        }

        // 初始化位置到第一个巡逻点
        if (waypoints.Length > 0)
        {
            transform.position = waypoints[0].position;
        }

        // 确保碰撞器是触发器
        if (col != null)
            col.isTrigger = true;
    }

    private void Update()
    {
        if (!isAlive) return;

        if (isWaiting)
        {
            // 等待时间计数
            waitCounter += Time.deltaTime;
            if (waitCounter >= waitTime)
            {
                isWaiting = false;

                // 只有水平巡逻时才需要翻转方向
                if (patrolAxis == PatrolAxis.Horizontal)
                {
                    Flip();
                }
            }
        }
        else
        {
            // 向目标巡逻点移动
            MoveToWaypoint();

            // 检查是否到达巡逻点
            if (Vector2.Distance(transform.position, waypoints[currentWaypointIndex].position) < 0.1f)
            {
                // 到达巡逻点，开始等待
                waitCounter = 0f;
                isWaiting = true;

                // 更新下一个巡逻点索引
                currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
            }
        }

        // 更新动画状态
        if (anim != null)
        {
            anim.SetBool("isAlive", isAlive);
            anim.SetFloat("moveSpeed", moveSpeed);
        }
    }

    private void MoveToWaypoint()
    {
        // 获取目标位置
        Vector3 targetPosition = waypoints[currentWaypointIndex].position;

        // 根据巡逻轴选择移动方向
        if (patrolAxis == PatrolAxis.Horizontal)
        {
            // 水平移动
            transform.position = Vector3.MoveTowards(
                transform.position,
                new Vector3(targetPosition.x, transform.position.y, transform.position.z),
                moveSpeed * Time.deltaTime
            );

            // 自动翻转方向
            if ((targetPosition.x > transform.position.x && !isFacingRight) ||
                (targetPosition.x < transform.position.x && isFacingRight))
            {
                Flip();
            }
        }
        else
        {
            // 垂直移动
            transform.position = Vector3.MoveTowards(
                transform.position,
                new Vector3(transform.position.x, targetPosition.y, transform.position.z),
                moveSpeed * Time.deltaTime
            );
        }
    }

    private void Flip()
    {
        // 翻转方向
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
        // 检测玩家碰撞
       // Debug.LogError("准备踩头");
        if (((1 << collision.gameObject.layer) & playerLayer) != 0)
        {
           // Debug.LogError("采种");

            // 检查是否从头顶踩下
            bool isHeadStomp = CheckHeadStomp(collision);

            if (isHeadStomp)
            {
                // 玩家踩头击杀怪物
                KillEnemy(collision.gameObject);
            }
            else
            {
                // 普通碰撞，对玩家造成伤害
                ApplyDamageToPlayer(collision.gameObject);
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
                    playerRb.velocity = new Vector2(playerRb.velocity.x, stompBounce);
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

    private void KillEnemy(GameObject player)
    {
        // 标记怪物为死亡状态
        if (player.GetComponent<CatController>().isDead)
        {
            return;
        }
        isAlive = false;

        // 禁用碰撞器
        if (col != null)
        {
            col.enabled = false;
        }

        // 播放死亡动画
        if (anim != null)
        {
            anim.SetBool("isAlive", false);
        }

        // 触发死亡事件
       // OnEnemyKilled?.Invoke();

        // 一段时间后销毁怪物
        Destroy(gameObject, 0f);
    }

    // 在编辑器中绘制巡逻路径和头顶检测区域
    private void OnDrawGizmosSelected()
    {
        // 绘制巡逻路径
        if (waypoints != null && waypoints.Length > 1)
        {
            Gizmos.color = Color.red;
            for (int i = 0; i < waypoints.Length - 1; i++)
            {
                Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
            }

            // 闭合回路
            if (waypoints.Length > 2)
            {
                Gizmos.DrawLine(waypoints[waypoints.Length - 1].position, waypoints[0].position);
            }
        }

        // 绘制头顶检测区域
        if (headCheck != null)
        {
            Gizmos.color = Color.green;
            Vector2 boxSize = new Vector2(col.bounds.size.x * 0.8f, headCheckRadius * 2);
            Gizmos.DrawWireCube(headCheck.position, boxSize);
        }
    }
}