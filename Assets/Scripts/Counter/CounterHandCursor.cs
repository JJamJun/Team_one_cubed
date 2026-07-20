using UnityEngine;
using UnityEngine.InputSystem;

public class CounterHandCursor : MonoBehaviour
{
    [SerializeField] private RectTransform counterPanel;
    [SerializeField] private RectTransform hand;
    [SerializeField] private Canvas handCanvas;
    [SerializeField] private Vector2 handOffset = Vector2.zero;
    [SerializeField] private bool forceTopLeftPivot = true;
    [SerializeField] private float clickZRotation = 2f;

    private void Awake()
    {
        AutoBindMissingReferences();
        ApplyHandPivot();

        if (hand != null)
        {
            hand.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (counterPanel == null || hand == null)
        {
            return;
        }

        Vector2 pointerPosition = GetPointerScreenPosition();
        bool pointerInsideCounter = IsPointerInsideCounterPanel(pointerPosition);
        if (hand.gameObject.activeSelf != pointerInsideCounter)
        {
            hand.gameObject.SetActive(pointerInsideCounter);
        }

        if (!pointerInsideCounter)
        {
            return;
        }

        FollowPointer(pointerPosition);

        UpdateClickRotation();
    }

    private void FollowPointer(Vector2 pointerPosition)
    {
        RectTransform canvasRect = GetHandCanvasRect();
        if (canvasRect == null)
        {
            hand.position = pointerPosition + handOffset;
            return;
        }

        Camera eventCamera = GetHandEventCamera();
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, pointerPosition, eventCamera, out Vector2 localPoint))
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, pointerPosition, null, out localPoint);
        }

        hand.anchoredPosition = localPoint + handOffset;
        hand.SetAsLastSibling();
    }

    private void UpdateClickRotation()
    {
        hand.localEulerAngles = IsPrimaryPointerPressed()
            ? new Vector3(0f, 0f, clickZRotation)
            : Vector3.zero;
    }

    private bool IsPointerInsideCounterPanel(Vector2 pointerPosition)
    {
        return RectTransformUtility.RectangleContainsScreenPoint(counterPanel, pointerPosition, GetCounterPanelEventCamera())
            || RectTransformUtility.RectangleContainsScreenPoint(counterPanel, pointerPosition, null)
            || RectTransformUtility.RectangleContainsScreenPoint(counterPanel, pointerPosition, Camera.main);
    }

    private Vector2 GetPointerScreenPosition()
    {
        if (Mouse.current != null)
        {
            return Mouse.current.position.ReadValue();
        }

        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            return Touchscreen.current.primaryTouch.position.ReadValue();
        }

        return Vector2.zero;
    }

    private bool IsPrimaryPointerPressed()
    {
        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            return true;
        }

        return Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed;
    }

    private void ApplyHandPivot()
    {
        if (!forceTopLeftPivot || hand == null)
        {
            return;
        }

        hand.pivot = new Vector2(0f, 1f);
    }

    private RectTransform GetHandCanvasRect()
    {
        if (handCanvas == null)
        {
            return null;
        }

        return handCanvas.transform as RectTransform;
    }

    private Camera GetHandEventCamera()
    {
        if (handCanvas == null || handCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            return null;
        }

        return handCanvas.worldCamera;
    }

    private Camera GetCounterPanelEventCamera()
    {
        Canvas counterCanvas = counterPanel != null ? counterPanel.GetComponentInParent<Canvas>(true) : null;
        if (counterCanvas == null || counterCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            return null;
        }

        return counterCanvas.worldCamera;
    }

    private void AutoBindMissingReferences()
    {
        if (counterPanel == null)
        {
            GameObject counterPanelObject = FindGameObjectByName("CounterPanel");
            if (counterPanelObject != null)
            {
                counterPanel = counterPanelObject.transform as RectTransform;
            }
        }

        if (hand == null)
        {
            GameObject handObject = FindGameObjectByName("hand");
            if (handObject != null)
            {
                hand = handObject.transform as RectTransform;
            }
        }

        if (handCanvas == null && hand != null)
        {
            handCanvas = hand.GetComponentInParent<Canvas>(true);
        }
    }

    private static GameObject FindGameObjectByName(string targetName)
    {
        Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform target = transforms[i];
            if (target.gameObject.scene.IsValid() && target.name == targetName)
            {
                return target.gameObject;
            }
        }

        return null;
    }
}
