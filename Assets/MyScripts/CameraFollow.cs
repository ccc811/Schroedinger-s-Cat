using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target; // ��������Ŀ��
    [SerializeField] private float smoothSpeed = 0.125f; // ���ƽ���ƶ����ٶ�
    [SerializeField] private Vector3 offset; // �����Ŀ���ƫ����

    [Header("�߽�����")]
    [SerializeField] private bool limitX = true; // �Ƿ�����X���ƶ�
    [SerializeField] private bool limitY = true; // �Ƿ�����Y���ƶ�
    [SerializeField] private float minX; // X����С�߽�
    [SerializeField] private float maxX; // X�����߽�
    [SerializeField] private float minY; // Y����С�߽�
    [SerializeField] private float maxY; // Y�����߽�

    private void LateUpdate()
    {
        print(123123123);
        if (target == null) return;

        // ���������Ŀ��λ��
        Vector3 desiredPosition = target.position + offset;

        // Ӧ�ñ߽�����
        if (limitX)
        {
            desiredPosition.x = Mathf.Clamp(desiredPosition.x, minX, maxX);
        }

        if (limitY)
        {
            desiredPosition.y = Mathf.Clamp(desiredPosition.y, minY, maxY);
        }

        // �������Z�᲻��
        desiredPosition.z = transform.position.z;

        // ƽ���ƶ����
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.position = smoothedPosition;
    }

    // �ڱ༭���л��Ʊ߽���ӻ�
    private void OnDrawGizmosSelected()
    {
        if (!limitX && !limitY) return;

        Gizmos.color = Color.red;

        // ��������İ���
        float cameraHeight = Camera.main.orthographicSize * 2;
        float cameraWidth = cameraHeight * Camera.main.aspect;

        // ����X��߽�
        if (limitX)
        {
            Vector3 topLeft = new Vector3(minX, transform.position.y + cameraHeight / 2, 0);
            Vector3 bottomLeft = new Vector3(minX, transform.position.y - cameraHeight / 2, 0);
            Vector3 topRight = new Vector3(maxX, transform.position.y + cameraHeight / 2, 0);
            Vector3 bottomRight = new Vector3(maxX, transform.position.y - cameraHeight / 2, 0);

            Gizmos.DrawLine(topLeft, bottomLeft);
            Gizmos.DrawLine(topRight, bottomRight);
        }

        // ����Y��߽�
        if (limitY)
        {
            Vector3 topLeft = new Vector3(transform.position.x - cameraWidth / 2, maxY, 0);
            Vector3 topRight = new Vector3(transform.position.x + cameraWidth / 2, maxY, 0);
            Vector3 bottomLeft = new Vector3(transform.position.x - cameraWidth / 2, minY, 0);
            Vector3 bottomRight = new Vector3(transform.position.x + cameraWidth / 2, minY, 0);

            Gizmos.DrawLine(topLeft, topRight);
            Gizmos.DrawLine(bottomLeft, bottomRight);
        }
    }
}