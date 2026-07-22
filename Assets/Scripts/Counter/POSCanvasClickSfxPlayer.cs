using UnityEngine;
using UnityEngine.InputSystem;

public class POSCanvasClickSfxPlayer : MonoBehaviour
{
    [SerializeField] private RectTransform clickArea;
    [SerializeField] private Camera eventCamera;

    private void Awake()
    {
        if (clickArea == null)
        {
            clickArea = transform as RectTransform;
        }

        if (eventCamera == null)
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                eventCamera = canvas.worldCamera;
            }
        }
    }

    private void Update()
    {
        if (clickArea == null || !WasPrimaryPointerPressedThisFrame())
        {
            return;
        }

        if (RectTransformUtility.RectangleContainsScreenPoint(clickArea, GetPointerScreenPosition(), eventCamera))
        {
            SoundManager.Instance?.SFX?.PlayClick();
        }
    }

    private static bool WasPrimaryPointerPressedThisFrame()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            return true;
        }

        return Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame;
    }

    private static Vector2 GetPointerScreenPosition()
    {
        if (Mouse.current != null)
        {
            return Mouse.current.position.ReadValue();
        }

        if (Touchscreen.current != null)
        {
            return Touchscreen.current.primaryTouch.position.ReadValue();
        }

        return Vector2.zero;
    }
}
