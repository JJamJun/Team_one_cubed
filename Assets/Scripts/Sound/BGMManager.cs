using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(AudioLowPassFilter))]
public class BgmManager : MonoBehaviour
{
    [Header("Playlist")]
    [SerializeField] private AudioClip[] bgmPlaylist;
    [SerializeField] private float crossfadeDuration = 3f;

    [Header("Muffle Settings")]
    [SerializeField] private float normalCutoff = 22000f; // oepn EQ
    [SerializeField] private float muffledCutoff = 800f;   // muffled EQ for ghost effect
    [SerializeField] private float muffleTransitionDuration = 1.5f;

    private AudioSource[] bgmSources;
    private int currentSourceIndex = 0;
    private AudioLowPassFilter lowPassFilter;
    private bool isCrossfading = false;

    private void Awake()
    {
        //two audio sources for crossfading
        bgmSources = new AudioSource[2];
        for (int i = 0; i < 2; i++)
        {
            bgmSources[i] = gameObject.AddComponent<AudioSource>();
            bgmSources[i].loop = false; 
            bgmSources[i].playOnAwake = false;
            bgmSources[i].volume = 0f;
        }

        lowPassFilter = GetComponent<AudioLowPassFilter>();
        lowPassFilter.cutoffFrequency = normalCutoff;
    }

    private void Start()
    {
        PlayNextTrack();
    }

    private void Update()
    {
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


    public void SetMuffled(bool isMuffled)
    {
        float targetCutoff = isMuffled ? muffledCutoff : normalCutoff;

        DOTween.To(() => lowPassFilter.cutoffFrequency,
                   x => lowPassFilter.cutoffFrequency = x,
                   targetCutoff,
                   muffleTransitionDuration).SetEase(Ease.InOutSine);
    }
}