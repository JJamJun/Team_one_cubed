using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class UpgradeMenuSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/UpgradeScene.unity";
    private const string PrefabFolderPath = "Assets/Prefab";
    private const string PrefabPath = "Assets/Prefab/UpgradeButton.prefab";
    private const string KoreanFontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/KERISKEDU_B SDF.asset";
    private const string UpgradePanelName = "upgradePanel";
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

        UpgradeMenuRuntimeBuilder runtimeBuilder = scrollView.GetComponent<UpgradeMenuRuntimeBuilder>();
        if (runtimeBuilder == null)
        {
            runtimeBuilder = scrollView.gameObject.AddComponent<UpgradeMenuRuntimeBuilder>();
        }

        SerializedObject serializedBuilder = new SerializedObject(runtimeBuilder);
        serializedBuilder.FindProperty("recipeResourcePath").stringValue = "temp_recipe";
        serializedBuilder.FindProperty("contentRoot").objectReferenceValue = content;
        serializedBuilder.FindProperty("upgradeButtonPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        serializedBuilder.FindProperty("buttonSpacing").floatValue = 95f;
        serializedBuilder.FindProperty("generatedButtonPrefix").stringValue = GeneratedButtonPrefix;
        serializedBuilder.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(runtimeBuilder);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        Debug.Log($"{nameof(UpgradeMenuSceneBuilder)}: Runtime upgrade menu setup complete.");
    }

    private static void EnsureUpgradeButtonPrefab()
    {
        if (!AssetDatabase.IsValidFolder(PrefabFolderPath))
        {
            AssetDatabase.CreateFolder("Assets", "Prefab");
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

        GameObject labelObject = new GameObject("Text (TMP)", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(buttonObject.transform, false);

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(24f, 24f);
        labelRect.offsetMax = new Vector2(-24f, -24f);

        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        TMP_FontAsset koreanFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(KoreanFontPath);
        if (koreanFont != null)
        {
            label.font = koreanFont;
        }

        label.text = "UpgradeButton";
        label.color = Color.black;
        label.fontSize = 42f;
        label.alignment = TextAlignmentOptions.Center;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.overflowMode = TextOverflowModes.Ellipsis;

        PrefabUtility.SaveAsPrefabAsset(buttonObject, PrefabPath);
        Object.DestroyImmediate(buttonObject);
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
