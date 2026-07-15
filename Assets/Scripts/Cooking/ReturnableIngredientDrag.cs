using UnityEngine;
using UnityEngine.EventSystems;

public class ReturnableIngredientDrag : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [SerializeField] private RectTransform targetRect;
    [SerializeField] private RectTransform canvasRect;
    [SerializeField] private CupDragController cupDragController;
    [SerializeField] private CupContentState cupStateOnDrop = CupContentState.Normal;

    private Camera eventCamera;
    private Vector2 originalPosition;
    private bool isDragging;

    private void Awake()
    {
        if (targetRect == null)
        {
            targetRect = GetComponent<RectTransform>();
        }

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            canvasRect = canvas.transform as RectTransform;
            eventCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        }

        if (targetRect != null)
        {
            originalPosition = targetRect.anchoredPosition;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isDragging = true;
        MoveToPointer(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging)
        {
            return;
        }

        MoveToPointer(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isDragging)
        {
            return;
        }

        isDragging = false;

        if (cupDragController != null && cupDragController.IsPointerInsideCup(eventData))
        {
            cupDragController.ApplyIngredient(cupStateOnDrop);
        }

        ReturnToOriginalPosition();
    }

    private void MoveToPointer(PointerEventData eventData)
    {
        if (canvasRect == null || targetRect == null)
        {
            return;
        }

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, eventData.position, eventCamera, out Vector2 localPoint))
        {
            targetRect.anchoredPosition = localPoint;
        }
    }

    private void ReturnToOriginalPosition()
    {
        if (targetRect != null)
        {
            targetRect.anchoredPosition = originalPosition;
        }
    }
}
