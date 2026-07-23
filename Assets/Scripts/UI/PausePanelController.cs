using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;

public class PausePanelController : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel;
    [SerializeField, Min(0f)] private float fadeDuration = 0.35f;

    private CanvasGroup canvasGroup;
    private bool isPaused;
    private bool wasInputBlockedBeforePause;
    private float timeScaleBeforePause = 1f;
    private Tween fadeTween;

    private void Awake()
    {
        if (pausePanel == null)
        {
            pausePanel = gameObject;
        }

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

        if (!isPaused && GameInputBlocker.IsInputBlocked)
        {
            return;
        }

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

        EnsureCanvasGroup();
        isPaused = true;
        wasInputBlockedBeforePause = GameInputBlocker.IsInputBlocked;
        timeScaleBeforePause = Time.timeScale;
        Time.timeScale = 0f;
        GameInputBlocker.SetBlocked(true);

        pausePanel.SetActive(true);
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;
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

        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
        FadeTo(0f, () => pausePanel.SetActive(false));
    }

    private void HideImmediately()
    {
        EnsureCanvasGroup();
        isPaused = false;
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
        pausePanel.SetActive(false);
    }

    private void FadeTo(float alpha, TweenCallback onComplete = null)
    {
        fadeTween?.Kill();
        fadeTween = canvasGroup
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

        if (canvasGroup == null)
        {
            canvasGroup = pausePanel.GetComponent<CanvasGroup>();
        }

        if (canvasGroup == null)
        {
            canvasGroup = pausePanel.AddComponent<CanvasGroup>();
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
