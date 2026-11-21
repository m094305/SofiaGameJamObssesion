using UnityEngine;

// Plays a 4-direction walk cycle for an NPC from its sprite sheet frames,
// picking the direction from how the NPC's Rigidbody2D is moving.
// Shows the first frame of the last direction while standing still.
[RequireComponent(typeof(SpriteRenderer))]
public class NPCSpriteAnimator : MonoBehaviour
{
    [Header("Walk frames (4 each)")]
    public Sprite[] down;   // facing the camera
    public Sprite[] up;     // facing away
    public Sprite[] left;
    public Sprite[] right;

    [Header("Playback")]
    public float framesPerSecond = 8f;
    [Tooltip("Below this speed the NPC counts as standing still.")]
    public float moveThreshold = 0.1f;

    SpriteRenderer sr;
    Rigidbody2D body;
    Sprite[] current;
    float timer;
    int frame;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        body = GetComponentInParent<Rigidbody2D>();
        current = down;
        Show(0);
    }

    void Update()
    {
        Vector2 v = body != null ? body.linearVelocity : Vector2.zero;

        if (v.sqrMagnitude < moveThreshold * moveThreshold)
        {
            // Standing: hold the first frame of the way it was facing.
            frame = 0;
            timer = 0f;
            Show(0);
            return;
        }

        Sprite[] dir = Mathf.Abs(v.x) > Mathf.Abs(v.y)
            ? (v.x > 0f ? right : left)
            : (v.y > 0f ? up : down);

        if (dir != current)
        {
            current = dir;
            frame = 0;
            timer = 0f;
        }

        timer += Time.deltaTime;
        float step = 1f / Mathf.Max(0.01f, framesPerSecond);
        while (timer >= step)
        {
            timer -= step;
            frame++;
        }
        Show(frame);
    }

    // Turn to face a direction while standing (e.g. toward the player in a conversation).
    public void Face(Vector2 dir)
    {
        if (dir.sqrMagnitude < 0.0001f) return;
        current = Mathf.Abs(dir.x) > Mathf.Abs(dir.y)
            ? (dir.x > 0f ? right : left)
            : (dir.y > 0f ? up : down);
        frame = 0;
        timer = 0f;
        Show(0);
    }

    void Show(int i)
    {
        if (current == null || current.Length == 0) return;
        sr.sprite = current[i % current.Length];
    }
}
