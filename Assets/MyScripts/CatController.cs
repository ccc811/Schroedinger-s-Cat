using UnityEngine;

public class CatController : MonoBehaviour
{
    // 移动参数
    public float moveSpeed = 5f;      // 左右移动速度
    public float jumpForce = 20f;      // 跳跃力度

    // 按键设置
    public KeyCode leftKey = KeyCode.A;      // 左移按键
    public KeyCode rightKey = KeyCode.D;     // 右移按键
    public KeyCode jumpKey = KeyCode.Space;  // 跳跃按键

    // 地面检测
    public Transform groundCheck;
    public float groundCheckRadius = 0.1f;
    public LayerMask groundLayer;
    private bool isGrounded;

    // 组件引用
    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    // 移动状态
    private float horizontalInput = 0f;
    private bool isJumping = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        // 地面检测
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        animator.SetBool("IsGrounded", isGrounded);

        // 明确按键控制移动
        horizontalInput = 0f;
        if (Input.GetKey(leftKey))
            horizontalInput = -1f;
        if (Input.GetKey(rightKey))
            horizontalInput = 1f;

        // 翻转角色朝向
        if (horizontalInput > 0)
            spriteRenderer.flipX = false;
        else if (horizontalInput < 0)
            spriteRenderer.flipX = true;

        // 设置移动动画
        animator.SetFloat("Speed", Mathf.Abs(horizontalInput));

        // 跳跃控制
        if (Input.GetKeyDown(jumpKey) && isGrounded)
        {
            isJumping = true;
            animator.SetTrigger("Jump");
        }
    }

    void FixedUpdate()
    {
        // 应用移动
        rb.velocity = new Vector2(horizontalInput * moveSpeed, rb.velocity.y);

        // 应用跳跃力（在FixedUpdate中确保物理一致性）
        if (isJumping)
        {
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            isJumping = false;
        }
    }

    // 绘制地面检测范围（仅调试时可见）
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}