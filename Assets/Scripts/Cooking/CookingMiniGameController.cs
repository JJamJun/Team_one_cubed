using System;
using System.Collections.Generic;
using System.Text;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class CookingRecipe
{
    public CookingRecipe(string menuName, HashSet<CupContentState> requiredStates, string arrowCommand)
    {
        MenuName = menuName;
        RequiredStates = requiredStates;
        ArrowCommand = arrowCommand;
    }

    public string MenuName { get; }
    public HashSet<CupContentState> RequiredStates { get; }
    public string ArrowCommand { get; }
}

public class CookingMiniGameController : MonoBehaviour
{
    private const char UpArrow = '\u25B2';
    private const char DownArrow = '\u25BC';
    private const char LeftArrow = '\u25C0';
    private const char RightArrow = '\u25B6';

    [SerializeField] private GameObject cookingSpriteRoot;
    [SerializeField] private TMP_Text arrowCmdText;
    [SerializeField] private TMP_Text errorText;
    [SerializeField] private string recipeResourcePath = "temp_recipe";
    [SerializeField] private bool allowExtraIngredients;
    [SerializeField] private Color pendingColor = Color.black;
    [SerializeField] private Color correctColor = new Color(1f, 0.82f, 0f);
    [SerializeField] private Color wrongColor = new Color(1f, 0.1f, 0.05f);
    [SerializeField] private float errorAnimationSpeed = 1f;
    [SerializeField] private float errorShowDuration = 0.45f;
    [SerializeField] private float errorVisibleDuration = 1.2f;
    [SerializeField] private float errorFadeDuration = 0.35f;
    [SerializeField] private float errorBounceStartScale = 0.75f;
    [SerializeField] private bool autoHideError = true;

    private readonly List<CookingRecipe> recipes = new List<CookingRecipe>();
    private bool initialized;
    private bool recipesLoaded;
    private string currentCommand = string.Empty;
    private string currentMenuName = string.Empty;
    private int inputIndex;
    private int wrongIndex = -1;
    private bool commandComplete;
    private bool arrowInputEnabled;
    private bool escapeExitEnabled;
    private bool hasCookingAttemptStarted;
    private Tween errorTween;
    private Vector3 originalErrorScale = Vector3.one;
    private Color originalErrorColor = Color.white;
    private bool hasCapturedErrorVisuals;

    private static readonly Dictionary<string, string> ArrowCommandsByMenu = new Dictionary<string, string>
    {
        { "\uC544\uC774\uC2A4\uD2F0", "\u25B2\u25B2\u25BC\u25BC" },
        { "\uC544\uC774\uC2A4\uC544\uBA54\uB9AC\uCE74\uB178", "\u25B2\u25BC\u25B2\u25BC" },
        { "\uC544\uBA54\uB9AC\uCE74\uB178", "\u25C0\u25C0\u25B6\u25B6" },
        { "\uC544\uC0F7\uCD94", "\u25B2\u25BC\u25C0\u25B6" },
        { "\uC5BC\uC74C\uBB3C", "\u25B2\u25C0" }
    };

    private void Awake()
    {
        EnsureInitialized();
        if (!hasCookingAttemptStarted)
        {
            HideCookingScreen();
        }
    }

    private void Update()
    {
        if (TryHandleEscapeExit())
        {
            return;
        }

        if (!arrowInputEnabled || string.IsNullOrEmpty(currentCommand))
        {
            return;
        }

        if (cookingSpriteRoot != null && !cookingSpriteRoot.activeInHierarchy)
        {
            return;
        }

        char? pressedArrow = GetPressedArrow();
        if (!pressedArrow.HasValue)
        {
            return;
        }

        if (pressedArrow.Value == currentCommand[inputIndex])
        {
            wrongIndex = -1;
            inputIndex++;

            if (inputIndex >= currentCommand.Length)
            {
                commandComplete = true;
                arrowInputEnabled = false;
                escapeExitEnabled = true;
                Debug.Log($"Cooking command complete: {currentMenuName}");
            }
        }
        else
        {
            wrongIndex = inputIndex;
            arrowInputEnabled = false;
            escapeExitEnabled = true;
            Debug.Log($"Cooking command failed: {currentMenuName}");
        }

        RenderArrowCommand();
    }

