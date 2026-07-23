using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Image))]
public class CustomCursor : MonoBehaviour
{
    public static CustomCursor Instance { get; private set; }

    [Header("Cursor Graphics")]
    [SerializeField] private Sprite idleSprite;
    [SerializeField] private Sprite clickSprite;

    private Image cursorImage;
    private RectTransform rectTransform;
    private Canvas parentCanvas;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject.transform.root.gameObject);

        cursorImage = GetComponent<Image>();
        rectTransform = GetComponent<RectTransform>();
        parentCanvas = GetComponentInParent<Canvas>();

        cursorImage.raycastTarget = false;

        SetCursorState(true);
    }

    private void Update()
    {
        // Safety check in case the mouse isn't connected or initialized yet
        if (Mouse.current == null) return;

        // 1. Get Mouse Position via New Input System
        Vector2 mousePosition = Mouse.current.position.ReadValue();

        // 2. Position the UI Element dynamically based on Canvas scale
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentCanvas.transform as RectTransform,
            mousePosition,
            parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : parentCanvas.worldCamera,
            out Vector2 movePos);

        rectTransform.localPosition = movePos;

        // 3. Handle visual state via New Input System
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            cursorImage.sprite = clickSprite;
        }
        else if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            cursorImage.sprite = idleSprite;
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        SetCursorState(hasFocus);
    }

    private void SetCursorState(bool isFocused)
    {
        if (isFocused)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Confined;
        }
        else
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }
}