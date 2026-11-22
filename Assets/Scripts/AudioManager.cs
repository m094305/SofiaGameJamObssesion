using UnityEngine;

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

    private void Start()
    {
        musicSource.clip = Main_theme_extended;
        musicSource.Play();
    }
}
