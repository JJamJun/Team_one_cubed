using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum CupContentState
{
    Normal,
    IceTeaEd,
    WaterPotEd,
    IceMachineEd,
    CoffeeMachineEd,
    SyrupEd,
    Failed
}

public enum CupCookingResultState
{
    None,
    Succeeded,
    Failed
}

public class CupDragController : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    private static readonly List<CupDragController> spawnedCups = new List<CupDragController>();

    public static event Action<bool> CupContentAvailabilityChanged;

    [SerializeField] private RectTransform cupRect;
    [SerializeField] private Image cupImage;
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
    [SerializeField] private bool hideOnStart = true;
    [SerializeField] private bool isTemplate = true;
    [Header("Cup Content Sprites")]
    [SerializeField] private Sprite emptyCupSprite;
    [SerializeField] private Sprite waterCupSprite;
    [SerializeField] private Sprite iceCupSprite;
    [SerializeField] private Sprite waterIceCupSprite;
    [SerializeField] private Sprite teaCupSprite;
    [SerializeField] private Sprite teaIceCupSprite;
    [SerializeField] private Sprite shotCupSprite;
    [SerializeField] private Sprite shotIceCupSprite;
    [SerializeField] private Sprite shotWaterCupSprite;
    [SerializeField] private Sprite shotWaterIceCupSprite;
    [SerializeField] private Sprite failedCupSprite;
    [SerializeField] private Sprite lockedCupSprite;
    [SerializeField] private GameObject successObject;

    private Camera eventCamera;
    private bool isDragging;
    private readonly List<CupContentState> contentStates = new List<CupContentState>();
    [SerializeField] private CupCookingResultState cookingResultState = CupCookingResultState.None;
    [SerializeField] private string completedMenuName = string.Empty;

    public static IReadOnlyList<CupDragController> SpawnedCups => spawnedCups;
    public IReadOnlyList<CupContentState> ContentStates => contentStates;
    public CupCookingResultState CookingResultState => cookingResultState;
    public string CompletedMenuName => completedMenuName;

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

        AutoBindSuccessObject();
        CaptureDefaultCupSprite();
        SetSuccessVisible(false);

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

        if (isTemplate && cookingScreen != null)
        {
            cookingScreen.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        spawnedCups.Remove(this);
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
        ResetContentStates();
        BeginDrag(eventData);
    }

    public void InitializeSpawnedCup(
        RectTransform trashArea,
        RectTransform iceMachineDropArea,
        RectTransform coffeeMachineDropArea,
        RectTransform coffeeMachineSlotArea,
        RectTransform syrupSnapDropArea,
        RectTransform syrupSlotArea,
        RectTransform cookingStartDropArea,
        GameObject cookingScreenPopup,
        CookingMiniGameController miniGameController,
        RectTransform rootCanvasRect)
    {
        isTemplate = false;
        hideOnStart = false;
        trashCanArea = trashArea;
        iceMachineArea = iceMachineDropArea;
        coffeeMachineArea = coffeeMachineDropArea;
        coffeeMachinePosArea = coffeeMachineSlotArea;
        syrupSnapArea = syrupSnapDropArea;
        syrupPosArea = syrupSlotArea;
        cookingStartArea = cookingStartDropArea;
        cookingScreen = cookingScreenPopup;
        cookingMiniGameController = miniGameController;
        canvasRect = rootCanvasRect;

        if (cupRect == null)
        {
            cupRect = GetComponent<RectTransform>();
        }

        if (cupImage == null)
        {
            cupImage = GetComponent<Image>();
        }

        AutoBindSuccessObject();
        CaptureDefaultCupSprite();
        SetSuccessVisible(false);

        Canvas canvas = GetComponentInParent<Canvas>();
        eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;

        ShowCup();
        ResetContentStates();

        if (!spawnedCups.Contains(this))
        {
            spawnedCups.Add(this);
        }

        NotifyCupContentAvailabilityChanged();
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

        Receipt receipt = FindReceiptAtPointer(eventData);
        if (receipt != null)
        {
            if (receipt.TryFulfillReceipt(this))
            {
                HideCup();
            }

            return;
        }

        if (IsPointerInsideTrashCan(eventData))
        {
            HideCup();
            return;
        }

        if (IsPointerInside(iceMachineArea, eventData))
        {
            ApplyIngredient(CupContentState.IceMachineEd);
        }

        if (IsPointerInside(coffeeMachineArea, eventData))
        {
            ApplyIngredient(CupContentState.CoffeeMachineEd);
            SnapToCoffeeMachinePos();
        }

        if (IsPointerInside(syrupSnapArea, eventData))
        {
            SnapToSyrupPos();
        }

        if (HasCookableContent() && IsPointerInside(cookingStartArea, eventData))
        {
            TryStartCooking();
        }
    }

    public bool IsPointerInsideCup(PointerEventData eventData)
    {
        return IsCupVisible()
            && cupRect != null
            && RectTransformUtility.RectangleContainsScreenPoint(cupRect, eventData.position, eventCamera);
    }

    public static CupDragController FindSpawnedCupAtPointer(PointerEventData eventData)
    {
        for (int i = spawnedCups.Count - 1; i >= 0; i--)
        {
            CupDragController cup = spawnedCups[i];
            if (cup == null)
            {
                spawnedCups.RemoveAt(i);
                continue;
            }

            if (cup.IsPointerInsideCup(eventData))
            {
                return cup;
            }
        }

        return null;
    }

    public void ApplyIngredient(CupContentState newState)
    {
        if (!IsCupVisible() || cookingResultState != CupCookingResultState.None)
        {
            return;
        }

        ApplyContentState(newState);
    }

    public bool IsInsideArea(RectTransform area)
    {
        if (!IsCupVisible() || cupRect == null || area == null)
        {
            return false;
        }

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(eventCamera, cupRect.position);
        return RectTransformUtility.RectangleContainsScreenPoint(area, screenPoint, eventCamera);
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
        return IsPointerInside(trashCanArea, eventData);
    }

    private bool IsPointerInside(RectTransform area, PointerEventData eventData)
    {
        return area != null && RectTransformUtility.RectangleContainsScreenPoint(area, eventData.position, eventCamera);
    }

    private Receipt FindReceiptAtPointer(PointerEventData eventData)
    {
        if (EventSystem.current != null)
        {
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            for (int i = 0; i < results.Count; i++)
            {
                Receipt receipt = results[i].gameObject.GetComponentInParent<Receipt>();
                if (receipt != null && receipt.gameObject.activeInHierarchy)
                {
                    return receipt;
                }
            }
        }

        Receipt[] receipts = Resources.FindObjectsOfTypeAll<Receipt>();
        for (int i = 0; i < receipts.Length; i++)
        {
            Receipt receipt = receipts[i];
            if (receipt == null || !receipt.gameObject.scene.IsValid() || !receipt.gameObject.activeInHierarchy)
            {
                continue;
            }

            RectTransform receiptRect = receipt.transform as RectTransform;
            if (receiptRect != null && RectTransformUtility.RectangleContainsScreenPoint(receiptRect, eventData.position, eventCamera))
            {
                return receipt;
            }
        }

        return null;
    }

    private void SnapToSyrupPos()
    {
        SnapToArea(syrupPosArea);
    }

    private void SnapToCoffeeMachinePos()
    {
        SnapToArea(coffeeMachinePosArea);
    }

    private void SnapToArea(RectTransform targetArea)
    {
        if (canvasRect == null || cupRect == null || targetArea == null)
        {
            return;
        }

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(eventCamera, targetArea.position);
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, eventCamera, out Vector2 localPoint))
        {
            cupRect.anchoredPosition = localPoint;
        }
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
        UpdateCupSprite();
    }

    public void HideForCooking()
    {
        isDragging = false;

        if (!isTemplate)
        {
            spawnedCups.Remove(this);
            NotifyCupContentAvailabilityChanged();
        }

        gameObject.SetActive(false);
    }

    public void ShowCookingResult(bool succeeded)
    {
        ShowCookingResult(succeeded, string.Empty);
    }

    public void ShowCookingResult(bool succeeded, string menuName)
    {
        isDragging = false;
        cookingResultState = succeeded ? CupCookingResultState.Succeeded : CupCookingResultState.Failed;
        completedMenuName = succeeded ? menuName : string.Empty;
        SetSuccessVisible(succeeded);

        if (!succeeded)
        {
            SetFailedState();
        }
        else
        {
            UpdateCupSprite();
        }

        gameObject.SetActive(true);
        MoveToCupSpriteSetOrigin();

        if (cupImage != null)
        {
            cupImage.enabled = true;
            cupImage.raycastTarget = true;
        }

        if (!isTemplate && !spawnedCups.Contains(this))
        {
            spawnedCups.Add(this);
        }

        NotifyCupContentAvailabilityChanged();
    }

    public void HideCup()
    {
        isDragging = false;
        ResetContentStates();

        if (cupImage == null)
        {
            return;
        }

        cupImage.enabled = false;
        cupImage.raycastTarget = false;

        if (!isTemplate)
        {
            spawnedCups.Remove(this);
            NotifyCupContentAvailabilityChanged();
            Destroy(gameObject);
        }
    }

    private void ApplyContentState(CupContentState newState)
    {
        if (newState == CupContentState.Normal)
        {
            ResetContentStates();
            return;
        }

        bool addedNewState = !contentStates.Contains(newState);
        if (addedNewState)
        {
            contentStates.Add(newState);
            PlayIngredientSfx(newState);
        }

        UpdateCupSprite();
        LogContentStates();
        NotifyCupContentAvailabilityChanged();
    }

    private void PlayIngredientSfx(CupContentState newState)
    {
        if (SoundManager.Instance == null || SoundManager.Instance.SFX == null)
        {
            return;
        }

        switch (newState)
        {
            case CupContentState.IceMachineEd:
                SoundManager.Instance.SFX.PlayIceCube();
                break;
            case CupContentState.IceTeaEd:
            case CupContentState.WaterPotEd:
                SoundManager.Instance.SFX.PlayPouringWater();
                break;
            case CupContentState.CoffeeMachineEd:
                SoundManager.Instance.SFX.PlayMachine();
                break;
        }
    }

    private void ResetContentStates()
    {
        contentStates.Clear();
        cookingResultState = CupCookingResultState.None;
        completedMenuName = string.Empty;
        SetSuccessVisible(false);
        UpdateCupSprite();
        LogContentStates();
        NotifyCupContentAvailabilityChanged();
    }

    private void SetFailedState()
    {
        contentStates.Clear();
        contentStates.Add(CupContentState.Failed);
        UpdateCupSprite();
        LogContentStates();
        NotifyCupContentAvailabilityChanged();
    }

    private void CaptureDefaultCupSprite()
    {
        if (emptyCupSprite == null && cupImage != null)
        {
            emptyCupSprite = cupImage.sprite;
        }
    }

    private void UpdateCupSprite()
    {
        if (cupImage == null)
        {
            return;
        }

        Sprite selectedSprite = GetSpriteForCurrentContents();
        if (selectedSprite != null)
        {
            cupImage.sprite = selectedSprite;
        }
    }

    private Sprite GetSpriteForCurrentContents()
    {
        if (cookingResultState == CupCookingResultState.Failed)
        {
            return failedCupSprite != null ? failedCupSprite : emptyCupSprite;
        }

        if (BuffDebuffManager.LittleGhostDebuffActive && lockedCupSprite != null)
        {
            return lockedCupSprite;
        }

        bool hasTea = contentStates.Contains(CupContentState.IceTeaEd);
        bool hasWater = contentStates.Contains(CupContentState.WaterPotEd);
        bool hasIce = contentStates.Contains(CupContentState.IceMachineEd);
        bool hasShot = contentStates.Contains(CupContentState.CoffeeMachineEd);

        if (hasShot)
        {
            if (hasWater && hasIce)
            {
                return shotWaterIceCupSprite != null ? shotWaterIceCupSprite : shotWaterCupSprite;
            }

            if (hasWater)
            {
                return shotWaterCupSprite != null ? shotWaterCupSprite : shotCupSprite;
            }

            if (hasIce)
            {
                return shotIceCupSprite != null ? shotIceCupSprite : shotCupSprite;
            }

            return shotCupSprite;
        }

        if (hasTea)
        {
            return hasIce && teaIceCupSprite != null ? teaIceCupSprite : teaCupSprite;
        }

        if (hasWater)
        {
            return hasIce && waterIceCupSprite != null ? waterIceCupSprite : waterCupSprite;
        }

        if (hasIce)
        {
            return iceCupSprite;
        }

        return emptyCupSprite;
    }

    private void AutoBindSuccessObject()
    {
        if (successObject != null)
        {
            return;
        }

        Transform successTransform = transform.Find("Success");
        if (successTransform != null)
        {
            successObject = successTransform.gameObject;
        }
    }

    private void SetSuccessVisible(bool visible)
    {
        AutoBindSuccessObject();
        if (successObject != null)
        {
            successObject.SetActive(visible);
        }
    }

    private void MoveToCupSpriteSetOrigin()
    {
        if (cupRect == null)
        {
            return;
        }

        cupRect.localPosition = Vector3.zero;
    }

    private void LogContentStates()
    {
        string stateLog = contentStates.Count == 0
            ? CupContentState.Normal.ToString()
            : string.Join(", ", contentStates);

        Debug.Log($"{name} states: [{stateLog}]");
    }

    private void TryStartCooking()
    {
        if (cookingMiniGameController == null)
        {
            Debug.LogWarning($"{nameof(CupDragController)}: CookingMiniGameController is not assigned.");
            return;
        }

        if (!cookingMiniGameController.TryStartCooking(this))
        {
            return;
        }

        if (!isTemplate)
        {
            HideForCooking();
        }
    }

    public static bool HasAnyCupWithContent()
    {
        for (int i = spawnedCups.Count - 1; i >= 0; i--)
        {
            CupDragController cup = spawnedCups[i];
            if (cup == null)
            {
                spawnedCups.RemoveAt(i);
                continue;
            }

            if (cup.HasCookableContent())
            {
                return true;
            }
        }

        return false;
    }

    private bool HasCookableContent()
    {
        return cookingResultState == CupCookingResultState.None
            && contentStates.Count > 0
            && !contentStates.Contains(CupContentState.Failed);
    }

    private static void NotifyCupContentAvailabilityChanged()
    {
        CupContentAvailabilityChanged?.Invoke(HasAnyCupWithContent());
    }
}
