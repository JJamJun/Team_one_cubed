using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeMenuRuntimeBuilder : MonoBehaviour
{
    [SerializeField] private string recipeResourcePath = "menu_info";
    [SerializeField] private string unlockSaveFileName = "menu_progress.json";
    [SerializeField] private string walletSaveFileName = "player_wallet.json";
    [SerializeField] private RectTransform contentRoot;
    [SerializeField] private GameObject upgradeButtonPrefab;
    [Header("Menu Sprite Manager")]
    [SerializeField] private Sprite[] menuSprites = new Sprite[0];
    [SerializeField, Min(0f), InspectorName("\uD574\uAE08 \uC2DC\uAC04")] private float unlockAnimationDuration = 1f;
    [SerializeField, Min(0f)] private float warningFadeInDuration = 0.35f;
    [SerializeField, Min(0f)] private float warningVisibleDuration = 0.35f;
    [SerializeField, Min(0f)] private float warningFadeOutDuration = 0.85f;
    [SerializeField, Min(0f), InspectorName("\uBBF8\uD574\uAE08 \uB4F1\uC7A5\uC2DC\uAC04")] private float revealedButtonFadeInDuration = 0.6f;
    [SerializeField, Min(0f)] private float resourceIconTextGap = 8f;
    [SerializeField, Min(0)] private int maxDisplayedAmount = 999999;
    [SerializeField, Min(0f)] private float buttonSpacing = 95f;
    [SerializeField] private string generatedButtonPrefix = "UpgradeButton_";
    [SerializeField] private string debugCoinButtonName = "\uB514\uBC84\uADF8_coin_999";
    [SerializeField] private string debugSoulButtonName = "\uB514\uBC84\uADF8_soul_999";
    [SerializeField] private string debugResetButtonName = "\uB514\uBC84\uADF8_reset";

    private readonly Dictionary<Transform, Vector3> warningOriginalScales = new Dictionary<Transform, Vector3>();
    private readonly Dictionary<Transform, Vector3> upgradeOriginalScales = new Dictionary<Transform, Vector3>();

    private void Awake()
    {
        BindDebugButtons();
        BuildButtons();
    }

    public void BuildButtons()
    {
        if (contentRoot == null)
        {
            Debug.LogWarning($"{nameof(UpgradeMenuRuntimeBuilder)}: Content root is not assigned.");
            return;
        }

        if (upgradeButtonPrefab == null)
        {
            Debug.LogWarning($"{nameof(UpgradeMenuRuntimeBuilder)}: Upgrade button prefab is not assigned.");
            return;
        }

        ApplyButtonSpacing();
        ClearExistingButtons();

        List<UpgradeMenuEntry> entries = LoadUpgradeEntries();
        MenuUnlockSaveData unlockSaveData = LoadOrCreateUnlockSaveData(entries);
        for (int i = 0; i < entries.Count; i++)
        {
            UpgradeMenuEntry entry = entries[i];
            MenuUnlockState state = unlockSaveData.GetOrCreate(entry.MenuId);
            bool isUnlocked = state.isUnlocked;
            if (!isUnlocked && !IsLockedMenuRevealed(entries, unlockSaveData, i))
            {
                break;
            }

            CreateButton(entry, i, state, false);

            if (!isUnlocked)
            {
                break;
            }
        }
    }

    public void UnlockMenu(string menuId)
    {
        if (string.IsNullOrWhiteSpace(menuId))
        {
            return;
        }

        List<UpgradeMenuEntry> entries = LoadUpgradeEntries();
        MenuUnlockSaveData unlockSaveData = LoadOrCreateUnlockSaveData(entries);
        unlockSaveData.SetUnlocked(menuId, true, 1);
        SaveUnlockData(unlockSaveData);
        BuildButtons();
    }

    public void SetDebugCoinMax()
    {
        PlayerWalletSaveData walletSaveData = LoadWalletData();
        walletSaveData.coin = maxDisplayedAmount;
        SaveWalletData(walletSaveData);
        Debug.Log($"{nameof(UpgradeMenuRuntimeBuilder)}: coin set to {maxDisplayedAmount}.");
    }

    public void SetDebugSoulMax()
    {
        PlayerWalletSaveData walletSaveData = LoadWalletData();
        walletSaveData.soul = maxDisplayedAmount;
        SaveWalletData(walletSaveData);
        Debug.Log($"{nameof(UpgradeMenuRuntimeBuilder)}: soul set to {maxDisplayedAmount}.");
    }

    public void ResetDebugSaveData()
    {
        List<UpgradeMenuEntry> entries = LoadUpgradeEntries();
        SaveWalletData(new PlayerWalletSaveData());
        SaveUnlockData(CreateDefaultUnlockSaveData(entries));
        BuildButtons();
        Debug.Log($"{nameof(UpgradeMenuRuntimeBuilder)}: wallet and unlock save data reset.");
    }

    private void OnValidate()
    {
        ApplyButtonSpacing();
    }

    private void ApplyButtonSpacing()
    {
        if (contentRoot == null)
        {
            return;
        }

        GridLayoutGroup layoutGroup = contentRoot.GetComponent<GridLayoutGroup>();
        if (layoutGroup == null)
        {
            return;
        }

        layoutGroup.spacing = new Vector2(buttonSpacing, layoutGroup.spacing.y);
    }

    private void ClearExistingButtons()
    {
        for (int i = contentRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = contentRoot.GetChild(i);
            if (child.name.StartsWith(generatedButtonPrefix, StringComparison.Ordinal))
            {
                Destroy(child.gameObject);
            }
        }
    }

    private GameObject CreateButton(UpgradeMenuEntry entry, int spriteIndex, MenuUnlockState state, bool playFadeIn)
    {
        GameObject buttonObject = Instantiate(upgradeButtonPrefab, contentRoot);
        buttonObject.name = $"{generatedButtonPrefix}{entry.MenuId}";
        buttonObject.SetActive(false);

        SetText(buttonObject.transform, "MenuName", entry.MenuName);
        SetText(buttonObject.transform, "Level", state.level.ToString(CultureInfo.InvariantCulture));
        SetResourceText(buttonObject.transform, "Price", "price_image", entry.GetSalePrice(state.level).ToString(CultureInfo.InvariantCulture));
        SetText(buttonObject.transform, "Req", GetUpgradeRequirementText(entry, state));
        SetResourceText(buttonObject.transform, "cost", "cost_image", GetDiscountedUnlockPrice(entry).ToString(CultureInfo.InvariantCulture), "UpgradeCostText");
        SetOptionalObjectActive(buttonObject.transform, "soul", false);
        AlignUnlockedInfoTextPairs(buttonObject.transform);
        SetMenuSprite(buttonObject.transform, spriteIndex);
        ApplyUnlockViewState(buttonObject.transform, state.isUnlocked);
        SetButtonInteractable(buttonObject, state.isUnlocked);
        BindUpgradeButton(buttonObject, entry, state.isUnlocked);

        CanvasGroup canvasGroup = GetOrAddCanvasGroup(buttonObject.transform);
        canvasGroup.alpha = playFadeIn ? 0f : 1f;
        buttonObject.SetActive(true);

        if (playFadeIn)
        {
            PlayRevealedButtonFadeIn(buttonObject);
        }

        return buttonObject;
    }

    private void RevealNextLockedButton(List<UpgradeMenuEntry> entries, MenuUnlockSaveData unlockSaveData, string menuId)
    {
        if (string.IsNullOrEmpty(menuId) || FindExistingGeneratedButton(menuId) != null)
        {
            return;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            UpgradeMenuEntry entry = entries[i];
            if (entry.MenuId == menuId && !unlockSaveData.IsUnlocked(entry.MenuId) && IsLockedMenuRevealed(entries, unlockSaveData, i))
            {
                CreateButton(entry, i, unlockSaveData.GetOrCreate(entry.MenuId), true);
                return;
            }
        }
    }

    private Transform FindExistingGeneratedButton(string menuId)
    {
        if (contentRoot == null)
        {
            return null;
        }

        return contentRoot.Find($"{generatedButtonPrefix}{menuId}");
    }

    private void BindUpgradeButton(GameObject buttonObject, UpgradeMenuEntry entry, bool isUnlocked)
    {
        Button button = buttonObject.GetComponent<Button>();
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => TryPurchaseOrUpgradeMenu(entry, buttonObject));
    }

    private void TryPurchaseOrUpgradeMenu(UpgradeMenuEntry entry, GameObject buttonObject)
    {
        List<UpgradeMenuEntry> entries = LoadUpgradeEntries();
        MenuUnlockSaveData unlockSaveData = LoadOrCreateUnlockSaveData(entries);
        MenuUnlockState state = unlockSaveData.GetOrCreate(entry.MenuId);
        if (state.isUnlocked)
        {
            TryUpgradeMenu(entry, state, buttonObject, unlockSaveData);
            return;
        }

        if (!IsLockedMenuPurchasable(entries, unlockSaveData, entry.MenuId))
        {
            PlayWarningAnimation(buttonObject.transform, "ReqWARNING");
            Debug.Log($"{nameof(UpgradeMenuRuntimeBuilder)}: {entry.MenuId} is revealed but cannot be purchased yet.");
            return;
        }

        PlayerWalletSaveData walletSaveData = LoadWalletData();
        int unlockPrice = GetDiscountedUnlockPrice(entry);
        if (walletSaveData.coin < unlockPrice)
        {
            PlayWarningAnimation(buttonObject.transform, "MoneyWARNING");
            Debug.Log($"{nameof(UpgradeMenuRuntimeBuilder)}: Not enough currency to unlock {entry.MenuId}.");
            return;
        }

        if (!TrySpendCoins(walletSaveData, unlockPrice))
        {
            PlayWarningAnimation(buttonObject.transform, "MoneyWARNING");
            Debug.Log($"{nameof(UpgradeMenuRuntimeBuilder)}: Not enough currency to unlock {entry.MenuId}.");
            return;
        }

        unlockSaveData.SetUnlocked(entry.MenuId, true, 1);
        SaveUnlockData(unlockSaveData);
        MenuUnlockState unlockedState = unlockSaveData.GetOrCreate(entry.MenuId);
        SetText(buttonObject.transform, "Level", unlockedState.level.ToString(CultureInfo.InvariantCulture));
        SetResourceText(buttonObject.transform, "Price", "price_image", entry.GetSalePrice(unlockedState.level).ToString(CultureInfo.InvariantCulture));
        SetText(buttonObject.transform, "Req", GetUpgradeRequirementText(entry, unlockedState));
        AlignUnlockedInfoTextPairs(buttonObject.transform);
        string nextLockedMenuId = FindFirstLockedMenuId(entries, unlockSaveData);

        Button button = buttonObject.GetComponent<Button>();
        if (button != null)
        {
            button.interactable = false;
        }

        PlayUnlockAnimation(buttonObject.transform, () => RevealNextLockedButton(entries, unlockSaveData, nextLockedMenuId));
    }

    private void TryUpgradeMenu(UpgradeMenuEntry entry, MenuUnlockState state, GameObject buttonObject, MenuUnlockSaveData unlockSaveData)
    {
        if (state.level >= UpgradeMenuEntry.MaxLevel)
        {
            PlayWarningAnimation(buttonObject.transform, "LvlWARNING");
            return;
        }

        int upgradeCost = entry.GetUpgradeCostToNextLevel(state.level);
        PlayerWalletSaveData walletSaveData = LoadWalletData();
        if (walletSaveData.coin < upgradeCost)
        {
            PlayWarningAnimation(buttonObject.transform, "MoneyWARNING");
            Debug.Log($"{nameof(UpgradeMenuRuntimeBuilder)}: Not enough currency to upgrade {entry.MenuId}.");
            return;
        }

        if (!TrySpendCoins(walletSaveData, upgradeCost))
        {
            PlayWarningAnimation(buttonObject.transform, "MoneyWARNING");
            Debug.Log($"{nameof(UpgradeMenuRuntimeBuilder)}: Not enough currency to upgrade {entry.MenuId}.");
            return;
        }

        state.level = Mathf.Clamp(state.level + 1, 1, UpgradeMenuEntry.MaxLevel);
        SaveUnlockData(unlockSaveData);
        RefreshUnlockedButtonView(buttonObject.transform, entry, state);
        PlayUpgradePulseAnimation(buttonObject.transform);
    }

    private void BindDebugButtons()
    {
        BindDebugButton(debugCoinButtonName, SetDebugCoinMax);
        BindDebugButton(debugSoulButtonName, SetDebugSoulMax);
        BindDebugButton(debugResetButtonName, ResetDebugSaveData);
    }

    private static void BindDebugButton(string objectName, UnityEngine.Events.UnityAction action)
    {
        if (string.IsNullOrEmpty(objectName))
        {
            return;
        }

        GameObject buttonObject = GameObject.Find(objectName);
        if (buttonObject == null)
        {
            return;
        }

        Button button = buttonObject.GetComponent<Button>();
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    private List<UpgradeMenuEntry> LoadUpgradeEntries()
    {
        List<UpgradeMenuEntry> entries = new List<UpgradeMenuEntry>();
        TextAsset recipeAsset = Resources.Load<TextAsset>(recipeResourcePath);
        if (recipeAsset == null)
        {
            Debug.LogWarning($"{nameof(UpgradeMenuRuntimeBuilder)}: Resources/{recipeResourcePath} was not found.");
            return entries;
        }

        string[] lines = recipeAsset.text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (string rawLine in lines)
        {
            List<string> cells = ParseCsvLine(rawLine);
            if (cells.Count == 0)
            {
                continue;
            }

            string firstCell = cells[0].Trim().TrimStart('\uFEFF');
            string menuId = firstCell;
            if (IsHeaderRow(menuId))
            {
                continue;
            }

            string menuName = cells.Count > 1 ? cells[1].Trim() : string.Empty;
            int unlockRequiredPreviousLevel = cells.Count > 2 ? ParseAmount(cells[2].Trim(), UpgradeMenuEntry.MaxLevel) : 0;
            int unlockPrice = cells.Count > 3 ? ParseAmount(cells[3].Trim(), int.MaxValue) : 0;
            int lv1Price = cells.Count > 4 ? ParseAmount(cells[4].Trim(), int.MaxValue) : 0;
            int lv2Req = cells.Count > 5 ? ParseAmount(cells[5].Trim(), int.MaxValue) : 0;
            int lv2Price = cells.Count > 6 ? ParseAmount(cells[6].Trim(), int.MaxValue) : lv1Price;
            int lv3Req = cells.Count > 7 ? ParseAmount(cells[7].Trim(), int.MaxValue) : 0;
            int lv3Price = cells.Count > 8 ? ParseAmount(cells[8].Trim(), int.MaxValue) : lv2Price;
            int lv4Req = cells.Count > 9 ? ParseAmount(cells[9].Trim(), int.MaxValue) : 0;
            int lv4Price = cells.Count > 10 ? ParseAmount(cells[10].Trim(), int.MaxValue) : lv3Price;
            int lv5Req = cells.Count > 11 ? ParseAmount(cells[11].Trim(), int.MaxValue) : 0;
            int lv5Price = cells.Count > 12 ? ParseAmount(cells[12].Trim(), int.MaxValue) : lv4Price;

            if (!string.IsNullOrEmpty(menuId) && !string.IsNullOrEmpty(menuName))
            {
                entries.Add(new UpgradeMenuEntry(
                    menuId,
                    menuName,
                    unlockRequiredPreviousLevel,
                    unlockPrice,
                    new[] { lv1Price, lv2Price, lv3Price, lv4Price, lv5Price },
                    new[] { 0, lv2Req, lv3Req, lv4Req, lv5Req }));
            }
        }

        return entries;
    }

    private static List<string> ParseCsvLine(string line)
    {
        List<string> cells = new List<string>();

        if (string.IsNullOrEmpty(line))
        {
            return cells;
        }

        System.Text.StringBuilder cell = new System.Text.StringBuilder();
        bool insideQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char character = line[i];
            if (character == '"')
            {
                bool escapedQuote = insideQuotes && i + 1 < line.Length && line[i + 1] == '"';
                if (escapedQuote)
                {
                    cell.Append('"');
                    i++;
                }
                else
                {
                    insideQuotes = !insideQuotes;
                }

                continue;
            }

            if ((character == ',' || character == '\t') && !insideQuotes)
            {
                cells.Add(cell.ToString());
                cell.Clear();
                continue;
            }

            cell.Append(character);
        }

        cells.Add(cell.ToString());
        return cells;
    }

    private static string JoinCsvCells(List<string> cells, int startIndex)
    {
        if (cells.Count <= startIndex)
        {
            return string.Empty;
        }

        System.Text.StringBuilder value = new System.Text.StringBuilder(cells[startIndex]);
        for (int i = startIndex + 1; i < cells.Count; i++)
        {
            value.Append(',');
            value.Append(cells[i]);
        }

        return value.ToString();
    }

    private static bool IsHeaderRow(string firstCell)
    {
        return string.Equals(firstCell, "menuID", StringComparison.OrdinalIgnoreCase)
            || string.Equals(firstCell, "menuId", StringComparison.OrdinalIgnoreCase)
            || string.Equals(firstCell, "id", StringComparison.OrdinalIgnoreCase);
    }

    private static TMP_Text SetText(Transform root, string objectName, string value, string fallbackObjectName = null)
    {
        Transform target = FindChildRecursive(root, objectName);
        if (target == null && !string.IsNullOrEmpty(fallbackObjectName))
        {
            target = FindChildRecursive(root, fallbackObjectName);
        }

        if (target == null)
        {
            Debug.LogWarning($"{nameof(UpgradeMenuRuntimeBuilder)}: {objectName} was not found in {root.name}.");
            return null;
        }

        TMP_Text label = target.GetComponent<TMP_Text>();
        if (label == null)
        {
            Debug.LogWarning($"{nameof(UpgradeMenuRuntimeBuilder)}: {objectName} does not have a TMP_Text component.");
            return null;
        }

        label.text = value;
        return label;
    }

    private void SetResourceText(Transform root, string textObjectName, string imageObjectName, string value, string fallbackObjectName = null)
    {
        TMP_Text label = SetText(root, textObjectName, FormatAmount(value), fallbackObjectName);
        if (label == null)
        {
            return;
        }

        AlignResourceIcon(label, imageObjectName);
    }

    private string FormatAmount(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        int amount = ParseAmount(value, -1);
        if (amount < 0)
        {
            return value;
        }

        amount = Mathf.Clamp(amount, 0, maxDisplayedAmount);
        return amount.ToString("N0", CultureInfo.InvariantCulture);
    }

    private static int ParseAmount(string value, int maxValue)
    {
        if (string.IsNullOrEmpty(value))
        {
            return 0;
        }

        string normalizedValue = value.Replace(",", string.Empty);
        if (!int.TryParse(normalizedValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out int amount))
        {
            return 0;
        }

        return maxValue >= 0 ? Mathf.Clamp(amount, 0, maxValue) : Mathf.Max(0, amount);
    }

    private void AlignResourceIcon(TMP_Text label, string imageObjectName)
    {
        RectTransform labelRect = label.rectTransform;
        Transform imageTransform = FindChildRecursive(label.transform, imageObjectName);
        if (labelRect == null || imageTransform == null)
        {
            return;
        }

        RectTransform imageRect = imageTransform as RectTransform;
        if (imageRect == null)
        {
            return;
        }

        label.ForceMeshUpdate();
        float textWidth = label.GetPreferredValues(label.text).x;
        float iconWidth = imageRect.rect.width > 0f ? imageRect.rect.width : imageRect.sizeDelta.x;
        Vector2 anchoredPosition = imageRect.anchoredPosition;
        anchoredPosition.x = -(textWidth * 0.5f + resourceIconTextGap + iconWidth * 0.5f);
        imageRect.anchoredPosition = anchoredPosition;
    }

    private void AlignUnlockedInfoTextPairs(Transform root)
    {
        AlignInlineValueText(root, "Level_text", "Level");
        AlignInlineValueText(root, "Price_text", "Price");
        AlignInlineValueText(root, "UpgradeReq", "Req");
    }

    private void AlignInlineValueText(Transform root, string labelObjectName, string valueObjectName)
    {
        Transform labelTransform = FindChildRecursive(root, labelObjectName);
        if (labelTransform == null)
        {
            return;
        }

        Transform valueTransform = FindChildRecursive(labelTransform, valueObjectName);
        if (valueTransform == null)
        {
            valueTransform = FindChildRecursive(root, valueObjectName);
        }

        TMP_Text label = labelTransform.GetComponent<TMP_Text>();
        TMP_Text value = valueTransform != null ? valueTransform.GetComponent<TMP_Text>() : null;
        RectTransform valueRect = valueTransform as RectTransform;
        if (label == null || value == null || valueRect == null)
        {
            return;
        }

        label.ForceMeshUpdate();
        value.ForceMeshUpdate();
        float labelWidth = label.GetPreferredValues(label.text).x;
        float valueWidth = value.GetPreferredValues(value.text).x;
        Vector2 anchoredPosition = valueRect.anchoredPosition;
        anchoredPosition.x = labelWidth * 0.5f + resourceIconTextGap + valueWidth * 0.5f;
        valueRect.anchoredPosition = anchoredPosition;
    }

    private string GetUpgradeRequirementText(UpgradeMenuEntry entry, MenuUnlockState state)
    {
        if (state.level >= UpgradeMenuEntry.MaxLevel)
        {
            return "MAX";
        }

        return FormatAmount(entry.GetUpgradeCostToNextLevel(state.level).ToString(CultureInfo.InvariantCulture));
    }

    private void SetButtonInteractable(GameObject buttonObject, bool isUnlocked)
    {
        Button button = buttonObject.GetComponent<Button>();
        if (button == null)
        {
            return;
        }

        button.interactable = true;
    }

    private void RefreshUnlockedButtonView(Transform root, UpgradeMenuEntry entry, MenuUnlockState state)
    {
        SetText(root, "Level", state.level.ToString(CultureInfo.InvariantCulture));
        SetResourceText(root, "Price", "price_image", entry.GetSalePrice(state.level).ToString(CultureInfo.InvariantCulture));
        SetText(root, "Req", GetUpgradeRequirementText(entry, state));
        AlignUnlockedInfoTextPairs(root);
        ApplyUnlockViewState(root, true);
    }

    private static int GetDiscountedUnlockPrice(UpgradeMenuEntry entry)
    {
        return BuffDebuffManager.ApplyLittleGhostUnlockDiscount(entry.UnlockPrice);
    }

    private void ApplyUnlockViewState(Transform root, bool isUnlocked)
    {
        SetObjectActive(root, "LockSprite", !isUnlocked);
        SetObjectActive(root, "cost", !isUnlocked);
        SetOptionalObjectActive(root, "soul", false);

        SetTextVisible(root, "MenuName", isUnlocked);
        SetObjectActive(root, "MenuSprite", isUnlocked);
        SetOptionalObjectActive(root, "description", false);
        SetObjectActive(root, "Level_text", isUnlocked);
        SetObjectActive(root, "Price_text", isUnlocked);
        SetObjectActive(root, "UpgradeReq", isUnlocked);
        SetObjectActive(root, "Price", isUnlocked);
        SetObjectActive(root, "Level", isUnlocked);
        SetObjectActive(root, "Req", isUnlocked);
        HideWarning(root, "MoneyWARNING");
        HideWarning(root, "ReqWARNING");
        HideWarning(root, "LvlWARNING");
    }

    private static void SetTextVisible(Transform root, string objectName, bool isVisible)
    {
        Transform target = FindChildRecursive(root, objectName);
        if (target == null)
        {
            Debug.LogWarning($"{nameof(UpgradeMenuRuntimeBuilder)}: {objectName} was not found in {root.name}.");
            return;
        }

        target.gameObject.SetActive(true);
        TMP_Text label = target.GetComponent<TMP_Text>();
        if (label == null)
        {
            Debug.LogWarning($"{nameof(UpgradeMenuRuntimeBuilder)}: {objectName} does not have a TMP_Text component.");
            return;
        }

        SetTextAlpha(label, isVisible ? 1f : 0f);
    }

    private static void SetObjectActive(Transform root, string objectName, bool isActive)
    {
        Transform target = FindChildRecursive(root, objectName);
        if (target == null)
        {
            Debug.LogWarning($"{nameof(UpgradeMenuRuntimeBuilder)}: {objectName} was not found in {root.name}.");
            return;
        }

        target.gameObject.SetActive(isActive);
        CanvasGroup canvasGroup = GetOrAddCanvasGroup(target);
        canvasGroup.alpha = isActive ? 1f : 0f;
    }

    private static void SetOptionalObjectActive(Transform root, string objectName, bool isActive)
    {
        Transform target = FindChildRecursive(root, objectName);
        if (target == null)
        {
            return;
        }

        target.gameObject.SetActive(isActive);
        CanvasGroup canvasGroup = GetOrAddCanvasGroup(target);
        canvasGroup.alpha = isActive ? 1f : 0f;
    }

    private void PlayUnlockAnimation(Transform root, Action onComplete = null)
    {
        Transform lockSprite = FindChildRecursive(root, "LockSprite");
        Transform cost = FindChildRecursive(root, "cost");
        Transform menuName = FindChildRecursive(root, "MenuName");
        Transform menuSprite = FindChildRecursive(root, "MenuSprite");
        Transform levelText = FindChildRecursive(root, "Level_text");
        Transform priceText = FindChildRecursive(root, "Price_text");
        Transform upgradeReq = FindChildRecursive(root, "UpgradeReq");
        Transform price = FindChildRecursive(root, "Price");
        Transform level = FindChildRecursive(root, "Level");
        Transform req = FindChildRecursive(root, "Req");
        TMP_Text menuNameLabel = menuName != null ? menuName.GetComponent<TMP_Text>() : null;

        CanvasGroup[] fadeOutGroups = GetCanvasGroups(lockSprite, cost);
        CanvasGroup[] fadeInGroups = GetCanvasGroups(menuSprite, levelText, priceText, upgradeReq);
        Vector3[] originalScales = GetLocalScales(menuSprite, levelText, priceText, upgradeReq);

        SetTargetsActive(true, menuName, menuSprite, levelText, level, priceText, price, upgradeReq, req);
        SetCanvasGroupAlpha(1f, GetCanvasGroups(level, price, req));
        if (menuNameLabel != null)
        {
            SetTextAlpha(menuNameLabel, 0f);
        }

        SetCanvasGroupAlpha(0f, fadeInGroups);
        SetScaledIntroState(originalScales, menuSprite, levelText, priceText, upgradeReq);

        Sequence sequence = DOTween.Sequence();
        foreach (CanvasGroup canvasGroup in fadeOutGroups)
        {
            sequence.Join(canvasGroup.DOFade(0f, unlockAnimationDuration));
        }

        if (menuNameLabel != null)
        {
            sequence.Join(TweenTextAlpha(menuNameLabel, 1f, unlockAnimationDuration).SetEase(Ease.InBack));
        }

        for (int i = 0; i < fadeInGroups.Length; i++)
        {
            CanvasGroup canvasGroup = fadeInGroups[i];
            Transform target = canvasGroup.transform;
            sequence.Join(canvasGroup.DOFade(1f, unlockAnimationDuration).SetEase(Ease.InBack));
            sequence.Join(target.DOScale(originalScales[i], unlockAnimationDuration).SetEase(Ease.InBack));
        }

        sequence.OnComplete(() =>
        {
            SetTargetsActive(false, lockSprite, cost);
            SetButtonInteractable(root.gameObject, true);
            onComplete?.Invoke();
        });
    }

    private void PlayRevealedButtonFadeIn(GameObject buttonObject)
    {
        CanvasGroup canvasGroup = GetOrAddCanvasGroup(buttonObject.transform);
        DOTween.Kill(canvasGroup, false);
        canvasGroup.alpha = 0f;
        canvasGroup.DOFade(1f, revealedButtonFadeInDuration);
    }

    private void PlayUpgradePulseAnimation(Transform root)
    {
        List<Transform> targets = GetVisibleUpgradePulseTargets(root);
        if (targets.Count == 0)
        {
            return;
        }

        Sequence sequence = DOTween.Sequence().SetTarget(root);
        foreach (Transform target in targets)
        {
            Vector3 originalScale = GetUpgradeOriginalScale(target);
            DOTween.Kill(target, false);
            target.DOKill(false);
            target.localScale = originalScale;

            Sequence targetSequence = DOTween.Sequence().SetTarget(target);
            targetSequence.Append(target.DOScale(originalScale * 1.1f, 0.12f).SetEase(Ease.OutQuint));
            targetSequence.Append(target.DOScale(originalScale * 0.96f, 0.08f).SetEase(Ease.OutQuint));
            targetSequence.Append(target.DOScale(originalScale, 0.16f).SetEase(Ease.OutQuint));
            sequence.Join(targetSequence);
        }
    }

    private List<Transform> GetVisibleUpgradePulseTargets(Transform root)
    {
        List<Transform> targets = new List<Transform>();
        Graphic[] graphics = root.GetComponentsInChildren<Graphic>(false);
        foreach (Graphic graphic in graphics)
        {
            Transform target = graphic.transform;
            if (target == root || !IsVisibleGraphic(graphic) || IsWarningTransform(target))
            {
                continue;
            }

            if (!targets.Contains(target))
            {
                targets.Add(target);
            }
        }

        return targets;
    }

    private static bool IsVisibleGraphic(Graphic graphic)
    {
        if (graphic == null || !graphic.gameObject.activeInHierarchy || graphic.color.a <= 0.01f)
        {
            return false;
        }

        CanvasGroup[] canvasGroups = graphic.GetComponentsInParent<CanvasGroup>(true);
        foreach (CanvasGroup canvasGroup in canvasGroups)
        {
            if (canvasGroup.alpha <= 0.01f)
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsWarningTransform(Transform target)
    {
        while (target != null)
        {
            if (target.name.Contains("WARNING"))
            {
                return true;
            }

            target = target.parent;
        }

        return false;
    }

    private Vector3 GetUpgradeOriginalScale(Transform target)
    {
        if (!upgradeOriginalScales.TryGetValue(target, out Vector3 originalScale))
        {
            originalScale = target.localScale == Vector3.zero ? Vector3.one : target.localScale;
            upgradeOriginalScales.Add(target, originalScale);
        }

        return originalScale;
    }

    private void PlayWarningAnimation(Transform root, string warningObjectName = "MoneyWARNING")
    {
        Transform lockSprite = FindChildRecursive(root, "LockSprite");
        Transform warning = lockSprite != null ? FindChildRecursive(lockSprite, warningObjectName) : FindChildRecursive(root, warningObjectName);
        if (warning == null)
        {
            warning = FindChildRecursive(root, warningObjectName);
        }

        if (warning == null)
        {
            Debug.LogWarning($"{nameof(UpgradeMenuRuntimeBuilder)}: {warningObjectName} was not found in {root.name}.");
            return;
        }

        CanvasGroup canvasGroup = GetOrAddCanvasGroup(warning);
        Vector3 originalScale = GetWarningOriginalScale(warning);

        StopWarningTweens(warning, canvasGroup);
        warning.gameObject.SetActive(true);
        warning.localScale = originalScale * 0.65f;
        canvasGroup.alpha = 0f;

        Sequence sequence = DOTween.Sequence().SetTarget(warning);
        sequence.Join(canvasGroup.DOFade(1f, warningFadeInDuration));
        sequence.Join(warning.DOScale(originalScale, warningFadeInDuration).SetEase(Ease.OutBounce));
        sequence.AppendInterval(warningVisibleDuration);
        sequence.Append(canvasGroup.DOFade(0f, warningFadeOutDuration));
        sequence.OnComplete(() => warning.gameObject.SetActive(false));
    }

    private void HideWarning(Transform root, string warningObjectName)
    {
        Transform warning = FindChildRecursive(root, warningObjectName);
        if (warning == null)
        {
            return;
        }

        CanvasGroup canvasGroup = GetOrAddCanvasGroup(warning);
        StopWarningTweens(warning, canvasGroup);
        warning.localScale = GetWarningOriginalScale(warning);
        canvasGroup.alpha = 0f;
        warning.gameObject.SetActive(false);
    }

    private Vector3 GetWarningOriginalScale(Transform warning)
    {
        if (!warningOriginalScales.TryGetValue(warning, out Vector3 originalScale))
        {
            originalScale = warning.localScale == Vector3.zero ? Vector3.one : warning.localScale;
            warningOriginalScales.Add(warning, originalScale);
        }

        return originalScale;
    }

    private static void StopWarningTweens(Transform warning, CanvasGroup canvasGroup)
    {
        DOTween.Kill(warning, false);
        DOTween.Kill(canvasGroup, false);
        warning.DOKill(false);
        canvasGroup.DOKill(false);
    }

    private static string FindFirstLockedMenuId(List<UpgradeMenuEntry> entries, MenuUnlockSaveData unlockSaveData)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            UpgradeMenuEntry entry = entries[i];
            if (!unlockSaveData.IsUnlocked(entry.MenuId))
            {
                return IsLockedMenuRevealed(entries, unlockSaveData, i) ? entry.MenuId : null;
            }
        }

        return null;
    }

    private static bool IsLockedMenuRevealed(List<UpgradeMenuEntry> entries, MenuUnlockSaveData unlockSaveData, int entryIndex)
    {
        if (entryIndex <= 0)
        {
            return true;
        }

        UpgradeMenuEntry previousEntry = entries[entryIndex - 1];
        return unlockSaveData.IsUnlocked(previousEntry.MenuId);
    }

    private static bool IsLockedMenuPurchasable(List<UpgradeMenuEntry> entries, MenuUnlockSaveData unlockSaveData, string menuId)
    {
        int entryIndex = entries.FindIndex(entry => entry.MenuId == menuId);
        if (entryIndex < 0)
        {
            return false;
        }

        return IsLockedMenuPurchasable(entries, unlockSaveData, entryIndex);
    }

    private static bool IsLockedMenuPurchasable(List<UpgradeMenuEntry> entries, MenuUnlockSaveData unlockSaveData, int entryIndex)
    {
        if (entryIndex <= 0)
        {
            return true;
        }

        UpgradeMenuEntry entry = entries[entryIndex];
        if (entry.UnlockRequiredPreviousLevel <= 0)
        {
            return true;
        }

        UpgradeMenuEntry previousEntry = entries[entryIndex - 1];
        MenuUnlockState previousState = unlockSaveData.GetOrCreate(previousEntry.MenuId);
        return previousState.isUnlocked && previousState.level >= entry.UnlockRequiredPreviousLevel;
    }

    private static void SetTextAlpha(TMP_Text label, float alpha)
    {
        Color color = label.color;
        color.a = alpha;
        label.color = color;
    }

    private static Tween TweenTextAlpha(TMP_Text label, float targetAlpha, float duration)
    {
        return DOTween.To(
            () => label.color.a,
            alpha => SetTextAlpha(label, alpha),
            targetAlpha,
            duration);
    }

    private static CanvasGroup[] GetCanvasGroups(params Transform[] targets)
    {
        List<CanvasGroup> canvasGroups = new List<CanvasGroup>();
        foreach (Transform target in targets)
        {
            if (target != null)
            {
                canvasGroups.Add(GetOrAddCanvasGroup(target));
            }
        }

        return canvasGroups.ToArray();
    }

    private static CanvasGroup GetOrAddCanvasGroup(Transform target)
    {
        CanvasGroup canvasGroup = target.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = target.gameObject.AddComponent<CanvasGroup>();
        }

        return canvasGroup;
    }

    private static Vector3[] GetLocalScales(params Transform[] targets)
    {
        List<Vector3> scales = new List<Vector3>();
        foreach (Transform target in targets)
        {
            if (target != null)
            {
                scales.Add(target.localScale);
            }
        }

        return scales.ToArray();
    }

    private static void SetScaledIntroState(Vector3[] originalScales, params Transform[] targets)
    {
        int scaleIndex = 0;
        foreach (Transform target in targets)
        {
            if (target == null)
            {
                continue;
            }

            target.localScale = originalScales[scaleIndex] * 0.85f;
            scaleIndex++;
        }
    }

    private static void SetCanvasGroupAlpha(float alpha, params CanvasGroup[] canvasGroups)
    {
        foreach (CanvasGroup canvasGroup in canvasGroups)
        {
            canvasGroup.alpha = alpha;
        }
    }

    private static void SetTargetsActive(bool isActive, params Transform[] targets)
    {
        foreach (Transform target in targets)
        {
            if (target != null)
            {
                target.gameObject.SetActive(isActive);
            }
        }
    }

    private void SetMenuSprite(Transform root, int spriteIndex)
    {
        Transform target = FindChildRecursive(root, "MenuSprite");
        if (target == null)
        {
            Debug.LogWarning($"{nameof(UpgradeMenuRuntimeBuilder)}: MenuSprite was not found in {root.name}.");
            return;
        }

        Image image = target.GetComponent<Image>();
        if (image == null)
        {
            Debug.LogWarning($"{nameof(UpgradeMenuRuntimeBuilder)}: MenuSprite does not have an Image component.");
            return;
        }

        Sprite sprite = menuSprites != null && spriteIndex >= 0 && spriteIndex < menuSprites.Length ? menuSprites[spriteIndex] : null;
        image.sprite = sprite;
        image.enabled = sprite != null;
        image.preserveAspect = true;
    }

    private static Transform FindChildRecursive(Transform current, string objectName)
    {
        if (current.name == objectName)
        {
            return current;
        }

        foreach (Transform child in current)
        {
            Transform found = FindChildRecursive(child, objectName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private struct UpgradeMenuEntry
    {
        public const int MaxLevel = 5;

        public UpgradeMenuEntry(
            string menuId,
            string menuName,
            int unlockRequiredPreviousLevel,
            int unlockPrice,
            int[] salePrices,
            int[] upgradeCosts)
        {
            MenuId = menuId;
            MenuName = menuName;
            UnlockRequiredPreviousLevel = unlockRequiredPreviousLevel;
            UnlockPrice = unlockPrice;
            SalePrices = salePrices;
            UpgradeCosts = upgradeCosts;
        }

        public string MenuId { get; }
        public string MenuName { get; }
        public int UnlockRequiredPreviousLevel { get; }
        public int UnlockPrice { get; }
        private int[] SalePrices { get; }
        private int[] UpgradeCosts { get; }

        public int GetSalePrice(int level)
        {
            int index = Mathf.Clamp(level, 1, MaxLevel) - 1;
            return SalePrices != null && index < SalePrices.Length ? SalePrices[index] : 0;
        }

        public int GetUpgradeCostToNextLevel(int currentLevel)
        {
            int nextLevel = Mathf.Clamp(currentLevel + 1, 1, MaxLevel);
            int index = nextLevel - 1;
            return UpgradeCosts != null && index < UpgradeCosts.Length ? UpgradeCosts[index] : 0;
        }
    }

    private MenuUnlockSaveData LoadOrCreateUnlockSaveData(List<UpgradeMenuEntry> entries)
    {
        MenuUnlockSaveData saveData = LoadUnlockData();
        bool changed = false;

        foreach (UpgradeMenuEntry entry in entries)
        {
            if (!saveData.Contains(entry.MenuId))
            {
                saveData.menus.Add(CreateDefaultUnlockState(entry.MenuId));
                changed = true;
            }
        }

        MenuUnlockState defaultMenuState = saveData.GetOrCreate("0");
        if (!defaultMenuState.isUnlocked || defaultMenuState.level <= 0)
        {
            defaultMenuState.isUnlocked = true;
            defaultMenuState.level = 1;
            changed = true;
        }

        if (changed)
        {
            SaveUnlockData(saveData);
        }

        return saveData;
    }

    private static MenuUnlockSaveData CreateDefaultUnlockSaveData(List<UpgradeMenuEntry> entries)
    {
        MenuUnlockSaveData saveData = new MenuUnlockSaveData();
        foreach (UpgradeMenuEntry entry in entries)
        {
            saveData.menus.Add(CreateDefaultUnlockState(entry.MenuId));
        }

        return saveData;
    }

    private static MenuUnlockState CreateDefaultUnlockState(string menuId)
    {
        bool isDefaultUnlocked = menuId == "0";
        return new MenuUnlockState
        {
            menuID = menuId,
            isUnlocked = isDefaultUnlocked,
            level = isDefaultUnlocked ? 1 : 0
        };
    }

    private MenuUnlockSaveData LoadUnlockData()
    {
        string path = GetUnlockSavePath();
        if (!File.Exists(path))
        {
            return new MenuUnlockSaveData();
        }

        try
        {
            MenuUnlockSaveData saveData = JsonUtility.FromJson<MenuUnlockSaveData>(File.ReadAllText(path));
            if (saveData == null)
            {
                saveData = new MenuUnlockSaveData();
            }

            saveData.EnsureInitialized();
            return saveData;
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"{nameof(UpgradeMenuRuntimeBuilder)}: Failed to load {path}. {exception.Message}");
            return new MenuUnlockSaveData();
        }
    }

    private void SaveUnlockData(MenuUnlockSaveData saveData)
    {
        string path = GetUnlockSavePath();
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, JsonUtility.ToJson(saveData, true));
    }

    private string GetUnlockSavePath()
    {
        return Path.Combine(Application.persistentDataPath, unlockSaveFileName);
    }

    private PlayerWalletSaveData LoadWalletData()
    {
        string path = GetWalletSavePath();
        if (!File.Exists(path))
        {
            PlayerWalletSaveData newWallet = new PlayerWalletSaveData();
            SaveWalletData(newWallet);
            return newWallet;
        }

        try
        {
            PlayerWalletSaveData saveData = JsonUtility.FromJson<PlayerWalletSaveData>(File.ReadAllText(path));
            if (saveData == null)
            {
                saveData = new PlayerWalletSaveData();
            }

            saveData.MigrateLegacyMoneyField();
            return saveData;
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"{nameof(UpgradeMenuRuntimeBuilder)}: Failed to load {path}. {exception.Message}");
            return new PlayerWalletSaveData();
        }
    }

    private void SaveWalletData(PlayerWalletSaveData saveData)
    {
        if (saveData == null)
        {
            saveData = new PlayerWalletSaveData();
        }

        saveData.SyncLegacyMoneyField();
        string path = GetWalletSavePath();
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, JsonUtility.ToJson(saveData, true));
    }

    private bool TrySpendCoins(PlayerWalletSaveData walletSaveData, int amount)
    {
        amount = Mathf.Max(0, amount);
        if (GameStatsManager.Instance != null)
        {
            bool spent = GameStatsManager.Instance.TrySpendCoins(amount);
            if (spent)
            {
                PlayerWalletSaveData refreshedWallet = LoadWalletData();
                walletSaveData.coin = refreshedWallet.coin;
                walletSaveData.soul = refreshedWallet.soul;
                walletSaveData.money = refreshedWallet.money;
            }

            return spent;
        }

        if (walletSaveData.coin < amount)
        {
            return false;
        }

        walletSaveData.coin -= amount;
        SaveWalletData(walletSaveData);
        return true;
    }

    private string GetWalletSavePath()
    {
        return Path.Combine(Application.persistentDataPath, walletSaveFileName);
    }

    [Serializable]
    private class MenuUnlockSaveData
    {
        public List<MenuUnlockState> menus = new List<MenuUnlockState>();

        public void EnsureInitialized()
        {
            if (menus == null)
            {
                menus = new List<MenuUnlockState>();
            }
        }

        public bool Contains(string menuId)
        {
            EnsureInitialized();
            return menus.Exists(unlock => unlock.menuID == menuId);
        }

        public MenuUnlockState GetOrCreate(string menuId)
        {
            EnsureInitialized();
            MenuUnlockState state = menus.Find(unlock => unlock.menuID == menuId);
            if (state == null)
            {
                state = new MenuUnlockState { menuID = menuId, isUnlocked = false, level = 0 };
                menus.Add(state);
            }

            if (!state.isUnlocked)
            {
                state.level = 0;
            }
            else
            {
                state.level = Mathf.Clamp(state.level <= 0 ? 1 : state.level, 1, UpgradeMenuEntry.MaxLevel);
            }

            return state;
        }

        public bool IsUnlocked(string menuId)
        {
            return GetOrCreate(menuId).isUnlocked;
        }

        public void SetUnlocked(string menuId, bool isUnlocked, int level)
        {
            MenuUnlockState state = GetOrCreate(menuId);
            state.isUnlocked = isUnlocked;
            state.level = isUnlocked ? Mathf.Clamp(level <= 0 ? 1 : level, 1, UpgradeMenuEntry.MaxLevel) : 0;
        }
    }

    [Serializable]
    private class MenuUnlockState
    {
        public string menuID;
        public bool isUnlocked;
        public int level;
    }

    [Serializable]
    private class PlayerWalletSaveData
    {
        public int coin;
        public int soul;
        public int money;

        public void MigrateLegacyMoneyField()
        {
            if (coin <= 0 && money > 0)
            {
                coin = money;
            }

            SyncLegacyMoneyField();
        }

        public void SyncLegacyMoneyField()
        {
            money = coin;
        }
    }
}
