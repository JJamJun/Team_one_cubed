using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class AngryManager : MonoBehaviour
{
    public static AngryManager Instance { get; private set; }

    [Header("몬스터화 기믹 확률")]
    [SerializeField, InspectorName("0점대"), Range(0f, 1f)] private float monsterChanceRating0 = 1f;
    [SerializeField, InspectorName("1점대"), Range(0f, 1f)] private float monsterChanceRating1 = 0.9f;
    [SerializeField, InspectorName("2점대"), Range(0f, 1f)] private float monsterChanceRating2 = 0.5f;
    [SerializeField, InspectorName("3점대"), Range(0f, 1f)] private float monsterChanceRating3 = 0.3f;
    [SerializeField, InspectorName("4점대"), Range(0f, 1f)] private float monsterChanceRating4 = 0.2f;
    [SerializeField, InspectorName("5점대"), Range(0f, 1f)] private float monsterChanceRating5 = 0f;

    [Header("New Angry Effect References")]
    [Tooltip("A full-screen black image with a CanvasGroup to handle the lights-out effect.")]
    [SerializeField] private CanvasGroup blackoutGroup;
    [Tooltip("The actual jumpscare image you want to show.")]
    [SerializeField] private RectTransform jumpscareImage;

    [Header("Effect Settings")]
    [SerializeField, Tooltip("How long the screen blinks before going completely black.")]
    private float blinkPhaseDuration = 1.5f;
    [SerializeField, Tooltip("How many times the lights flicker during the blink phase.")]
    private int blinkCount = 3;
    [SerializeField, Tooltip("How long the image vibrates on screen.")]
    private float vibrateDuration = 2.0f;
    [SerializeField, Tooltip("How violently the image shakes.")]
    private float vibrateStrength = 50f;
    [SerializeField] private int vibrateVibrato = 20;

    [Header("Audio")]
    [SerializeField] private AudioClip riserSfx;

    private Sequence angrySequence;
    private Tween unlockFailsafe;

    private bool pendingAngryEvent;
    private bool isAngryEventPlaying;

    // BGM Volume Tracking
    private AudioSource grabbedBgmSource;
    private float originalBgmVolume = 1f;

    private Action pendingAngryEventCompleted;
    private Action activeAngryEventCompleted;
    private Vector2 originalJumpscarePos;

    public bool HasPendingAngryEvent =>
        pendingAngryEvent
        || isAngryEventPlaying
        || pendingAngryEventCompleted != null
        || activeAngryEventCompleted != null;

    public bool IsAngryEventPlaying => isAngryEventPlaying;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Force this to 1.5s in case the Unity Inspector tries to load an old value
        blinkPhaseDuration = 1.5f;

        if (jumpscareImage != null)
        {
            originalJumpscarePos = jumpscareImage.anchoredPosition;
            jumpscareImage.gameObject.SetActive(false);
        }

        if (blackoutGroup != null)
        {
            blackoutGroup.alpha = 0f;
            blackoutGroup.gameObject.SetActive(false);
            blackoutGroup.blocksRaycasts = false;
        }
    }

    private void OnEnable()
    {
        GameInputBlocker.SetBlocked(false);
        RestoreEventSystem();
        UIStationScoller.CounterStationSettled += TryPlayPendingAngryEvent;
    }

    private void OnDisable()
    {
        UIStationScoller.CounterStationSettled -= TryPlayPendingAngryEvent;
        angrySequence?.Kill();
        unlockFailsafe?.Kill();

        if (isAngryEventPlaying) EndAngryEventLock();
        else GameInputBlocker.SetBlocked(false);

        if (Instance == this) Instance = null;
    }

    public bool TryTriggerAngryEvent(GhostType ghostType, Action onComplete = null)
    {
        Debug.Log($"[AngryManager] TryTriggerAngryEvent called. Customer Type: {ghostType}");

        if (ghostType != GhostType.None)
        {
            Debug.Log("[AngryManager] ABORTED: Customer is a ghost. Jumpscare skipped.");
            return false;
        }

        float chance = GetMonsterChanceForCurrentRating();
        float roll = UnityEngine.Random.value;
        Debug.Log($"[AngryManager] RNG Roll: {roll:F2}, Required Chance: <= {chance:F2}");

        if (roll < chance)
        {
            Debug.Log("[AngryManager] ABORTED: RNG failed. Jumpscare skipped.");
            return false;
        }

        Debug.Log("[AngryManager] SUCCESS: Jumpscare event queued.");
        pendingAngryEventCompleted += onComplete;
        pendingAngryEvent = true;
        TryPlayPendingAngryEvent();
        return true;
    }

    private float GetMonsterChanceForCurrentRating()
    {
        if (ReputationRatingManager.Instance != null && !ReputationRatingManager.Instance.IsRatingUnlocked)
            return 0f;

        int ratingBand = ReputationRatingManager.Instance != null
            ? ReputationRatingManager.Instance.CurrentAngryEventBand : 0;

        ratingBand = Mathf.Clamp(ratingBand, 0, 5);

        return ratingBand switch
        {
            0 => monsterChanceRating0,
            1 => monsterChanceRating1,
            2 => monsterChanceRating2,
            3 => monsterChanceRating3,
            4 => monsterChanceRating4,
            5 => monsterChanceRating5,
            _ => 0f,
        };
    }

    private void TryPlayPendingAngryEvent()
    {
        if (!pendingAngryEvent) return;

        if (isAngryEventPlaying)
        {
            Debug.LogWarning("[AngryManager] Blocked: An angry event is already playing.");
            return;
        }

        UIStationScoller stationScroller = UIStationScoller.Instance;
        if (stationScroller != null && !stationScroller.IsCounterSettled)
        {
            Debug.LogWarning("[AngryManager] Waiting: UI counter station is not settled yet. Will trigger when ready.");
            return;
        }

        Debug.Log("[AngryManager] Playing the Jumpscare Event now!");
        activeAngryEventCompleted = pendingAngryEventCompleted;
        pendingAngryEventCompleted = null;
        pendingAngryEvent = false;

        PlayAngryEvent();
    }

    private void PlayAngryEvent()
    {
        BeginAngryEventLock();

        float totalDuration = blinkPhaseDuration + vibrateDuration + 1f;
        ScheduleUnlockFailsafe(totalDuration);

        blackoutGroup.gameObject.SetActive(true);
        blackoutGroup.alpha = 0f;
        jumpscareImage.gameObject.SetActive(false);
        jumpscareImage.anchoredPosition = originalJumpscarePos;

        if (riserSfx != null && SoundManager.Instance != null && SoundManager.Instance.SFX != null)
        {
            SoundManager.Instance.SFX.PlaySfx(riserSfx);
        }

        angrySequence?.Kill();
        angrySequence = DOTween.Sequence();

        float singleFlickerTime = blinkPhaseDuration / (blinkCount * 2);
        for (int i = 0; i < blinkCount; i++)
        {
            angrySequence.Append(blackoutGroup.DOFade(1f, singleFlickerTime).SetEase(Ease.Flash));
            angrySequence.Append(blackoutGroup.DOFade(0f, singleFlickerTime).SetEase(Ease.Flash));
        }

        // 1. Final cut to black
        angrySequence.Append(blackoutGroup.DOFade(1f, 0.05f));

        // 2. Turn on the jumpscare image
        angrySequence.AppendCallback(() => jumpscareImage.gameObject.SetActive(true));

        // 3. Shake it for the full duration WHILE the background remains pitch black
        angrySequence.Append(jumpscareImage.DOShakeAnchorPos(vibrateDuration, vibrateStrength, vibrateVibrato));

        // 4. Fade the darkness away ONLY after the shaking is done
        angrySequence.Append(blackoutGroup.DOFade(0f, 0.1f));

        angrySequence.OnComplete(() =>
        {
            Debug.Log("[AngryManager] Jumpscare sequence finished.");
            jumpscareImage.gameObject.SetActive(false);
            blackoutGroup.gameObject.SetActive(false);
            EndAngryEventLock();
        });
    }

    private void BeginAngryEventLock()
    {
        isAngryEventPlaying = true;
        GameInputBlocker.SetBlocked(true);

        BgmManager bgmManager = BgmManager.Instance;
        if (bgmManager != null)
        {
            grabbedBgmSource = bgmManager.GetComponent<AudioSource>();

            if (grabbedBgmSource != null)
            {
                originalBgmVolume = grabbedBgmSource.volume;
                grabbedBgmSource.volume = 0f;
            }
            else
            {
                Debug.LogWarning("[AngryManager] Could not find an AudioSource on BgmManager to mute! If you use a custom volume method, add it here.");
                // e.g., bgmManager.SetVolume(0f);
            }
        }
    }

    private void EndAngryEventLock()
    {
        if (!isAngryEventPlaying && activeAngryEventCompleted == null) return;

        unlockFailsafe?.Kill();
        unlockFailsafe = null;

        GameInputBlocker.SetBlocked(false);
        RestoreEventSystem();

        // Restore BGM Volume
        if (grabbedBgmSource != null)
        {
            grabbedBgmSource.volume = originalBgmVolume;
            grabbedBgmSource = null;
        }

        if (blackoutGroup != null) blackoutGroup.gameObject.SetActive(false);
        if (jumpscareImage != null) jumpscareImage.gameObject.SetActive(false);

        isAngryEventPlaying = false;

        Action completed = activeAngryEventCompleted;
        activeAngryEventCompleted = null;
        completed?.Invoke();

        if (pendingAngryEvent) TryPlayPendingAngryEvent();
    }

    private void ScheduleUnlockFailsafe(float delay)
    {
        unlockFailsafe?.Kill();
        unlockFailsafe = DOVirtual.DelayedCall(delay, EndAngryEventLock).SetUpdate(true);
    }

    private void RestoreEventSystem()
    {
        EventSystem eventSystem = EventSystem.current != null
            ? EventSystem.current
            : FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include);

        if (eventSystem != null) eventSystem.enabled = true;
    }

    // ==========================================
    // DEBUG TOOLS
    // ==========================================

    [ContextMenu("DEBUG: Force Play Jumpscare")]
    public void DebugForcePlayJumpscare()
    {
        Debug.Log("[AngryManager] Force triggering jumpscare from Inspector!");

        if (isAngryEventPlaying)
        {
            Debug.LogWarning("[AngryManager] Cannot force play: Event is already running!");
            return;
        }

        if (blackoutGroup == null || jumpscareImage == null)
        {
            Debug.LogError("[AngryManager] Missing UI references! Assign Blackout Group and Jumpscare Image.");
            return;
        }

        pendingAngryEventCompleted = null;
        PlayAngryEvent();
    }
}