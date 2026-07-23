using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TransitionManager : MonoBehaviour
{
    // Singleton instance
    public static TransitionManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Material transitionMaterial;
    [SerializeField] private CanvasGroup overlayCanvasGroup;

    [Header("Settings")]
    [SerializeField] private float transitionTime = 0.5f;

    // Set this high enough so the shape fully clears the corners of the screen.
    // A value of 2 or 3 usually works depending on your mask's built-in padding.
    [SerializeField] private float maxRadius = 2.5f;

    // Cache property IDs for better performance
    private readonly int radiusProp = Shader.PropertyToID("_Radius");
    private readonly int centerProp = Shader.PropertyToID("_Center");

    private void Awake()
    {
        // Enforce Singleton pattern and keep this alive between scenes
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Reset state on boot
        transitionMaterial.SetFloat(radiusProp, maxRadius);
        overlayCanvasGroup.blocksRaycasts = false;
    }

    /// <summary>
    /// Call this from any script: TransitionManager.Instance.ChangeScene("Level_02", playerTransform);
    /// </summary>
    public void ChangeScene(string sceneName, Transform focusTarget = null)
    {
        StartCoroutine(TransitionRoutine(sceneName, focusTarget));
    }

    private IEnumerator TransitionRoutine(string sceneName, Transform focusTarget)
    {
        // 1. Lock UI and inputs
        overlayCanvasGroup.blocksRaycasts = true;

        // 2. Set the initial focus point (e.g., the player)
        UpdateShaderCenter(focusTarget);

        // 3. Animate Close (Wipe In)
        float elapsed = 0f;
        while (elapsed < transitionTime)
        {
            elapsed += Time.deltaTime;
            // SmoothStep makes the animation ease in and out nicely
            float t = Mathf.SmoothStep(0f, 1f, elapsed / transitionTime);
            transitionMaterial.SetFloat(radiusProp, Mathf.Lerp(maxRadius, 0f, t));
            yield return null;
        }
        transitionMaterial.SetFloat(radiusProp, 0f);

        // 4. Load the new scene while the screen is totally black
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // 5. Update focus point in the new scene
        // Grabbing the player by tag is a reliable way to find them after a load
        GameObject newPlayer = GameObject.FindWithTag("Player");
        UpdateShaderCenter(newPlayer ? newPlayer.transform : null);

        // 6. Animate Open (Wipe Out)
        elapsed = 0f;
        while (elapsed < transitionTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / transitionTime);
            transitionMaterial.SetFloat(radiusProp, Mathf.Lerp(0f, maxRadius, t));
            yield return null;
        }
        transitionMaterial.SetFloat(radiusProp, maxRadius);

        // 7. Unlock inputs for the new scene
        overlayCanvasGroup.blocksRaycasts = false;
    }

    private void UpdateShaderCenter(Transform target)
    {
        Vector2 uvCenter = new Vector2(0.5f, 0.5f); // Default to exact middle of the screen

        if (target != null)
        {
            // Check if the target is part of the UI Canvas
            RectTransform uiElement = target.GetComponent<RectTransform>();

            if (uiElement != null)
            {
                // SCENARIO A: Target is a UI Element (e.g., your Main Menu empty GameObject)
                // Figure out if we need a camera for the math based on Canvas settings
                Canvas parentCanvas = uiElement.GetComponentInParent<Canvas>();
                Camera cam = null;
                if (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
                {
                    cam = Camera.main;
                }

                Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(cam, uiElement.position);
                uvCenter.x = screenPos.x / Screen.width;
                uvCenter.y = screenPos.y / Screen.height;
            }
            else if (Camera.main != null)
            {
                // SCENARIO B: Target is a standard World Object (e.g., your Player)
                Vector3 screenPos = Camera.main.WorldToScreenPoint(target.position);
                uvCenter.x = screenPos.x / Screen.width;
                uvCenter.y = screenPos.y / Screen.height;
            }
        }

        transitionMaterial.SetVector(centerProp, uvCenter);
    }

    public void QuitGame(Transform focusTarget = null)
    {
        StartCoroutine(QuitRoutine(focusTarget));
    }

    private IEnumerator QuitRoutine(Transform focusTarget)
    {
        // 1. Lock UI and inputs
        overlayCanvasGroup.blocksRaycasts = true;

        // 2. Set the focus point
        UpdateShaderCenter(focusTarget);

        // 3. Animate Close (Wipe In)
        float elapsed = 0f;
        while (elapsed < transitionTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / transitionTime);
            transitionMaterial.SetFloat(radiusProp, Mathf.Lerp(maxRadius, 0f, t));
            yield return null;
        }
        transitionMaterial.SetFloat(radiusProp, 0f);

        // 4. Quit the application
        // Note: Application.Quit() is ignored inside the Unity Editor. 
        // We use preprocessor directives to stop Play Mode in the Editor, and quit normally in a build.
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}