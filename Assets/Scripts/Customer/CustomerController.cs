using DG.Tweening;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

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

    [Header("Dialogue Settings")]
    [SerializeField] private string[] successDialogues = { "감사합니다!", "감사합니다~", "좋은 하루 되세요~!" };
    [SerializeField] private float typeSpeed = 0.05f;
    [SerializeField] private float readDelayAfterTyping = 1.0f;

    [Header("Order Dialogue Formats (Keep array lengths matched!)")]
    [SerializeField] private string[] sentenceStarters = { "" };
    [SerializeField] private string[] separators = { ", " };
    [SerializeField] private string[] lastSeparators = { "이랑 " };
    [SerializeField] private string[] sentenceClosers = { " 주세요." };

    private CustomerState currentState;
    private float currentPatience;
    private Tween movementTween;
    private Sequence bobbingSequence;
    private ICustomerVisuals visuals;
    private float currentFootstepVolume = 1f;
    private bool hasPendingDrinkResult;
    private bool pendingDrinkSucceeded;

    public CustomerState CurrentState => currentState;
    public GhostType CustomerGhostType => ghostType;
    public bool IsHappy { get; private set; }
    public int TotalDrinksOrdered { get; private set; }

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

    public void SetOrderText(string rawOrderText, int totalDrinks)
    {
        TotalDrinksOrdered = totalDrinks;

        if (orderTextLabel != null)
        {
            orderTextLabel.text = FormatOrderString(rawOrderText);
        }
    }

    // Extracted the formatting logic so we can test it without UI references
    private string FormatOrderString(string rawOrderText)
    {
        string[] orderItems = rawOrderText.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

        if (orderItems.Length == 0) return "";

        // 1. Determine which pattern index to use
        int patternCount = sentenceStarters.Length; // We base the count on the starters array
        int chosenIndex = 0;

        if (patternCount > 1)
        {
            chosenIndex = UnityEngine.Random.Range(0, patternCount);
        }

        // 2. Safely grab the matching string components for this index
        string starter = GetSafeString(sentenceStarters, chosenIndex);
        string sep = GetSafeString(separators, chosenIndex);
        string lastSep = GetSafeString(lastSeparators, chosenIndex);
        string closer = GetSafeString(sentenceClosers, chosenIndex);

        // 3. Build the sentence
        System.Text.StringBuilder formattedOrder = new System.Text.StringBuilder();
        formattedOrder.Append(starter);

        for (int i = 0; i < orderItems.Length; i++)
        {
            formattedOrder.Append(orderItems[i]);

            if (i < orderItems.Length - 1)
            {
                if (i == orderItems.Length - 2)
                {
                    formattedOrder.Append(lastSep);
                }
                else
                {
                    formattedOrder.Append(sep);
                }
            }
        }

        formattedOrder.Append(closer);
        return formattedOrder.ToString();
    }

    // Helper to prevent out-of-bounds errors if the user forgot to match array lengths in the Inspector
    private string GetSafeString(string[] array, int index)
    {
        if (array == null || array.Length == 0) return "";
        if (index >= array.Length) return array[array.Length - 1]; // Fallback to the last available string
        return array[index];
    }

    public void Spawn()
    {
        if (spawnLocation != null) rectTransform.anchoredPosition = spawnLocation.anchoredPosition;

        IsHappy = false;
        hasPendingDrinkResult = false;
        pendingDrinkSucceeded = false;
        currentFootstepVolume = 1f;
        visuals?.SetNeutral();
        currentPatience = maxPatience;

        if (SoundManager.Instance != null && SoundManager.Instance.SFX != null)
        {
            SoundManager.Instance.SFX.PlayBell();
        }

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
        else if (currentState == CustomerState.MovingToPickup)
        {
            hasPendingDrinkResult = true;
            pendingDrinkSucceeded = true;
        }
    }

    public void OrderFailed()
    {
        if (currentState == CustomerState.WaitingForDrink)
        {
            IsHappy = false;
            ChangeState(CustomerState.Angry);
        }
        else if (currentState == CustomerState.MovingToPickup)
        {
            hasPendingDrinkResult = true;
            pendingDrinkSucceeded = false;
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

                // Show speech bubble with the formatted text
                if (speechBubble != null) speechBubble.SetActive(true);

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
                if (hasPendingDrinkResult)
                {
                    bool succeeded = pendingDrinkSucceeded;
                    hasPendingDrinkResult = false;
                    pendingDrinkSucceeded = false;
                    ChangeState(succeeded ? CustomerState.Completed : CustomerState.Angry);
                    break;
                }

                visuals?.SetVisible(false);
                break;

            case CustomerState.Completed:
                visuals?.SetVisible(true);
                visuals?.SetHappy();

                string chosenLine = "Thank you!";
                if (successDialogues != null && successDialogues.Length > 0)
                {
                    chosenLine = successDialogues[UnityEngine.Random.Range(0, successDialogues.Length)];
                }

                StartCoroutine(TypeDialogue(chosenLine));
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
                DOTween.To(() => currentFootstepVolume, x => currentFootstepVolume = x, 0f, moveDuration);
                break;
        }
    }

    private void LeaveScreen()
    {
        ChangeState(CustomerState.Leaving);
    }

    private IEnumerator TypeDialogue(string message)
    {
        if (speechBubble != null) speechBubble.SetActive(true);
        if (orderTextLabel != null) orderTextLabel.text = "";

        bool isSkipping = false;

        for (int i = 0; i < message.Length; i++)
        {
            if (orderTextLabel != null)
            {
                orderTextLabel.text += message[i];
            }

            if (Keyboard.current != null && Keyboard.current.spaceKey.isPressed)
            {
                isSkipping = true;
            }
            else
            {
                isSkipping = false;
            }

            if (!isSkipping)
            {
                yield return new WaitForSeconds(typeSpeed);
            }
        }

        yield return new WaitForSeconds(readDelayAfterTyping);
        LeaveScreen();
    }

    private void StartBobbing(float baseY)
    {
        StopBobbing();
        bobbingSequence = DOTween.Sequence();
        bobbingSequence.Append(rectTransform.DOAnchorPosY(baseY + bobbingAmplitude, bobbingSpeed).SetEase(Ease.OutSine));
        bobbingSequence.Append(rectTransform.DOAnchorPosY(baseY, bobbingSpeed).SetEase(Ease.InSine).OnComplete(() =>
        {
            if (SoundManager.Instance != null && SoundManager.Instance.SFX != null)
            {
                SoundManager.Instance.SFX.PlayFootstep(ghostType, currentFootstepVolume);
            }
        }));
        bobbingSequence.SetLoops(-1);
    }

    private void StopBobbing()
    {
        bobbingSequence?.Kill();
    }


    //DEBUG FUNCTION
    [ContextMenu("Test Order Formatting")]
    private void TestOrderFormatting()
    {
        string dummyOrder = "<음료1> 1잔\n<음료2> 2잔\n<음료3> 3잔";
        string result = FormatOrderString(dummyOrder);

        Debug.Log($"<b>[{gameObject.name} - {ghostType}]</b> Test Result:\n<color=yellow>{result}</color>");
    }
}