using DG.Tweening;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum CustomerState
{
    Arriving,
    Ordering,
    MovingToPickup,
    WaitingForDrink,
    Completed,
    Angry,
    Leaving
}

public class CustomerController : MonoBehaviour
{
    public event Action<CustomerController> OnCustomerLeft;

    [Header("UI References")]
    [SerializeField] private RectTransform rectTransform;
    [SerializeField] private GameObject speechBubble;
    [SerializeField] private Image patienceMeterFill;
    [SerializeField] private TMP_Text orderTextLabel;


    [Header("Movement Waypoints")]
    [SerializeField] private RectTransform spawnLocation;
    [SerializeField] private RectTransform counterLocation;
    [SerializeField] private RectTransform pickupLocation;
    [SerializeField] private RectTransform exitLocation;

    [Header("Movement Settings")]
    [SerializeField] private float arrivalDuration = 1.5f;
    [SerializeField] private float moveDuration = 1.0f;
    [SerializeField] private float bobbingAmplitude = 20f;

    [Header("Patience Settings")]
    [SerializeField] private float maxPatience = 15f;


    private CustomerState currentState;
    private float currentPatience;
    private Tween movementTween;
    private ICustomerVisuals visuals;

    public CustomerState CurrentState => currentState;

    private void Awake()
    {
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
        if (speechBubble != null) speechBubble.SetActive(false);
        if (patienceMeterFill != null) patienceMeterFill.fillAmount = 1f;

        visuals = GetComponent<ICustomerVisuals>();
    }

    public void InitializeWaypoints(RectTransform spawn, RectTransform counter, RectTransform pickup, RectTransform exit)
    {
        spawnLocation = spawn;
        counterLocation = counter;
        pickupLocation = pickup;
        exitLocation = exit;
    }
    public void Spawn()
    {
        if (spawnLocation != null)
        {
            rectTransform.anchoredPosition = spawnLocation.anchoredPosition;
        }

        visuals?.SetNeutral();
        currentPatience = maxPatience;
        ChangeState(CustomerState.Arriving);
    }

    private void Update()
    {
        if (currentState == CustomerState.Ordering)
        {
            UpdatePatience();
        }
    }

    private void UpdatePatience()
    {
        currentPatience -= Time.deltaTime;

        if (patienceMeterFill != null)
        {
            patienceMeterFill.fillAmount = Mathf.Clamp01(currentPatience / maxPatience);
        }

        if (currentPatience <= 0f)
        {
            ChangeState(CustomerState.Angry);
        }
    }

    public void SetOrderText(string text)
    {
        if (orderTextLabel != null)
        {
            orderTextLabel.text = text;
        }
    }



    // --- External Triggers ---

    public void AcceptOrder()
    {
        if (currentState == CustomerState.Ordering)
        {
            ChangeState(CustomerState.MovingToPickup);
        }
    }

    public void OrderFulfilled()
    {
        if (currentState == CustomerState.WaitingForDrink)
        {
            ChangeState(CustomerState.Completed);
        }
    }

    public void OrderFailed()
    {
        if (currentState == CustomerState.WaitingForDrink)
        {
            ChangeState(CustomerState.Angry);
        }
    }

    // --- Core State Machine ---

    private void ChangeState(CustomerState newState)
    {
        currentState = newState;
        movementTween?.Kill();

        switch (currentState)
        {
            case CustomerState.Arriving:
                Vector2 targetCounter = counterLocation != null ? counterLocation.anchoredPosition : Vector2.zero;

                movementTween = rectTransform.DOAnchorPos(targetCounter, arrivalDuration)
                    .SetEase(Ease.OutQuad)
                    .OnComplete(() => ChangeState(CustomerState.Ordering));

                rectTransform.DOAnchorPosY(targetCounter.y + bobbingAmplitude, 0.25f)
                    .SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine)
                    .SetId("Bobbing");
                break;

            case CustomerState.Ordering:
                DOTween.Kill("Bobbing");
                if (counterLocation != null)
                {
                    rectTransform.anchoredPosition = counterLocation.anchoredPosition;
                }

                if (speechBubble != null) speechBubble.SetActive(true);
                break;

            case CustomerState.MovingToPickup:
                if (speechBubble != null) speechBubble.SetActive(false);
                if (patienceMeterFill != null) patienceMeterFill.transform.parent.gameObject.SetActive(false);

                Vector2 targetPickup = pickupLocation != null ? pickupLocation.anchoredPosition : Vector2.zero;

                movementTween = rectTransform.DOAnchorPos(targetPickup, moveDuration)
                    .SetEase(Ease.InOutQuad)
                    .OnComplete(() => ChangeState(CustomerState.WaitingForDrink));
                break;

            case CustomerState.WaitingForDrink:
                break;

            case CustomerState.Completed:
                visuals?.SetHappy();
                Debug.Log("Customer is happy");
                DOVirtual.DelayedCall(2f, LeaveScreen);
                break;

            case CustomerState.Angry:
                DOTween.Kill("Bobbing");
                if (speechBubble != null) speechBubble.SetActive(false);

                visuals?.SetAngry();
                Debug.Log("Customer is angry");
                DOVirtual.DelayedCall(1f, LeaveScreen);
                break;

            case CustomerState.Leaving:
                Vector2 targetExit = exitLocation != null ? exitLocation.anchoredPosition : Vector2.zero;

                movementTween = rectTransform.DOAnchorPos(targetExit, moveDuration)
                    .SetEase(Ease.InQuad)
                    .OnComplete(() =>
                    {
                        OnCustomerLeft?.Invoke(this);
                        Destroy(gameObject);
                    });
                break;
        }
    }

    private void LeaveScreen()
    {
        ChangeState(CustomerState.Leaving);
    }
}