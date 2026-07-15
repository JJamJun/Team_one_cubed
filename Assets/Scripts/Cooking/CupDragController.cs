using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum CupContentState
{
    Normal,
    IceTeaEd,
    WaterPotEd
}

public class CupDragController : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [SerializeField] private RectTransform cupRect;
    [SerializeField] private Image cupImage;
    [SerializeField] private RectTransform trashCanArea;
    [SerializeField] private RectTransform canvasRect;
    [SerializeField] private bool hideOnStart = true;

    private Camera eventCamera;
    private bool isDragging;
    private CupContentState contentState = CupContentState.Normal;

    public CupContentState ContentState => contentState;

    private void Awake()
    {
        if (cupRect == null)
        {
            cupRect = GetComponent<RectTransform>();
        }

        if (cupImage == null)
        {
            cupImage = GetComponent<Image>();
        }

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            canvasRect = canvas.transform as RectTransform;
            eventCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        }

        if (hideOnStart)
        {
            HideCup();
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!IsCupVisible())
        {
            return;
        }

        BeginDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        Drag(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        EndDrag(eventData);
    }

    public void BeginDragFromSource(PointerEventData eventData)
    {
        ShowCup();
        SetContentState(CupContentState.Normal);
        BeginDrag(eventData);
    }

    public void DragFromSource(PointerEventData eventData)
    {
        Drag(eventData);
    }

    public void EndDragFromSource(PointerEventData eventData)
    {
        EndDrag(eventData);
    }

    private void BeginDrag(PointerEventData eventData)
    {
        isDragging = true;
        MoveCupToPointer(eventData);
    }

    private void Drag(PointerEventData eventData)
    {
        if (!isDragging)
        {
            return;
        }

        MoveCupToPointer(eventData);
    }

    private void EndDrag(PointerEventData eventData)
    {
        if (!isDragging)
        {
            return;
        }

        MoveCupToPointer(eventData);
        isDragging = false;

        if (IsPointerInsideTrashCan(eventData))
        {
            HideCup();
        }
    }

    public bool IsPointerInsideCup(PointerEventData eventData)
    {
        return IsCupVisible()
            && cupRect != null
            && RectTransformUtility.RectangleContainsScreenPoint(cupRect, eventData.position, eventCamera);
    }

    public void ApplyIngredient(CupContentState newState)
    {
        if (!IsCupVisible())
        {
            return;
        }

        SetContentState(newState);
    }

    private void MoveCupToPointer(PointerEventData eventData)
    {
        if (canvasRect == null || cupRect == null)
        {
            return;
        }

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, eventData.position, eventCamera, out Vector2 localPoint))
        {
            cupRect.anchoredPosition = localPoint;
        }
    }

    private bool IsPointerInsideTrashCan(PointerEventData eventData)
    {
        return trashCanArea != null && RectTransformUtility.RectangleContainsScreenPoint(trashCanArea, eventData.position, eventCamera);
    }

    private bool IsCupVisible()
    {
        return cupImage != null && cupImage.enabled;
    }

    private void ShowCup()
    {
        if (cupImage == null)
        {
            return;
        }

        cupImage.enabled = true;
        cupImage.raycastTarget = true;
    }

    private void HideCup()
    {
        isDragging = false;
        SetContentState(CupContentState.Normal);

        if (cupImage == null)
        {
            return;
        }

        cupImage.enabled = false;
        cupImage.raycastTarget = false;
    }

    private void SetContentState(CupContentState newState)
    {
        contentState = newState;
        Debug.Log($"CUP state: {contentState}");
    }
}
