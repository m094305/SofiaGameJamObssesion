using System.Collections;
using UnityEngine;

// Narcissus' punch: a wind-up frame, then the strike frame, made from his own walk sprites
// (Assets/Objects/MainCharacter_Punch.png). The walk Animator is paused while it plays.
public class PlayerPunchAnimation : MonoBehaviour
{
    [Header("Frames (wind-up, strike)")]
    public Sprite downWindup, downStrike;
    public Sprite upWindup, upStrike;
    public Sprite rightWindup, rightStrike;
    public Sprite leftWindup, leftStrike;

    [Header("Timing")]
    public float windupTime = 0.07f;
    public float strikeTime = 0.16f;

    public bool Playing { get; private set; }
    public float TimeToImpact => windupTime;

    SpriteRenderer sr;
    Animator animator;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
    }

    public void Play(Vector2 facing)
    {
        if (sr == null) return;
        Sprite w, s;
        if (Mathf.Abs(facing.x) > Mathf.Abs(facing.y))
        {
            w = facing.x > 0 ? rightWindup : leftWindup;
            s = facing.x > 0 ? rightStrike : leftStrike;
        }
        else
        {
            w = facing.y > 0 ? upWindup : downWindup;
            s = facing.y > 0 ? upStrike : downStrike;
        }
        if (w == null || s == null) return;
        StopAllCoroutines();
        StartCoroutine(Run(w, s));
    }

    IEnumerator Run(Sprite windup, Sprite strike)
    {
        Playing = true;
        Sprite before = sr.sprite;
        bool animOn = animator != null && animator.enabled;
        if (animOn) animator.enabled = false;

        sr.sprite = windup;
        yield return new WaitForSeconds(windupTime);
        sr.sprite = strike;
        yield return new WaitForSeconds(strikeTime);

        sr.sprite = before;
        if (animOn) animator.enabled = true;
        Playing = false;
    }

    void OnDisable()
    {
        if (animator != null) animator.enabled = true;
        Playing = false;
    }
}
