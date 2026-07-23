using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ReputationRatingManager : MonoBehaviour
{
    public static ReputationRatingManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private TMP_Text blankStar;
    [SerializeField] private TMP_Text fullStar;
    [SerializeField] private RectTransform fullStarMask;
    [SerializeField] private TMP_Text ratingLabelText;
    [SerializeField] private TMP_Text ratingValueText;
    [SerializeField, Min(0f)] private float ratingTextGap = 4f;

    [Header("Rating Range")]
    [SerializeField] private float minRating = 0f;
    [SerializeField] private float maxRating = 5f;

    [Header("\uD3C9\uD310 \uC0C1\uC2B9")]
    [SerializeField, InspectorName("\uD3C9\uD310 \uC0C1\uC2B9 \uACC4\uC218"), Min(0f)] private float reputationIncreaseMultiplier = 1f;
    [SerializeField, InspectorName("\uC77C\uBC18 \uC74C\uB8CC \uC131\uACF5")] private float normalDrinkSuccessIncrease = 1f;
    [SerializeField, InspectorName("50% \uB0B4 1\uC794 \uC131\uACF5")] private float fastOneDrinkIncrease = 2f;
    [SerializeField, InspectorName("50% \uB0B4 2\uC794 \uC131\uACF5")] private float fastTwoDrinkIncrease = 4f;
    [SerializeField, InspectorName("50% \uB0B4 3\uC794 \uC774\uC0C1 \uC131\uACF5")] private float fastThreeDrinkIncrease = 5f;
    [SerializeField, InspectorName("\uADC0\uC2E0 \uC74C\uB8CC \uC131\uACF5")] private float ghostDrinkSuccessIncrease = 5f;

    [Header("\uD3C9\uD310 \uD558\uB77D")]
    [SerializeField, InspectorName("\uD3C9\uD310 \uD558\uB77D \uACC4\uC218"), Min(0f)] private float reputationDecreaseMultiplier = 1f;
    [SerializeField, InspectorName("\uC8FC\uBB38 \uC624\uC785\uB825")] private float wrongOrderDecrease = 3f;
    [SerializeField, InspectorName("\uC601\uC218\uC99D \uC2DC\uAC04 \uCD08\uACFC")] private float receiptTimeoutDecrease = 5f;
    [SerializeField, InspectorName("Failed \uC74C\uB8CC \uD22C\uC785")] private float failedDrinkDecrease = 10f;

    private float currentRating;
    private float successRatingTotal;
    private int successRatingCount;
    private float reputationPenaltyTotal;
    private float fullStarWidth;


    public float CurrentRating => currentRating;


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
        ResetRating();
    }

    private void OnEnable()
    {
        Receipt.ReceiptTimedOut += RegisterReceiptTimeout;
        Receipt.FailedDrinkSubmitted += RegisterFailedDrinkSubmitted;
        CustomerManager.WrongOrderSubmitted += RegisterWrongOrderSubmitted;
    }

    private void OnDisable()
    {
        Receipt.ReceiptTimedOut -= RegisterReceiptTimeout;
        Receipt.FailedDrinkSubmitted -= RegisterFailedDrinkSubmitted;
        CustomerManager.WrongOrderSubmitted -= RegisterWrongOrderSubmitted;
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void OnValidate()
    {
        AutoBindReferences();
        CacheFullStarWidth();
        currentRating = Mathf.Clamp(currentRating, minRating, maxRating);
        UpdateUI();
    }

    public void ResetRating()
    {
        successRatingTotal = 0f;
        successRatingCount = 0;
        reputationPenaltyTotal = 0f;
        RecalculateRating();
        UpdateUI();
    }

    public void RegisterReceiptSuccess(int drinkCount, bool completedWithinHalfTime, bool isGhostCustomer)
    {
        if (isGhostCustomer)
        {
            RegisterSuccessRating(ghostDrinkSuccessIncrease);
            return;
        }

        if (completedWithinHalfTime)
        {
            RegisterSuccessRating(GetFastSuccessIncrease(drinkCount));
            return;
        }

        RegisterSuccessRating(normalDrinkSuccessIncrease);
    }

    private void RegisterWrongOrderSubmitted()
    {
        DecreaseRating(wrongOrderDecrease);
    }

    private void RegisterReceiptTimeout()
    {
        DecreaseRating(receiptTimeoutDecrease);
    }

    private void RegisterFailedDrinkSubmitted()
    {
        DecreaseRating(failedDrinkDecrease);
    }

    private void RegisterSuccessRating(float amount)
    {
        successRatingTotal += amount * reputationIncreaseMultiplier;
        successRatingCount++;
        RecalculateRating();
        UpdateUI();
    }

    private void DecreaseRating(float amount)
    {
        reputationPenaltyTotal += amount * reputationDecreaseMultiplier;
        RecalculateRating();
        UpdateUI();
    }

    private void RecalculateRating()
    {
        float successAverage = successRatingCount > 0 ? successRatingTotal / successRatingCount : minRating;
        currentRating = Mathf.Clamp(successAverage - reputationPenaltyTotal, minRating, maxRating);
    }

    private float GetFastSuccessIncrease(int drinkCount)
    {
        if (drinkCount <= 1)
        {
            return fastOneDrinkIncrease;
        }

        if (drinkCount == 2)
        {
            return fastTwoDrinkIncrease;
        }

        return fastThreeDrinkIncrease;
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

        float range = Mathf.Max(0.01f, maxRating - minRating);
        float normalizedRating = Mathf.Clamp01((currentRating - minRating) / range);
        fullStarMask.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, fullStarWidth * normalizedRating);
    }

    private void UpdateRatingText()
    {
        if (ratingValueText != null)
        {
            ratingValueText.text = currentRating.ToString("0.0");
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

