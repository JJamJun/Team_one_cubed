using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(AudioLowPassFilter))]
public class BgmManager : MonoBehaviour
{
    public static BgmManager Instance { get; private set; }

    [Header("Playlist")]
    [SerializeField] private AudioClip[] bgmPlaylist;
    [SerializeField] private float crossfadeDuration = 3f;

    [Header("Muffle Settings")]
    [SerializeField] private float normalCutoff = 22000f; // oepn EQ
    [SerializeField] private float muffledCutoff = 800f;   // muffled EQ for ghost effect
    [SerializeField] private float muffleTransitionDuration = 1.5f;

    [Header("Ghost BGM")]
    [SerializeField] private AudioClip littleGhostBgm;
    [SerializeField] private AudioClip grimReaperBgm;
    [SerializeField] private AudioClip womanGhostBgm;
    [SerializeField] private AudioClip dokaebiBgm;
    [SerializeField] private AudioSource ghostBgmSource;

    private AudioSource[] bgmSources;
    private bool[] pausedPlaylistSources;
    private int currentSourceIndex = 0;
    private AudioLowPassFilter lowPassFilter;
    private bool isCrossfading = false;
    private bool isGhostBgmPlaying;
    private bool shouldRestoreGhostBgmAfterStopAll;
    private AudioClip stoppedGhostBgmClip;
    private float stoppedGhostBgmTime;

    public bool IsAnyTrackPlaying
    {
        get
        {
            if (bgmSources == null)
            {
                return false;
            }

            for (int i = 0; i < bgmSources.Length; i++)
            {
                if (bgmSources[i] != null && bgmSources[i].isPlaying)
                {
                    return true;
                }
            }

            if (ghostBgmSource != null && ghostBgmSource.isPlaying)
            {
                return true;
            }

            return false;
        }
    }

    private void Awake()
    {
        Instance = this;

        //two audio sources for crossfading
        bgmSources = new AudioSource[2];
        pausedPlaylistSources = new bool[2];
        for (int i = 0; i < 2; i++)
        {
            bgmSources[i] = gameObject.AddComponent<AudioSource>();
            bgmSources[i].loop = false; 
            bgmSources[i].playOnAwake = false;
            bgmSources[i].volume = 0f;
        }

        lowPassFilter = GetComponent<AudioLowPassFilter>();
        lowPassFilter.cutoffFrequency = normalCutoff;
        EnsureGhostBgmSource();
    }

    public void StopAllTracks()
    {
        DOTween.Kill(this);
        if (bgmSources == null)
        {
            return;
        }

        for (int i = 0; i < bgmSources.Length; i++)
        {
            AudioSource source = bgmSources[i];
            if (source == null)
            {
                continue;
            }

            source.DOKill();
            source.Stop();
            source.volume = 0f;
        }

        isCrossfading = false;

        shouldRestoreGhostBgmAfterStopAll = ghostBgmSource != null && ghostBgmSource.isPlaying;
        stoppedGhostBgmClip = shouldRestoreGhostBgmAfterStopAll ? ghostBgmSource.clip : null;
        stoppedGhostBgmTime = shouldRestoreGhostBgmAfterStopAll ? ghostBgmSource.time : 0f;
        StopGhostBgm(false);
    }

    private void Start()
    {
        PlayNextTrack();
    }

    private void Update()
    {
        if (isGhostBgmPlaying)
        {
            return;
        }

        AudioSource currentSource = bgmSources[currentSourceIndex];

        if (currentSource.isPlaying && currentSource.clip != null && !isCrossfading)
        {
            float timeRemaining = currentSource.clip.length - currentSource.time;
            if (timeRemaining <= crossfadeDuration)
            {
                PlayNextTrack();
            }
        }
    }

    public void PlayNextTrack()
    {
        if (bgmPlaylist == null || bgmPlaylist.Length == 0) return;

        isCrossfading = true;
        int nextSourceIndex = 1 - currentSourceIndex;
        AudioSource activeSource = bgmSources[currentSourceIndex];
        AudioSource nextSource = bgmSources[nextSourceIndex];

        //ramdom clip
        AudioClip nextClip = bgmPlaylist[Random.Range(0, bgmPlaylist.Length)];

        //prevent same clip replay
        if (bgmPlaylist.Length > 1)
        {
            while (nextClip == activeSource.clip)
            {
                nextClip = bgmPlaylist[Random.Range(0, bgmPlaylist.Length)];
            }
        }

        nextSource.clip = nextClip;
        nextSource.volume = 0f;

        float halfFade = crossfadeDuration * 0.5f;

        //fade out to 0
        activeSource.DOFade(0f, halfFade).OnComplete(() =>
        {
            activeSource.Stop();

            //fade in from 0
            nextSource.Play();
            nextSource.DOFade(1f, halfFade).OnComplete(() =>
            {
                isCrossfading = false;
            });
        });

        currentSourceIndex = nextSourceIndex;
    }

