using UnityEngine;

public class CatController : MonoBehaviour
{
    // �ƶ�����
    public float moveSpeed = 5f;      // �����ƶ��ٶ�
    public float jumpForce = 20f;      // ��Ծ����

    // ��������
    public KeyCode leftKey = KeyCode.A;      // ���ư���
    public KeyCode rightKey = KeyCode.D;     // ���ư���
    public KeyCode jumpKey = KeyCode.Space;  // ��Ծ����

    // ������
    public Transform groundCheck;
    public float groundCheckRadius = 0.1f;
    public LayerMask groundLayer;
    private bool isGrounded;

    // �������
    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    // �ƶ�״̬
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
        // ������
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        animator.SetBool("IsGrounded", isGrounded);

        // ��ȷ���������ƶ�
        horizontalInput = 0f;
        if (Input.GetKey(leftKey))
            horizontalInput = -1f;
        if (Input.GetKey(rightKey))
            horizontalInput = 1f;

        // ��ת��ɫ����
        if (horizontalInput > 0)
            spriteRenderer.flipX = false;
        else if (horizontalInput < 0)
            spriteRenderer.flipX = true;

        // �����ƶ�����
        animator.SetFloat("Speed", Mathf.Abs(horizontalInput));

        // ��Ծ����
        if (Input.GetKeyDown(jumpKey) && isGrounded)
        {
            isJumping = true;
            animator.SetTrigger("Jump");
        }
    }

    void FixedUpdate()
    {
        // Ӧ���ƶ�
        rb.velocity = new Vector2(horizontalInput * moveSpeed, rb.velocity.y);

        // Ӧ����Ծ������FixedUpdate��ȷ������һ���ԣ�
        if (isJumping)
        {
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            isJumping = false;
        }
    }

    // ���Ƶ����ⷶΧ��������ʱ�ɼ���
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}