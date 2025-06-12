using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CatController : MonoBehaviour
{
    // Movement parameters
    public float moveSpeed = 5f;      // Left/right movement speed
    public float jumpForce = 20f;      // Jumping force

    // Key settings
    public KeyCode leftKey = KeyCode.A;      // Left movement key
    public KeyCode rightKey = KeyCode.D;     // Right movement key
    public KeyCode jumpKey = KeyCode.W;  // Jump key
    public KeyCode downKey = KeyCode.S;  // Teleport key

    // Ground detection
    public Transform groundCheck;
    public float groundCheckRadius = 0.1f;
    public LayerMask groundLayer;
    private bool isGrounded;

    // Component references
    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    // Movement state
    private float horizontalInput = 0f;
    private bool isJumping = false;
    public bool isInBoxForward = false;
    public BoxController curBox;
    public bool isInDoor = false;
    public LayerMask detectionLayers;

    // Currently observed box
    public BoxController curLookBox;

    public bool isDead = false;


    // left and right Wall check
 
    private float wallCheckDistance = 0.5f;
    public LayerMask whatIsWall;
    private bool isTouchingLeftWall;
    private bool isTouchingRightWall;
    private void CheckSurroundings()
    {
        // left wall
        isTouchingLeftWall = Physics2D.Raycast(transform.position, -Vector2.right, wallCheckDistance, whatIsWall);

        // right wall
        isTouchingRightWall = Physics2D.Raycast(transform.position, Vector2.right, wallCheckDistance, whatIsWall);

        if (isTouchingRightWall)
        {
            Debug.LogError("touch right wall");
        }
    }
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        // Ground detection
        if (isDead)
        {
            return;
        }

        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        animator.SetBool("IsGrounded", isGrounded);

        CheckSurroundings();
        // Explicit key control for movement
        horizontalInput = 0f;
        if (Input.GetKey(leftKey))
            horizontalInput = -1f;
        if (Input.GetKey(rightKey))
            horizontalInput = 1f;

        if (Input.GetKey(downKey) && isInBoxForward && curBox != null)
        {
            curBox.StartIn();
        }
        else if (Input.GetKey(downKey) && isInDoor)
        {
            //if (SceneManager.sceneCountInBuildSettings > SceneManager.GetActiveScene().buildIndex + 1)
            //    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
            UIController._instance.CurLevelWin();
        }

        // Flip character direction
        if (horizontalInput > 0)
            spriteRenderer.flipX = false;
        else if (horizontalInput < 0)
            spriteRenderer.flipX = true;

        // Set movement animation
        animator.SetFloat("Speed", Mathf.Abs(horizontalInput));

        // Jump control
        if (Input.GetKeyDown(jumpKey) && isGrounded)
        {
            isJumping = true;
            animator.SetTrigger("Jump");
        }


        // Start point of the ray (usually character position)
        Vector2 origin = transform.position;

        // Ray direction (determined by character facing)
        Vector2 direction = !spriteRenderer.flipX ? Vector2.right : Vector2.left;

        // Cast the ray and get results
        RaycastHit2D hit = Physics2D.Raycast(origin, direction, 3, detectionLayers);

        // Draw the ray (visible only in debug mode)
        Debug.DrawRay(origin, direction * 3, Color.red);

        // Process collision results
        if (hit.collider != null)
        {
            // Add specific logic here, such as stopping movement or triggering interaction
            if (hit.collider.CompareTag("Box"))
            {
                curLookBox = hit.transform.GetComponent<BoxController>();
                curLookBox.boxCanMove = false;
            }
        }
        else
        {
            if (curLookBox)
            {
                curLookBox.boxCanMove = true;
            }
        }
    }

    void FixedUpdate()
    {
        // Apply movement
        if (isTouchingLeftWall && horizontalInput < 0)
        {
            horizontalInput = 0;
        }

        // When reaching the right wall, do not move to the right.
        if (isTouchingRightWall && horizontalInput > 0)
        {
            horizontalInput = 0;
        }

        rb.velocity = new Vector2(horizontalInput * moveSpeed, rb.velocity.y);

        // Apply jump force (in FixedUpdate for physics consistency)
        if (isJumping)
        {
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            isJumping = false;
        }
    }

    // Draw ground detection range (visible only in debug mode)
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if ((collision.transform.tag == "DC") && !isDead)
        {
            TakeDamage();
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
        // Calculate knockback direction (away from spikes)
        Vector2 knockbackDirection = (playerRb.transform.position - transform.position).normalized;

        // Adjust vertical force
        knockbackDirection.y = 2;

        // Apply knockback force
        playerRb.velocity = Vector2.zero;
        playerRb.AddForce(knockbackDirection * 5, ForceMode2D.Impulse);
    }

    IEnumerator WaitToLoadSceen()
    {
        yield return new WaitForSeconds(2f);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
