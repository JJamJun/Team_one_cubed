using UnityEngine;
using UnityEngine.EventSystems;

public class ReturnableIngredientDrag : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [SerializeField] private RectTransform targetRect;
    [SerializeField] private CupContentState cupStateOnDrop = CupContentState.Normal;

    private Camera eventCamera;
    private RectTransform dragParentRect;
    private Vector2 originalAnchoredPosition;
    private Vector2 dragCenterOffset;
    private bool isDragging;
    private bool hasCachedOriginalPosition;

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
        }
    }

    private void Start()
    {
        Canvas.ForceUpdateCanvases();
        CacheOriginalPosition();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!hasCachedOriginalPosition)
        {
            CacheOriginalPosition();
        }

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
            targetRect.anchoredPosition = ConvertParentLocalPointToAnchoredPosition(localPoint - dragCenterOffset);
        }
    }

    private Vector2 ConvertParentLocalPointToAnchoredPosition(Vector2 parentLocalPoint)
    {
        Rect parentRect = dragParentRect.rect;
        Vector2 anchorCenter = (targetRect.anchorMin + targetRect.anchorMax) * 0.5f;
        Vector2 anchorReferencePoint = new Vector2(
            Mathf.Lerp(parentRect.xMin, parentRect.xMax, anchorCenter.x),
            Mathf.Lerp(parentRect.yMin, parentRect.yMax, anchorCenter.y));

        return parentLocalPoint - anchorReferencePoint;
    }

    private Vector2 GetCenterOffset()
    {
        if (targetRect == null)
        {
            return Vector2.zero;
        }

        Rect rect = targetRect.rect;
        return new Vector2(
            (0.5f - targetRect.pivot.x) * rect.width,
            (0.5f - targetRect.pivot.y) * rect.height);
    }

    private void ReturnToOriginalPosition()
    {
        if (targetRect != null)
        {
            targetRect.anchoredPosition = originalAnchoredPosition;
        }
    }

    private void CacheOriginalPosition()
    {
        if (targetRect == null)
        {
            return;
        }

        originalAnchoredPosition = targetRect.anchoredPosition;
        hasCachedOriginalPosition = true;
    }
}
