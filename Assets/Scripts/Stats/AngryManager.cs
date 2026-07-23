using System;
using System.Collections;
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

    [Header("Angry References")]
    [SerializeField] private RectTransform angryObject;
    [SerializeField] private RectTransform angryPos;
    [SerializeField] private RectTransform counterPanel;
    [SerializeField] private RectTransform waypointCounter;
    [SerializeField] private RectTransform vignetteOverlay;
    [SerializeField] private Image vignetteOverlayImage;
    [SerializeField, Range(0f, 1f)] private float vignetteAlpha = 1f;

    [Header("Screen Glitch")]
    [SerializeField] private RawImage invertOverlay;
    [SerializeField] private Material invertOverlayMaterial;
    [SerializeField, Min(0f)] private float maxGlitchInvertedDuration = 0.06f;
    [SerializeField, Min(0f)] private float maxGlitchNormalDuration = 0.12f;

    [Header("Glitch Sound")]
    [SerializeField] private AudioClip glitchSfx;
    [SerializeField] private AudioSource glitchAudioSource;

    [Header("Camera Approach")]
    [SerializeField, InspectorName("다가가는 시간"), Min(0.01f)] private float approachDuration = 1.2f;
    [SerializeField, InspectorName("돌아오는 시간"), Min(0.01f)] private float returnDuration = 0.35f;
    [SerializeField, InspectorName("다가가는 배율"), Min(1f)] private float approachScale = 1.25f;

    [Header("ANGRY Motion")]
    [SerializeField, InspectorName("ANGRY가 올라오는 시간"), Min(0.01f)] private float angryRiseDuration = 0.35f;
    [SerializeField, InspectorName("ANGRY가 올라오는 강도"), Min(0f)] private float angryRiseStrength = 1f;
    [SerializeField, InspectorName("AngryPos 대기 시간"), Min(0f)] private float angryHoldDuration = 1f;
    [SerializeField, InspectorName("사라지는 시간"), Min(0.01f)] private float angryHideDuration = 0.25f;

    private Vector2 angryHiddenPosition;
    private Vector2 originalCounterPanelPosition;
    private Vector3 originalCounterPanelScale;
    private Vector3 originalVignetteScale;
    private Sequence angrySequence;
    private Sequence approachSequence;
    private Coroutine glitchCoroutine;
    private Tween unlockFailsafe;
    private bool hasCapturedAngryHiddenPosition;
    private bool hasCapturedCounterPanelTransform;
    private bool hasCapturedVignetteScale;
    private bool pendingAngryEvent;
    private bool isAngryEventPlaying;
    private bool shouldResumeBgmAfterAngryEvent;
    private Action pendingAngryEventCompleted;
    private Action activeAngryEventCompleted;

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
        AutoBindReferences();
        CaptureAngryHiddenPosition();
        CaptureCounterPanelTransform();
        CaptureVignetteScale();
        SetAngryVisible(false);
        SetVignetteVisible(false);
        EnsureInvertOverlay();
        EnsureGlitchAudioSource();
        SetInvertVisible(false);
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
        approachSequence?.Kill();
        StopGlitchSequence();
        unlockFailsafe?.Kill();
        if (isAngryEventPlaying)
        {
            EndAngryEventLock();
        }
        else
        {
            GameInputBlocker.SetBlocked(false);
            SetVignetteVisible(false);
            StopGlitchSequence();
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void OnValidate()
    {
        AutoBindReferences();
        CaptureAngryHiddenPosition();
        CaptureCounterPanelTransform();
        CaptureVignetteScale();
    }

    public bool TryTriggerAngryEvent(Action onComplete = null)
    {
        if (UnityEngine.Random.value > GetMonsterChanceForCurrentRating())
        {
            return false;
        }

        pendingAngryEventCompleted += onComplete;
        pendingAngryEvent = true;
        TryPlayPendingAngryEvent();
        return true;
    }

    private float GetMonsterChanceForCurrentRating()
    {
        if (ReputationRatingManager.Instance != null && !ReputationRatingManager.Instance.IsRatingUnlocked)
        {
            return 0f;
        }

        int ratingBand = ReputationRatingManager.Instance != null
            ? ReputationRatingManager.Instance.CurrentAngryEventBand
            : 0;
        ratingBand = Mathf.Clamp(ratingBand, 0, 5);

        switch (ratingBand)
        {
            case 0: return monsterChanceRating0;
            case 1: return monsterChanceRating1;
            case 2: return monsterChanceRating2;
            case 3: return monsterChanceRating3;
            case 4: return monsterChanceRating4;
            case 5: return monsterChanceRating5;
            default: return 0f;
        }
    }

    private void TryPlayPendingAngryEvent()
    {
        if (!pendingAngryEvent || isAngryEventPlaying)
        {
            return;
        }

        UIStationScoller stationScroller = UIStationScoller.Instance;
        if (stationScroller != null && !stationScroller.IsCounterSettled)
        {
            return;
        }

        activeAngryEventCompleted = pendingAngryEventCompleted;
        pendingAngryEventCompleted = null;
        pendingAngryEvent = false;
        PlayAngryEvent();
    }

    private void PlayAngryEvent()
    {
        BeginAngryEventLock();
        ScheduleUnlockFailsafe();
        CaptureVignetteScale();
        SetVignetteVisible(true);
        PlayGlitchSequence();

        if (counterPanel == null || waypointCounter == null)
        {
            PlayAngryMotion(EndAngryEventLock);
            return;
        }

        CaptureCounterPanelTransform();
        CaptureVignetteScale();
        Vector2 approachedPosition = GetCounterPanelApproachPosition();
        Vector3 approachedScale = originalCounterPanelScale * approachScale;
        Vector3 approachedVignetteScale = originalVignetteScale * approachScale;

        approachSequence?.Kill();
        counterPanel.DOKill();
        counterPanel.anchoredPosition = originalCounterPanelPosition;
        counterPanel.localScale = originalCounterPanelScale;
        if (vignetteOverlay != null)
        {
            vignetteOverlay.DOKill();
            vignetteOverlay.localScale = originalVignetteScale;
        }
        approachSequence = DOTween.Sequence().SetTarget(counterPanel);
        approachSequence.Join(counterPanel.DOAnchorPos(approachedPosition, approachDuration).SetEase(Ease.InOutSine));
        approachSequence.Join(counterPanel.DOScale(approachedScale, approachDuration).SetEase(Ease.InOutSine));
        if (vignetteOverlay != null)
        {
            approachSequence.Join(vignetteOverlay.DOScale(approachedVignetteScale, approachDuration).SetEase(Ease.InOutSine));
        }
        approachSequence.Append(counterPanel.DOAnchorPos(originalCounterPanelPosition, returnDuration).SetEase(Ease.OutQuad));
        approachSequence.Join(counterPanel.DOScale(originalCounterPanelScale, returnDuration).SetEase(Ease.OutQuad));
        if (vignetteOverlay != null)
        {
            approachSequence.Join(vignetteOverlay.DOScale(originalVignetteScale, returnDuration).SetEase(Ease.OutQuad));
        }
        approachSequence.OnComplete(() => PlayAngryMotion(EndAngryEventLock));
    }

    private void BeginAngryEventLock()
    {
        isAngryEventPlaying = true;
        GameInputBlocker.SetBlocked(true);

        BgmManager bgmManager = BgmManager.Instance;
        shouldResumeBgmAfterAngryEvent = bgmManager != null && bgmManager.IsAnyTrackPlaying;
        bgmManager?.StopAllTracks();
    }

    private void EndAngryEventLock()
    {
        if (!isAngryEventPlaying && activeAngryEventCompleted == null)
        {
            return;
        }

        unlockFailsafe?.Kill();
        unlockFailsafe = null;

        GameInputBlocker.SetBlocked(false);
        RestoreEventSystem();
        ResumeBgmAfterAngryEvent();
        SetVignetteVisible(false);
        StopGlitchSequence();
        isAngryEventPlaying = false;

        Action completed = activeAngryEventCompleted;
        activeAngryEventCompleted = null;
        completed?.Invoke();

        if (pendingAngryEvent)
        {
            TryPlayPendingAngryEvent();
        }
    }

    private void PlayAngryMotion(Action onComplete)
    {
        if (angryObject == null || angryPos == null)
        {
            onComplete?.Invoke();
            return;
        }

        CaptureAngryHiddenPosition();

        angrySequence?.Kill();
        angryObject.DOKill();
        SetAngryVisible(true);
        angryObject.anchoredPosition = angryHiddenPosition;

        angrySequence = DOTween.Sequence().SetTarget(angryObject);
        angrySequence.Append(
            angryObject.DOAnchorPos(angryPos.anchoredPosition, angryRiseDuration)
                .SetEase(Ease.OutElastic, angryRiseStrength, 0.3f));
        angrySequence.AppendInterval(angryHoldDuration);
        angrySequence.Append(angryObject.DOAnchorPos(angryHiddenPosition, angryHideDuration).SetEase(Ease.InQuad));
        angrySequence.OnComplete(() =>
        {
            SetAngryVisible(false);
            onComplete?.Invoke();
        });
    }

    private void ScheduleUnlockFailsafe()
    {
        unlockFailsafe?.Kill();

        float counterDuration = counterPanel != null && waypointCounter != null
            ? approachDuration + returnDuration
            : 0f;
        float angryDuration = angryRiseDuration + angryHoldDuration + angryHideDuration;
        float failsafeDelay = counterDuration + angryDuration + 0.5f;

        unlockFailsafe = DOVirtual.DelayedCall(failsafeDelay, EndAngryEventLock)
            .SetUpdate(true);
    }

    private void RestoreEventSystem()
    {
        EventSystem eventSystem = EventSystem.current != null
            ? EventSystem.current
            : FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include);

        if (eventSystem != null)
        {
            eventSystem.enabled = true;
        }
    }

    private void ResumeBgmAfterAngryEvent()
    {
        if (!shouldResumeBgmAfterAngryEvent)
        {
            return;
        }

        shouldResumeBgmAfterAngryEvent = false;
        BgmManager.Instance?.ResumePlaylist();
    }

    private void CaptureAngryHiddenPosition()
    {
        if (hasCapturedAngryHiddenPosition || angryObject == null)
        {
            return;
        }

        angryHiddenPosition = angryObject.anchoredPosition;
        hasCapturedAngryHiddenPosition = true;
    }

    private void CaptureCounterPanelTransform()
    {
        if (hasCapturedCounterPanelTransform || counterPanel == null)
        {
            return;
        }

        originalCounterPanelPosition = counterPanel.anchoredPosition;
        originalCounterPanelScale = counterPanel.localScale;
        hasCapturedCounterPanelTransform = true;
    }

    private void CaptureVignetteScale()
    {
        if (hasCapturedVignetteScale || vignetteOverlay == null)
        {
            return;
        }

        originalVignetteScale = vignetteOverlay.localScale;
        hasCapturedVignetteScale = true;
    }

    private Vector2 GetCounterPanelApproachPosition()
    {
        RectTransform parentRect = counterPanel.parent as RectTransform;
        if (parentRect == null)
        {
            return originalCounterPanelPosition;
        }

        Vector2 waypointParentPosition = parentRect.InverseTransformPoint(waypointCounter.position);
        Vector2 panelParentPosition = parentRect.InverseTransformPoint(counterPanel.position);
        Vector2 waypointOffsetFromPanel = waypointParentPosition - panelParentPosition;
        return originalCounterPanelPosition - waypointOffsetFromPanel * approachScale;
    }

    private void SetAngryVisible(bool isVisible)
    {
        if (angryObject != null)
        {
            angryObject.gameObject.SetActive(isVisible);
        }
    }

    private void SetVignetteVisible(bool isVisible)
    {
        if (vignetteOverlay == null)
        {
            return;
        }

        CaptureVignetteScale();
        vignetteOverlay.gameObject.SetActive(isVisible);
        vignetteOverlay.localScale = originalVignetteScale;

        if (vignetteOverlayImage != null)
        {
            Color color = vignetteOverlayImage.color;
            color.a = isVisible ? vignetteAlpha : 0f;
            vignetteOverlayImage.color = color;
        }
    }

    private void PlayGlitchSequence()
    {
        EnsureInvertOverlay();
        EnsureGlitchAudioSource();
        if (invertOverlay == null)
        {
            return;
        }

        StopGlitchSequence();
        glitchCoroutine = StartCoroutine(GlitchRoutine());
    }

    private IEnumerator GlitchRoutine()
    {
        float totalWindow = Mathf.Max(0f, approachDuration);
        float[] flashTimes = new float[3];
        for (int i = 0; i < flashTimes.Length; i++)
        {
            flashTimes[i] = UnityEngine.Random.Range(0f, totalWindow);
        }

        Array.Sort(flashTimes);
        float elapsed = 0f;

        for (int i = 0; i < flashTimes.Length; i++)
        {
            float waitBeforeFlash = Mathf.Max(0f, flashTimes[i] - elapsed);
            if (waitBeforeFlash > 0f)
            {
                yield return new WaitForSecondsRealtime(waitBeforeFlash);
                elapsed += waitBeforeFlash;
            }

            float nextBoundary = i < flashTimes.Length - 1 ? flashTimes[i + 1] : totalWindow;
            float onDuration = Mathf.Min(GetRandomGlitchDuration(maxGlitchInvertedDuration), Mathf.Max(0f, nextBoundary - elapsed));
            if (onDuration <= 0f)
            {
                continue;
            }

            SetInvertVisible(true);
            PlayGlitchSfx();
            yield return new WaitForSecondsRealtime(onDuration);
            elapsed += onDuration;

            SetInvertVisible(false);
            StopGlitchSfx();

            if (i < flashTimes.Length - 1)
            {
                float offDuration = Mathf.Min(GetRandomGlitchDuration(maxGlitchNormalDuration), Mathf.Max(0f, flashTimes[i + 1] - elapsed));
                if (offDuration > 0f)
                {
                    yield return new WaitForSecondsRealtime(offDuration);
                    elapsed += offDuration;
                }
            }
        }
    }

    private float GetRandomGlitchDuration(float maxDuration)
    {
        return Mathf.Max(0.01f, UnityEngine.Random.Range(0f, Mathf.Max(0f, maxDuration)));
    }

    private void StopGlitchSequence()
    {
        if (glitchCoroutine != null)
        {
            StopCoroutine(glitchCoroutine);
            glitchCoroutine = null;
        }

        StopGlitchSfx();
        SetInvertVisible(false);
    }

    private void PlayGlitchSfx()
    {
        if (glitchAudioSource == null || glitchSfx == null)
        {
            return;
        }

        glitchAudioSource.clip = glitchSfx;
        glitchAudioSource.loop = true;
        glitchAudioSource.Play();
    }

    private void StopGlitchSfx()
    {
        if (glitchAudioSource != null)
        {
            glitchAudioSource.Stop();
        }
    }

    private void SetInvertVisible(bool isVisible)
    {
        if (invertOverlay != null)
        {
            invertOverlay.gameObject.SetActive(isVisible);
        }
    }

    private void EnsureInvertOverlay()
    {
        if (invertOverlay != null)
        {
            if (invertOverlayMaterial != null)
            {
                invertOverlay.material = invertOverlayMaterial;
            }
            return;
        }

        RectTransform parent = vignetteOverlay != null ? vignetteOverlay.parent as RectTransform : null;
        if (parent == null)
        {
            Canvas canvas = FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
            parent = canvas != null ? canvas.transform as RectTransform : null;
        }

        if (parent == null)
        {
            return;
        }

        GameObject overlayObject = new GameObject("InvertOverlay_for_ANGRY", typeof(RectTransform), typeof(RawImage));
        RectTransform overlayRect = overlayObject.transform as RectTransform;
        overlayRect.SetParent(parent, false);
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;
        overlayRect.localScale = Vector3.one;
        overlayRect.SetAsLastSibling();

        invertOverlay = overlayObject.GetComponent<RawImage>();
        invertOverlay.raycastTarget = false;
        invertOverlay.color = Color.white;

        if (invertOverlayMaterial == null)
        {
            Shader shader = Shader.Find("UI/ScreenInvertOverlay");
            if (shader != null)
            {
                invertOverlayMaterial = new Material(shader);
            }
        }

        if (invertOverlayMaterial != null)
        {
            invertOverlay.material = invertOverlayMaterial;
        }

        SetInvertVisible(false);
    }

    private void EnsureGlitchAudioSource()
    {
        if (glitchAudioSource == null)
        {
            glitchAudioSource = GetComponent<AudioSource>();
        }

        if (glitchAudioSource == null)
        {
            glitchAudioSource = gameObject.AddComponent<AudioSource>();
        }

        glitchAudioSource.playOnAwake = false;
        glitchAudioSource.loop = true;
    }

    private void AutoBindReferences()
    {
        if (angryObject == null)
        {
            angryObject = FindSceneRectTransform("ANGRY");
        }

        if (angryPos == null)
        {
            angryPos = FindSceneRectTransform("angryPos");
        }

        if (counterPanel == null)
        {
            counterPanel = FindSceneRectTransform("CounterPanel");
        }

        if (waypointCounter == null)
        {
            waypointCounter = FindSceneRectTransform("Waypoint_Counter");
        }

        if (vignetteOverlay == null)
        {
            vignetteOverlay = FindSceneRectTransform("VignetteOverlay_for_ANGRY");
        }

        if (vignetteOverlayImage == null && vignetteOverlay != null)
        {
            vignetteOverlayImage = vignetteOverlay.GetComponent<Image>();
        }
    }

    private static RectTransform FindSceneRectTransform(string targetName)
    {
        Transform target = FindSceneTransform(targetName);
        return target != null ? target as RectTransform : null;
    }

    private static Transform FindSceneTransform(string targetName)
    {
        Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform candidate = transforms[i];
            if (candidate == null
                || candidate.name != targetName
                || !candidate.gameObject.scene.IsValid())
            {
                continue;
            }

            return candidate;
        }

        return null;
    }
}
