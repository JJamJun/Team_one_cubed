using UnityEngine;
using UnityEngine.EventSystems;

public class CupDragSource : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [SerializeField] private CupDragController cupTemplate;
    [SerializeField] private RectTransform spawnParent;
    [SerializeField] private RectTransform trashCanArea;
    [SerializeField] private RectTransform iceMachineArea;
    [SerializeField] private RectTransform coffeeMachineArea;
    [SerializeField] private RectTransform coffeeMachinePosArea;
    [SerializeField] private RectTransform syrupSnapArea;
    [SerializeField] private RectTransform syrupPosArea;
    [SerializeField] private RectTransform cookingStartArea;
    [SerializeField] private GameObject cookingScreen;
    [SerializeField] private CookingMiniGameController cookingMiniGameController;
    [SerializeField] private RectTransform canvasRect;

    private CupDragController activeSpawnedCup;

    private void Awake()
    {
        if (spawnParent == null && cupTemplate != null)
        {
            spawnParent = cupTemplate.transform.parent as RectTransform;
        }

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvasRect == null && canvas != null)
        {
            canvasRect = canvas.transform as RectTransform;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (cupTemplate == null)
        {
            Debug.LogWarning($"{nameof(CupDragSource)}: Cup template is not assigned.");
            return;
        }

        activeSpawnedCup = Instantiate(cupTemplate, spawnParent);
        activeSpawnedCup.gameObject.SetActive(true);
        activeSpawnedCup.name = "CupSprite";
        SoundManager.Instance?.SFX?.PlayCupDrag();
        activeSpawnedCup.InitializeSpawnedCup(
            trashCanArea,
            iceMachineArea,
            coffeeMachineArea,
            coffeeMachinePosArea,
            syrupSnapArea,
            syrupPosArea,
            cookingStartArea,
            cookingScreen,
            cookingMiniGameController,
            canvasRect);
        activeSpawnedCup.BeginDragFromSource(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (activeSpawnedCup != null)
        {
            activeSpawnedCup.DragFromSource(eventData);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (activeSpawnedCup != null)
        {
            activeSpawnedCup.EndDragFromSource(eventData);
            activeSpawnedCup = null;
        }
    }
}
