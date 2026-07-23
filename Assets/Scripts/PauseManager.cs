using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;

public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }

    [SerializeField] private GameObject pausePanel;
    [SerializeField, Min(0f)] private float fadeDuration = 0.35f;

    private CanvasGroup pauseCanvasGroup;
    private Tween fadeTween;
    private bool isPaused;
    private bool wasInputBlockedBeforePause;
    private float timeScaleBeforePause = 1f;

    public bool IsPaused => isPaused;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        AutoBindPausePanel();
        EnsureCanvasGroup();
        HideImmediately();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame)
        {
            return;
        }

        if (CookingMiniGameController.IsCookingMiniGameOpen
            || CookingMiniGameController.WasCookingMiniGameClosedThisFrame)
        {
            return;
        }

        TogglePause();
    }

    public void TogglePause()
    {
        if (isPaused)
        {
            Resume();
        }
        else
        {
            Pause();
        }
    }

    public void Pause()
    {
        if (isPaused)
        {
            return;
        }

        AutoBindPausePanel();
        EnsureCanvasGroup();
        if (pausePanel == null || pauseCanvasGroup == null)
        {
            Debug.LogWarning($"{nameof(PauseManager)}: PausePanel is not assigned.");
            return;
        }

        isPaused = true;
        wasInputBlockedBeforePause = GameInputBlocker.IsInputBlocked;
        timeScaleBeforePause = Time.timeScale;
        Time.timeScale = 0f;
        GameInputBlocker.SetBlocked(true);

        pausePanel.SetActive(true);
        pauseCanvasGroup.blocksRaycasts = true;
        pauseCanvasGroup.interactable = true;
        FadeTo(1f);
    }

    public void Resume()
    {
        if (!isPaused)
        {
            return;
        }

        EnsureCanvasGroup();
        isPaused = false;
        Time.timeScale = timeScaleBeforePause;
        GameInputBlocker.SetBlocked(wasInputBlockedBeforePause);

        if (pauseCanvasGroup == null)
        {
            if (pausePanel != null)
            {
                pausePanel.SetActive(false);
            }

            return;
        }

        pauseCanvasGroup.blocksRaycasts = false;
        pauseCanvasGroup.interactable = false;
        FadeTo(0f, () =>
        {
            if (pausePanel != null)
            {
                pausePanel.SetActive(false);
            }
        });
    }

    private void HideImmediately()
    {
        if (pausePanel == null || pauseCanvasGroup == null)
        {
            return;
        }

        isPaused = false;
        pauseCanvasGroup.alpha = 0f;
        pauseCanvasGroup.blocksRaycasts = false;
        pauseCanvasGroup.interactable = false;
        pausePanel.SetActive(false);
    }

    private void FadeTo(float alpha, TweenCallback onComplete = null)
    {
        fadeTween?.Kill();
        pauseCanvasGroup.alpha = alpha > 0f ? 0f : pauseCanvasGroup.alpha;
        fadeTween = pauseCanvasGroup
            .DOFade(alpha, fadeDuration)
            .SetUpdate(true)
            .OnComplete(onComplete);
    }

    private void EnsureCanvasGroup()
    {
        if (pausePanel == null)
        {
            return;
        }

        if (pauseCanvasGroup == null)
        {
            pauseCanvasGroup = pausePanel.GetComponent<CanvasGroup>();
        }

        if (pauseCanvasGroup == null)
        {
            pauseCanvasGroup = pausePanel.AddComponent<CanvasGroup>();
        }
    }

    private void AutoBindPausePanel()
    {
        if (pausePanel != null)
        {
            return;
        }

        Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform candidate = transforms[i];
            if (candidate != null
                && candidate.name == "PausePanel"
                && candidate.gameObject.scene.IsValid())
            {
                pausePanel = candidate.gameObject;
                return;
            }
        }
    }

    private void OnDisable()
    {
        fadeTween?.Kill();
        if (isPaused)
        {
            Time.timeScale = timeScaleBeforePause;
            GameInputBlocker.SetBlocked(wasInputBlockedBeforePause);
        }
    }
}
