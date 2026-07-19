using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UpgradeSceneLoader : MonoBehaviour
{
    [SerializeField] private string upgradeSceneName = "UpgradeScene";
    [SerializeField] private Button upgradeButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private RectTransform targetCanvasRect;
    [SerializeField] private RectTransform embeddedUpgradePanel;
    [SerializeField] private UpgradeMenuRuntimeBuilder embeddedUpgradeBuilder;
    [SerializeField] private bool rebuildEmbeddedPanelOnOpen = true;
    [SerializeField] private bool loadOnStart;
    [SerializeField] private bool setLoadedSceneActive;
    [SerializeField] private bool unloadOnDestroy;
    [SerializeField] private bool matchTargetCanvasSize = true;
    [SerializeField] private bool showAsWindow = true;
    [SerializeField] private Vector2 windowSize = new Vector2(874f, 568f);
    [SerializeField] private Vector2 windowAnchoredPosition;
    [SerializeField] private bool clampWindowToTarget = true;
    [SerializeField] private bool scaleContentToFit = true;
    [SerializeField] private bool disableLoadedSceneSystems = true;
    [SerializeField] private int sortingOrderOffset = 10;

    private bool isLoading;
    private GameObject loadedCanvasRoot;

    private void Awake()
    {
        BindMissingReferences();

        if (upgradeButton != null)
        {
            upgradeButton.onClick.RemoveListener(HandleUpgradeButtonClicked);
            upgradeButton.onClick.AddListener(HandleUpgradeButtonClicked);
        }

        BindExitButtonClick();

        if (embeddedUpgradePanel != null)
        {
            embeddedUpgradePanel.gameObject.SetActive(false);
        }
    }

    private void Start()
    {
        if (loadOnStart)
        {
            LoadUpgradeScene();
        }
    }

    private void OnDestroy()
    {
        if (upgradeButton != null)
        {
            upgradeButton.onClick.RemoveListener(HandleUpgradeButtonClicked);
        }

        if (exitButton != null)
        {
            exitButton.onClick.RemoveListener(CloseUpgradePanel);
        }

        if (unloadOnDestroy)
        {
            UnloadUpgradeScene();
        }
    }

    public void ToggleUpgradeScene()
    {
        if (embeddedUpgradePanel != null)
        {
            ToggleEmbeddedUpgradePanel();
            return;
        }

        if (IsUpgradeSceneLoaded())
        {
            UnloadUpgradeScene();
        }
        else
        {
            LoadUpgradeScene();
        }
    }

    public void LoadUpgradeScene()
    {
        if (embeddedUpgradePanel != null)
        {
            ShowEmbeddedUpgradePanel();
            return;
        }

        if (isLoading || IsUpgradeSceneLoaded())
        {
            return;
        }

        isLoading = true;
        AsyncOperation operation = SceneManager.LoadSceneAsync(upgradeSceneName, LoadSceneMode.Additive);
        if (operation == null)
        {
            isLoading = false;
            Debug.LogWarning($"{nameof(UpgradeSceneLoader)}: Failed to start loading scene '{upgradeSceneName}'. Make sure it is added to Build Settings.");
            return;
        }

        operation.completed += HandleUpgradeSceneLoaded;
    }

    public void UnloadUpgradeScene()
    {
        if (embeddedUpgradePanel != null)
        {
            HideEmbeddedUpgradePanel();
            return;
        }

        if (loadedCanvasRoot != null)
        {
            Destroy(loadedCanvasRoot);
            loadedCanvasRoot = null;
        }

        Scene upgradeScene = SceneManager.GetSceneByName(upgradeSceneName);
        if (!upgradeScene.isLoaded)
        {
            return;
        }

        SceneManager.UnloadSceneAsync(upgradeScene);
    }

    public void CloseUpgradePanel()
    {
        if (embeddedUpgradePanel != null)
        {
            HideEmbeddedUpgradePanel();
            return;
        }

        UnloadUpgradeScene();
    }

    private void HandleUpgradeButtonClicked()
    {
        if (embeddedUpgradePanel != null)
        {
            ToggleEmbeddedUpgradePanel();
            return;
        }

        LoadUpgradeScene();
    }

    private void ToggleEmbeddedUpgradePanel()
    {
        if (embeddedUpgradePanel.gameObject.activeSelf)
        {
            HideEmbeddedUpgradePanel();
        }
        else
        {
            ShowEmbeddedUpgradePanel();
        }
    }

    private void ShowEmbeddedUpgradePanel()
    {
        embeddedUpgradePanel.gameObject.SetActive(true);
        BindExitButtonClick();

        if (rebuildEmbeddedPanelOnOpen && embeddedUpgradeBuilder != null)
        {
            embeddedUpgradeBuilder.BuildButtons();
        }
    }

    private void HideEmbeddedUpgradePanel()
    {
        embeddedUpgradePanel.gameObject.SetActive(false);
    }

    private void HandleUpgradeSceneLoaded(AsyncOperation operation)
    {
        isLoading = false;

        Scene upgradeScene = SceneManager.GetSceneByName(upgradeSceneName);
        if (!upgradeScene.isLoaded)
        {
            return;
        }

        if (disableLoadedSceneSystems)
        {
            DisableLoadedSceneSystems(upgradeScene);
        }

        if (matchTargetCanvasSize)
        {
            FitLoadedSceneToTargetCanvas(upgradeScene);
        }

        if (setLoadedSceneActive)
        {
            SceneManager.SetActiveScene(upgradeScene);
        }
    }

    private bool IsUpgradeSceneLoaded()
    {
        return SceneManager.GetSceneByName(upgradeSceneName).isLoaded;
    }

    private void DisableLoadedSceneSystems(Scene upgradeScene)
    {
        foreach (GameObject rootObject in upgradeScene.GetRootGameObjects())
        {
            foreach (EventSystem eventSystem in rootObject.GetComponentsInChildren<EventSystem>(true))
            {
                eventSystem.gameObject.SetActive(false);
            }

            foreach (AudioListener audioListener in rootObject.GetComponentsInChildren<AudioListener>(true))
            {
                audioListener.enabled = false;
            }

            foreach (Camera sceneCamera in rootObject.GetComponentsInChildren<Camera>(true))
            {
                sceneCamera.enabled = false;
            }
        }
    }

    private void BindMissingReferences()
    {
        if (upgradeButton == null)
        {
            GameObject upgradeButtonObject = GameObject.Find("UpgradeBtn");
            if (upgradeButtonObject != null)
            {
                upgradeButton = upgradeButtonObject.GetComponent<Button>();
            }
        }

        if (targetCanvasRect == null && upgradeButton != null)
        {
            Canvas parentCanvas = upgradeButton.GetComponentInParent<Canvas>();
            if (parentCanvas != null)
            {
                targetCanvasRect = parentCanvas.transform as RectTransform;
            }
        }

        if (exitButton == null && embeddedUpgradePanel != null)
        {
            exitButton = FindButtonInChildren(embeddedUpgradePanel, "ExitBtn", "Exit", "CloseBtn", "Close");
        }
    }

    private void BindExitButtonClick()
    {
        if (exitButton == null && embeddedUpgradePanel != null)
        {
            exitButton = FindButtonInChildren(embeddedUpgradePanel, "ExitBtn", "Exit", "CloseBtn", "Close");
        }

        if (exitButton == null)
        {
            return;
        }

        exitButton.onClick.RemoveListener(CloseUpgradePanel);
        exitButton.onClick.AddListener(CloseUpgradePanel);
    }

    private Button FindButtonInChildren(Transform root, params string[] names)
    {
        foreach (string buttonName in names)
        {
            Transform child = FindChildRecursive(root, buttonName);
            if (child != null && child.TryGetComponent(out Button button))
            {
                return button;
            }
        }

        return null;
    }

    private Transform FindChildRecursive(Transform root, string objectName)
    {
        if (root.name == objectName)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChildRecursive(root.GetChild(i), objectName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private void FitLoadedSceneToTargetCanvas(Scene upgradeScene)
    {
        BindMissingReferences();

        if (targetCanvasRect == null)
        {
            Debug.LogWarning($"{nameof(UpgradeSceneLoader)}: Target Canvas RectTransform is missing.");
            return;
        }

        Canvas loadedCanvas = FindCanvasInScene(upgradeScene);
        if (loadedCanvas == null)
        {
            Debug.LogWarning($"{nameof(UpgradeSceneLoader)}: Could not find a Canvas in scene '{upgradeSceneName}'.");
            return;
        }

        RectTransform loadedCanvasRect = loadedCanvas.transform as RectTransform;
        if (loadedCanvasRect == null)
        {
            return;
        }

        Canvas targetCanvas = targetCanvasRect.GetComponent<Canvas>();
        if (targetCanvas == null)
        {
            targetCanvas = targetCanvasRect.GetComponentInParent<Canvas>();
        }

        loadedCanvasRoot = loadedCanvas.gameObject;
        loadedCanvasRect.SetParent(targetCanvasRect, false);
        if (showAsWindow)
        {
            ApplyWindowRect(loadedCanvasRect);
        }
        else
        {
            ApplyFullTargetRect(loadedCanvasRect);
        }

        loadedCanvas.overrideSorting = true;
        loadedCanvas.sortingOrder = targetCanvas != null ? targetCanvas.sortingOrder + sortingOrderOffset : sortingOrderOffset;

        MatchCanvasScaler(loadedCanvas, targetCanvas, loadedCanvasRect.rect.size);

        if (scaleContentToFit)
        {
            ScaleContentToFitTarget(loadedCanvasRect);
        }
    }

    private void ApplyFullTargetRect(RectTransform loadedCanvasRect)
    {
        loadedCanvasRect.anchorMin = Vector2.zero;
        loadedCanvasRect.anchorMax = Vector2.one;
        loadedCanvasRect.pivot = new Vector2(0.5f, 0.5f);
        loadedCanvasRect.anchoredPosition = Vector2.zero;
        loadedCanvasRect.sizeDelta = Vector2.zero;
        loadedCanvasRect.localScale = Vector3.one;
    }

    private void ApplyWindowRect(RectTransform loadedCanvasRect)
    {
        Vector2 size = windowSize;
        if (clampWindowToTarget)
        {
            Vector2 targetSize = targetCanvasRect.rect.size;
            if (targetSize.x > 0f && targetSize.y > 0f)
            {
                size.x = Mathf.Min(size.x, targetSize.x);
                size.y = Mathf.Min(size.y, targetSize.y);
            }
        }

        loadedCanvasRect.anchorMin = new Vector2(0.5f, 0.5f);
        loadedCanvasRect.anchorMax = new Vector2(0.5f, 0.5f);
        loadedCanvasRect.pivot = new Vector2(0.5f, 0.5f);
        loadedCanvasRect.anchoredPosition = windowAnchoredPosition;
        loadedCanvasRect.sizeDelta = size;
        loadedCanvasRect.localScale = Vector3.one;
    }

    private Canvas FindCanvasInScene(Scene scene)
    {
        foreach (GameObject rootObject in scene.GetRootGameObjects())
        {
            Canvas canvas = rootObject.GetComponentInChildren<Canvas>(true);
            if (canvas != null)
            {
                return canvas;
            }
        }

        return null;
    }

    private void MatchCanvasScaler(Canvas loadedCanvas, Canvas targetCanvas, Vector2 fittedSize)
    {
        CanvasScaler loadedScaler = loadedCanvas.GetComponent<CanvasScaler>();
        if (loadedScaler == null)
        {
            return;
        }

        Vector2 targetSize = fittedSize;
        if (targetSize.x <= 0f || targetSize.y <= 0f)
        {
            targetSize = targetCanvasRect.rect.size;
        }

        CanvasScaler targetScaler = targetCanvas != null ? targetCanvas.GetComponent<CanvasScaler>() : null;
        if (targetScaler != null)
        {
            loadedScaler.uiScaleMode = targetScaler.uiScaleMode;
            loadedScaler.referencePixelsPerUnit = targetScaler.referencePixelsPerUnit;
            loadedScaler.scaleFactor = targetScaler.scaleFactor;
            loadedScaler.referenceResolution = targetSize;
            loadedScaler.screenMatchMode = targetScaler.screenMatchMode;
            loadedScaler.matchWidthOrHeight = targetScaler.matchWidthOrHeight;
            return;
        }

        loadedScaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        loadedScaler.scaleFactor = 1f;
        loadedScaler.referenceResolution = targetSize;
    }

    private void ScaleContentToFitTarget(RectTransform loadedCanvasRect)
    {
        RectTransform content = null;
        for (int i = 0; i < loadedCanvasRect.childCount; i++)
        {
            RectTransform child = loadedCanvasRect.GetChild(i) as RectTransform;
            if (child != null && child.gameObject.activeSelf)
            {
                content = child;
                break;
            }
        }

        if (content == null)
        {
            return;
        }

        Vector2 targetSize = targetCanvasRect.rect.size;
        Vector2 contentSize = content.rect.size;
        if (targetSize.x <= 0f || targetSize.y <= 0f || contentSize.x <= 0f || contentSize.y <= 0f)
        {
            return;
        }

        float scale = Mathf.Min(targetSize.x / contentSize.x, targetSize.y / contentSize.y);
        content.anchorMin = new Vector2(0.5f, 0.5f);
        content.anchorMax = new Vector2(0.5f, 0.5f);
        content.pivot = new Vector2(0.5f, 0.5f);
        content.anchoredPosition = Vector2.zero;
        content.localScale = new Vector3(scale, scale, 1f);
    }
}