    public bool TryStartCooking(IReadOnlyList<CupContentState> cupStates, string cupName)
    {
        EnsureInitialized();

        if (!TryFindMatchingRecipe(cupStates, out CookingRecipe recipe))
        {
            ShowRecipeError(cupStates);
            Debug.Log($"Cooking recipe mismatch: {cupName} states [{FormatStates(cupStates)}]");
            return false;
        }

        StartCooking(recipe, cupName, cupStates);
        return true;
    }

    private void StartCooking(CookingRecipe recipe, string cupName, IReadOnlyList<CupContentState> cupStates)
    {
        hasCookingAttemptStarted = true;
        currentMenuName = recipe.MenuName;
        currentCommand = recipe.ArrowCommand;
        inputIndex = 0;
        wrongIndex = -1;
        commandComplete = string.IsNullOrEmpty(currentCommand);
        arrowInputEnabled = !commandComplete;
        escapeExitEnabled = commandComplete;

        HideRecipeError(true);

        if (cookingSpriteRoot != null)
        {
            cookingSpriteRoot.SetActive(true);
        }

        RenderArrowCommand();
        Debug.Log($"CookingSprite opened for {recipe.MenuName} with {cupName} states [{FormatStates(cupStates)}]");
    }

    private void ShowRecipeError(IReadOnlyList<CupContentState> cupStates)
    {
        hasCookingAttemptStarted = true;

        if (cookingSpriteRoot != null)
        {
            cookingSpriteRoot.SetActive(false);
        }

        ShowRecipeErrorAnimation();
    }

    private void HideCookingScreen()
    {
        if (arrowCmdText != null)
        {
            arrowCmdText.text = string.Empty;
        }

        currentMenuName = string.Empty;
        currentCommand = string.Empty;
        inputIndex = 0;
        wrongIndex = -1;
        commandComplete = false;
        arrowInputEnabled = false;
        escapeExitEnabled = false;

        HideRecipeError(false);

        if (cookingSpriteRoot != null)
        {
            cookingSpriteRoot.SetActive(false);
        }
    }

    private bool TryFindMatchingRecipe(IReadOnlyList<CupContentState> cupStates, out CookingRecipe recipe)
    {
        LoadRecipes();

        HashSet<CupContentState> currentStates = BuildStateSet(cupStates);
        foreach (CookingRecipe candidate in recipes)
        {
            if (MatchesRecipe(currentStates, candidate.RequiredStates))
            {
                recipe = candidate;
                return true;
            }
        }

        recipe = null;
        return false;
    }

    private bool MatchesRecipe(HashSet<CupContentState> currentStates, HashSet<CupContentState> requiredStates)
    {
        if (!allowExtraIngredients && currentStates.Count != requiredStates.Count)
        {
            return false;
        }

        foreach (CupContentState requiredState in requiredStates)
        {
            if (!currentStates.Contains(requiredState))
            {
                return false;
            }
        }

        return true;
    }

