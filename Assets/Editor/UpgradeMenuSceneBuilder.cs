using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class UpgradeMenuSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/UpgradeScene.unity";
    private const string PrefabFolderPath = "Assets/Prefabs";
    private const string PrefabPath = "Assets/Prefabs/UpgradeButton.prefab";
    private const string KoreanFontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/KERISKEDU_B SDF.asset";
    private const string UpgradePanelName = "upgradePanel";
    private const string ManagersName = "Managers";
    private const string ScrollViewName = "UpgradeScrollView";
    private const string GeneratedButtonPrefix = "UpgradeButton_";

    [MenuItem("Tools/Upgrade/Setup Runtime Upgrade Menu")]
    public static void SetupRuntimeUpgradeMenu()
    {
        EnsureUpgradeButtonPrefab();

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject upgradePanel = FindGameObjectByName(scene, UpgradePanelName);
        if (upgradePanel == null)
        {
            Debug.LogWarning($"{nameof(UpgradeMenuSceneBuilder)}: {UpgradePanelName} was not found in {ScenePath}.");
            return;
        }

        RectTransform scrollView = FindOrCreateScrollView(upgradePanel.transform);
        RectTransform viewport = FindOrCreateViewport(scrollView);
        RectTransform content = FindOrCreateContent(viewport);

        ScrollRect scrollRect = scrollView.GetComponent<ScrollRect>();
        scrollRect.horizontal = true;
        scrollRect.vertical = false;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.inertia = true;
        scrollRect.scrollSensitivity = 35f;
        scrollRect.viewport = viewport;
        scrollRect.content = content;

        RemoveGeneratedSceneButtons(content);

        UpgradeMenuRuntimeBuilder scrollViewBuilder = scrollView.GetComponent<UpgradeMenuRuntimeBuilder>();
        if (scrollViewBuilder != null)
        {
            Object.DestroyImmediate(scrollViewBuilder);
        }

        GameObject managers = FindOrCreateRootGameObject(scene, ManagersName);
        UpgradeMenuRuntimeBuilder runtimeBuilder = managers.GetComponent<UpgradeMenuRuntimeBuilder>();
        if (runtimeBuilder == null)
        {
            runtimeBuilder = managers.AddComponent<UpgradeMenuRuntimeBuilder>();
        }

        SerializedObject serializedBuilder = new SerializedObject(runtimeBuilder);
        serializedBuilder.FindProperty("recipeResourcePath").stringValue = "menu_info";
        serializedBuilder.FindProperty("unlockSaveFileName").stringValue = "menu_progress.json";
        serializedBuilder.FindProperty("walletSaveFileName").stringValue = "player_wallet.json";
        serializedBuilder.FindProperty("contentRoot").objectReferenceValue = content;
        serializedBuilder.FindProperty("upgradeButtonPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        serializedBuilder.FindProperty("unlockAnimationDuration").floatValue = 1f;
        serializedBuilder.FindProperty("warningFadeInDuration").floatValue = 0.35f;
        serializedBuilder.FindProperty("warningVisibleDuration").floatValue = 0.35f;
        serializedBuilder.FindProperty("warningFadeOutDuration").floatValue = 0.85f;
        serializedBuilder.FindProperty("revealedButtonFadeInDuration").floatValue = 0.6f;
        serializedBuilder.FindProperty("resourceIconTextGap").floatValue = 8f;
        serializedBuilder.FindProperty("maxDisplayedAmount").intValue = 999999;
        serializedBuilder.FindProperty("buttonSpacing").floatValue = 95f;
        serializedBuilder.FindProperty("generatedButtonPrefix").stringValue = GeneratedButtonPrefix;
        serializedBuilder.FindProperty("debugCoinButtonName").stringValue = "\uB514\uBC84\uADF8_coin_999";
        serializedBuilder.FindProperty("debugSoulButtonName").stringValue = "\uB514\uBC84\uADF8_soul_999";
        serializedBuilder.FindProperty("debugResetButtonName").stringValue = "\uB514\uBC84\uADF8_reset";
        serializedBuilder.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(runtimeBuilder);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        Debug.Log($"{nameof(UpgradeMenuSceneBuilder)}: Runtime upgrade menu setup complete.");
    }

    private static GameObject FindOrCreateRootGameObject(Scene scene, string objectName)
    {
        foreach (GameObject rootObject in scene.GetRootGameObjects())
        {
            if (rootObject.name == objectName)
            {
                return rootObject;
            }
        }

        GameObject newRootObject = new GameObject(objectName);
        SceneManager.MoveGameObjectToScene(newRootObject, scene);
        return newRootObject;
    }

    private static void EnsureUpgradeButtonPrefab()
    {
        if (!AssetDatabase.IsValidFolder(PrefabFolderPath))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }

        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
        {
            return;
        }

        GameObject buttonObject = new GameObject("UpgradeButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.sizeDelta = new Vector2(420f, 620f);
        buttonRect.anchorMin = new Vector2(0f, 0.5f);
        buttonRect.anchorMax = new Vector2(0f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);

        Image buttonImage = buttonObject.GetComponent<Image>();
        buttonImage.color = Color.white;

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = buttonImage;

        GameObject spriteObject = new GameObject("MenuSprite", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        spriteObject.transform.SetParent(buttonObject.transform, false);

        RectTransform spriteRect = spriteObject.GetComponent<RectTransform>();
        spriteRect.anchorMin = new Vector2(0.5f, 0.5f);
        spriteRect.anchorMax = new Vector2(0.5f, 0.5f);
        spriteRect.anchoredPosition = new Vector2(0f, 83f);
        spriteRect.sizeDelta = new Vector2(326.6166f, 372.08f);

        Image spriteImage = spriteObject.GetComponent<Image>();
        spriteImage.preserveAspect = true;

        GameObject lockObject = new GameObject("LockSprite", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        lockObject.transform.SetParent(buttonObject.transform, false);

        RectTransform lockRect = lockObject.GetComponent<RectTransform>();
        lockRect.anchorMin = new Vector2(0.5f, 0.5f);
        lockRect.anchorMax = new Vector2(0.5f, 0.5f);
        lockRect.anchoredPosition = new Vector2(0f, 83f);
        lockRect.sizeDelta = new Vector2(326.6166f, 372.08f);

        Image lockImage = lockObject.GetComponent<Image>();
        lockImage.color = Color.white;
        lockImage.preserveAspect = true;

        GameObject textRootObject = new GameObject("Text", typeof(RectTransform));
        textRootObject.transform.SetParent(buttonObject.transform, false);

        RectTransform textRootRect = textRootObject.GetComponent<RectTransform>();
        textRootRect.anchorMin = new Vector2(0.5f, 0f);
        textRootRect.anchorMax = new Vector2(0.5f, 0f);
        textRootRect.anchoredPosition = new Vector2(0f, 117.333f);
        textRootRect.sizeDelta = new Vector2(100f, 100f);
        textRootRect.pivot = new Vector2(0.5f, 0f);

        TMP_FontAsset koreanFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(KoreanFontPath);
        CreateMenuNameLabel(textRootObject.transform, koreanFont);
        CreateInfoPairLabel(textRootObject.transform, "Level_text", "\uB808\uBCA8:", "Level", "1", new Vector2(-62f, 42f), koreanFont);
        CreateInfoPairLabel(textRootObject.transform, "Price_text", "\uAC00\uACA9:", "Price", "1", new Vector2(-62f, -42f), koreanFont);
        CreateInfoPairLabel(textRootObject.transform, "UpgradeReq", "\uC5C5\uADF8\uB808\uC774\uB4DC:", "Req", "5", new Vector2(-62f, -84f), koreanFont);
        CreateDescriptionLabel(textRootObject.transform, koreanFont);
        CreateResourceLabel(textRootObject.transform, "cost", "cost_image", "999,999", -42f, new Color(1f, 0.95686066f, 0f, 1f), koreanFont);
        CreateResourceLabel(textRootObject.transform, "soul", "soul_image", "999,999", -79f, new Color(0f, 0.7605362f, 1f, 1f), koreanFont);

        PrefabUtility.SaveAsPrefabAsset(buttonObject, PrefabPath);
        Object.DestroyImmediate(buttonObject);
    }

    private static void CreateMenuNameLabel(Transform parent, TMP_FontAsset font)
    {
        GameObject labelObject = new GameObject("MenuName", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(parent, false);

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.anchoredPosition = new Vector2(0f, -2.333f);
        labelRect.sizeDelta = new Vector2(272f, -12.666f);

        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        if (font != null)
        {
            label.font = font;
        }

        label.text = "MenuName";
        label.color = Color.black;
        label.fontSize = 42f;
        label.alignment = TextAlignmentOptions.Center;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.overflowMode = TextOverflowModes.Ellipsis;
    }

    private static void CreateResourceLabel(Transform parent, string name, string imageName, string text, float yPosition, Color imageColor, TMP_FontAsset font)
    {
        GameObject labelObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(parent, false);

        RectTransform rectTransform = labelObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = new Vector2(0f, yPosition);
        rectTransform.sizeDelta = new Vector2(320f, 48f);

        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        if (font != null)
        {
            label.font = font;
        }

        label.text = text;
        label.color = Color.black;
        label.fontSize = 24f;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.overflowMode = TextOverflowModes.Ellipsis;

        GameObject imageObject = new GameObject(imageName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(labelObject.transform, false);

        RectTransform imageRect = imageObject.GetComponent<RectTransform>();
        imageRect.anchorMin = new Vector2(0.5f, 0.5f);
        imageRect.anchorMax = new Vector2(0.5f, 0.5f);
        imageRect.anchoredPosition = new Vector2(-74f, 1.7f);
        imageRect.sizeDelta = new Vector2(25f, 25f);

        Image image = imageObject.GetComponent<Image>();
        image.color = imageColor;
    }

    private static void CreateInfoLabel(Transform parent, string name, string text, Vector2 position, TMP_FontAsset font)
    {
        GameObject labelObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(parent, false);

        RectTransform rectTransform = labelObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = new Vector2(320f, 48f);

        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        if (font != null)
        {
            label.font = font;
        }

        label.text = text;
        label.color = Color.black;
        label.fontSize = 24f;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.overflowMode = TextOverflowModes.Ellipsis;
    }

    private static void CreateInfoPairLabel(Transform parent, string labelName, string labelText, string valueName, string valueText, Vector2 position, TMP_FontAsset font)
    {
        GameObject labelObject = new GameObject(labelName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(parent, false);

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0.5f, 0.5f);
        labelRect.anchorMax = new Vector2(0.5f, 0.5f);
        labelRect.anchoredPosition = position;
        labelRect.sizeDelta = new Vector2(320f, 48f);

        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        if (font != null)
        {
            label.font = font;
        }

        label.text = labelText;
        label.color = Color.black;
        label.fontSize = 24f;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.overflowMode = TextOverflowModes.Ellipsis;

        GameObject valueObject = new GameObject(valueName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        valueObject.transform.SetParent(labelObject.transform, false);

        RectTransform valueRect = valueObject.GetComponent<RectTransform>();
        valueRect.anchorMin = new Vector2(0.5f, 0.5f);
        valueRect.anchorMax = new Vector2(0.5f, 0.5f);
        valueRect.anchoredPosition = new Vector2(96f, 0f);
        valueRect.sizeDelta = new Vector2(160f, 48f);

        TextMeshProUGUI value = valueObject.GetComponent<TextMeshProUGUI>();
        if (font != null)
        {
            value.font = font;
        }

        value.text = valueText;
        value.color = Color.black;
        value.fontSize = 24f;
        value.alignment = TextAlignmentOptions.Center;
        value.raycastTarget = false;
        value.textWrappingMode = TextWrappingModes.Normal;
        value.overflowMode = TextOverflowModes.Ellipsis;
    }

    private static void CreateDescriptionLabel(Transform parent, TMP_FontAsset font)
    {
        GameObject labelObject = new GameObject("description", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(parent, false);

        RectTransform rectTransform = labelObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = new Vector2(0f, -4f);
        rectTransform.sizeDelta = new Vector2(320f, 96f);

        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        if (font != null)
        {
            label.font = font;
        }

        label.text = "description";
        label.color = Color.black;
        label.fontSize = 18f;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.overflowMode = TextOverflowModes.Ellipsis;
    }

    private static RectTransform FindOrCreateScrollView(Transform parent)
    {
        Transform existing = parent.Find(ScrollViewName);
        GameObject scrollView = existing != null ? existing.gameObject : new GameObject(ScrollViewName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(ScrollRect));
        if (existing == null)
        {
            scrollView.transform.SetParent(parent, false);
            scrollView.layer = parent.gameObject.layer;
        }

        RectTransform rectTransform = scrollView.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = new Vector2(45f, 35f);
        rectTransform.offsetMax = new Vector2(-45f, -35f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);

        Image image = scrollView.GetComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0f);
        image.raycastTarget = false;

        return rectTransform;
    }

    private static RectTransform FindOrCreateViewport(RectTransform parent)
    {
        Transform existing = parent.Find("Viewport");
        GameObject viewport = existing != null ? existing.gameObject : new GameObject("Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Mask));
        if (existing == null)
        {
            viewport.transform.SetParent(parent, false);
            viewport.layer = parent.gameObject.layer;
        }

        RectTransform rectTransform = viewport.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        Image image = viewport.GetComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.01f);
        image.raycastTarget = true;

        Mask mask = viewport.GetComponent<Mask>();
        mask.showMaskGraphic = false;

        return rectTransform;
    }

    private static RectTransform FindOrCreateContent(RectTransform parent)
    {
        Transform existing = parent.Find("Content");
        GameObject content = existing != null ? existing.gameObject : new GameObject("Content", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
        if (existing == null)
        {
            content.transform.SetParent(parent, false);
            content.layer = parent.gameObject.layer;
        }

        RectTransform rectTransform = content.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0f, 0.5f);
        rectTransform.anchorMax = new Vector2(0f, 0.5f);
        rectTransform.pivot = new Vector2(0f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = new Vector2(0f, 760f);

        GridLayoutGroup grid = content.GetComponent<GridLayoutGroup>();
        grid.padding = new RectOffset(80, 80, 70, 70);
        grid.cellSize = new Vector2(420f, 620f);
        grid.spacing = new Vector2(95f, 55f);
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.MiddleLeft;
        grid.constraint = GridLayoutGroup.Constraint.FixedRowCount;
        grid.constraintCount = 1;

        ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

        return rectTransform;
    }

    private static void RemoveGeneratedSceneButtons(RectTransform content)
    {
        for (int i = content.childCount - 1; i >= 0; i--)
        {
            Transform child = content.GetChild(i);
            if (child.name.StartsWith(GeneratedButtonPrefix, System.StringComparison.Ordinal))
            {
                Object.DestroyImmediate(child.gameObject);
            }
        }
    }

    private static GameObject FindGameObjectByName(Scene scene, string objectName)
    {
        foreach (GameObject rootObject in scene.GetRootGameObjects())
        {
            Transform found = FindChildRecursive(rootObject.transform, objectName);
            if (found != null)
            {
                return found.gameObject;
            }
        }

        return null;
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
}
