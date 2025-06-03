using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target; // 相机跟随的目标
    [SerializeField] private float smoothSpeed = 0.125f; // 相机平滑移动的速度
    [SerializeField] private Vector3 offset; // 相机与目标的偏移量

    [Header("边界限制")]
    [SerializeField] private bool limitX = true; // 是否限制X轴移动
    [SerializeField] private bool limitY = true; // 是否限制Y轴移动
    [SerializeField] private float minX; // X轴最小边界
    [SerializeField] private float maxX; // X轴最大边界
    [SerializeField] private float minY; // Y轴最小边界
    [SerializeField] private float maxY; // Y轴最大边界

    private void LateUpdate()
    {
        print(123123123);
        if (target == null) return;

        // 计算相机的目标位置
        Vector3 desiredPosition = target.position + offset;

        // 应用边界限制
        if (limitX)
        {
            desiredPosition.x = Mathf.Clamp(desiredPosition.x, minX, maxX);
        }

        if (limitY)
        {
            desiredPosition.y = Mathf.Clamp(desiredPosition.y, minY, maxY);
        }

        // 保持相机Z轴不变
        desiredPosition.z = transform.position.z;

        // 平滑移动相机
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.position = smoothedPosition;
    }

    // 在编辑器中绘制边界可视化
    private void OnDrawGizmosSelected()
    {
        if (!limitX && !limitY) return;

        Gizmos.color = Color.red;

        // 计算相机的半宽高
        float cameraHeight = Camera.main.orthographicSize * 2;
        float cameraWidth = cameraHeight * Camera.main.aspect;

        // 绘制X轴边界
        if (limitX)
        {
            Vector3 topLeft = new Vector3(minX, transform.position.y + cameraHeight / 2, 0);
            Vector3 bottomLeft = new Vector3(minX, transform.position.y - cameraHeight / 2, 0);
            Vector3 topRight = new Vector3(maxX, transform.position.y + cameraHeight / 2, 0);
            Vector3 bottomRight = new Vector3(maxX, transform.position.y - cameraHeight / 2, 0);

            Gizmos.DrawLine(topLeft, bottomLeft);
            Gizmos.DrawLine(topRight, bottomRight);
        }

        // 绘制Y轴边界
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