    private void LoadRecipes()
    {
        if (recipesLoaded)
        {
            return;
        }

        recipesLoaded = true;
        recipes.Clear();

        TextAsset recipeAsset = Resources.Load<TextAsset>(recipeResourcePath);
        if (recipeAsset == null)
        {
            Debug.LogWarning($"{nameof(CookingMiniGameController)}: Resources/{recipeResourcePath} recipe file was not found.");
            return;
        }

        string[] lines = recipeAsset.text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (string rawLine in lines)
        {
            string line = rawLine.Trim();
            if (string.IsNullOrEmpty(line))
            {
                continue;
            }

            char separator = line.Contains("\t") ? '\t' : ',';
            string[] cells = line.Split(separator);
            if (cells.Length == 0)
            {
                continue;
            }

            string menuName = cells[0].Trim();
            if (string.IsNullOrEmpty(menuName))
            {
                continue;
            }

            HashSet<CupContentState> requiredStates = new HashSet<CupContentState>();
            for (int i = 1; i < cells.Length; i++)
            {
                string ingredientName = cells[i].Trim();
                if (string.IsNullOrEmpty(ingredientName))
                {
                    continue;
                }

                if (TryParseIngredientState(ingredientName, out CupContentState state))
                {
                    requiredStates.Add(state);
                }
                else
                {
                    Debug.LogWarning($"{nameof(CookingMiniGameController)}: Unknown recipe ingredient '{ingredientName}' in menu '{menuName}'.");
                }
            }

            if (!ArrowCommandsByMenu.TryGetValue(menuName, out string arrowCommand))
            {
                Debug.LogWarning($"{nameof(CookingMiniGameController)}: Arrow command is not configured for menu '{menuName}'.");
                arrowCommand = string.Empty;
            }

            recipes.Add(new CookingRecipe(menuName, requiredStates, arrowCommand));
        }

        Debug.Log($"{nameof(CookingMiniGameController)} loaded {recipes.Count} recipes.");
    }

    private bool TryParseIngredientState(string ingredientName, out CupContentState state)
    {
        string normalized = ingredientName.Replace(" ", string.Empty)
            .Replace("_", string.Empty)
            .Replace("-", string.Empty)
            .ToLowerInvariant();

        switch (normalized)
        {
            case "icetea":
            case "iceteaed":
                state = CupContentState.IceTeaEd;
                return true;
            case "waterpot":
            case "waterpotted":
                state = CupContentState.WaterPotEd;
                return true;
            case "icemachine":
            case "icemachineed":
                state = CupContentState.IceMachineEd;
                return true;
            case "coffeemachine":
            case "coffeemachineed":
                state = CupContentState.CoffeeMachineEd;
                return true;
            case "syrup":
            case "syruped":
                state = CupContentState.SyrupEd;
                return true;
            default:
                state = CupContentState.Normal;
                return false;
        }
    }

    private HashSet<CupContentState> BuildStateSet(IReadOnlyList<CupContentState> sourceStates)
    {
        HashSet<CupContentState> stateSet = new HashSet<CupContentState>();
        if (sourceStates == null)
        {
            return stateSet;
        }

        for (int i = 0; i < sourceStates.Count; i++)
        {
            if (sourceStates[i] != CupContentState.Normal)
            {
                stateSet.Add(sourceStates[i]);
            }
        }

        return stateSet;
    }