    public void ResumePlaylist()
    {
        if (shouldRestoreGhostBgmAfterStopAll)
        {
            RestoreGhostBgmAfterStopAll();
            return;
        }

        if (isGhostBgmPlaying || bgmPlaylist == null || bgmPlaylist.Length == 0 || bgmSources == null || IsAnyTrackPlaying)
        {
            return;
        }

        isCrossfading = true;
        AudioSource source = bgmSources[currentSourceIndex];
        if (source == null)
        {
            isCrossfading = false;
            return;
        }

        source.DOKill();
        source.clip = GetRandomClip(source.clip);
        source.volume = 0f;
        source.Play();
        source.DOFade(1f, Mathf.Max(0.01f, crossfadeDuration * 0.5f))
            .OnComplete(() => isCrossfading = false);
    }

    private AudioClip GetRandomClip(AudioClip previousClip)
    {
        AudioClip nextClip = bgmPlaylist[Random.Range(0, bgmPlaylist.Length)];
        if (bgmPlaylist.Length > 1)
        {
            while (nextClip == previousClip)
            {
                nextClip = bgmPlaylist[Random.Range(0, bgmPlaylist.Length)];
            }
        }

        return nextClip;
    }

    public void PlayGhostBgm(GhostType ghostType)
    {
        AudioClip ghostClip = GetGhostBgmClip(ghostType);
        if (ghostClip == null)
        {
            Debug.LogWarning($"{nameof(BgmManager)}: Ghost BGM for {ghostType} is not assigned.");
            return;
        }

        EnsureGhostBgmSource();
        if (ghostBgmSource == null)
        {
            return;
        }

        PausePlaylistForGhost();
        ghostBgmSource.clip = ghostClip;
        ghostBgmSource.loop = true;
        ghostBgmSource.volume = 1f;
        ghostBgmSource.Play();
        isGhostBgmPlaying = true;
    }

    public void StopGhostBgmAndResumePlaylist()
    {
        StopGhostBgm(true);
        ResumePausedPlaylistSources();

        if (!IsAnyTrackPlaying)
        {
            ResumePlaylist();
        }
    }

    private void PausePlaylistForGhost()
    {
        if (bgmSources == null)
        {
            return;
        }

        DOTween.Kill(this);
        for (int i = 0; i < bgmSources.Length; i++)
        {
            AudioSource source = bgmSources[i];
            pausedPlaylistSources[i] = source != null && source.isPlaying;
            if (pausedPlaylistSources[i])
            {
                source.Pause();
            }
        }

        isCrossfading = false;
    }

    private void ResumePausedPlaylistSources()
    {
        if (bgmSources == null || pausedPlaylistSources == null)
        {
            return;
        }

        for (int i = 0; i < bgmSources.Length; i++)
        {
            AudioSource source = bgmSources[i];
            if (source != null && pausedPlaylistSources[i])
            {
                source.UnPause();
            }

            pausedPlaylistSources[i] = false;
        }
    }

    private void StopGhostBgm(bool clearStoppedGhostRestore)
    {
        isGhostBgmPlaying = false;
        if (clearStoppedGhostRestore)
        {
            shouldRestoreGhostBgmAfterStopAll = false;
            stoppedGhostBgmClip = null;
            stoppedGhostBgmTime = 0f;
        }

        if (ghostBgmSource != null)
        {
            ghostBgmSource.Stop();
        }
    }

    private void RestoreGhostBgmAfterStopAll()
    {
        EnsureGhostBgmSource();
        if (ghostBgmSource == null || stoppedGhostBgmClip == null)
        {
            shouldRestoreGhostBgmAfterStopAll = false;
            ResumePlaylist();
            return;
        }

        ghostBgmSource.clip = stoppedGhostBgmClip;
        ghostBgmSource.loop = true;
        ghostBgmSource.volume = 1f;
        ghostBgmSource.Play();
        ghostBgmSource.time = Mathf.Clamp(stoppedGhostBgmTime, 0f, Mathf.Max(0f, stoppedGhostBgmClip.length - 0.01f));

        isGhostBgmPlaying = true;
        shouldRestoreGhostBgmAfterStopAll = false;
        stoppedGhostBgmClip = null;
        stoppedGhostBgmTime = 0f;
    }

    private AudioClip GetGhostBgmClip(GhostType ghostType)
    {
        switch (ghostType)
        {
            case GhostType.Little: return littleGhostBgm;
            case GhostType.DeadLion: return grimReaperBgm;
            case GhostType.Woman: return womanGhostBgm;
            case GhostType.Dokaebi: return dokaebiBgm;
            default: return null;
        }
    }

    private void EnsureGhostBgmSource()
    {
        if (ghostBgmSource != null)
        {
            return;
        }

        ghostBgmSource = gameObject.AddComponent<AudioSource>();
        ghostBgmSource.playOnAwake = false;
        ghostBgmSource.loop = true;
    }


    public void SetMuffled(bool isMuffled)
    {
        float targetCutoff = isMuffled ? muffledCutoff : normalCutoff;

        DOTween.To(() => lowPassFilter.cutoffFrequency,
                   x => lowPassFilter.cutoffFrequency = x,
                   targetCutoff,
                   muffleTransitionDuration).SetEase(Ease.InOutSine);
    }
}
