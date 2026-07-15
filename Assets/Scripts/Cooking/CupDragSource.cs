using UnityEngine;
using UnityEngine.EventSystems;

public class CupDragSource : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [SerializeField] private CupDragController cupDragController;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (cupDragController == null)
        {
            Debug.LogWarning($"{nameof(CupDragSource)}: CupDragController is not assigned.");
            return;
        }

        cupDragController.BeginDragFromSource(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (cupDragController != null)
        {
            cupDragController.DragFromSource(eventData);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (cupDragController != null)
        {
            cupDragController.EndDragFromSource(eventData);
        }
    }
}
