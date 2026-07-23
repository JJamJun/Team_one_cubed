using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ReputationRatingManager : MonoBehaviour
{
    public static ReputationRatingManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject ratingArea;
    [SerializeField] private TMP_Text blankStar;
    [SerializeField] private TMP_Text fullStar;
    [SerializeField] private RectTransform fullStarMask;
    [SerializeField] private TMP_Text ratingLabelText;
    [SerializeField] private TMP_Text ratingValueText;
    [SerializeField, Min(0f)] private float ratingTextGap = 4f;

    [Header("Unlock Settings")]
    [SerializeField] private string unlockSaveFileName = "menu_progress.json";
    [SerializeField] private string defaultMenuId = "0";
    [SerializeField, Min(0)] private int ratingUnlockCount = 2;
    [SerializeField, Min(0.1f)] private float unlockSavePollInterval = 0.5f;

    [Header("Rating Range")]
    [SerializeField] private float minScore = 0f;
    [SerializeField] private float maxScore = 50f;
    [SerializeField] private float initialUnlockedScore = 25f;
    [SerializeField, Min(0.01f)] private float pointsPerStar = 10f;

    [Header("Reputation Increase")]
    [SerializeField, InspectorName("귀신 만족")] private float ghostSatisfiedIncrease = 10f;
    [SerializeField, InspectorName("영수증 시간 50% 이상 남김")] private float fastReceiptIncrease = 2f;
    [SerializeField, InspectorName("음료 1잔당 성공")] private float perDrinkSuccessIncrease = 2f;
    [SerializeField, InspectorName("2~3연속 성공")] private float successStreak2To3Increase = 2f;
    [SerializeField, InspectorName("4~6연속 성공")] private float successStreak4To6Increase = 4f;
    [SerializeField, InspectorName("7~9연속 성공")] private float successStreak7To9Increase = 6f;
    [SerializeField, InspectorName("10연속 이상 성공")] private float successStreak10PlusIncrease = 8f;

    [Header("Reputation Decrease")]
    [SerializeField, InspectorName("Failed 음료 투입")] private float failedDrinkDecrease = 14f;
    [SerializeField, InspectorName("손님 화남 ANGRY 연출")] private float angryEventDecrease = 8f;
    [SerializeField, InspectorName("카운터 인내심 초과")] private float counterPatienceDecrease = 4f;
    [SerializeField, InspectorName("영수증 소실")] private float receiptLostDecrease = 4f;
    [SerializeField, InspectorName("POS 주문 오입력")] private float wrongOrderDecrease = 2f;

    [Header("Band Multipliers")]
    [SerializeField, InspectorName("1점대 상승"), Min(0f)] private float band1IncreaseMultiplier = 2f;
    [SerializeField, InspectorName("1점대 하락"), Min(0f)] private float band1DecreaseMultiplier = 0.2f;
    [SerializeField, InspectorName("2점대 상승"), Min(0f)] private float band2IncreaseMultiplier = 1f;
    [SerializeField, InspectorName("2점대 하락"), Min(0f)] private float band2DecreaseMultiplier = 1f;
    [SerializeField, InspectorName("3점대 상승"), Min(0f)] private float band3IncreaseMultiplier = 1f;
    [SerializeField, InspectorName("3점대 하락"), Min(0f)] private float band3DecreaseMultiplier = 1f;
    [SerializeField, InspectorName("4점대 상승"), Min(0f)] private float band4IncreaseMultiplier = 0.8f;
    [SerializeField, InspectorName("4점대 하락"), Min(0f)] private float band4DecreaseMultiplier = 1.2f;
    [SerializeField, InspectorName("5점대 상승"), Min(0f)] private float band5IncreaseMultiplier = 0.5f;
    [SerializeField, InspectorName("5점대 하락"), Min(0f)] private float band5DecreaseMultiplier = 1.5f;

    private float currentScore;
    private int successStreak;
    private bool isRatingUnlocked;
    private float nextUnlockSavePollTime;
    private float fullStarWidth;

    public float CurrentRating => CurrentStarRating;
    public float CurrentScore => currentScore;
    public float CurrentStarRating => Mathf.Clamp(currentScore / Mathf.Max(0.01f, pointsPerStar), 0f, 5f);
    public int CurrentRatingBand => GetScoreBand(currentScore);
    public int CurrentAngryEventBand => currentScore <= minScore ? 0 : CurrentRatingBand;
    public bool IsRatingUnlocked => isRatingUnlocked;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        AutoBindReferences();
        PrepareFullStarMask();
        RefreshUnlockState(true);
    }

    private void OnEnable()
    {
        Receipt.FailedDrinkSubmitted += RegisterFailedDrinkSubmitted;
    }

    private void OnDisable()
    {
        Receipt.FailedDrinkSubmitted -= RegisterFailedDrinkSubmitted;
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void OnValidate()
    {
        AutoBindReferences();
        CacheFullStarWidth();
        currentScore = Mathf.Clamp(currentScore, minScore, maxScore);
        UpdateUI();
    }

    private void Update()
    {
        if (Time.unscaledTime < nextUnlockSavePollTime)
        {
            return;
        }

        nextUnlockSavePollTime = Time.unscaledTime + unlockSavePollInterval;
        RefreshUnlockState(false);
    }

    public void ResetRating()
    {
        successStreak = 0;
        currentScore = isRatingUnlocked ? initialUnlockedScore : minScore;
        UpdateUI();
    }

    public void RegisterReceiptSuccess(int drinkCount, bool completedWithinHalfTime, bool isGhostCustomer)
    {
        if (!isRatingUnlocked)
        {
            return;
        }

        successStreak++;
        float amount = Mathf.Max(1, drinkCount) * perDrinkSuccessIncrease;

        if (isGhostCustomer)
        {
            amount += ghostSatisfiedIncrease;
        }

        if (completedWithinHalfTime)
        {
            amount += fastReceiptIncrease;
        }

        amount += GetSuccessStreakIncrease();
        IncreaseScore(amount);
    }

    public void RegisterCustomerAngryEvent()
    {
        DecreaseScore(angryEventDecrease);
    }

    public void RegisterCounterPatienceExpired()
    {
        DecreaseScore(counterPatienceDecrease);
    }

    public void RegisterReceiptLost()
    {
        DecreaseScore(receiptLostDecrease);
    }

    public void RegisterWrongOrderSubmitted()
    {
        DecreaseScore(wrongOrderDecrease);
    }

    private void RegisterFailedDrinkSubmitted()
    {
        DecreaseScore(failedDrinkDecrease);
    }

    private void IncreaseScore(float amount)
    {
        currentScore = Mathf.Clamp(currentScore + amount * GetIncreaseMultiplierForCurrentBand(), minScore, maxScore);
        UpdateUI();
    }

    private void DecreaseScore(float amount)
    {
        if (!isRatingUnlocked)
        {
            return;
        }

        successStreak = 0;
        currentScore = Mathf.Clamp(currentScore - amount * GetDecreaseMultiplierForCurrentBand(), minScore, maxScore);
        UpdateUI();
    }

    private float GetSuccessStreakIncrease()
    {
        if (successStreak >= 10)
        {
            return successStreak10PlusIncrease;
        }

        if (successStreak >= 7)
        {
            return successStreak7To9Increase;
        }

        if (successStreak >= 4)
        {
            return successStreak4To6Increase;
        }

        if (successStreak >= 2)
        {
            return successStreak2To3Increase;
        }

        return 0f;
    }

    private float GetIncreaseMultiplierForCurrentBand()
    {
        switch (CurrentRatingBand)
        {
            case 1: return band1IncreaseMultiplier;
            case 2: return band2IncreaseMultiplier;
            case 3: return band3IncreaseMultiplier;
            case 4: return band4IncreaseMultiplier;
            case 5: return band5IncreaseMultiplier;
            default: return band1IncreaseMultiplier;
        }
    }

    private float GetDecreaseMultiplierForCurrentBand()
    {
        switch (CurrentRatingBand)
        {
            case 1: return band1DecreaseMultiplier;
            case 2: return band2DecreaseMultiplier;
            case 3: return band3DecreaseMultiplier;
            case 4: return band4DecreaseMultiplier;
            case 5: return band5DecreaseMultiplier;
            default: return band1DecreaseMultiplier;
        }
    }

    private int GetScoreBand(float score)
    {
        if (score <= 10f) return 1;
        if (score <= 20f) return 2;
        if (score <= 30f) return 3;
        if (score <= 40f) return 4;
        return 5;
    }

    private void RefreshUnlockState(bool force)
    {
        bool nextUnlocked = UnlockProgressUtility.CountUnlockedMenusExcludingDefault(unlockSaveFileName, defaultMenuId) >= ratingUnlockCount;
        if (!force && nextUnlocked == isRatingUnlocked)
        {
            return;
        }

        isRatingUnlocked = nextUnlocked;
        if (ratingArea != null)
        {
            ratingArea.SetActive(isRatingUnlocked);
        }

        ResetRating();
    }

    private void UpdateUI()
    {
        UpdateStarFill();
        UpdateRatingText();
    }

    private void UpdateStarFill()
    {
        if (fullStarMask == null || fullStarWidth <= 0f)
        {
            return;
        }

        float range = Mathf.Max(0.01f, maxScore - minScore);
        float normalizedRating = Mathf.Clamp01((currentScore - minScore) / range);
        fullStarMask.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, fullStarWidth * normalizedRating);
    }

    private void UpdateRatingText()
    {
        if (ratingValueText != null)
        {
            ratingValueText.text = CurrentStarRating.ToString("0.0");
        }

        UpdateRatingValuePosition();
    }

    private void UpdateRatingValuePosition()
    {
        if (ratingLabelText == null || ratingValueText == null)
        {
            return;
        }

        RectTransform labelRect = ratingLabelText.rectTransform;
        RectTransform valueRect = ratingValueText.rectTransform;
        Vector2 labelPreferred = ratingLabelText.GetPreferredValues(ratingLabelText.text);
        Vector2 valuePreferred = ratingValueText.GetPreferredValues(ratingValueText.text);
        Vector2 valuePosition = valueRect.anchoredPosition;

        valuePosition.x = labelRect.anchoredPosition.x
            + labelPreferred.x * 0.5f
            + ratingTextGap
            + valuePreferred.x * 0.5f;

        valueRect.anchoredPosition = valuePosition;
    }

    private void PrepareFullStarMask()
    {
        if (fullStar == null)
        {
            return;
        }

        RectTransform fullStarRect = fullStar.rectTransform;
        CacheFullStarWidth();

        if (fullStarMask == null)
        {
            GameObject maskObject = new GameObject("FullStarMask", typeof(RectTransform), typeof(RectMask2D));
            RectTransform maskRect = maskObject.transform as RectTransform;
            RectTransform originalParent = fullStarRect.parent as RectTransform;
            int siblingIndex = fullStarRect.GetSiblingIndex();
            Vector2 originalAnchoredPosition = fullStarRect.anchoredPosition;
            Vector2 originalSizeDelta = fullStarRect.sizeDelta;
            Vector2 originalAnchorMin = fullStarRect.anchorMin;
            Vector2 originalAnchorMax = fullStarRect.anchorMax;
            Vector2 originalPivot = fullStarRect.pivot;

            maskRect.SetParent(originalParent, false);
            maskRect.SetSiblingIndex(siblingIndex);
            maskRect.anchorMin = originalAnchorMin;
            maskRect.anchorMax = originalAnchorMax;
            maskRect.pivot = new Vector2(0f, originalPivot.y);
            maskRect.anchoredPosition = new Vector2(originalAnchoredPosition.x - fullStarWidth * originalPivot.x, originalAnchoredPosition.y);
            maskRect.sizeDelta = originalSizeDelta;

            fullStarRect.SetParent(maskRect, false);
            fullStarRect.anchorMin = new Vector2(0f, 0.5f);
            fullStarRect.anchorMax = new Vector2(0f, 0.5f);
            fullStarRect.pivot = new Vector2(0f, originalPivot.y);
            fullStarRect.anchoredPosition = Vector2.zero;
            fullStarRect.sizeDelta = originalSizeDelta;

            fullStarMask = maskRect;
        }

        fullStarMask.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, fullStarWidth);
    }

    private void CacheFullStarWidth()
    {
        if (fullStar == null)
        {
            return;
        }

        float width = fullStar.rectTransform.rect.width;
        if (width <= 0f)
        {
            width = fullStar.rectTransform.sizeDelta.x;
        }

        if (width > 0f)
        {
            fullStarWidth = width;
        }
    }

    private void AutoBindReferences()
    {
        if (ratingArea == null)
        {
            Transform ratingAreaTransform = FindSceneTransform("RatingArea");
            if (ratingAreaTransform != null)
            {
                ratingArea = ratingAreaTransform.gameObject;
            }
        }

        if (blankStar == null)
        {
            blankStar = FindSceneComponent<TMP_Text>("BlankStar");
        }

        if (fullStar == null)
        {
            fullStar = FindSceneComponent<TMP_Text>("FullStar");
        }

        if (ratingLabelText == null)
        {
            ratingLabelText = FindSceneComponent<TMP_Text>("RatingTXT");
        }

        if (ratingValueText == null)
        {
            ratingValueText = FindSceneComponent<TMP_Text>("Rating");
        }
    }

    private static T FindSceneComponent<T>(string targetName) where T : Component
    {
        Transform target = FindSceneTransform(targetName);
        return target != null ? target.GetComponent<T>() : null;
    }

    private static Transform FindSceneTransform(string targetName)
    {
        Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform target = transforms[i];
            if (target.gameObject.scene.IsValid() && target.name == targetName)
            {
                return target;
            }
        }

        return null;
    }
}
