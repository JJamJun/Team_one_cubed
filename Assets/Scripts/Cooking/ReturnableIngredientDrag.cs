using UnityEngine;
using UnityEngine.EventSystems;

public class ReturnableIngredientDrag : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [SerializeField] private RectTransform targetRect;
    [SerializeField] private CupContentState cupStateOnDrop = CupContentState.Normal;

    private Camera eventCamera;
    private RectTransform dragParentRect;
    private Vector3 originalLocalPosition;
    private Vector3 dragCenterOffset;
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
            eventCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        }

        if (targetRect != null)
        {
            dragParentRect = targetRect.parent as RectTransform;
            originalLocalPosition = targetRect.localPosition;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isDragging = true;
        dragCenterOffset = GetCenterOffset();
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

        CupDragController targetCup = CupDragController.FindSpawnedCupAtPointer(eventData);
        if (targetCup != null)
        {
            targetCup.ApplyIngredient(cupStateOnDrop);
        }

        ReturnToOriginalPosition();
    }

    private void MoveToPointer(PointerEventData eventData)
    {
        if (dragParentRect == null || targetRect == null)
        {
            return;
        }

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(dragParentRect, eventData.position, eventCamera, out Vector2 localPoint))
        {
            targetRect.localPosition = new Vector3(localPoint.x, localPoint.y, originalLocalPosition.z) - dragCenterOffset;
        }
    }

    private Vector3 GetCenterOffset()
    {
        if (targetRect == null)
        {
            return Vector3.zero;
        }

        Rect rect = targetRect.rect;
        return new Vector3(
            (0.5f - targetRect.pivot.x) * rect.width,
            (0.5f - targetRect.pivot.y) * rect.height,
            0f);
    }

    private void ReturnToOriginalPosition()
    {
        if (targetRect != null)
        {
            targetRect.localPosition = originalLocalPosition;
        }
    }
}
