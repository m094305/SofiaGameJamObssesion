using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// A short kill cutscene made of still "shots": each shot fades in from black, slowly zooms in,
// then fades to black before the next one. The NPC's death lines type out over the last shot.
// Everyone in the world glides to a stop and stays frozen until it's over.
// Used for the knight (stealth kill with a thrown item) - see NPCDialogue.deathCutscene.
public class DeathCutscene : MonoBehaviour
{
    [System.Serializable]
    public class Shot
    {
        public Sprite image;
        [Tooltip("Where the action is across the image (0 = left edge, 1 = right edge); kept in the middle of the screen.")]
        [Range(0f, 1f)] public float focusX = 0.5f;
        [Tooltip("How much of the screen height the shot fills (1 = all; less leaves room for the text box).")]
        [Range(0.3f, 1f)] public float screenHeight = 1f;
        [Tooltip("Where the top of the shot sits on screen (1 = top edge; lower moves the shot down).")]
        [Range(0.3f, 1f)] public float screenTop = 1f;

        [Header("Zoom")]
        [Tooltip("Scale at the start of the shot (>1 = starts zoomed in).")]
        public float zoomFrom = 1f;
        [Tooltip("Scale at the end of the shot (<zoomFrom = pulls back to reveal).")]
        public float zoomTo = 1.1f;
        [Tooltip("Height of the zoom's centre point in the image (0 = bottom, 1 = top), e.g. where the hit lands.")]
        [Range(0f, 1f)] public float zoomCentreY = 0.5f;
        [Tooltip("On: zoom fast at first and settle. Off: smooth and steady (good for walking up to something).")]
        public bool zoomEaseOut = true;

        [Header("Walking overlay (optional, e.g. the knife hand)")]
        [Tooltip("Drawn on top of the shot, same size as it (a full-frame image with transparency).")]
        public Sprite overlay;
        [Tooltip("Where the overlay's subject is in the image (0-1); it scales around this point.")]
        public Vector2 overlayPivot = new Vector2(0.5f, 0.5f);
        [Tooltip("Where it starts, offset from where it's drawn in the image (fraction of the image size).")]
        public Vector2 overlayStart = Vector2.zero;
        [Tooltip("How far it travels over the shot from that start, as a fraction of the image size.")]
        public Vector2 overlayMove = Vector2.zero;
        [Tooltip("Its scale at the end of the shot (bigger = it comes closer).")]
        public float overlayScaleTo = 1f;
        [Tooltip("Footsteps over the shot (each one bobs the overlay).")]
        public int walkSteps = 6;
        [Tooltip("How high each step bobs, as a fraction of the image height.")]
        public float walkBob = 0.02f;
        [Tooltip("Side-to-side tilt per step, in degrees.")]
        public float walkSway = 3f;

        [Header("Cut")]
        [Tooltip("End this shot with a white flash straight into the next one (instead of fading to black).")]
        public bool flashOut;
    }

    [Header("Shots (played in order)")]
    public Shot[] shots;

    [Header("Timing")]
    public float shotDuration = 5f;
    public float fadeTime = 0.6f;
    [Tooltip("White flash at the moment of the kill; it fades straight into the first shot.")]
    public bool flashOnStart = true;
    public Color flashColor = Color.white;
    public float flashTime = 0.5f;
    [Tooltip("How fast the screen goes white on a flash cut.")]
    public float flashRise = 0.08f;

    [Header("UI (built under the Canvas)")]
    public GameObject screen;          // black backdrop + shot, hidden when not playing
    public RectTransform shotRect;
    public Image shotImage;
    public AspectRatioFitter shotFitter;
    public Image fade;                 // full-screen black used for the fades
    [Tooltip("Empty object handed to the dialogue so no character portrait covers the shot.")]
    public GameObject noPortrait;

    public static bool Playing { get; private set; }

    // Statics survive between Play sessions when domain reload is off (this project) - reset them.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { Playing = false; }

    void OnDestroy() { Playing = false; }

    public void Play(List<DialogueLine> lines)
    {
        if (Playing || shots == null || shots.Length == 0) return;
        StartCoroutine(Run(lines));
    }

