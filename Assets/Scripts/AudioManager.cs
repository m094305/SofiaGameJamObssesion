using UnityEngine;

// ttizuto's sound manager, now also directing the music:
//   - Opening_song_before_main_theme once, then Main_theme_extended on loop.
//   - An NPC's fight song while it's angry at you, and during its death cutscene.
//   - lake_sound fades in as you get close to the lake.
//   - Death_by_npc when an NPC kills you (game over).
//   - Footsteps loop while you walk; SFX through PlaySFX (pickup, knife, rock, punch).
// The endings (Killed_at_lake_ending, suicide_ending, drowning_for_the_ending) are not wired yet.
public class AudioManager : MonoBehaviour
{
    [Header("---------- Audio Source ----------")]
    [SerializeField] AudioSource musicSource;
    [SerializeField] AudioSource SFXSource;

    [Header("---------- Audio Clip ----------")]
    public AudioClip Death_by_npc;
    public AudioClip drowning_for_the_ending;
    public AudioClip Footsteps;
    public AudioClip Grandma_fight_song;
    public AudioClip Killed_at_lake_ending;
    public AudioClip knife_slashing_sound;
    public AudioClip Knight_fight_song;
    public AudioClip lake_sound;
    public AudioClip Main_theme_extended;
    public AudioClip Merchant_fight_song;
    public AudioClip Opening_song_before_main_theme;
    public AudioClip pickup_sound;
    public AudioClip punch_sound;
    public AudioClip rock_hit_sound;
    public AudioClip suicide_ending;

    [Header("---------- Mix ----------")]
    [Range(0f, 1f)] public float musicVolume = 0.6f;
    [Range(0f, 1f)] public float fightVolume = 0.7f;
    [Range(0f, 1f)] public float lakeVolume = 0.8f;
    [Range(0f, 1f)] public float footstepVolume = 0.5f;
    [Tooltip("Seconds to fade between the theme and a fight song.")]
    public float crossfadeTime = 1.2f;
    [Tooltip("Lake sound is full at this distance from the lake...")]
    public float lakeNear = 3f;
    [Tooltip("...and silent beyond this one.")]
    public float lakeFar = 14f;

    public static AudioManager Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { Instance = null; }

    AudioSource fightSource, lakeSource, stepSource;
    bool openingDone;
    bool gameOver;
    AudioClip deathSong;          // the fight song held through a death cutscene
    NPCGuard[] guards;
    Transform lake;
    Transform player;
    Rigidbody2D playerBody;
    PlayerHealth playerHealth;
    Vector2 lastPlayerPos;

