using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CatController : MonoBehaviour
{
    // �ƶ�����
    public float moveSpeed = 5f;      // �����ƶ��ٶ�
    public float jumpForce = 20f;      // ��Ծ����

    // ��������
    public KeyCode leftKey = KeyCode.A;      // ���ư���
    public KeyCode rightKey = KeyCode.D;     // ���ư���
    public KeyCode jumpKey = KeyCode.W;  // ��Ծ����
    public KeyCode DownKey = KeyCode.S;  // ���Ͱ���

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
    public bool isInBoxForward = false;
    public BoxController curBox;
    public bool isInDoor = false;
    public LayerMask detectionLayers;

    //��ǰ�۲��box
    public BoxController curLookBox;

    public bool isDead = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        // ������
        if (isDead)
        {
            return;
        }
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        animator.SetBool("IsGrounded", isGrounded);

        // ��ȷ���������ƶ�
        horizontalInput = 0f;
        if (Input.GetKey(leftKey))
            horizontalInput = -1f;
        if (Input.GetKey(rightKey))
            horizontalInput = 1f;

        if (Input.GetKey(DownKey)&& isInBoxForward&& curBox!=null)
        {
            curBox.StartIn();
        }
        else if (Input.GetKey(DownKey) && isInDoor)
        {
            if (SceneManager.sceneCountInBuildSettings> SceneManager.GetActiveScene().buildIndex + 1)
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex +1);

           // print("����");
        }

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


        // �������ߵ���㣨ͨ���ǽ�ɫλ�ã�
        Vector2 origin = transform.position;

        // ���߷��򣨸��ݽ�ɫ���������
        Vector2 direction = !spriteRenderer.flipX ? Vector2.right : Vector2.left;

        // �������߲���ȡ���
        RaycastHit2D hit = Physics2D.Raycast(origin, direction, 3, detectionLayers);

        // �������ߣ�������ʱ�ɼ���
        Debug.DrawRay(origin, direction * 3, Color.red);

        // ������ײ���
        if (hit.collider != null)
        {
            Debug.Log("��⵽����: " + hit.collider.tag);

            // ���������Ӿ����߼�����ֹͣ�ƶ�������������
            if (hit.collider.CompareTag("Box"))
            {
                curLookBox = hit.transform.GetComponent<BoxController>();
                curLookBox.boxCanMove = false;

            }
        }
        else
        {
            Debug.Log("û��� ");
            if (curLookBox)
            {
                curLookBox.boxCanMove = true;
            }
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

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if ((collision.transform.tag == "DC")&& !isDead)
        {
            Debug.LogError("����");
            isDead = true;
            rb.freezeRotation = false;
            transform.eulerAngles = new Vector3(0, 0, 150);
            ApplyKnockback(rb);
            transform.GetComponent<Collider2D>().isTrigger = true;
        }
    }
    public void TakeDamage() 
    {
        if (!isDead)
        {
            isDead = true;
            transform.GetComponent<Collider2D>().isTrigger = true;
            rb.freezeRotation = false;
            transform.eulerAngles = new Vector3(0, 0, 150);
            ApplyKnockback(rb);
            transform.GetComponent<Collider2D>().isTrigger = true;

            StartCoroutine(WaitToLoadSceen());
        }
    }
    private void ApplyKnockback(Rigidbody2D playerRb)
    {
        // ������˷���Զ��ش̣�
        Vector2 knockbackDirection = (playerRb.transform.position - transform.position).normalized;

        // ������ֱ���������
        knockbackDirection.y = 2;

        // Ӧ�û�����
        playerRb.velocity = Vector2.zero;
        playerRb.AddForce(knockbackDirection * 5, ForceMode2D.Impulse);
    }

    IEnumerator WaitToLoadSceen()
    {
        yield return new WaitForSeconds(5f);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}