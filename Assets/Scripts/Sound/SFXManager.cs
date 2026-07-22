using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(AudioSource))]
public class SfxManager : MonoBehaviour
{
    [Header("General SFX")]
    [SerializeField] private AudioClip bellDingSfx;
    [SerializeField] private AudioClip clickSfx;

    //TODO: add more sfx as needed like for minigame and stuff 

    [Header("Cooking SFX")]
    [SerializeField] private AudioClip iceCubeSfx;
    [SerializeField] private AudioClip pouringWaterSfx;
    [SerializeField] private AudioClip machineSfx;

    [Header("Receipt SFX")]
    [SerializeField] private AudioClip moneySfx;
    [SerializeField] private AudioClip failSfx;

    [Header("Footstep SFX")]
    [SerializeField] private AudioClip normalFootstep;
    [SerializeField] private AudioClip deadLionFootstep;
    [SerializeField] private AudioClip littleFootstep;
    [SerializeField] private AudioClip womanFootstep;
    [SerializeField] private AudioClip dokaebiFootstep;

    private AudioSource sfxSource;

    private void Awake()
    {
        sfxSource = GetComponent<AudioSource>();
    }

    public void PlayBell()
    {
        if (bellDingSfx != null)
        {
            sfxSource.PlayOneShot(bellDingSfx);
        }
    }

    public void PlayClick()
    {
        PlaySfx(clickSfx);
    }

    public void PlayIceCube()
    {
        PlaySfx(iceCubeSfx);
    }

    public void PlayPouringWater()
    {
        PlaySfx(pouringWaterSfx);
    }

    public void PlayMachine()
    {
        PlaySfx(machineSfx);
    }

    public void PlayMoney()
    {
        PlaySfx(moneySfx);
    }

    public void PlayFail()
    {
        PlaySfx(failSfx);
    }

    public void PlayFootstep(GhostType ghostType, float volume)
    {
        AudioClip clipToPlay = GetFootstepClip(ghostType);

        if (clipToPlay != null && volume > 0.01f)
        {
            sfxSource.PlayOneShot(clipToPlay, volume);
        }
    }

    private AudioClip GetFootstepClip(GhostType ghostType)
    {
        AudioClip selectedClip = null;

        switch (ghostType)
        {
            case GhostType.DeadLion: selectedClip = deadLionFootstep; break;
            case GhostType.Little: selectedClip = littleFootstep; break;
            case GhostType.Woman: selectedClip = womanFootstep; break;
            case GhostType.Dokaebi: selectedClip = dokaebiFootstep; break;
            case GhostType.None:
            default:
                selectedClip = normalFootstep;
                break;
        }

        if (selectedClip == null && ghostType != GhostType.None)
        {
            Debug.Log($"SfxManager: Footstep audio for {ghostType} is not assigned. Falling back to normal footstep.");
            selectedClip = normalFootstep;
        }

        if (selectedClip == null)
        {
            Debug.LogWarning("SfxManager: Normal footstep SFX is missing! Please assign it in the Inspector.");
        }

        return selectedClip;
    }

    public void PlaySfx(AudioClip clip, float volume = 1f)
    {
        if (clip != null) sfxSource.PlayOneShot(clip, volume);
    }
}
