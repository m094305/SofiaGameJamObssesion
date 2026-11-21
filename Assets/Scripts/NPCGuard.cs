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

    [Tooltip("Off: walk straight between the FIRST and LAST point only (log to log). " +
             "On: also pass through the points in between, e.g. to route around something.")]
    public bool useMiddlePoints = false;

    [Header("Patrol Wander (walks the path end to end, loosely)")]
    [Tooltip("How far it slowly drifts sideways off the path.")]
    public float strayRadius = 0.5f;
    [Tooltip("Small quick left/right wobble while walking.")]
    public float weaveAmount = 0.2f;
    [Tooltip("How fast the wobble is.")]
    public float weaveSpeed = 1.2f;
    [Tooltip("How far one walk goes before stopping, as a fraction of the whole path (min/max).")]
    public Vector2 walkLength = new Vector2(1f, 1f);   // 1 = always walk the whole way end to end
    [Tooltip("After stopping mid-path, chance to keep going the same way instead of turning around.")]
    [Range(0f, 1f)] public float keepDirectionChance = 0.65f;
    [Tooltip("Random pause between walks (min/max seconds).")]
    public Vector2 pauseAtPoint = new Vector2(0.2f, 0.8f);
    [Tooltip("How far ahead along the path it aims. Bigger = smoother corners.")]
    public float lookAhead = 0.8f;
    [Tooltip("Radius used to keep the sideways drift out of walls.")]
    public float wallCheckRadius = 0.25f;

    [Header("Chase (only after being attacked)")]
    public float chaseSpeed = 3f;
    public float losePlayerAfter = 2f;

    [Header("Vision")]
    [Tooltip("How far the NPC sees straight ahead.")]
    public float visionDistance = 7f;
    [Tooltip("Width of the main (long-range) sight cone.")]
    [Range(10f, 160f)] public float visionAngle = 90f;
    [Tooltip("Peripheral vision: a wider, shorter-range view around the NPC.")]
    public float peripheralDistance = 3f;
    [Range(90f, 360f)] public float peripheralAngle = 240f;
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
    [Tooltip("Rotate the visual to face the walking direction. Leave off for NPCs with " +
             "front/back/side sprite frames (NPCSpriteAnimator picks the frame instead).")]
    public bool rotateVisual = false;

    [Header("Sees You Indicator")]
    [Tooltip("Show a red \"!\" over the NPC's head while it can see the player.")]
    public bool showSeesYouIcon = true;
    public float seesYouIconHeight = 1.0f;

    private Rigidbody2D rb;
    private PlayerHealth playerHealth;
    private Collider2D playerCollider;

    private int walkDirection;        // +1 = towards last log, -1 = towards first log, 0 = not chosen yet
    private bool walking;
    private float walkTargetS;        // distance along the path where this walk ends
    private float pauseUntil;
    private float weaveSeed;
    private float driftSeed;
    private Vector2 lastGoal;
    private float stuckCheckS;
    private float stuckCheckTime;
    private bool isHostile;
    private bool chasing;
    private bool interacted;
    private float lastSeenTime = -100f;
    private float nextPunchTime;
    private bool inDialogue;
    private Vector2 dialogueStartVelocity;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        weaveSeed = Random.Range(0f, 100f);
        driftSeed = Random.Range(0f, 100f);

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

        if (showSeesYouIcon)
            CreateSeesYouIcon();
    }

    // ---- "!" over the head while this NPC can see the player ----

    private GameObject seesYouIcon;

    void CreateSeesYouIcon()
    {
        seesYouIcon = new GameObject("Sees You Icon");
        seesYouIcon.transform.SetParent(transform, false);
        seesYouIcon.transform.localPosition = new Vector3(0f, seesYouIconHeight, 0f);
        var text = seesYouIcon.AddComponent<TMPro.TextMeshPro>();
        text.text = "!";
        text.fontSize = 7f;
        text.fontStyle = TMPro.FontStyles.Bold;
        text.alignment = TMPro.TextAlignmentOptions.Center;
        text.color = new Color(1f, 0.25f, 0.2f);
        text.outlineWidth = 0.25f;
        text.outlineColor = Color.black;
        text.sortingOrder = 50;
        text.rectTransform.sizeDelta = new Vector2(1f, 1f);
        seesYouIcon.SetActive(false);
    }

    void UpdateSeesYouIcon()
    {
        if (seesYouIcon == null) return;
        bool sees = CanSeePlayerNow();
        if (seesYouIcon.activeSelf != sees)
            seesYouIcon.SetActive(sees);
    }

    void Update()
    {
        UpdateSeesYouIcon();

        if (!CanInteract())
        {
            interacted = false;
            return;
        }

        if (GameInput.InteractPressed)
            Interact();
    }

    void FixedUpdate()
    {
        // Everyone glides to a stop while a conversation is on screen.
        if (DialogueManager.AnyDialogueActive || DialogueDirector.Freezing)
        {
            if (!inDialogue)
            {
                inDialogue = true;
                dialogueStartVelocity = rb.linearVelocity;
            }
            rb.linearVelocity = dialogueStartVelocity * DialogueDirector.MoveScale;
            stuckCheckTime = Time.time;     // don't count the pause as being stuck
            lastSeenTime = Time.time;       // and don't lose the player during it
            return;
        }
        inDialogue = false;

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

    // The patrol points form a path (first log ... last log). The NPC picks a
    // random direction at the start, walks a good stretch of the path that way,
    // pauses, then either keeps going or turns around. At either end it always
    // turns around.
    void Patrol()
    {
        if (CountValidPoints() < 2)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (Time.time < pauseUntil)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (!walking)
            StartWalk(false);

        float s = ClosestS(rb.position);

        bool arrived = walkDirection > 0
            ? s >= walkTargetS - waypointDistance
            : s <= walkTargetS + waypointDistance;
        if (arrived)
        {
            walking = false;
            rb.linearVelocity = Vector2.zero;
            pauseUntil = Time.time + Random.Range(pauseAtPoint.x, pauseAtPoint.y);
            return;
        }

        // Aim a bit ahead along the path, never past this walk's end.
        float lookS = s + walkDirection * lookAhead;
        lookS = walkDirection > 0 ? Mathf.Min(lookS, walkTargetS) : Mathf.Max(lookS, walkTargetS);

        Vector2 basePoint = PointAt(lookS, out Vector2 tangent);
        Vector2 normal = new Vector2(-tangent.y, tangent.x);

        // Slow sideways drift + small quick wobble, so it never walks a strict line.
        float drift = (Mathf.PerlinNoise(driftSeed, Time.time * 0.15f) - 0.5f) * 2f * strayRadius;
        float wobble = (Mathf.PerlinNoise(weaveSeed, Time.time * weaveSpeed) - 0.5f) * 2f * weaveAmount;
        Vector2 goal = basePoint + normal * (drift + wobble);

        if (!IsSpotFree(goal, rb.position))
            goal = basePoint;
        lastGoal = goal;

        Vector2 toGoal = goal - rb.position;
        if (toGoal.sqrMagnitude < 0.0001f)
            toGoal = tangent * walkDirection;

        Vector2 moveDir = toGoal.normalized;
        FaceDirection(moveDir);
        rb.linearVelocity = moveDir * patrolSpeed;

        CheckIfStuck(s);
    }

    void StartWalk(bool forceReverse)
    {
        float total = PathLength();
        float s = ClosestS(rb.position);

        if (walkDirection == 0)
            walkDirection = Random.value < 0.5f ? 1 : -1;      // first walk: random side
        else if (forceReverse || Random.value > keepDirectionChance)
            walkDirection = -walkDirection;

        // At an end there's only one way to go.
        if (s <= waypointDistance) walkDirection = 1;
        else if (s >= total - waypointDistance) walkDirection = -1;

        float remaining = walkDirection > 0 ? total - s : s;
        float run = Random.Range(walkLength.x, walkLength.y) * total;

        // If it would stop just short of the end, go all the way to the log.
        if (run >= remaining || remaining - run < total * 0.2f)
            run = remaining;

        walkTargetS = s + walkDirection * run;
        walking = true;
        stuckCheckS = s;
        stuckCheckTime = Time.time;
    }

    // If it hasn't made progress along the path for a while, turn around.
    void CheckIfStuck(float s)
    {
        if (Time.time - stuckCheckTime < 1.5f)
            return;

        if (Mathf.Abs(s - stuckCheckS) < 0.2f)
            StartWalk(true);

        stuckCheckS = ClosestS(rb.position);
        stuckCheckTime = Time.time;
    }

    // Spot isn't inside a wall and there's a clear line to it from 'from'.
    bool IsSpotFree(Vector2 spot, Vector2 from)
    {
        if (wallLayer.value == 0)
            return true;

        if (Physics2D.OverlapCircle(spot, wallCheckRadius, wallLayer) != null)
            return false;

        float dist = Vector2.Distance(from, spot);
        if (dist < 0.01f)
            return true;

        return Physics2D.CircleCast(from, wallCheckRadius * 0.8f,
            spot - from, dist, wallLayer).collider == null;
    }

    // ---- Path helpers (path = patrol points in order, empty slots skipped) ----

    int CountValidPoints()
    {
        if (patrolPoints == null) return 0;
        int n = 0;
        foreach (Transform p in patrolPoints) if (p != null) n++;
        return n;
    }

    Vector2[] PathPoints()
    {
        Vector2[] pts = new Vector2[CountValidPoints()];
        int i = 0;
        foreach (Transform p in patrolPoints) if (p != null) pts[i++] = p.position;

        // Just the two ends: first log <-> last log.
        if (!useMiddlePoints && pts.Length > 2)
            return new[] { pts[0], pts[pts.Length - 1] };

        return pts;
    }

    float PathLength()
    {
        Vector2[] pts = PathPoints();
        float total = 0f;
        for (int i = 0; i < pts.Length - 1; i++)
            total += Vector2.Distance(pts[i], pts[i + 1]);
        return total;
    }

    // Distance along the path of the point closest to 'pos'.
    float ClosestS(Vector2 pos)
    {
        Vector2[] pts = PathPoints();
        float bestS = 0f, bestDist = float.MaxValue, acc = 0f;
        for (int i = 0; i < pts.Length - 1; i++)
        {
            Vector2 a = pts[i];
            Vector2 seg = pts[i + 1] - pts[i];
            float len = seg.magnitude;
            float t = len > 0.0001f ? Mathf.Clamp01(Vector2.Dot(pos - a, seg) / (len * len)) : 0f;
            float d = (a + seg * t - pos).sqrMagnitude;
            if (d < bestDist)
            {
                bestDist = d;
                bestS = acc + len * t;
            }
            acc += len;
        }
        return bestS;
    }

    // Point at distance 's' along the path, plus the path direction there.
    Vector2 PointAt(float s, out Vector2 tangent)
    {
        Vector2[] pts = PathPoints();
        tangent = Vector2.right;
        for (int i = 0; i < pts.Length - 1; i++)
        {
            Vector2 seg = pts[i + 1] - pts[i];
            float len = seg.magnitude;
            if (len > 0.0001f) tangent = seg / len;
            if (s <= len || i == pts.Length - 2)
                return pts[i] + tangent * Mathf.Clamp(s, 0f, len);
            s -= len;
        }
        return pts.Length > 0 ? pts[0] : rb.position;
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

        // Hiding in a bush makes the player invisible to NPCs.
        if (PlayerHiding.IsPlayerHidden(player))
            return false;

        Vector2 origin = visionCone != null
            ? (Vector2)visionCone.position
            : rb.position;
        Vector2 target = playerCollider.bounds.center;
        Vector2 toPlayer = target - origin;

        Vector2 facing = visionCone != null
            ? (Vector2)visionCone.up
            : Vector2.up;

        // Main sight: long range, in front.
        bool inMainCone = toPlayer.magnitude <= visionDistance &&
            (toPlayer.sqrMagnitude < 0.0001f ||
             Vector2.Angle(facing, toPlayer) <= visionAngle * 0.5f);

        // Peripheral sight: shorter range, much wider (measured from the NPC's body).
        Vector2 fromBody = target - rb.position;
        bool inPeripheral = fromBody.magnitude <= peripheralDistance &&
            (fromBody.sqrMagnitude < 0.0001f ||
             Vector2.Angle(facing, fromBody) <= peripheralAngle * 0.5f);

        if (!inMainCone && !inPeripheral)
            return false;

        if (inPeripheral && !inMainCone)
            origin = rb.position;

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
        // No interact prompt (or E-to-interact) while a conversation is on screen;
        // E belongs to the dialogue then.
        if (DialogueManager.InputBlocked || DialogueDirector.Freezing)
            return false;

        // Only when close AND the NPC can see you.
        return !isHostile && player != null &&
            (playerHealth == null || !playerHealth.IsDead) &&
            Vector2.Distance(transform.position, player.position) <= interactionDistance &&
            CanSeePlayerNow();
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
            interacted ? "Interacted!" : "Interact (E / J)"))
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

        if (npcVisual != null && rotateVisual)
            npcVisual.localRotation = Quaternion.Euler(0f, 0f,
                angle + spriteRotationOffset);
    }

    // Sight areas: red = main cone, orange = peripheral.
    void DrawVisionGizmos()
    {
        Vector3 facing = visionCone != null ? visionCone.up : Vector3.up;
        Vector3 coneOrigin = visionCone != null ? visionCone.position : transform.position;
        DrawArc(coneOrigin, facing, visionAngle, visionDistance, new Color(1f, 0.2f, 0.2f, 0.8f));
        DrawArc(transform.position, facing, peripheralAngle, peripheralDistance, new Color(1f, 0.6f, 0.1f, 0.8f));
    }

    static void DrawArc(Vector3 origin, Vector3 facing, float angle, float radius, Color color)
    {
        Gizmos.color = color;
        int steps = Mathf.Max(6, Mathf.CeilToInt(angle / 10f));
        Vector3 prev = origin;
        for (int i = 0; i <= steps; i++)
        {
            float a = -angle * 0.5f + angle * i / steps;
            Vector3 p = origin + Quaternion.Euler(0f, 0f, a) * facing * radius;
            Gizmos.DrawLine(prev, p);
            prev = p;
        }
        if (angle < 360f) Gizmos.DrawLine(prev, origin);
    }

    void OnDrawGizmosSelected()
    {
        DrawVisionGizmos();

        if (patrolPoints == null || patrolPoints.Length == 0)
            return;

        Gizmos.color = Color.yellow;
        // The path it actually walks (just first <-> last unless useMiddlePoints is on).
        Vector2[] pts = PathPoints();
        for (int i = 0; i < pts.Length; i++)
        {
            Gizmos.DrawWireSphere(pts[i], 0.15f);
            if (i + 1 < pts.Length)
                Gizmos.DrawLine(pts[i], pts[i + 1]);
        }

        // Where it's currently steering towards.
        if (Application.isPlaying && walking)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, lastGoal);
            Gizmos.DrawWireSphere(lastGoal, 0.1f);
        }
    }
}