    private char? GetPressedArrow()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return null;
        }

        if (keyboard.upArrowKey.wasPressedThisFrame)
        {
            return UpArrow;
        }

        if (keyboard.downArrowKey.wasPressedThisFrame)
        {
            return DownArrow;
        }

        if (keyboard.leftArrowKey.wasPressedThisFrame)
        {
            return LeftArrow;
        }

        if (keyboard.rightArrowKey.wasPressedThisFrame)
        {
            return RightArrow;
        }

        return null;
    }

    private bool TryHandleEscapeExit()
    {
        if (!escapeExitEnabled)
        {
            return false;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame)
        {
            return false;
        }

        HideCookingScreen();
        Debug.Log("CookingSprite closed by ESC.");
        return true;
    }

    private void RenderArrowCommand()
    {
        if (arrowCmdText == null)
        {
            return;
        }

        arrowCmdText.richText = true;

        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < currentCommand.Length; i++)
        {
            Color color = pendingColor;
            if (i < inputIndex)
            {
                color = correctColor;
            }
            else if (i == wrongIndex)
            {
                color = wrongColor;
            }

            builder.Append("<color=#");
            builder.Append(ColorUtility.ToHtmlStringRGB(color));
            builder.Append('>');
            builder.Append(currentCommand[i]);
            builder.Append("</color>");
        }

        arrowCmdText.text = builder.ToString();
    }

    private void EnsureInitialized()
    {
        if (initialized)
        {
            return;
        }

        initialized = true;

        if (cookingSpriteRoot == null)
        {
            cookingSpriteRoot = gameObject;
        }

        if (arrowCmdText == null)
        {
            TMP_Text[] childTexts = GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < childTexts.Length; i++)
            {
                if (childTexts[i].name == "ArrowCmd")
                {
                    arrowCmdText = childTexts[i];
                    break;
                }
            }
        }

        if (errorText == null)
        {
            TMP_Text[] allTexts = Resources.FindObjectsOfTypeAll<TMP_Text>();
            for (int i = 0; i < allTexts.Length; i++)
            {
                if (allTexts[i].name == "ERROR" && allTexts[i].gameObject.scene.IsValid())
                {
                    errorText = allTexts[i];
                    break;
                }
            }
        }

        PrepareErrorText();
        LoadRecipes();
    }

    private void PrepareErrorText()
    {
        if (errorText == null)
        {
            return;
        }

        errorText.raycastTarget = false;

        if (!hasCapturedErrorVisuals)
        {
            hasCapturedErrorVisuals = true;
            originalErrorScale = errorText.transform.localScale;
            originalErrorColor = errorText.color;
        }
    }

    private void ShowRecipeErrorAnimation()
    {
        if (errorText == null)
        {
            return;
        }

        PrepareErrorText();
        errorTween?.Kill();

        errorText.gameObject.SetActive(true);
        errorText.transform.localScale = originalErrorScale * Mathf.Max(0.01f, errorBounceStartScale);
        SetErrorAlpha(0f);

        Sequence sequence = DOTween.Sequence();
        sequence.Join(errorText.transform
            .DOScale(originalErrorScale, GetAdjustedErrorDuration(errorShowDuration))
            .SetEase(Ease.OutBounce));
        sequence.Join(DOTween.To(GetErrorAlpha, SetErrorAlpha, originalErrorColor.a, GetAdjustedErrorDuration(errorShowDuration * 0.65f))
            .SetEase(Ease.OutQuad));

        if (autoHideError)
        {
            sequence.AppendInterval(GetAdjustedErrorDuration(errorVisibleDuration));
            sequence.Append(DOTween.To(GetErrorAlpha, SetErrorAlpha, 0f, GetAdjustedErrorDuration(errorFadeDuration))
                .SetEase(Ease.OutQuad));
            sequence.OnComplete(() => errorText.gameObject.SetActive(false));
        }

        errorTween = sequence;
    }

    private void HideRecipeError(bool animated)
    {
        if (errorText == null)
        {
            return;
        }

        PrepareErrorText();
        errorTween?.Kill();

        if (!errorText.gameObject.activeSelf)
        {
            SetErrorAlpha(0f);
            return;
        }

        if (!animated)
        {
            SetErrorAlpha(0f);
            errorText.transform.localScale = originalErrorScale;
            errorText.gameObject.SetActive(false);
            return;
        }

        errorTween = DOTween.To(GetErrorAlpha, SetErrorAlpha, 0f, GetAdjustedErrorDuration(errorFadeDuration))
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                errorText.transform.localScale = originalErrorScale;
                errorText.gameObject.SetActive(false);
            });
    }

    private float GetAdjustedErrorDuration(float duration)
    {
        return Mathf.Max(0f, duration) / Mathf.Max(0.01f, errorAnimationSpeed);
    }

    private float GetErrorAlpha()
    {
        return errorText != null ? errorText.color.a : 0f;
    }

    private void SetErrorAlpha(float alpha)
    {
        if (errorText == null)
        {
            return;
        }

        Color color = errorText.color;
        color.a = alpha;
        errorText.color = color;
    }

    private string FormatStates(IReadOnlyCollection<CupContentState> cupStates)
    {
        if (cupStates == null || cupStates.Count == 0)
        {
            return CupContentState.Normal.ToString();
        }

        return string.Join(", ", cupStates);
    }
}