    void Awake()
    {
        Instance = this;
        if (musicSource == null) musicSource = NewSource("Music", false);
        if (SFXSource == null) SFXSource = NewSource("SFX", false);
        musicSource.playOnAwake = false;
        SFXSource.playOnAwake = false;
        fightSource = NewSource("Fight Music", true);
        lakeSource = NewSource("Lake Ambience", true);
        stepSource = NewSource("Footsteps", true);
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    AudioSource NewSource(string n, bool loop)
    {
        var go = new GameObject(n);
        go.transform.SetParent(transform, false);
        var s = go.AddComponent<AudioSource>();
        s.playOnAwake = false;
        s.loop = loop;
        s.spatialBlend = 0f;
        s.volume = 0f;
        return s;
    }

    private void Start()
    {
        guards = FindObjectsByType<NPCGuard>(FindObjectsInactive.Include);
        playerHealth = FindAnyObjectByType<PlayerHealth>();
        if (playerHealth != null)
        {
            player = playerHealth.transform;
            playerBody = playerHealth.GetComponent<Rigidbody2D>();
            lastPlayerPos = player.position;
        }
        var ending = FindAnyObjectByType<InteractableObject>();
        if (ending != null) lake = ending.transform;
        else { var l = GameObject.Find("Lake Ending"); if (l != null) lake = l.transform; }

        // Opening song first (once), then the main theme loops.
        musicSource.volume = musicVolume;
        if (Opening_song_before_main_theme != null)
        {
            musicSource.clip = Opening_song_before_main_theme;
            musicSource.loop = false;
        }
        else StartMainTheme();
        musicSource.Play();

        if (lake_sound != null) { lakeSource.clip = lake_sound; lakeSource.Play(); }
        if (Footsteps != null) { stepSource.clip = Footsteps; }
    }

    void StartMainTheme()
    {
        openingDone = true;
        musicSource.clip = Main_theme_extended;
        musicSource.loop = true;
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;

        // Opening -> main theme.
        if (!openingDone && !musicSource.isPlaying && !gameOver && Main_theme_extended != null)
        {
            StartMainTheme();
            musicSource.Play();
        }

        if (playerHealth != null && playerHealth.IsDead && !gameOver)
            OnGameOver();

        // Which fight song (if any)?
        AudioClip fight = null;
        if (!gameOver)
        {
            if (deathSong != null)
            {
                if (DeathCutscene.Playing) fight = deathSong;
                else deathSong = null;
            }
            if (fight == null) fight = ClosestAngryNpcSong();
        }

        if (fight != null && fightSource.clip != fight)
        {
            // Switch songs: fade the old one out first, then start the new one.
            fightSource.volume = Mathf.MoveTowards(fightSource.volume, 0f, dt * fightVolume / (crossfadeTime * 0.5f));
            if (fightSource.volume <= 0.001f || !fightSource.isPlaying)
            {
                fightSource.clip = fight;
                fightSource.Play();
            }
        }
        float fightTarget = fight != null && fightSource.clip == fight ? fightVolume : 0f;
        if (fight == null || fightSource.clip == fight)
            fightSource.volume = Mathf.MoveTowards(fightSource.volume, fightTarget, dt * fightVolume / crossfadeTime);
        if (fightSource.isPlaying && fight == null && fightSource.volume <= 0.001f) { fightSource.Stop(); fightSource.clip = null; }

        // The theme ducks under a fight song (keeps playing underneath).
        float musicTarget = gameOver ? musicVolume : (fight != null ? 0f : musicVolume);
        musicSource.volume = Mathf.MoveTowards(musicSource.volume, musicTarget, dt * musicVolume / crossfadeTime);

        // Lake ambience by distance.
        if (lake != null && player != null && lakeSource.clip != null)
        {
            float d = Vector2.Distance(player.position, lake.position);
            float t = 1f - Mathf.InverseLerp(lakeNear, lakeFar, d);
            lakeSource.volume = gameOver ? 0f : lakeVolume * t * t;
        }

        UpdateFootsteps(dt);
    }

    AudioClip ClosestAngryNpcSong()
    {
        if (guards == null || player == null) return null;
        AudioClip best = null;
        float bestD = float.MaxValue;
        foreach (NPCGuard g in guards)
        {
            if (g == null || !g.isActiveAndEnabled || !g.IsHostile) continue;
            AudioClip c = SongFor(g.name);
            if (c == null) continue;
            float d = ((Vector2)g.transform.position - (Vector2)player.position).sqrMagnitude;
            if (d < bestD) { bestD = d; best = c; }
        }
        return best;
    }

    public AudioClip SongFor(string npcName)
    {
        string n = npcName.ToLowerInvariant();
        if (n.Contains("knight")) return Knight_fight_song;
        if (n.Contains("merchant") || n.Contains("vendor")) return Merchant_fight_song;
        if (n.Contains("grandma") || n.Contains("baba")) return Grandma_fight_song;
        return null;
    }

    // Called when an NPC's death cutscene starts: its fight song plays over it.
    public static void PlayDeathSong(string npcName)
    {
        if (Instance == null) return;
        Instance.deathSong = Instance.SongFor(npcName);
    }

    void OnGameOver()
    {
        gameOver = true;
        fightSource.Stop();
        stepSource.Stop();
        if (Death_by_npc != null)
        {
            musicSource.clip = Death_by_npc;
            musicSource.loop = false;
            musicSource.volume = musicVolume;
            musicSource.Play();
            openingDone = true;
        }
    }

    void UpdateFootsteps(float dt)
    {
        if (player == null || stepSource.clip == null) return;
        Vector2 pos = player.position;
        float speed = playerBody != null ? playerBody.linearVelocity.magnitude
                                         : (pos - lastPlayerPos).magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
        lastPlayerPos = pos;

        bool walking = speed > 0.3f && !gameOver && Time.timeScale > 0f &&
                       !DialogueManager.AnyDialogueActive && !DeathCutscene.Playing;
        if (walking && !stepSource.isPlaying) stepSource.Play();
        stepSource.volume = Mathf.MoveTowards(stepSource.volume, walking ? footstepVolume : 0f, dt * 6f);
        if (!walking && stepSource.isPlaying && stepSource.volume <= 0.001f) stepSource.Pause();
    }

    public static void PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (Instance == null || clip == null || Instance.SFXSource == null) return;
        Instance.SFXSource.PlayOneShot(clip, volume);
    }

    public static void PlayPickup() { if (Instance != null) PlaySFX(Instance.pickup_sound); }
    public static void PlayKnife()  { if (Instance != null) PlaySFX(Instance.knife_slashing_sound); }
    public static void PlayRock()   { if (Instance != null) PlaySFX(Instance.rock_hit_sound); }
    public static void PlayPunch()  { if (Instance != null) PlaySFX(Instance.punch_sound); }
}
