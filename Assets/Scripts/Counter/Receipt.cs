using TMPro;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class Receipt : MonoBehaviour
{
    public static event Action<int> ReceiptSlotEmptied;

    [SerializeField] private TMP_Text orderNameText;
    [SerializeField] private string receiptText;
    [SerializeField] private int slotIndex = -1;
    [SerializeField] private Image timerImage;
    [SerializeField] private RecepitTimerManager timerManager;

    private readonly List<ReceiptLine> orderLines = new List<ReceiptLine>();
    private float timerRemaining;
    private float timerMaxDuration;
    private bool timerRunning;
    private RectTransform receiptRect;
    private Vector2 originalAnchoredPosition;
    private bool hasCapturedAnchoredPosition;
    private Image receiptImage;
    private Color originalReceiptColor = Color.white;
    private bool hasCapturedReceiptColor;
    private Color originalTimerColor = Color.white;
    private bool hasCapturedTimerColor;

    public string ReceiptText => string.IsNullOrWhiteSpace(receiptText) ? GetDisplayText() : receiptText;
    public int SlotIndex => slotIndex;

    private void Awake()
    {
        AutoBindText();
        AutoBindTimerImage();
        AutoBindTimerManager();
        AutoBindReceiptVisuals();
        UpdateTimerVisual(0f);
    }

    private void Update()
    {
        if (!timerRunning)
        {
            ResetShake();
            return;
        }

        timerRemaining -= Time.deltaTime;
        float fillAmount = Mathf.Clamp01(timerRemaining / Mathf.Max(0.01f, timerMaxDuration));
        UpdateTimerVisual(fillAmount);

        if (timerRemaining > 0f)
        {
            return;
        }

        Debug.Log($"\uC601\uC218\uC99D {GetDisplaySlotIndex()}\uBC88\uC9F8: \uC2DC\uAC04 \uCD08\uACFC!");
        ClearReceiptSlot();
    }

    private void LateUpdate()
    {
        UpdateShake();
    }

    public void Initialize(string receiptText)
    {
        Initialize(receiptText, slotIndex);
    }

    public void Initialize(string receiptText, int slotIndex)
    {
        AutoBindText();
        this.slotIndex = slotIndex;
        this.receiptText = receiptText;
        ParseReceiptText(receiptText);
        RefreshText();
        StartTimer();
        ApplyReceiptBuffVisual();
    }

    public bool TryFulfillReceipt(CupDragController cup)
    {
        if (cup == null)
        {
            return false;
        }

        IncreaseRemainingTime();

        if (cup.CookingResultState != CupCookingResultState.Succeeded)
        {
            Debug.Log($"{nameof(Receipt)} ignored cup because it is not successful.");
            return false;
        }

        string completedMenuName = cup.CompletedMenuName;
        if (string.IsNullOrWhiteSpace(completedMenuName))
        {
            Debug.LogWarning($"{nameof(Receipt)} received successful cup without a menu name.");
            return false;
        }

        for (int i = 0; i < orderLines.Count; i++)
        {
            ReceiptLine line = orderLines[i];
            if (line.MenuName != completedMenuName)
            {
                continue;
            }

            line.Count--;
            if (line.Count <= 0)
            {
                orderLines.RemoveAt(i);
            }

            RefreshText();
            Debug.Log($"{nameof(Receipt)} fulfilled {completedMenuName} at slot {slotIndex}.");

            if (orderLines.Count == 0)
            {
                Debug.Log($"\uC601\uC218\uC99D {GetDisplaySlotIndex()}\uBC88\uC9F8: \uC8FC\uBB38 \uC644\uC218!");
                ClearReceiptSlot();
            }

            return true;
        }

        Debug.Log($"{nameof(Receipt)} ignored unmatched cup '{completedMenuName}' for slot {slotIndex}: {ReceiptText}");
        return false;
    }

    private void ParseReceiptText(string sourceText)
    {
        orderLines.Clear();

        if (string.IsNullOrWhiteSpace(sourceText))
        {
            return;
        }

        string[] lines = sourceText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line))
            {
                continue;
            }

            int count = 1;
            string menuName = line;
            int lastSpaceIndex = line.LastIndexOf(' ');
            if (lastSpaceIndex >= 0 && lastSpaceIndex + 1 < line.Length)
            {
                menuName = line.Substring(0, lastSpaceIndex).Trim();
                string countText = line.Substring(lastSpaceIndex + 1).Trim().Replace("\uC794", string.Empty);
                if (!int.TryParse(countText, out count))
                {
                    count = 1;
                }
            }

            if (!string.IsNullOrWhiteSpace(menuName))
            {
                orderLines.Add(new ReceiptLine(menuName, Mathf.Max(1, count)));
            }
        }
    }

    private void RefreshText()
    {
        receiptText = BuildReceiptText();

        if (orderNameText != null)
        {
            orderNameText.text = receiptText;
        }
    }

    private string BuildReceiptText()
    {
        if (orderLines.Count == 0)
        {
            return string.Empty;
        }

        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < orderLines.Count; i++)
        {
            ReceiptLine line = orderLines[i];
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

    private void ClearReceiptSlot()
    {
        timerRunning = false;
        UpdateTimerVisual(0f);
        ResetShake();
        if (receiptImage != null && hasCapturedReceiptColor)
        {
            receiptImage.color = originalReceiptColor;
        }

        ReceiptSlotEmptied?.Invoke(slotIndex);
        Debug.Log($"{nameof(Receipt)} slot emptied: {slotIndex}");
        gameObject.SetActive(false);
    }

    private void AutoBindText()
    {
        if (orderNameText == null)
        {
            orderNameText = GetComponentInChildren<TMP_Text>(true);
        }
    }

    private void AutoBindTimerImage()
    {
        if (timerImage != null)
        {
            CaptureTimerColor();
            return;
        }

        Image[] childImages = GetComponentsInChildren<Image>(true);
        for (int i = 0; i < childImages.Length; i++)
        {
            if (childImages[i].name == "CircularProgressBar" || childImages[i].name == "ReceiptTimer")
            {
                timerImage = childImages[i];
                CaptureTimerColor();
                break;
            }
        }
    }

    private void AutoBindTimerManager()
    {
        if (timerManager == null)
        {
            timerManager = RecepitTimerManager.Instance;
        }
    }

    private void StartTimer()
    {
        AutoBindTimerImage();
        AutoBindTimerManager();
        timerMaxDuration = GetTimeLimitSeconds();
        if (BuffDebuffManager.GrimReaperBuffActive)
        {
            timerMaxDuration += BuffDebuffManager.ReceiptPatienceBonusSeconds;
        }

        timerRemaining = timerMaxDuration;
        timerRunning = true;
        UpdateTimerVisual(1f);
    }

    private void IncreaseRemainingTime()
    {
        if (!timerRunning)
        {
            return;
        }

        AutoBindTimerManager();
        timerRemaining += GetTimeIncreaseSeconds();
        UpdateTimerVisual(Mathf.Clamp01(timerRemaining / Mathf.Max(0.01f, timerMaxDuration)));
        Debug.Log($"\uC601\uC218\uC99D {GetDisplaySlotIndex()}\uBC88\uC9F8: \uC2DC\uAC04 {GetTimeIncreaseSeconds():0.##}\uCD08 \uC99D\uAC00!");
    }

    private float GetTimeLimitSeconds()
    {
        AutoBindTimerManager();
        return timerManager != null ? timerManager.TimeLimitSeconds : 30f;
    }

    private float GetTimeIncreaseSeconds()
    {
        AutoBindTimerManager();
        return timerManager != null ? timerManager.TimeIncreaseSeconds : 5f;
    }

    private void AutoBindReceiptVisuals()
    {
        if (receiptRect == null)
        {
            receiptRect = transform as RectTransform;
        }

        if (!hasCapturedAnchoredPosition && receiptRect != null)
        {
            hasCapturedAnchoredPosition = true;
            originalAnchoredPosition = receiptRect.anchoredPosition;
        }

        if (receiptImage == null)
        {
            receiptImage = GetComponent<Image>();
        }

        if (!hasCapturedReceiptColor && receiptImage != null)
        {
            hasCapturedReceiptColor = true;
            originalReceiptColor = receiptImage.color;
        }
    }

    private void ApplyReceiptBuffVisual()
    {
        AutoBindTimerImage();
        UpdateTimerVisual(timerImage != null ? timerImage.fillAmount : 1f);
    }

    private void CaptureTimerColor()
    {
        if (hasCapturedTimerColor || timerImage == null)
        {
            return;
        }

        hasCapturedTimerColor = true;
        originalTimerColor = timerImage.color;
    }

    private void UpdateShake()
    {
        if (!BuffDebuffManager.VirginGhostDebuffActive || receiptRect == null || !timerRunning)
        {
            ResetShake();
            return;
        }

        AutoBindReceiptVisuals();
        float amplitude = BuffDebuffManager.ShakeAmplitude;
        float speed = BuffDebuffManager.ShakeSpeed;
        Vector2 offset = new Vector2(
            Mathf.Sin((Time.time + slotIndex) * speed) * amplitude,
            Mathf.Cos((Time.time + slotIndex) * speed * 1.27f) * amplitude);
        receiptRect.anchoredPosition = originalAnchoredPosition + offset;
    }

    private void ResetShake()
    {
        if (receiptRect != null && hasCapturedAnchoredPosition)
        {
            receiptRect.anchoredPosition = originalAnchoredPosition;
        }
    }

    private void UpdateTimerVisual(float fillAmount)
    {
        if (timerImage == null)
        {
            return;
        }

        timerImage.fillAmount = Mathf.Clamp01(fillAmount);
        CaptureTimerColor();
        Color color = BuffDebuffManager.GrimReaperBuffActive
            ? BuffDebuffManager.UpgradedReceiptColor
            : originalTimerColor;
        color.a = 0.5f;
        timerImage.color = color;
    }

    private int GetDisplaySlotIndex()
    {
        return slotIndex >= 0 ? slotIndex + 1 : 0;
    }

    private string GetDisplayText()
    {
        if (orderNameText == null || string.IsNullOrWhiteSpace(orderNameText.text))
        {
            return gameObject.name;
        }

        return orderNameText.text;
    }

    private class ReceiptLine
    {
        public readonly string MenuName;
        public int Count;

        public ReceiptLine(string menuName, int count)
        {
            MenuName = menuName;
            Count = count;
        }
    }
}
