using UnityEngine;
using DG.Tweening;
using UnityEngine.InputSystem;

public class UIStationScoller : MonoBehaviour
{
    [SerializeField] private RectTransform mainContentPanel;
    [SerializeField] private float transitionDuration = 0.5f;
    [SerializeField] private Ease transitionEase = Ease.InOutQuad;

    private float slideDistance = 1920f;
    private int currentStationIndex;

    private void Awake()
    {
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
        currentStationIndex = 0;
        mainContentPanel.DOKill();
        mainContentPanel.DOLocalMoveX(0f, transitionDuration).SetEase(transitionEase);
        Debug.Log("Sliding to Counter!");
    }

    public void MoveToKitchen()
    {
        currentStationIndex = 1;
        mainContentPanel.DOKill();
        mainContentPanel.DOLocalMoveX(-slideDistance, transitionDuration).SetEase(transitionEase);
        Debug.Log("Sliding to Kitchen!");
    }

    public void MoveToPickup()
    {
        currentStationIndex = 2;
        mainContentPanel.DOKill();
        mainContentPanel.DOLocalMoveX(-slideDistance * 2f, transitionDuration).SetEase(transitionEase);
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
