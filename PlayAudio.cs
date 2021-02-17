using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayAudio : MonoBehaviour
{
    private AudioSource Aud;
    public AudioClip[] Clips;

    public float PitchMax = 1.25f;
    public float PitchMin = 0.75f;
    public float VolumeMin = 0.1f;

    /*
    public void SetAudio(AudioClip[] FedClip)
    {
        Clips = FedClip;
        PlaySelf();
    }
    */

    private void Start()
    {
        PlaySelf();
    }

    void PlaySelf()
    {
        Aud = GetComponent<AudioSource>();

        Aud.clip = Clips[Random.Range(0, Clips.Length)];

        Aud.pitch = Random.Range(PitchMin, PitchMax);

        float maxVol = Aud.volume;
        Aud.volume = Random.Range(VolumeMin, maxVol);

        Aud.Play();
    }
}
