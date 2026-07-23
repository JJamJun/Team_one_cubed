using UnityEngine;
using DG.Tweening;
using UnityEngine.InputSystem;
using System;

public class UIStationScoller : MonoBehaviour
{
    public static UIStationScoller Instance { get; private set; }
    public static event Action CounterStationSettled;

    [SerializeField] private RectTransform mainContentPanel;
    [SerializeField] private float transitionDuration = 0.5f;
    [SerializeField] private Ease transitionEase = Ease.InOutQuad;

    private float slideDistance = 1920f;
    private int currentStationIndex;
    private bool isTransitioning;

    public bool IsCounterSettled => currentStationIndex == 0 && !isTransitioning;

    private void Awake()
    {
        Instance = this;

        if (mainContentPanel == null)
        {
            mainContentPanel = GetComponent<RectTransform>();
        }

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            slideDistance = canvas.GetComponent<RectTransform>().rect.width;
        }
    }

    private void LateUpdate()
    {
        if (GameInputBlocker.IsInputBlocked)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        if (CookingMiniGameController.IsCookingMiniGameOpen)
        {
            return;
        }

        if (keyboard.qKey.wasPressedThisFrame)
        {
            MoveLeftOneStation();
            return;
        }

        if (keyboard.eKey.wasPressedThisFrame)
        {
            MoveRightOneStation();
        }
    }

    public void MoveToCounter()
    {
        if (GameInputBlocker.IsInputBlocked)
        {
            return;
        }

        MoveToCounterInternal();
    }

    public void ForceMoveToCounter()
    {
        MoveToCounterInternal();
    }

    private void MoveToCounterInternal()
    {
        currentStationIndex = 0;
        isTransitioning = true;
        mainContentPanel.DOKill();
        mainContentPanel.DOLocalMoveX(0f, transitionDuration)
            .SetEase(transitionEase)
            .OnComplete(() =>
            {
                isTransitioning = false;
                CounterStationSettled?.Invoke();
            });
        Debug.Log("Sliding to Counter!");
    }

    public void MoveToKitchen()
    {
        if (GameInputBlocker.IsInputBlocked)
        {
            return;
        }

        currentStationIndex = 1;
        isTransitioning = true;
        mainContentPanel.DOKill();
        mainContentPanel.DOLocalMoveX(-slideDistance, transitionDuration)
            .SetEase(transitionEase)
            .OnComplete(() => isTransitioning = false);
        Debug.Log("Sliding to Kitchen!");
    }

    public void MoveToPickup()
    {
        if (GameInputBlocker.IsInputBlocked)
        {
            return;
        }

        currentStationIndex = 2;
        isTransitioning = true;
        mainContentPanel.DOKill();
        mainContentPanel.DOLocalMoveX(-slideDistance * 2f, transitionDuration)
            .SetEase(transitionEase)
            .OnComplete(() => isTransitioning = false);
        Debug.Log("Sliding to Pickup!");
    }

    private void MoveLeftOneStation()
    {
        if (currentStationIndex >= 2)
        {
            MoveToKitchen();
            return;
        }

        if (currentStationIndex == 1)
        {
            MoveToCounter();
        }
    }

    private void MoveRightOneStation()
    {
        if (currentStationIndex <= 0)
        {
            MoveToKitchen();
            return;
        }

        if (currentStationIndex == 1)
        {
            MoveToPickup();
        }
    }
}