    IEnumerator Run(List<DialogueLine> lines)
    {
        Playing = true;
        DialogueDirector.HoldForCutscene(true);

        fade.gameObject.SetActive(true);
        if (!flashOnStart)
        {
            SetFadeColor(Color.black);
            SetFade(0f);
            yield return Fade(0f, 1f);                   // game -> black
        }

        screen.SetActive(true);
        for (int i = 0; i < shots.Length; i++)
        {
            bool last = i == shots.Length - 1;
            current = shots[i];
            ShowShot(current);
            float t = 0f;
            Zoom(0f);

            bool flashIn = i == 0 ? flashOnStart : shots[i - 1].flashOut;
            if (flashIn)
            {
                // The hit lands: flash, and the shot appears out of it (already zooming).
                SetFadeColor(flashColor);
                SetFade(1f);
                for (float f = 0f; f < flashTime; f += Time.deltaTime)
                {
                    t += Time.deltaTime; Zoom(t);
                    float k = f / flashTime;
                    SetFade(1f - k * k * (3f - 2f * k));
                    yield return null;
                }
            }
            else
            {
                // black -> shot, already zooming
                SetFadeColor(Color.black);
                for (float f = 0f; f < fadeTime; f += Time.deltaTime)
                {
                    t += Time.deltaTime; Zoom(t); SetFade(1f - f / fadeTime);
                    yield return null;
                }
            }
            SetFade(0f);
            SetFadeColor(Color.black);

            if (last && lines != null && lines.Count > 0)
            {
                // The death lines type out over this shot; hold it until they're done.
                DialogueDirector.Instance.Play(WithoutPortraits(lines), null, noPortrait);
                float waitStart = Time.time;
                while (!DialogueManager.AnyDialogueActive && Time.time - waitStart < 2f)
                { t += Time.deltaTime; Zoom(t); yield return null; }
                while (DialogueManager.AnyDialogueActive || t < shotDuration - fadeTime)
                { t += Time.deltaTime; Zoom(t); yield return null; }
            }
            else
            {
                while (t < shotDuration - fadeTime)
                { t += Time.deltaTime; Zoom(t); yield return null; }
            }

            if (current.flashOut && !last)
            {
                // shot -> white flash (the next shot comes out of it)
                SetFadeColor(flashColor);
                for (float f = 0f; f < flashRise; f += Time.deltaTime)
                {
                    t += Time.deltaTime; Zoom(t); SetFade(f / flashRise);
                    yield return null;
                }
            }
            else
            {
                // shot -> black
                for (float f = 0f; f < fadeTime; f += Time.deltaTime)
                {
                    t += Time.deltaTime; Zoom(t); SetFade(f / fadeTime);
                    yield return null;
                }
            }
            SetFade(1f);
        }
        if (overlayImage != null) overlayImage.gameObject.SetActive(false);

        screen.SetActive(false);
        yield return Fade(1f, 0f);                       // black -> game
        fade.gameObject.SetActive(false);

        DialogueDirector.HoldForCutscene(false);
        Playing = false;
    }

    void ShowShot(Shot s)
    {
        shotImage.sprite = s.image;
        if (s.image != null)
            shotFitter.aspectRatio = s.image.rect.width / s.image.rect.height;
        shotRect.anchorMin = new Vector2(0.5f, s.screenTop - s.screenHeight);
        shotRect.anchorMax = new Vector2(0.5f, s.screenTop);
        shotRect.pivot = new Vector2(s.focusX, s.zoomCentreY);   // zoom grows/shrinks around this point
        shotRect.anchoredPosition = Vector2.zero;
        shotRect.sizeDelta = new Vector2(shotRect.sizeDelta.x, 0f);
        shotRect.localScale = Vector3.one;

        // The walking overlay sits exactly on top of the shot and moves with its zoom.
        if (s.overlay != null)
        {
            if (overlayImage == null)
            {
                var go = new GameObject("Overlay", typeof(RectTransform));
                go.transform.SetParent(shotRect, false);
                overlayImage = go.AddComponent<Image>();
                overlayImage.raycastTarget = false;
            }
            overlayImage.gameObject.SetActive(true);
            overlayImage.sprite = s.overlay;
            RectTransform rt = overlayImage.rectTransform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.pivot = s.overlayPivot;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
        }
        else if (overlayImage != null)
        {
            overlayImage.gameObject.SetActive(false);
        }
    }

    Shot current;
    Image overlayImage;

    // Walk the overlay towards its target: travels with ease-in-out, bobs once per step,
    // and grows faster near the end as it closes in.
    void MoveOverlay(float t)
    {
        if (current == null || current.overlay == null || overlayImage == null) return;
        float walkTime = Mathf.Max(0.01f, shotDuration - fadeTime);
        float p = Mathf.Clamp01(t / walkTime);
        float travel = p * p * (3f - 2f * p);
        float stepPhase = p * Mathf.Max(1, current.walkSteps) * Mathf.PI;
        float bob = Mathf.Abs(Mathf.Sin(stepPhase)) * current.walkBob;

        RectTransform rt = overlayImage.rectTransform;
        Vector2 size = shotRect.rect.size;
        Vector2 offset = current.overlayStart + current.overlayMove * travel + new Vector2(0f, bob);
        rt.anchoredPosition = new Vector2(offset.x * size.x, offset.y * size.y);
        float s = Mathf.Lerp(1f, current.overlayScaleTo, travel);
        rt.localScale = new Vector3(s, s, 1f);
        rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(stepPhase) * current.walkSway);
    }

    // Moves from zoomFrom to zoomTo over the shot, easing out (fast at first, settling at the end).
    void Zoom(float t)
    {
        if (current == null) return;
        float k = Mathf.Clamp01(t / Mathf.Max(0.01f, shotDuration));
        float eased = current.zoomEaseOut
            ? 1f - (1f - k) * (1f - k) * (1f - k)
            : k * k * (3f - 2f * k);
        float s = Mathf.Lerp(current.zoomFrom, current.zoomTo, eased);
        shotRect.localScale = new Vector3(s, s, 1f);
        MoveOverlay(t);
    }

    void SetFadeColor(Color c)
    {
        float a = fade.color.a;
        fade.color = new Color(c.r, c.g, c.b, a);
    }

    IEnumerator Fade(float from, float to)
    {
        for (float f = 0f; f < fadeTime; f += Time.deltaTime)
        {
            SetFade(Mathf.Lerp(from, to, f / fadeTime));
            yield return null;
        }
        SetFade(to);
    }

    void SetFade(float a)
    {
        Color c = fade.color; c.a = a; fade.color = c;
    }

    List<DialogueLine> WithoutPortraits(List<DialogueLine> src)
    {
        var copy = new List<DialogueLine>(src.Count);
        foreach (DialogueLine l in src)
            copy.Add(new DialogueLine { characterName = l.characterName, line = l.line, side = l.side, portraitOverride = noPortrait });
        return copy;
    }
}
