using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class CounterOrderController : MonoBehaviour
{
    [SerializeField] private Button orderButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private Button counterBackButton;
    [SerializeField] private GameObject receiptMachine0;
    [SerializeField] private GameObject receiptMachine1;
    [SerializeField] private RectTransform receiptMachineHoverArea;
    [SerializeField] private GameObject[] receiptSlots = new GameObject[7];
    [SerializeField] private bool hideReceiptSlotsOnAwake = true;
    [SerializeField] private RectTransform orderTextBox;
    [SerializeField] private TMP_Text orderText;
    [SerializeField] private TMP_Text printZoneText;
    [SerializeField] private RectTransform receiptTextBox;
    [SerializeField] private TMP_Text receiptText;
    [SerializeField] private Color normalOrderTextColor = Color.black;
    [SerializeField] private Color emptyOrderTextColor = Color.red;
    [SerializeField] private Canvas canvas;
    [SerializeField] private UIStationScoller cameraTransition;
    [SerializeField] private Vector2 orderTextBoxOffset = Vector2.zero;
    [SerializeField] private Vector2 receiptTextBoxOffset = Vector2.zero;
    [SerializeField] private bool forceOrderTextBoxBottomRightPivot = true;
    [SerializeField] private bool forceReceiptTextBoxTopLeftPivot = true;
    [SerializeField] private float orderTextBoxMinHeight = 80f;
    [SerializeField] private float orderTextBoxExtraHeight = 20f;
    [SerializeField] private float receiptTextBoxMinHeight = 80f;
    [SerializeField] private float receiptTextBoxExtraHeight = 20f;

    private readonly List<string> pendingClicks = new List<string>();
    private readonly List<OrderLine> submittedOrderLines = new List<OrderLine>();
    private bool orderSubmitted;

    public static event System.Action<int> OnReceiptPrinted;

    private void Awake()
    {
        AutoBindMissingReferences();
        ApplyOrderTextBoxPivot();
        ApplyReceiptTextBoxPivot();

        if (orderButton != null)
        {
            orderButton.onClick.AddListener(SubmitOrder);
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.AddListener(CancelOrder);
        }

        if (counterBackButton != null)
        {
            counterBackButton.onClick.AddListener(MoveBackToCounter);
        }

        ResetReceiptState();
        UpdatePrintZoneText();
        if (hideReceiptSlotsOnAwake)
        {
            HideAllReceiptSlots();
        }
    }

    private void Update()
    {
        UpdateReceiptMachineClick();
        UpdateFloatingOrderTextBox();
        UpdateFloatingReceiptTextBox();
    }

    public void RegisterMenuClick(string menuName)
    {
        if (string.IsNullOrWhiteSpace(menuName))
        {
            return;
        }

        pendingClicks.Add(menuName);
        Debug.Log($"{nameof(CounterOrderController)} pending clicks: [{string.Join(", ", pendingClicks)}]");
        UpdatePrintZoneText();

        if (orderSubmitted)
        {
            BuildSubmittedOrder();
            UpdateOrderText();
        }
    }

    private void SubmitOrder()
    {
        orderSubmitted = true;
        BuildSubmittedOrder();
        UpdateOrderText();

        if (receiptMachine0 != null)
        {
            receiptMachine0.SetActive(false);
        }

        if (receiptMachine1 != null)
        {
            receiptMachine1.SetActive(true);
        }

        Debug.Log($"{nameof(CounterOrderController)} submitted order:\n{CreateOrderText()}");
    }

    private void CancelOrder()
    {
        pendingClicks.Clear();
        submittedOrderLines.Clear();
        orderSubmitted = false;

        ResetReceiptState();
        UpdateOrderText();
        UpdatePrintZoneText();
        Debug.Log($"{nameof(CounterOrderController)} canceled order.");
    }

    private void MoveBackToCounter()
    {
        if (cameraTransition != null)
        {
            cameraTransition.MoveToCounter();
        }
    }

    private void BuildSubmittedOrder()
    {
        submittedOrderLines.Clear();
        submittedOrderLines.AddRange(BuildOrderLines(pendingClicks));
    }

    private List<OrderLine> BuildOrderLines(IReadOnlyList<string> sourceMenuNames)
    {
        List<OrderLine> orderLines = new List<OrderLine>();
        if (sourceMenuNames == null)
        {
            return orderLines;
        }

        Dictionary<string, OrderLine> lineByName = new Dictionary<string, OrderLine>();

        for (int i = 0; i < sourceMenuNames.Count; i++)
        {
            string menuName = sourceMenuNames[i];
            if (lineByName.TryGetValue(menuName, out OrderLine line))
            {
                line.Count++;
                continue;
            }

            line = new OrderLine(menuName, 1);
            lineByName.Add(menuName, line);
            orderLines.Add(line);
        }

        return orderLines;
    }

    private void UpdateOrderText()
    {
        if (orderText != null)
        {
            orderText.text = orderSubmitted ? CreateOrderText() : string.Empty;
            orderText.color = HasSubmittedOrderLines() ? normalOrderTextColor : emptyOrderTextColor;
            ResizeOrderTextBoxToText();
        }
    }

    private void UpdatePrintZoneText()
    {
        if (printZoneText == null)
        {
            return;
        }

        IReadOnlyList<OrderLine> pendingOrderLines = BuildOrderLines(pendingClicks);
        printZoneText.text = pendingOrderLines.Count > 0 ? CreateOrderText(pendingOrderLines) : string.Empty;
    }

    private string CreateOrderText()
    {
        return CreateOrderText(submittedOrderLines);
    }

    private string CreateOrderText(IReadOnlyList<OrderLine> orderLines)
    {
        if (orderLines == null || orderLines.Count == 0)
        {
            return "\uC785\uB825\uB41C \uBA54\uB274\uAC00 \uC5C6\uC2B5\uB2C8\uB2E4!";
        }

        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < orderLines.Count; i++)
        {
            OrderLine line = orderLines[i];
            builder.Append(line.MenuName);
            builder.Append(' ');
            builder.Append(line.Count);
            builder.Append("\uC794");

            if (i + 1 < orderLines.Count)
            {
                builder.AppendLine();
            }
        }

        return builder.ToString();
    }

    private bool HasSubmittedOrderLines()
    {
        return submittedOrderLines.Count > 0;
    }

    private void ResizeOrderTextBoxToText()
    {
        if (orderTextBox == null || orderText == null)
        {
            return;
        }

        RectTransform textRect = orderText.rectTransform;
        float horizontalPadding = textRect != null ? Mathf.Abs(textRect.sizeDelta.x) : 0f;
        float verticalPadding = textRect != null ? Mathf.Abs(textRect.sizeDelta.y) : 0f;
        float preferredWidth = Mathf.Max(1f, orderTextBox.rect.width - horizontalPadding);
        float preferredHeight = orderText.GetPreferredValues(orderText.text, preferredWidth, 0f).y;
        float targetHeight = Mathf.Max(orderTextBoxMinHeight, preferredHeight + verticalPadding + orderTextBoxExtraHeight);

        orderTextBox.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetHeight);
    }

    private void ResizeReceiptTextBoxToText()
    {
        if (receiptTextBox == null || receiptText == null)
        {
            return;
        }

        RectTransform textRect = receiptText.rectTransform;
        float horizontalPadding = textRect != null ? Mathf.Abs(textRect.sizeDelta.x) : 0f;
        float verticalPadding = textRect != null ? Mathf.Abs(textRect.sizeDelta.y) : 0f;
        float preferredWidth = Mathf.Max(1f, receiptTextBox.rect.width - horizontalPadding);
        float preferredHeight = receiptText.GetPreferredValues(receiptText.text, preferredWidth, 0f).y;
        float targetHeight = Mathf.Max(receiptTextBoxMinHeight, preferredHeight + verticalPadding + receiptTextBoxExtraHeight);

        receiptTextBox.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetHeight);
    }

    private void UpdateFloatingOrderTextBox()
    {
        if (orderTextBox == null)
        {
            return;
        }

        bool shouldShow = orderSubmitted && IsPointerOverReceiptMachine();
        if (orderTextBox.gameObject.activeSelf != shouldShow)
        {
            orderTextBox.gameObject.SetActive(shouldShow);
            if (shouldShow)
            {
                orderTextBox.SetAsLastSibling();
            }
        }

        if (!shouldShow)
        {
            return;
        }

        RectTransform canvasRect = GetCanvasRect();
        if (canvasRect == null)
        {
            orderTextBox.position = GetPointerScreenPosition() + orderTextBoxOffset;
            return;
        }

        Camera eventCamera = GetEventCamera();
        Vector2 pointerPosition = GetPointerScreenPosition();
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, pointerPosition, eventCamera, out Vector2 localPoint))
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, pointerPosition, null, out localPoint);
        }

        orderTextBox.anchoredPosition = localPoint + orderTextBoxOffset;
    }

    private void UpdateFloatingReceiptTextBox()
    {
        if (receiptTextBox == null || receiptText == null)
        {
            return;
        }

        Receipt hoveredReceipt = GetHoveredReceipt();
        bool shouldShow = hoveredReceipt != null;
        if (receiptTextBox.gameObject.activeSelf != shouldShow)
        {
            receiptTextBox.gameObject.SetActive(shouldShow);
            if (shouldShow)
            {
                receiptTextBox.SetAsLastSibling();
            }
        }

        if (!shouldShow)
        {
            return;
        }

        receiptText.text = hoveredReceipt.ReceiptText;
        receiptText.color = normalOrderTextColor;
        ResizeReceiptTextBoxToText();

        RectTransform canvasRect = GetReceiptTextBoxCanvasRect();
        if (canvasRect == null)
        {
            receiptTextBox.position = GetPointerScreenPosition() + receiptTextBoxOffset;
            return;
        }

        Camera eventCamera = GetReceiptTextBoxEventCamera();
        Vector2 pointerPosition = GetPointerScreenPosition();
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, pointerPosition, eventCamera, out Vector2 localPoint))
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, pointerPosition, null, out localPoint);
        }

        receiptTextBox.anchoredPosition = localPoint + receiptTextBoxOffset;
    }

    private void UpdateReceiptMachineClick()
    {
        if (!WasPrimaryPointerPressedThisFrame())
        {
            return;
        }

        if (!orderSubmitted && IsPointerOverReceiptMachine0())
        {
            MoveToKitchen();
            return;
        }

        if (!orderSubmitted)
        {
            return;
        }

        if (IsPointerOverReceiptMachine())
        {
            if (!TryRegisterReceiptToSlot())
            {
                return;
            }

            MoveToKitchen();
            ClearSubmittedCounterOrder();
        }
    }

    private void MoveToKitchen()
    {
        if (cameraTransition != null)
        {
            cameraTransition.MoveToKitchen();
        }
    }

    private void ApplyOrderTextBoxPivot()
    {
        if (!forceOrderTextBoxBottomRightPivot || orderTextBox == null)
        {
            return;
        }

        Vector2 oldPivot = orderTextBox.pivot;
        Vector2 newPivot = new Vector2(1f, 0f);
        if (oldPivot == newPivot)
        {
            return;
        }

        Vector2 size = orderTextBox.rect.size;
        Vector2 pivotDelta = newPivot - oldPivot;
        orderTextBox.pivot = newPivot;
        orderTextBox.anchoredPosition += new Vector2(pivotDelta.x * size.x, pivotDelta.y * size.y);
    }

    private void ApplyReceiptTextBoxPivot()
    {
        if (!forceReceiptTextBoxTopLeftPivot || receiptTextBox == null)
        {
            return;
        }

        Vector2 oldPivot = receiptTextBox.pivot;
        Vector2 newPivot = new Vector2(0f, 1f);
        if (oldPivot == newPivot)
        {
            return;
        }

        Vector2 size = receiptTextBox.rect.size;
        Vector2 pivotDelta = newPivot - oldPivot;
        receiptTextBox.pivot = newPivot;
        receiptTextBox.anchoredPosition += new Vector2(pivotDelta.x * size.x, pivotDelta.y * size.y);
    }

    private bool IsPointerOverReceiptMachine()
    {
        if (receiptMachine1 == null || !receiptMachine1.activeInHierarchy)
        {
            return false;
        }

        return IsPointerOverReceiptMachineByRaycast()
            || IsPointerOverReceiptMachineByRect(null)
            || IsPointerOverReceiptMachineByRect(GetEventCamera())
            || IsPointerOverReceiptMachineByRect(Camera.main);
    }

    private bool IsPointerOverReceiptMachine0()
    {
        return IsPointerOverGameObject(receiptMachine0);
    }

    private bool IsPointerOverGameObject(GameObject targetObject)
    {
        if (targetObject == null || !targetObject.activeInHierarchy)
        {
            return false;
        }

        Transform targetTransform = targetObject.transform;
        if (IsPointerOverTransformByRaycast(targetTransform))
        {
            return true;
        }

        RectTransform targetRect = targetTransform as RectTransform;
        if (targetRect == null)
        {
            return false;
        }

        return IsPointerOverRect(targetRect, null)
            || IsPointerOverRect(targetRect, GetEventCamera())
            || IsPointerOverRect(targetRect, Camera.main);
    }

    private bool IsPointerOverTransformByRaycast(Transform targetTransform)
    {
        if (EventSystem.current == null || targetTransform == null)
        {
            return false;
        }

        PointerEventData pointerEventData = new PointerEventData(EventSystem.current)
        {
            position = GetPointerScreenPosition()
        };

        GraphicRaycaster[] raycasters = FindObjectsByType<GraphicRaycaster>(FindObjectsSortMode.None);
        for (int i = 0; i < raycasters.Length; i++)
        {
            List<RaycastResult> results = new List<RaycastResult>();
            raycasters[i].Raycast(pointerEventData, results);

            for (int j = 0; j < results.Count; j++)
            {
                Transform resultTransform = results[j].gameObject.transform;
                if (resultTransform == targetTransform || resultTransform.IsChildOf(targetTransform))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool IsPointerOverReceiptMachineByRaycast()
    {
        if (EventSystem.current == null)
        {
            return false;
        }

        PointerEventData pointerEventData = new PointerEventData(EventSystem.current)
        {
            position = GetPointerScreenPosition()
        };

        GraphicRaycaster[] raycasters = FindObjectsByType<GraphicRaycaster>(FindObjectsSortMode.None);
        for (int i = 0; i < raycasters.Length; i++)
        {
            List<RaycastResult> results = new List<RaycastResult>();
            raycasters[i].Raycast(pointerEventData, results);

            for (int j = 0; j < results.Count; j++)
            {
                if (IsReceiptMachineTransform(results[j].gameObject.transform))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool IsPointerOverReceiptMachineByRect(Camera eventCamera)
    {
        RectTransform hoverArea = GetReceiptMachineHoverArea();

        if (hoverArea == null)
        {
            return false;
        }

        return RectTransformUtility.RectangleContainsScreenPoint(
            hoverArea,
            GetPointerScreenPosition(),
            eventCamera);
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

    private bool WasPrimaryPointerPressedThisFrame()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            return true;
        }

        return Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame;
    }

    private bool IsReceiptMachineTransform(Transform target)
    {
        if (target == null || receiptMachine1 == null)
        {
            return false;
        }

        RectTransform hoverArea = GetReceiptMachineHoverArea();
        if (hoverArea != null)
        {
            Transform hoverTransform = hoverArea.transform;
            return target == hoverTransform || target.IsChildOf(hoverTransform);
        }

        Transform receiptTransform = receiptMachine1.transform;
        return target == receiptTransform || target.IsChildOf(receiptTransform);
    }

    private RectTransform GetReceiptMachineHoverArea()
    {
        if (receiptMachineHoverArea != null)
        {
            return receiptMachineHoverArea;
        }

        GameObject areaObject = FindGameObjectByName("ReceiptMachine1Area");
        if (areaObject != null)
        {
            receiptMachineHoverArea = areaObject.transform as RectTransform;
            if (receiptMachineHoverArea != null)
            {
                return receiptMachineHoverArea;
            }
        }

        return receiptMachine1 != null ? receiptMachine1.transform as RectTransform : null;
    }

    private Receipt GetHoveredReceipt()
    {
        AutoBindReceiptSlots();

        for (int i = 0; i < receiptSlots.Length; i++)
        {
            GameObject slot = receiptSlots[i];
            if (slot == null || !slot.activeInHierarchy)
            {
                continue;
            }

            RectTransform slotRect = slot.transform as RectTransform;
            if (slotRect == null)
            {
                continue;
            }

            if (IsPointerOverRect(slotRect, null)
                || IsPointerOverRect(slotRect, GetReceiptSlotEventCamera(slotRect))
                || IsPointerOverRect(slotRect, Camera.main))
            {
                Receipt receipt = slot.GetComponent<Receipt>();
                if (receipt != null)
                {
                    return receipt;
                }
            }
        }

        return null;
    }

    private bool IsPointerOverRect(RectTransform rectTransform, Camera eventCamera)
    {
        return RectTransformUtility.RectangleContainsScreenPoint(
            rectTransform,
            GetPointerScreenPosition(),
            eventCamera);
    }

    private void ResetReceiptState()
    {
        if (receiptMachine0 != null)
        {
            receiptMachine0.SetActive(true);
        }

        if (receiptMachine1 != null)
        {
            receiptMachine1.SetActive(false);
        }

        if (orderTextBox != null)
        {
            orderTextBox.gameObject.SetActive(false);
        }

        if (receiptTextBox != null)
        {
            receiptTextBox.gameObject.SetActive(false);
        }
    }

    private bool TryRegisterReceiptToSlot()
    {
        if (!orderSubmitted)
        {
            return false;
        }

        AutoBindReceiptSlots();

        string receiptText = CreateOrderText();
        if (!HasSubmittedOrderLines())
        {
            Debug.LogWarning($"{nameof(CounterOrderController)}: no menu was selected.");
            return false;
        }

        for (int i = 0; i < receiptSlots.Length; i++)
        {
            GameObject slot = receiptSlots[i];
            if (slot == null || slot.activeSelf)
            {
                continue;
            }

            slot.SetActive(true);
            Receipt receipt = slot.GetComponent<Receipt>();
            if (receipt == null)
            {
                receipt = slot.AddComponent<Receipt>();
            }

            receipt.Initialize(receiptText, i);
            Debug.Log($"{nameof(CounterOrderController)} registered receipt slot {i}: {receiptText}");
           
            OnReceiptPrinted?.Invoke(i);

            return true;
        }

        Debug.LogWarning($"{nameof(CounterOrderController)}: no empty receipt slot is available.");
        return false;
    }

    private void ClearSubmittedCounterOrder()
    {
        pendingClicks.Clear();
        submittedOrderLines.Clear();
        orderSubmitted = false;

        ResetReceiptState();
        UpdateOrderText();
        UpdatePrintZoneText();
    }

    private void HideAllReceiptSlots()
    {
        AutoBindReceiptSlots();

        for (int i = 0; i < receiptSlots.Length; i++)
        {
            if (receiptSlots[i] != null)
            {
                receiptSlots[i].SetActive(false);
            }
        }
    }

    private RectTransform GetCanvasRect()
    {
        if (canvas == null)
        {
            return null;
        }

        return canvas.transform as RectTransform;
    }

    private Camera GetEventCamera()
    {
        if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            return null;
        }

        return canvas.worldCamera;
    }

    private void AutoBindMissingReferences()
    {
        if (canvas == null)
        {
            canvas = GetComponentInChildren<Canvas>(true);
        }

        if (canvas == null)
        {
            canvas = FindFirstObjectByType<Canvas>();
        }

        if (cameraTransition == null)
        {
            cameraTransition = FindFirstObjectByType<UIStationScoller>();
        }

        if (orderButton == null)
        {
            orderButton = FindButtonByName("Order");
        }

        if (cancelButton == null)
        {
            cancelButton = FindButtonByName("Cancel");
        }

        if (counterBackButton == null)
        {
            counterBackButton = FindButtonByName("CounterBackBtn");
        }

        if (receiptMachine0 == null)
        {
            receiptMachine0 = FindGameObjectByName("ReceiptMachine0");
        }

        if (receiptMachine1 == null)
        {
            receiptMachine1 = FindGameObjectByName("ReceiptMachine1");
        }

        if (receiptMachineHoverArea == null)
        {
            GetReceiptMachineHoverArea();
        }

        GameObject textBoxObject = FindGameObjectByName("OrderTextBox");
        if (textBoxObject != null)
        {
            orderTextBox = textBoxObject.transform as RectTransform;
        }

        if (orderTextBox != null)
        {
            Canvas textBoxCanvas = orderTextBox.GetComponentInParent<Canvas>(true);
            if (textBoxCanvas != null)
            {
                canvas = textBoxCanvas;
            }
        }

        if (orderText == null && orderTextBox != null)
        {
            orderText = orderTextBox.GetComponentInChildren<TMP_Text>(true);
        }

        if (printZoneText == null)
        {
            GameObject printZoneObject = FindGameObjectByName("PrintZone");
            if (printZoneObject != null)
            {
                printZoneText = printZoneObject.GetComponentInChildren<TMP_Text>(true);
            }
        }

        GameObject receiptTextBoxObject = FindGameObjectByName("ReceiptTextBox");
        if (receiptTextBoxObject != null)
        {
            receiptTextBox = receiptTextBoxObject.transform as RectTransform;
        }

        if (receiptText == null && receiptTextBox != null)
        {
            receiptText = receiptTextBox.GetComponentInChildren<TMP_Text>(true);
        }

        AutoBindReceiptSlots();
    }

    private void AutoBindReceiptSlots()
    {
        if (receiptSlots == null || receiptSlots.Length != 7)
        {
            receiptSlots = new GameObject[7];
        }

        for (int i = 0; i < receiptSlots.Length; i++)
        {
            if (receiptSlots[i] != null)
            {
                continue;
            }

            string slotName = i == 0 ? "Recepit" : $"Recepit ({i})";
            receiptSlots[i] = FindGameObjectByName(slotName);
        }
    }

    private Button FindButtonByName(string targetName)
    {
        GameObject target = FindGameObjectByName(targetName);
        return target != null ? target.GetComponent<Button>() : null;
    }

    private RectTransform GetReceiptTextBoxCanvasRect()
    {
        if (receiptTextBox == null)
        {
            return null;
        }

        Canvas textBoxCanvas = receiptTextBox.GetComponentInParent<Canvas>(true);
        return textBoxCanvas != null ? textBoxCanvas.transform as RectTransform : GetCanvasRect();
    }

    private Camera GetReceiptTextBoxEventCamera()
    {
        if (receiptTextBox == null)
        {
            return GetEventCamera();
        }

        Canvas textBoxCanvas = receiptTextBox.GetComponentInParent<Canvas>(true);
        if (textBoxCanvas == null || textBoxCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            return null;
        }

        return textBoxCanvas.worldCamera;
    }

    private Camera GetReceiptSlotEventCamera(RectTransform slotRect)
    {
        Canvas slotCanvas = slotRect.GetComponentInParent<Canvas>(true);
        if (slotCanvas == null || slotCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            return null;
        }

        return slotCanvas.worldCamera;
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

    private class OrderLine
    {
        public readonly string MenuName;
        public int Count;

        public OrderLine(string menuName, int count)
        {
            MenuName = menuName;
            Count = count;
        }
    }
}
