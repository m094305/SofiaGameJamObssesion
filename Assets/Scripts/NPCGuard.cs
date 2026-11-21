using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class NPCGuard : MonoBehaviour
{
    [Header("Player")]
    public Transform player;

    [Header("Patrol (assign waypoints in order)")]
    public Transform[] patrolPoints;
    public float patrolSpeed = 1.5f;
    public float waypointDistance = 0.2f;

    [Header("Chase (only after being attacked)")]
    public float chaseSpeed = 3f;
    public float losePlayerAfter = 2f;

    [Header("Vision")]
    public float visionDistance = 4f;
    [Range(10f, 160f)] public float visionAngle = 70f;
    public Transform visionCone;
    public LayerMask wallLayer;

    [Header("Punch")]
    public CircleCollider2D punchHitbox;
    public float attackStopDistance = 1.1f;
    public int punchDamage = 20;
    public float punchDelay = 0.5f;

    [Header("Interaction")]
    public float interactionDistance = 1.5f;

    [Header("NPC Visual")]
    public Transform npcVisual;
    public float spriteRotationOffset = 0f;

    private Rigidbody2D rb;
    private PlayerHealth playerHealth;
    private Collider2D playerCollider;

    private int patrolIndex;
    private bool isHostile;
    private bool chasing;
    private bool interacted;
    private float lastSeenTime = -100f;
    private float nextPunchTime;

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
            Debug.LogWarning(name + ": Assign a Player with PlayerHealth and Collider2D.");

        if (visionCone == null)
            Debug.LogWarning(name + ": Assign Vision Cone for accurate facing checks.");

        if (punchHitbox == null)
            Debug.LogWarning(name + ": Punch Hitbox is not assigned.");
    }

    void Update()
    {
        if (!CanInteract())
        {
            interacted = false;
            return;
        }

        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            Interact();
    }

    void FixedUpdate()
    {
        // A peaceful NPC only patrols; spotting the player does not start combat.
        if (!isHostile)
        {
            Patrol();
            return;
        }

        if (player == null || playerHealth == null || playerHealth.IsDead)
        {
            chasing = false;
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (CanSeePlayerNow())
        {
            chasing = true;
            lastSeenTime = Time.time;
        }
        else if (chasing && Time.time - lastSeenTime > losePlayerAfter)
        {
            chasing = false;
        }

        if (chasing)
            ChasePlayer();
        else
            Patrol();
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

        Vector2 direction = (Vector2)point.position - rb.position;
        if (direction.magnitude <= waypointDistance)
        {
            patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
            rb.linearVelocity = Vector2.zero;
            return;
        }

        FaceDirection(direction);
        rb.linearVelocity = direction.normalized * patrolSpeed;
    }

    void ChasePlayer()
    {
        Vector2 direction = (Vector2)player.position - rb.position;
        FaceDirection(direction);

        if (direction.magnitude <= attackStopDistance)
        {
            rb.linearVelocity = Vector2.zero;
            TryPunch();
        }
        else
        {
            rb.linearVelocity = direction.normalized * chaseSpeed;
        }
    }

    // Used by NPCVision, which checks the player's trigger overlap.
    public void TrySpot(Collider2D other)
    {
        if (!isHostile || playerHealth == null || playerHealth.IsDead)
            return;

        if (other.GetComponentInParent<PlayerHealth>() != playerHealth)
            return;

        if (CanSeePlayerNow())
        {
            chasing = true;
            lastSeenTime = Time.time;
        }
    }

    // Current visibility, not whether this NPC was hostile previously.
    // This is also checked by GuardHealth at the exact moment of a hit.
    public bool CanSeePlayerNow()
    {
        if (player == null || playerCollider == null ||
            playerHealth == null || playerHealth.IsDead)
            return false;

        Vector2 origin = visionCone != null
            ? (Vector2)visionCone.position
            : rb.position;
        Vector2 target = playerCollider.bounds.center;
        Vector2 toPlayer = target - origin;

        if (toPlayer.magnitude > visionDistance)
            return false;

        Vector2 facing = visionCone != null
            ? (Vector2)visionCone.up
            : Vector2.up;

        if (toPlayer.sqrMagnitude > 0.0001f &&
            Vector2.Angle(facing, toPlayer) > visionAngle * 0.5f)
            return false;

        if (wallLayer.value != 0 &&
            Physics2D.Linecast(origin, target, wallLayer).collider != null)
            return false;

        return true;
    }

    // Called by GuardHealth when a visible attack does not kill this NPC.
    public void BecomeHostile()
    {
        if (isHostile)
            return;

        isHostile = true;
        chasing = true;
        lastSeenTime = Time.time;
        Debug.Log(name + ": Attacked! Now hostile.");
    }

    void TryPunch()
    {
        if (Time.time < nextPunchTime ||
            punchHitbox == null || playerCollider == null)
            return;

        if (!punchHitbox.IsTouching(playerCollider))
            return;

        if (wallLayer.value != 0 &&
            Physics2D.Linecast(rb.position, playerCollider.bounds.center,
                wallLayer).collider != null)
            return;

        nextPunchTime = Time.time + punchDelay;
        playerHealth.TakeDamage(punchDamage);
        Debug.Log(name + ": PUNCH! -" + punchDamage + " HP");
    }

    bool CanInteract()
    {
        return !isHostile && player != null &&
            (playerHealth == null || !playerHealth.IsDead) &&
            Vector2.Distance(transform.position, player.position) <= interactionDistance;
    }

    void Interact()
    {
        interacted = true;
        Debug.Log(name + ": Interacted");
    }

    // Temporary clickable button shown near the NPC; no Canvas required.

    void OnGUI()
    {
        if (!CanInteract())
            return;

        float width = 160f;
        float height = 45f;

        Rect buttonRect = new Rect(
            (Screen.width - width) / 2f,
            Screen.height - 100f,
            width,
            height
        );

        if (GUI.Button(buttonRect,
            interacted ? "Interacted!" : "Interact (E)"))
        {
            Interact();
        }
    }


    void FaceDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.0001f)
            return;

        float angle = Mathf.Atan2(-direction.x, direction.y) * Mathf.Rad2Deg;
        Quaternion rotation = Quaternion.Euler(0f, 0f, angle);

        if (visionCone != null)
            visionCone.localRotation = rotation;

        if (punchHitbox != null)
            punchHitbox.transform.localRotation = rotation;

        if (npcVisual != null)
            npcVisual.localRotation = Quaternion.Euler(0f, 0f,
                angle + spriteRotationOffset);
    }

    void OnDrawGizmosSelected()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
            return;

        Gizmos.color = Color.yellow;
        for (int i = 0; i < patrolPoints.Length; i++)
        {
            Transform from = patrolPoints[i];
            Transform to = patrolPoints[(i + 1) % patrolPoints.Length];
            if (from == null) continue;
            Gizmos.DrawWireSphere(from.position, 0.15f);
            if (to != null && patrolPoints.Length > 1)
                Gizmos.DrawLine(from.position, to.position);
        }
    }
}
