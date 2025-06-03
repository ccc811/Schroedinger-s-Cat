using UnityEngine;

public class OscillateRotation : MonoBehaviour
{
    [Header("旋转参数")]
    [SerializeField] private float minAngle = -70f; // 最小旋转角度
    [SerializeField] private float maxAngle = 70f; // 最大旋转角度
    [SerializeField] private float rotationSpeed = 45f; // 旋转速度 (度/秒)
    [SerializeField] private bool startAtMin = true; // 是否从最小角度开始
    [SerializeField] private bool rotateInstantly = false; // 是否瞬间转向

    [Header("等待时间")]
    [SerializeField] private float waitAtMin = 0.5f; // 在最小角度处等待的时间
    [SerializeField] private float waitAtMax = 0.5f; // 在最大角度处等待的时间

    private float currentAngle; // 当前角度
    private float direction = 1f; // 旋转方向 (1: 正向, -1: 反向)
    private float waitTimer = 0f; // 等待计时器
    private bool isWaiting = false; // 是否正在等待

    private void Start()
    {
        // 初始化角度
        currentAngle = startAtMin ? minAngle : maxAngle;
        direction = startAtMin ? 1f : -1f;

        // 设置初始旋转
        transform.rotation = Quaternion.Euler(0, 0, currentAngle);
    }

    private void Update()
    {
        if (isWaiting)
        {
            // 等待计时
            waitTimer += Time.deltaTime;

            if ((direction > 0 && waitTimer >= waitAtMax) ||
                (direction < 0 && waitTimer >= waitAtMin))
            {
                // 等待结束，切换方向
                isWaiting = false;
                waitTimer = 0f;

                if (rotateInstantly)
                {
                    // 瞬间转向模式
                    direction *= -1;
                }
            }
            else
            {
                // 仍在等待，不执行旋转
                return;
            }
        }

        // 计算本次旋转量
        float rotationAmount = rotationSpeed * Time.deltaTime * direction;

        // 预测下一个角度
        float nextAngle = currentAngle + rotationAmount;

        // 检查是否到达或超过边界
        if ((direction > 0 && nextAngle >= maxAngle) ||
            (direction < 0 && nextAngle <= minAngle))
        {
            // 到达或超过边界，调整到边界角度
            currentAngle = direction > 0 ? maxAngle : minAngle;
            transform.rotation = Quaternion.Euler(0, 0, currentAngle);

            // 开始等待
            isWaiting = true;

            if (!rotateInstantly)
            {
                // 平滑转向模式
                direction *= -1;
            }
        }
        else
        {
            // 未到达边界，继续旋转
            currentAngle = nextAngle;
            transform.rotation = Quaternion.Euler(0, 0, currentAngle);
        }
    }

    // 在编辑器中绘制旋转范围
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        // 绘制当前旋转角度
        Vector3 currentDirection = Quaternion.Euler(0, 0, currentAngle) * Vector3.right;
        Gizmos.DrawLine(transform.position, transform.position + currentDirection * 2f);

        // 绘制最小和最大旋转角度
        Vector3 minDirection = Quaternion.Euler(0, 0, minAngle) * Vector3.right;
        Vector3 maxDirection = Quaternion.Euler(0, 0, maxAngle) * Vector3.right;

        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + minDirection * 2f);

        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + maxDirection * 2f);

        // 绘制旋转范围弧线
        DrawArc(transform.position, minAngle, maxAngle, 2f, 20);

        // 绘制旋转方向
        if (isWaiting)
        {
            Gizmos.color = Color.white;
        }
        else
        {
            Gizmos.color = direction > 0 ? Color.cyan : Color.magenta;
        }

        float radius = 1.2f;
        Vector3 center = transform.position;
        Vector3 arrowDir = Quaternion.Euler(0, 0, currentAngle) * Vector3.right * radius;
        DrawArrow(center, arrowDir, 0.3f);
    }

    // 绘制弧线辅助方法
    private void DrawArc(Vector3 center, float startAngle, float endAngle, float radius, int segments)
    {
        Gizmos.color = Color.yellow;

        float angleStep = (endAngle - startAngle) / segments;
        Vector3 prevPoint = center + Quaternion.Euler(0, 0, startAngle) * Vector3.right * radius;

        for (int i = 1; i <= segments; i++)
        {
            float angle = startAngle + angleStep * i;
            Vector3 currentPoint = center + Quaternion.Euler(0, 0, angle) * Vector3.right * radius;

            Gizmos.DrawLine(prevPoint, currentPoint);
            prevPoint = currentPoint;
        }
    }

    // 绘制箭头辅助方法
    private void DrawArrow(Vector3 position, Vector3 direction, float size)
    {
        Gizmos.DrawLine(position, position + direction);

        // 绘制箭头头部
        Vector3 arrowHead = position + direction;
        Vector3 arrowSide1 = Quaternion.Euler(0, 0, 150) * direction.normalized * size;
        Vector3 arrowSide2 = Quaternion.Euler(0, 0, -150) * direction.normalized * size;

        Gizmos.DrawLine(arrowHead, arrowHead + arrowSide1);
        Gizmos.DrawLine(arrowHead, arrowHead + arrowSide2);
    }
}