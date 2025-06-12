using System.Collections;
using UnityEngine;

public class PingPongMovement : MonoBehaviour
{
    // Movement direction enum
    public enum MoveDirection
    {
        Horizontal,  // Horizontal movement
        Vertical     // Vertical movement
    }

    // Movement direction
    public MoveDirection direction = MoveDirection.Horizontal;

    // Movement distance (based on initial position)
    public float moveDistance = 5.0f;

    // Movement speed
    public float moveSpeed = 2.0f;

    // Whether to start moving immediately
    public bool startMoving = true;

    // Whether to rotate the object when it reaches the target point
    public bool rotateOnArrival = false;

    // Movement delay time
    public float delayTime = 0.5f;

    // Save initial position
    private Vector3 startPosition;

    // Save the left/right or up/down target points
    private Vector3 pointA;
    private Vector3 pointB;

    void Start()
    {
        // Record initial position
        startPosition = transform.position;

        // Set target points based on selected direction
        SetTargetPoints();

        if (startMoving)
        {
            StartCoroutine(MoveBetweenPoints());
        }
    }

    // Set target points
    private void SetTargetPoints()
    {
        if (direction == MoveDirection.Horizontal)
        {
            // Horizontal movement: left and right
            pointA = startPosition + Vector3.left * moveDistance / 2;
            pointB = startPosition + Vector3.right * moveDistance / 2;
        }
        else
        {
            // Vertical movement: up and down
            pointA = startPosition + Vector3.down * moveDistance / 2;
            pointB = startPosition + Vector3.up * moveDistance / 2;
        }
    }

    // Coroutine: Control object to move back and forth between two points
    IEnumerator MoveBetweenPoints()
    {
        // Movement flag, true means moving towards point B, false means moving towards point A
        bool movingToB = true;

        while (true)
        {
            // Select target point based on movement direction
            Vector3 target = movingToB ? pointB : pointA;

            // Calculate direction from current position to target point
            Vector3 direction = (target - transform.position).normalized;

            // Move until close to target point
            while (Vector3.Distance(transform.position, target) > 0.05f)
            {
                transform.position += direction * moveSpeed * Time.deltaTime;
                yield return null;
            }

            // If rotation is needed after reaching target point
            if (rotateOnArrival)
            {
                transform.Rotate(0, 0, 180); // Rotate around Z axis in 2D game
            }

            // Delay before starting next movement
            yield return new WaitForSeconds(delayTime);

            // Switch movement direction
            movingToB = !movingToB;
        }
    }

    // External call: Start movement
    public void StartMovement()
    {
        if (!IsInvoking("MoveBetweenPoints") && !startMoving)
        {
            StartCoroutine(MoveBetweenPoints());
            startMoving = true;
        }
    }

    // External call: Stop movement
    public void StopMovement()
    {
        if (startMoving)
        {
            StopCoroutine(MoveBetweenPoints());
            startMoving = false;
        }
    }

    // External call: Change movement direction
    public void ChangeDirection(MoveDirection newDirection)
    {
        direction = newDirection;
        SetTargetPoints();

        // If currently moving, restart coroutine to apply new direction
        if (startMoving)
        {
            StopCoroutine(MoveBetweenPoints());
            StartCoroutine(MoveBetweenPoints());
        }
    }
}