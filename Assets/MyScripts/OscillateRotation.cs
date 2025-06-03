using UnityEngine;

public class OscillateRotation : MonoBehaviour
{
    [Header("��ת����")]
    [SerializeField] private float minAngle = -70f; // ��С��ת�Ƕ�
    [SerializeField] private float maxAngle = 70f; // �����ת�Ƕ�
    [SerializeField] private float rotationSpeed = 45f; // ��ת�ٶ� (��/��)
    [SerializeField] private bool startAtMin = true; // �Ƿ����С�Ƕȿ�ʼ
    [SerializeField] private bool rotateInstantly = false; // �Ƿ�˲��ת��

    [Header("�ȴ�ʱ��")]
    [SerializeField] private float waitAtMin = 0.5f; // ����С�Ƕȴ��ȴ���ʱ��
    [SerializeField] private float waitAtMax = 0.5f; // �����Ƕȴ��ȴ���ʱ��

    private float currentAngle; // ��ǰ�Ƕ�
    private float direction = 1f; // ��ת���� (1: ����, -1: ����)
    private float waitTimer = 0f; // �ȴ���ʱ��
    private bool isWaiting = false; // �Ƿ����ڵȴ�

    private void Start()
    {
        // ��ʼ���Ƕ�
        currentAngle = startAtMin ? minAngle : maxAngle;
        direction = startAtMin ? 1f : -1f;

        // ���ó�ʼ��ת
        transform.rotation = Quaternion.Euler(0, 0, currentAngle);
    }

    private void Update()
    {
        if (isWaiting)
        {
            // �ȴ���ʱ
            waitTimer += Time.deltaTime;

            if ((direction > 0 && waitTimer >= waitAtMax) ||
                (direction < 0 && waitTimer >= waitAtMin))
            {
                // �ȴ��������л�����
                isWaiting = false;
                waitTimer = 0f;

                if (rotateInstantly)
                {
                    // ˲��ת��ģʽ
                    direction *= -1;
                }
            }
            else
            {
                // ���ڵȴ�����ִ����ת
                return;
            }
        }

        // ���㱾����ת��
        float rotationAmount = rotationSpeed * Time.deltaTime * direction;

        // Ԥ����һ���Ƕ�
        float nextAngle = currentAngle + rotationAmount;

        // ����Ƿ񵽴�򳬹��߽�
        if ((direction > 0 && nextAngle >= maxAngle) ||
            (direction < 0 && nextAngle <= minAngle))
        {
            // ����򳬹��߽磬�������߽�Ƕ�
            currentAngle = direction > 0 ? maxAngle : minAngle;
            transform.rotation = Quaternion.Euler(0, 0, currentAngle);

            // ��ʼ�ȴ�
            isWaiting = true;

            if (!rotateInstantly)
            {
                // ƽ��ת��ģʽ
                direction *= -1;
            }
        }
        else
        {
            // δ����߽磬������ת
            currentAngle = nextAngle;
            transform.rotation = Quaternion.Euler(0, 0, currentAngle);
        }
    }

    // �ڱ༭���л�����ת��Χ
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        // ���Ƶ�ǰ��ת�Ƕ�
        Vector3 currentDirection = Quaternion.Euler(0, 0, currentAngle) * Vector3.right;
        Gizmos.DrawLine(transform.position, transform.position + currentDirection * 2f);

        // ������С�������ת�Ƕ�
        Vector3 minDirection = Quaternion.Euler(0, 0, minAngle) * Vector3.right;
        Vector3 maxDirection = Quaternion.Euler(0, 0, maxAngle) * Vector3.right;

        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + minDirection * 2f);

        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + maxDirection * 2f);

        // ������ת��Χ����
        DrawArc(transform.position, minAngle, maxAngle, 2f, 20);

        // ������ת����
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

    // ���ƻ��߸�������
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

    // ���Ƽ�ͷ��������
    private void DrawArrow(Vector3 position, Vector3 direction, float size)
    {
        Gizmos.DrawLine(position, position + direction);

        // ���Ƽ�ͷͷ��
        Vector3 arrowHead = position + direction;
        Vector3 arrowSide1 = Quaternion.Euler(0, 0, 150) * direction.normalized * size;
        Vector3 arrowSide2 = Quaternion.Euler(0, 0, -150) * direction.normalized * size;

        Gizmos.DrawLine(arrowHead, arrowHead + arrowSide1);
        Gizmos.DrawLine(arrowHead, arrowHead + arrowSide2);
    }
}