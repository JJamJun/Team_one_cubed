using DG.Tweening;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum GhostType
{
    None, DeadLion, Little, Woman, Eight, Dokaebi
}

public enum CustomerState
{
    Arriving, Ordering, MovingToPickup, WaitingForDrink, Completed, Angry, Leaving
}

public class CustomerController : MonoBehaviour
{
    public event Action<CustomerController> OnCustomerLeft;

    [Header("Customer Identity")]
    [SerializeField] private GhostType ghostType = GhostType.None;

    [Header("UI References")]
    [SerializeField] private RectTransform rectTransform;
    [SerializeField] private GameObject speechBubble;
    [SerializeField] private TMP_Text orderTextLabel;
    [SerializeField] private Image patienceMeterFill;

    [Header("Movement Waypoints")]
    [SerializeField] private RectTransform spawnLocation;
    [SerializeField] private RectTransform counterLocation;
    [SerializeField] private RectTransform pickupLocation;
    [SerializeField] private RectTransform exitLocation;

    [Header("Movement Settings")]
    [SerializeField] private float arrivalDuration = 1.5f;
    [SerializeField] private float moveDuration = 1.0f;
    [SerializeField] private float bobbingAmplitude = 20f;
    [SerializeField] private float bobbingSpeed = 0.2f;

    [Header("Patience Settings")]
    [SerializeField] private float maxPatience = 15f;

    private CustomerState currentState;
    private float currentPatience;
    private Tween movementTween;
    private Sequence bobbingSequence;
    private ICustomerVisuals visuals;
    private float currentFootstepVolume = 1f;

    public CustomerState CurrentState => currentState;
    public GhostType CustomerGhostType => ghostType;
    public bool IsHappy { get; private set; }

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

    public void SetOrderText(string text)
    {
        if (orderTextLabel != null) orderTextLabel.text = text;
    }

    public void Spawn()
    {
        if (spawnLocation != null) rectTransform.anchoredPosition = spawnLocation.anchoredPosition;

        IsHappy = false;
        currentFootstepVolume = 1f;
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

        if (patienceMeterFill != null) patienceMeterFill.fillAmount = Mathf.Clamp01(currentPatience / maxPatience);
        if (currentPatience <= 0f) ChangeState(CustomerState.Angry);
    }

    public void AcceptOrder()
    {
        if (currentState == CustomerState.Ordering) ChangeState(CustomerState.MovingToPickup);
    }

    public void OrderFulfilled()
    {
        if (currentState == CustomerState.WaitingForDrink)
        {
            IsHappy = true;
            ChangeState(CustomerState.Completed);
        }
    }

    public void OrderFailed()
    {
        if (currentState == CustomerState.WaitingForDrink)
        {
            IsHappy = false;
            ChangeState(CustomerState.Angry);
        }
    }

    private void ChangeState(CustomerState newState)
    {
        currentState = newState;
        movementTween?.Kill();

        switch (currentState)
        {
            case CustomerState.Arriving:
                Vector2 targetCounter = counterLocation != null ? counterLocation.anchoredPosition : Vector2.zero;
                movementTween = rectTransform.DOAnchorPos(targetCounter, arrivalDuration)
                    .SetEase(Ease.Linear)
                    .OnComplete(() => ChangeState(CustomerState.Ordering));

                StartBobbing(targetCounter.y);
                break;

            case CustomerState.Ordering:
                StopBobbing();
                if (counterLocation != null) rectTransform.anchoredPosition = counterLocation.anchoredPosition;
                if (speechBubble != null) speechBubble.SetActive(true);

                //bell sound when customer ready to order
                if (SoundManager.Instance != null && SoundManager.Instance.SFX != null)
                {
                    SoundManager.Instance.SFX.PlayBell();
                }
                break;

            case CustomerState.MovingToPickup:
                if (speechBubble != null) speechBubble.SetActive(false);
                if (patienceMeterFill != null) patienceMeterFill.transform.parent.gameObject.SetActive(false);

                Vector2 targetPickup = pickupLocation != null ? pickupLocation.anchoredPosition : Vector2.zero;
                movementTween = rectTransform.DOAnchorPos(targetPickup, moveDuration)
                    .SetEase(Ease.Linear)
                    .OnComplete(() => ChangeState(CustomerState.WaitingForDrink));

                StartBobbing(targetPickup.y);
                break;

            case CustomerState.WaitingForDrink:
                StopBobbing();
                break;

            case CustomerState.Completed:
                visuals?.SetHappy();
                DOVirtual.DelayedCall(2f, LeaveScreen);
                break;

            case CustomerState.Angry:
                StopBobbing();
                if (speechBubble != null) speechBubble.SetActive(false);

                visuals?.SetAngry();
                DOVirtual.DelayedCall(1f, LeaveScreen);
                break;

            case CustomerState.Leaving:
                Vector2 targetExit = exitLocation != null ? exitLocation.anchoredPosition : Vector2.zero;
                movementTween = rectTransform.DOAnchorPos(targetExit, moveDuration)
                    .SetEase(Ease.Linear)
                    .OnComplete(() =>
                    {
                        OnCustomerLeft?.Invoke(this);
                        Destroy(gameObject);
                    });

                StartBobbing(targetExit.y);

                //footstep volume fade out when leaving
                DOTween.To(() => currentFootstepVolume, x => currentFootstepVolume = x, 0f, moveDuration);
                break;
        }
    }

    private void LeaveScreen()
    {
        ChangeState(CustomerState.Leaving);
    }

    

    private void StartBobbing(float baseY)
    {
        StopBobbing(); //clear sequences

        bobbingSequence = DOTween.Sequence();

        //up
        bobbingSequence.Append(rectTransform.DOAnchorPosY(baseY + bobbingAmplitude, bobbingSpeed).SetEase(Ease.OutSine));

        //down and audio
        bobbingSequence.Append(rectTransform.DOAnchorPosY(baseY, bobbingSpeed).SetEase(Ease.InSine).OnComplete(() =>
        {
            if (SoundManager.Instance != null && SoundManager.Instance.SFX != null)
            {
                SoundManager.Instance.SFX.PlayFootstep(ghostType, currentFootstepVolume);
            }
        }));

        //loop until stopped
        bobbingSequence.SetLoops(-1);
    }

    private void StopBobbing()
    {
        bobbingSequence?.Kill();
    }
}