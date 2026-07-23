using UnityEngine.EventSystems;
using UnityEngine.UI;

public class VerticalWheelScrollRect : ScrollRect
{
    protected override void Awake()
    {
        base.Awake();
        ForceVerticalOnly();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        ForceVerticalOnly();
    }

    public override void OnInitializePotentialDrag(PointerEventData eventData)
    {
    }

    public override void OnBeginDrag(PointerEventData eventData)
    {
    }

    public override void OnDrag(PointerEventData eventData)
    {
    }

    public override void OnEndDrag(PointerEventData eventData)
    {
    }

    private void ForceVerticalOnly()
    {
        horizontal = false;
        vertical = true;
    }
}
