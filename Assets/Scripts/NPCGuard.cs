
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class NPCGuard : MonoBehaviour
{
    [Header("Player")]
    public Transform player;

    [Header("Patrol")]
    public Transform[] patrolPoints;
    public float patrolSpeed = 1.5f;
    public float waypointDistance = 0.2f;

    [Header("Chase")]
    public float chaseSpeed = 3f;
    public float losePlayerAfter = 2f;

    [Header("Vision")]
    public float visionDistance = 4f;

    [Range(10f, 160f)]
    public float visionAngle = 70f;

    public Transform visionCone;
    public LayerMask wallLayer;

    [Header("Punch")]
    public CircleCollider2D punchHitbox;
    public float attackStopDistance = 1.1f;
    public int punchDamage = 20;
    public float punchDelay = 0.5f;

    private Rigidbody2D rb;
    private PlayerHealth playerHealth;
    private Collider2D playerCollider;

    private int patrolIndex = 0;
    private bool chasing = false;
    private float lastSeenTime = -100f;
    private float nextPunchTime = 0f;

    [Header("NPC Visual")]
    public Transform npcVisual;
    public float spriteRotationOffset = 0f;
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        if (player != null)
        {
            playerHealth = player.GetComponent<PlayerHealth>();
            playerCollider = player.GetComponent<Collider2D>();
        }

        if (playerHealth == null || playerCollider == null)
        {
            Debug.LogError(
                name + ": Assign Player with PlayerHealth and Collider2D!"
            );
        }

        if (visionCone == null)
            Debug.LogError(name + ": VisionCone is missing!");

        if (punchHitbox == null)
            Debug.LogError(name + ": PunchHitbox is missing!");
    }

    void FixedUpdate()
    {
        if (player == null || playerHealth == null ||
            playerHealth.IsDead)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // Stop chasing after losing sight.
        if (chasing &&
            Time.time - lastSeenTime > losePlayerAfter)
        {
            chasing = false;
            Debug.Log(name + ": Returning to patrol");
        }

        if (chasing)
        {
            ChasePlayer();
        }
        else
        {
            Patrol();
        }
    }

    void Patrol()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Transform point = patrolPoints[patrolIndex];

        if (point == null)
        {
            patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 direction =
            (Vector2)point.position - rb.position;

        if (direction.magnitude <= waypointDistance)
        {
            patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
            rb.linearVelocity = Vector2.zero;
            return;
        }

        FaceDirection(direction);

        rb.linearVelocity =
            direction.normalized * patrolSpeed;
    }

    void ChasePlayer()
    {
        Vector2 direction =
            (Vector2)player.position - rb.position;

        FaceDirection(direction);

        if (direction.magnitude <= attackStopDistance)
        {
            rb.linearVelocity = Vector2.zero;
            TryPunch();
        }
        else
        {
            rb.linearVelocity =
                direction.normalized * chaseSpeed;
        }
    }

    // Called by the vision cone trigger.
    public void TrySpot(Collider2D other)
    {
        if (playerHealth == null || playerHealth.IsDead)
            return;

        PlayerHealth detected =
            other.GetComponentInParent<PlayerHealth>();

        if (detected != playerHealth)
            return;

        // Walls block the guard's vision.
        if (wallLayer.value != 0)
        {
            RaycastHit2D wall = Physics2D.Linecast(
                rb.position,
                playerCollider.bounds.center,
                wallLayer
            );

            if (wall.collider != null)
                return;
        }

        if (!chasing)
            Debug.Log(name + ": PLAYER DETECTED!");

        chasing = true;
        lastSeenTime = Time.time;
    }

    void TryPunch()
    {
        if (Time.time < nextPunchTime)
            return;

        if (punchHitbox == null || playerCollider == null)
            return;

        // Only punch when the hitboxes overlap.
        if (!punchHitbox.IsTouching(playerCollider))
            return;

        // Cannot punch through walls.
        if (wallLayer.value != 0)
        {
            RaycastHit2D wall = Physics2D.Linecast(
                rb.position,
                playerCollider.bounds.center,
                wallLayer
            );

            if (wall.collider != null)
                return;
        }

        nextPunchTime = Time.time + punchDelay;

        playerHealth.TakeDamage(punchDamage);

        Debug.Log(name + ": PUNCH! -" + punchDamage + " HP");
    }


    void FaceDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.0001f)
            return;

        direction.Normalize();

        // Calculate the direction the NPC is facing.
        float angle = Mathf.Atan2(
            -direction.x,
            direction.y
        ) * Mathf.Rad2Deg;

        Quaternion rotation = Quaternion.Euler(
            0f,
            0f,
            angle
        );

        // Rotate the vision cone.
        if (visionCone != null)
        {
            visionCone.localRotation = rotation;
        }

        // Rotate the punch hitbox.
        if (punchHitbox != null)
        {
            punchHitbox.transform.localRotation = rotation;
        }

        // Rotate the NPC sprite in the same direction.
        if (npcVisual != null)
        {
            npcVisual.localRotation = Quaternion.Euler(
                0f,
                0f,
                angle + spriteRotationOffset
            );
        }
    }

}
