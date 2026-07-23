using System;
using System.Collections.Generic;
using System.Text;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[Serializable]
public class MenuArrowCommandSetting
{
    [SerializeField] private string menuName;
    [SerializeField] private string commandSequence;

    public string MenuName => menuName;
    public string CommandSequence => commandSequence;
}

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
    private const char UpArrow = '\u2191';
    private const char DownArrow = '\u2193';
    private const char LeftArrow = '\u2190';
    private const char RightArrow = '\u2192';

    public static bool IsCookingMiniGameOpen { get; private set; }

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
    [SerializeField] private TMP_Text debugSuccessCountText;
    [SerializeField] private CookingMenuCommandManager menuCommandManager;
    [SerializeField] private Image timerImage;
    [SerializeField] private RectTransform timerRect;
    [SerializeField] private float cookingTimeLimit = 10f;
    [SerializeField] private Color timerStartColor = Color.green;
    [SerializeField] private Color timerEndColor = Color.red;
    [Header("Virgin Ghost Command Shake")]
    [SerializeField, Min(0f)] private float virginGhostCommandShakeAmplitude = 6f;
    [SerializeField, Min(0f)] private float virginGhostCommandShakeSpeed = 26f;
    [SerializeField, Min(0f)] private float virginGhostCommandShakeHorizontalRange = 14f;
    [SerializeField, Min(0f)] private float virginGhostCommandShakeVerticalRange = 14f;
    [SerializeField, Range(0f, 1f)] private float virginGhostCommandShakeRandomness = 0.75f;

    private readonly List<CookingRecipe> recipes = new List<CookingRecipe>();
    private bool initialized;
    private bool recipesLoaded;
    private string currentBaseCommand = string.Empty;
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
    private CupDragController activeCookingCup;
    private bool cookingResultApplied;
    private bool dokkaebiShieldAvailable;
    private int dokkaebiShieldedIndex = -1;
    private int successCount;
    private float remainingCookingTime;
    private bool timerRunning;
    private float originalTimerWidth;
    private RectTransform arrowCmdRect;
    private Vector2 originalArrowCmdAnchoredPosition;
    private bool hasCapturedArrowCmdPosition;
    private bool arrowCmdMeshWasShaken;
    private bool lastVirginGhostBuffActive;
    private int nextCookingComboNumber = 1;

    private void Awake()
    {
        EnsureInitialized();
        if (!hasCookingAttemptStarted)
        {
            HideCookingScreen();
        }
    }

    private void LateUpdate()
    {
        UpdateArrowCmdCharacterShake();
    }

    private void Update()
    {
        if (TryHandleEscapeExit())
        {
            return;
        }

        RefreshCommandBuffState();

        if (!arrowInputEnabled || string.IsNullOrEmpty(currentCommand))
        {
            return;
        }

        if (cookingSpriteRoot != null && !cookingSpriteRoot.activeInHierarchy)
        {
            return;
        }

        if (timerRunning)
        {
            UpdateCookingTimer();
            if (!arrowInputEnabled)
            {
                return;
            }
        }

        char? pressedCommand = GetPressedCommand();
        if (!pressedCommand.HasValue)
        {
            return;
        }

        if (pressedCommand.Value == currentCommand[inputIndex])
        {
            wrongIndex = -1;
            PlayCookingComboSfx(nextCookingComboNumber);
            nextCookingComboNumber++;
            inputIndex++;

            if (inputIndex >= currentCommand.Length)
            {
                commandComplete = true;
                arrowInputEnabled = false;
                timerRunning = false;
                escapeExitEnabled = true;
                ApplyCookingResult(true);
                Debug.Log($"Cooking command complete: {currentMenuName}");
            }
        }
        else
        {
            if (BuffDebuffManager.CommandMistakeShieldBuffActive && dokkaebiShieldAvailable)
            {
                dokkaebiShieldAvailable = false;
                dokkaebiShieldedIndex = inputIndex;
                wrongIndex = -1;
                ResetCookingComboSfx();
                inputIndex++;

                if (inputIndex >= currentCommand.Length)
                {
                    commandComplete = true;
                    arrowInputEnabled = false;
                    timerRunning = false;
                    escapeExitEnabled = true;
                    ApplyCookingResult(true);
                    Debug.Log($"Cooking command complete with command shield: {currentMenuName}");
                }

                RenderArrowCommand();
                Debug.Log($"Command shield blocked wrong input: {currentMenuName}");
                return;
            }

            wrongIndex = inputIndex;
            ResetCookingComboSfx();
            arrowInputEnabled = false;
            timerRunning = false;
            escapeExitEnabled = true;
            ApplyCookingResult(false);
            Debug.Log($"Cooking command failed: {currentMenuName}");
        }

        RenderArrowCommand();
    }

    private void PlayCookingComboSfx(int comboNumber)
    {
        if (SoundManager.Instance == null || SoundManager.Instance.SFX == null)
        {
            return;
        }

        SoundManager.Instance.SFX.PlayCookingCombo(comboNumber);
    }

    private void ResetCookingComboSfx()
    {
        nextCookingComboNumber = 1;
    }

    public bool TryStartCooking(CupDragController cup)
    {
        EnsureInitialized();

        if (cup == null)
        {
            Debug.LogWarning($"{nameof(CookingMiniGameController)}: Cooking cup is not assigned.");
            return false;
        }

        IReadOnlyList<CupContentState> cupStates = cup.ContentStates;
        if (!TryFindMatchingRecipe(cupStates, out CookingRecipe recipe))
        {
            ShowRecipeError(cupStates);
            Debug.Log($"Cooking recipe mismatch: {cup.name} states [{FormatStates(cupStates)}]");
            return false;
        }

        StartCooking(recipe, cup, cupStates);
        return true;
    }

    private void StartCooking(CookingRecipe recipe, CupDragController cup, IReadOnlyList<CupContentState> cupStates)
    {
        IsCookingMiniGameOpen = true;
        hasCookingAttemptStarted = true;
        activeCookingCup = cup;
        cookingResultApplied = false;
        currentMenuName = recipe.MenuName;
        currentBaseCommand = GetBaseCommandForCooking(recipe);
        lastVirginGhostBuffActive = BuffDebuffManager.VirginGhostBuffActive;
        currentCommand = BuffDebuffManager.ApplyVirginGhostCommandReduction(currentBaseCommand);
        inputIndex = 0;
        ResetCookingComboSfx();
        wrongIndex = -1;
        dokkaebiShieldAvailable = BuffDebuffManager.CommandMistakeShieldBuffActive;
        dokkaebiShieldedIndex = -1;
        commandComplete = string.IsNullOrEmpty(currentCommand);
        arrowInputEnabled = !commandComplete;
        escapeExitEnabled = commandComplete;
        StartCookingTimer();

        HideRecipeError(true);

        if (cookingSpriteRoot != null)
        {
            cookingSpriteRoot.SetActive(true);
        }

        RenderArrowCommand();
        Debug.Log($"CookingSprite opened for {recipe.MenuName} with {cup.name} states [{FormatStates(cupStates)}]");
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
        IsCookingMiniGameOpen = false;
        if (arrowCmdText != null)
        {
            arrowCmdText.text = string.Empty;
        }

        currentMenuName = string.Empty;
        currentBaseCommand = string.Empty;
        currentCommand = string.Empty;
        inputIndex = 0;
        ResetCookingComboSfx();
        wrongIndex = -1;
        commandComplete = false;
        arrowInputEnabled = false;
        escapeExitEnabled = false;
        timerRunning = false;
        activeCookingCup = null;
        cookingResultApplied = false;
        dokkaebiShieldAvailable = false;
        dokkaebiShieldedIndex = -1;
        ResetTimerVisual();
        ResetArrowCmdCharacterShake();

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

            if (!TryGetArrowCommand(menuName, out string arrowCommand))
            {
                Debug.LogWarning($"{nameof(CookingMiniGameController)}: Arrow command is not configured for menu '{menuName}'.");
                arrowCommand = string.Empty;
            }

            recipes.Add(new CookingRecipe(menuName, requiredStates, arrowCommand));
        }

        Debug.Log($"{nameof(CookingMiniGameController)} loaded {recipes.Count} recipes.");
    }

    private bool TryGetArrowCommand(string menuName, out string arrowCommand)
    {
        if (menuCommandManager == null)
        {
            menuCommandManager = FindMenuCommandManager();
        }

        if (menuCommandManager != null && menuCommandManager.TryGetCommandSequence(menuName, out string commandSequence))
        {
            arrowCommand = ConvertCommandSequence(commandSequence, menuName);
            return !string.IsNullOrEmpty(arrowCommand);
        }

        arrowCommand = string.Empty;
        return false;
    }

    private CookingMenuCommandManager FindMenuCommandManager()
    {
        CookingMenuCommandManager[] managers = Resources.FindObjectsOfTypeAll<CookingMenuCommandManager>();
        for (int i = 0; i < managers.Length; i++)
        {
            if (managers[i] != null && managers[i].gameObject.scene.IsValid())
            {
                return managers[i];
            }
        }

        return null;
    }

    private string ConvertCommandSequence(string commandSequence, string menuName)
    {
        if (string.IsNullOrWhiteSpace(commandSequence))
        {
            return string.Empty;
        }

        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < commandSequence.Length; i++)
        {
            char rawCommand = commandSequence[i];
            if (char.IsLower(rawCommand) && rawCommand >= 'a' && rawCommand <= 'z')
            {
                builder.Append(char.ToUpperInvariant(rawCommand));
                continue;
            }

            switch (rawCommand)
            {
                case 'U':
                    builder.Append(UpArrow);
                    break;
                case 'D':
                    builder.Append(DownArrow);
                    break;
                case 'L':
                    builder.Append(LeftArrow);
                    break;
                case 'R':
                    builder.Append(RightArrow);
                    break;
                case ' ':
                case '\t':
                case '-':
                case ',':
                case '/':
                    break;
                default:
                    if (char.IsUpper(rawCommand) && rawCommand >= 'A' && rawCommand <= 'Z')
                    {
                        builder.Append(rawCommand);
                    }
                    else
                    {
                        Debug.LogWarning($"{nameof(CookingMiniGameController)}: Invalid command '{commandSequence[i]}' in menu '{menuName}'. Use uppercase U/D/L/R for arrows or lowercase letters for keyboard input.");
                    }

                    break;
            }
        }

        return builder.ToString();
    }

    private string GetBaseCommandForCooking(CookingRecipe recipe)
    {
        if (!BuffDebuffManager.DokkaebiDebuffActive || recipes.Count == 0)
        {
            return recipe.ArrowCommand;
        }

        CookingRecipe randomRecipe = recipes[UnityEngine.Random.Range(0, recipes.Count)];
        return randomRecipe != null ? randomRecipe.ArrowCommand : recipe.ArrowCommand;
    }

    private void RefreshCommandBuffState()
    {
        bool currentVirginGhostBuffActive = BuffDebuffManager.VirginGhostBuffActive;
        if (currentVirginGhostBuffActive == lastVirginGhostBuffActive)
        {
            return;
        }

        lastVirginGhostBuffActive = currentVirginGhostBuffActive;
        if (string.IsNullOrEmpty(currentBaseCommand))
        {
            return;
        }

        currentCommand = BuffDebuffManager.ApplyVirginGhostCommandReduction(currentBaseCommand);
        inputIndex = Mathf.Clamp(inputIndex, 0, currentCommand.Length);
        wrongIndex = -1;
        commandComplete = string.IsNullOrEmpty(currentCommand) || inputIndex >= currentCommand.Length;
        arrowInputEnabled = !commandComplete;
        escapeExitEnabled = commandComplete;
        RenderArrowCommand();
        Debug.Log($"Virgin ghost buff command refresh: {currentMenuName} {currentBaseCommand.Length}->{currentCommand.Length}");
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

    private char? GetPressedCommand()
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

        for (char command = 'A'; command <= 'Z'; command++)
        {
            if (WasLetterPressed(keyboard, command))
            {
                return command;
            }
        }

        return null;
    }

    private bool WasLetterPressed(Keyboard keyboard, char command)
    {
        if (keyboard == null || command < 'A' || command > 'Z')
        {
            return false;
        }

        string keyName = command.ToString();
        if (!Enum.TryParse(keyName, out Key key))
        {
            return false;
        }

        return keyboard[key].wasPressedThisFrame;
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
            if (i == dokkaebiShieldedIndex)
            {
                color = BuffDebuffManager.CommandMistakeShieldTextColor;
            }
            else if (i < inputIndex)
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

        if (arrowCmdText == null)
        {
            TMP_Text[] allTexts = Resources.FindObjectsOfTypeAll<TMP_Text>();
            for (int i = 0; i < allTexts.Length; i++)
            {
                if (allTexts[i].name == "ArrowCmd" && allTexts[i].gameObject.scene.IsValid())
                {
                    arrowCmdText = allTexts[i];
                    break;
                }
            }
        }

        if (arrowCmdText != null)
        {
            arrowCmdRect = arrowCmdText.rectTransform;
            CaptureArrowCmdPosition();
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

        if (debugSuccessCountText == null)
        {
            TMP_Text[] allTexts = Resources.FindObjectsOfTypeAll<TMP_Text>();
            for (int i = 0; i < allTexts.Length; i++)
            {
                if (allTexts[i].name == "DEBUG_Success_cnt" && allTexts[i].gameObject.scene.IsValid())
                {
                    debugSuccessCountText = allTexts[i];
                    break;
                }
            }
        }

        if (timerImage == null)
        {
            Image[] childImages = GetComponentsInChildren<Image>(true);
            for (int i = 0; i < childImages.Length; i++)
            {
                if (childImages[i].name == "timer")
                {
                    timerImage = childImages[i];
                    break;
                }
            }
        }

        if (timerImage == null)
        {
            Image[] allImages = Resources.FindObjectsOfTypeAll<Image>();
            for (int i = 0; i < allImages.Length; i++)
            {
                if (allImages[i].name == "timer" && allImages[i].gameObject.scene.IsValid())
                {
                    timerImage = allImages[i];
                    break;
                }
            }
        }

        if (timerRect == null && timerImage != null)
        {
            timerRect = timerImage.rectTransform;
        }

        if (timerRect == null)
        {
            RectTransform[] childRects = GetComponentsInChildren<RectTransform>(true);
            for (int i = 0; i < childRects.Length; i++)
            {
                if (childRects[i].name == "timer")
                {
                    timerRect = childRects[i];
                    break;
                }
            }
        }

        if (timerRect == null)
        {
            RectTransform[] allRects = Resources.FindObjectsOfTypeAll<RectTransform>();
            for (int i = 0; i < allRects.Length; i++)
            {
                if (allRects[i].name == "timer" && allRects[i].gameObject.scene.IsValid())
                {
                    timerRect = allRects[i];
                    break;
                }
            }
        }

        if (timerRect != null)
        {
            originalTimerWidth = timerRect.rect.width;
            if (originalTimerWidth <= 0f)
            {
                originalTimerWidth = timerRect.sizeDelta.x;
            }
        }

        InitializeSuccessCount();
        PrepareErrorText();
        ResetTimerVisual();
        LoadRecipes();
    }

    private void ApplyCookingResult(bool succeeded)
    {
        if (cookingResultApplied)
        {
            return;
        }

        cookingResultApplied = true;

        if (succeeded)
        {
            successCount++;
            UpdateSuccessCountText();
        }

        if (activeCookingCup != null)
        {
            activeCookingCup.ShowCookingResult(succeeded, currentMenuName);
        }
    }

    private void InitializeSuccessCount()
    {
        if (debugSuccessCountText == null)
        {
            return;
        }

        if (!int.TryParse(debugSuccessCountText.text.Trim(), out successCount))
        {
            successCount = 0;
        }

        UpdateSuccessCountText();
    }

    private void UpdateSuccessCountText()
    {
        if (debugSuccessCountText != null)
        {
            debugSuccessCountText.text = successCount.ToString();
        }
    }

    private void StartCookingTimer()
    {
        remainingCookingTime = Mathf.Max(0.01f, cookingTimeLimit);
        timerRunning = arrowInputEnabled;
        ResetTimerVisual();
    }

    private void UpdateCookingTimer()
    {
        remainingCookingTime -= Time.deltaTime * BuffDebuffManager.CookingTimerDecreaseMultiplier;

        float safeTimeLimit = Mathf.Max(0.01f, cookingTimeLimit);
        float elapsedRatio = Mathf.Clamp01(1f - remainingCookingTime / safeTimeLimit);
        SetTimerVisual(elapsedRatio);

        if (remainingCookingTime > 0f)
        {
            return;
        }

        timerRunning = false;
        arrowInputEnabled = false;
        escapeExitEnabled = true;

        if (!string.IsNullOrEmpty(currentCommand))
        {
            wrongIndex = Mathf.Clamp(inputIndex, 0, currentCommand.Length - 1);
        }

        ApplyCookingResult(false);
        RenderArrowCommand();
        Debug.Log($"Cooking command timed out: {currentMenuName}");
    }

    private void ResetTimerVisual()
    {
        remainingCookingTime = Mathf.Max(0.01f, cookingTimeLimit);
        SetTimerVisual(0f);
    }

    private void SetTimerVisual(float elapsedRatio)
    {
        if (timerImage == null)
        {
            return;
        }

        float clampedRatio = Mathf.Clamp01(elapsedRatio);
        timerImage.color = Color.Lerp(timerStartColor, timerEndColor, clampedRatio);

        if (timerRect != null && originalTimerWidth > 0f)
        {
            timerRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, originalTimerWidth * (1f - clampedRatio));
        }
        else if (timerImage.type == Image.Type.Filled)
        {
            timerImage.fillAmount = 1f - clampedRatio;
        }
    }

    private void UpdateArrowCmdCharacterShake()
    {
        if (!BuffDebuffManager.VirginGhostDebuffActive || arrowCmdText == null)
        {
            ResetArrowCmdCharacterShake();
            return;
        }

        if (cookingSpriteRoot != null && !cookingSpriteRoot.activeInHierarchy)
        {
            ResetArrowCmdCharacterShake();
            return;
        }

        CaptureArrowCmdPosition();
        if (arrowCmdRect != null)
        {
            arrowCmdRect.anchoredPosition = originalArrowCmdAnchoredPosition;
        }

        arrowCmdText.ForceMeshUpdate();

        TMP_TextInfo textInfo = arrowCmdText.textInfo;
        float intensity = virginGhostCommandShakeAmplitude;
        float speed = virginGhostCommandShakeSpeed;
        for (int i = 0; i < textInfo.characterCount; i++)
        {
            TMP_CharacterInfo characterInfo = textInfo.characterInfo[i];
            if (!characterInfo.isVisible)
            {
                continue;
            }

            int materialIndex = characterInfo.materialReferenceIndex;
            int vertexIndex = characterInfo.vertexIndex;
            Vector3[] vertices = textInfo.meshInfo[materialIndex].vertices;
            Vector3 offset = GetVirginGhostCharacterShakeOffset(i, speed, intensity);

            vertices[vertexIndex] += offset;
            vertices[vertexIndex + 1] += offset;
            vertices[vertexIndex + 2] += offset;
            vertices[vertexIndex + 3] += offset;
        }

        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            textInfo.meshInfo[i].mesh.vertices = textInfo.meshInfo[i].vertices;
            arrowCmdText.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
        }

        arrowCmdMeshWasShaken = true;
    }

    private Vector3 GetVirginGhostCharacterShakeOffset(int characterIndex, float speed, float intensity)
    {
        float seed = characterIndex * 37.719f + 11.37f;
        float randomRatio = Mathf.Clamp01(virginGhostCommandShakeRandomness);
        float time = Time.time * Mathf.Max(0f, speed);
        float xSpeed = Mathf.Lerp(1f, 0.47f + Hash01(seed + 1.13f) * 1.46f, randomRatio);
        float ySpeed = Mathf.Lerp(1f, 0.51f + Hash01(seed + 2.71f) * 1.58f, randomRatio);
        float xRange = virginGhostCommandShakeHorizontalRange * Mathf.Lerp(1f, 0.45f + Hash01(seed + 4.29f) * 1.35f, randomRatio);
        float yRange = virginGhostCommandShakeVerticalRange * Mathf.Lerp(1f, 0.45f + Hash01(seed + 8.61f) * 1.35f, randomRatio);

        float x = (Mathf.PerlinNoise(seed, time * xSpeed) - 0.5f) * 2f * xRange;
        float y = (Mathf.PerlinNoise(seed + 100f, time * ySpeed) - 0.5f) * 2f * yRange;
        return new Vector3(x, y, 0f) * (Mathf.Max(0f, intensity) / 6f);
    }

    private static float Hash01(float value)
    {
        return Mathf.Repeat(Mathf.Sin(value * 12.9898f) * 43758.5453f, 1f);
    }

    private void CaptureArrowCmdPosition()
    {
        if (hasCapturedArrowCmdPosition || arrowCmdRect == null)
        {
            return;
        }

        hasCapturedArrowCmdPosition = true;
        originalArrowCmdAnchoredPosition = arrowCmdRect.anchoredPosition;
    }

    private void ResetArrowCmdCharacterShake()
    {
        if (arrowCmdRect != null && hasCapturedArrowCmdPosition)
        {
            arrowCmdRect.anchoredPosition = originalArrowCmdAnchoredPosition;
        }

        if (arrowCmdMeshWasShaken && arrowCmdText != null)
        {
            arrowCmdText.ForceMeshUpdate();
            arrowCmdMeshWasShaken = false;
        }
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
