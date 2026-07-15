using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class CookingStartAnimator : MonoBehaviour
{
    [SerializeField] private RectTransform targetRect;
    [SerializeField] private Button startButton;
    [SerializeField] private CookingIngredientManager ingredientManager;
    [SerializeField] private Vector2 shownPosition = new Vector2(-20f, -15f);
    [SerializeField] private float showDuration = 0.45f;
    [SerializeField] private float hideDuration = 0.25f;
    [SerializeField] private float elasticAmplitude = 0.6f;
    [SerializeField] private float elasticPeriod = 0.25f;

    private Tween positionTween;
    private Vector2 hiddenPosition;
    private bool isShown;

    private void Awake()
    {
        if (targetRect == null)
        {
            targetRect = GetComponent<RectTransform>();
        }

        if (startButton == null)
        {
            startButton = GetComponent<Button>();
        }

        if (targetRect == null)
        {
            Debug.LogWarning($"{nameof(CookingStartAnimator)}: Target RectTransform is not assigned.");
            return;
        }

        hiddenPosition = targetRect.anchoredPosition;
        SetHiddenState();
    }

    private void OnEnable()
    {
        if (ingredientManager == null)
        {
            Debug.LogWarning($"{nameof(CookingStartAnimator)}: IngredientManager is not assigned.");
            return;
        }

        ingredientManager.IngredientSelectionChanged += HandleIngredientSelectionChanged;
        HandleIngredientSelectionChanged(ingredientManager.ClickedIngredients.Count > 0);
    }

    private void OnDisable()
    {
        if (ingredientManager != null)
        {
            ingredientManager.IngredientSelectionChanged -= HandleIngredientSelectionChanged;
        }

        positionTween?.Kill();
    }

    private void HandleIngredientSelectionChanged(bool hasAnyIngredient)
    {
        if (hasAnyIngredient)
        {
            ShowStartButton();
            return;
        }

        HideStartButton();
    }

    private void ShowStartButton()
    {
        if (isShown)
        {
            return;
        }

        isShown = true;

        positionTween?.Kill();

        if (startButton != null)
        {
            startButton.interactable = false;
        }

        positionTween = targetRect
            .DOAnchorPos(shownPosition, showDuration)
            .SetEase(Ease.OutElastic, elasticAmplitude, elasticPeriod)
            .OnComplete(EnableStartButton)
            .SetTarget(targetRect);
    }

    private void HideStartButton()
    {
        if (!isShown && targetRect.anchoredPosition == hiddenPosition)
        {
            SetHiddenState();
            return;
        }

        isShown = false;

        positionTween?.Kill();

        if (startButton != null)
        {
            startButton.interactable = false;
        }

        positionTween = targetRect
            .DOAnchorPos(hiddenPosition, hideDuration)
            .SetEase(Ease.InQuad)
            .OnComplete(SetHiddenState)
            .SetTarget(targetRect);
    }

    private void EnableStartButton()
    {
        if (startButton != null)
        {
            startButton.interactable = true;
        }
    }

    private void SetHiddenState()
    {
        if (targetRect == null)
        {
            return;
        }

        targetRect.anchoredPosition = hiddenPosition;

        if (startButton != null)
        {
            startButton.interactable = false;
        }
    }
}
