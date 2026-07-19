using TMPro;
using UnityEngine;

public class Receipt : MonoBehaviour
{
    [SerializeField] private TMP_Text orderNameText;
    [SerializeField] private string receiptText;

    public string ReceiptText => string.IsNullOrWhiteSpace(receiptText) ? GetDisplayText() : receiptText;

    public void Initialize(string receiptText)
    {
        this.receiptText = receiptText;

        if (orderNameText != null)
        {
            orderNameText.text = receiptText;
        }
    }

    public void TryFulfillReceipt(CupDragController cup)
    {
        if (cup == null)
        {
            return;
        }

        Debug.Log($"{nameof(Receipt)} received cup: {GetDisplayText()}");
    }

    private string GetDisplayText()
    {
        if (orderNameText == null || string.IsNullOrWhiteSpace(orderNameText.text))
        {
            return gameObject.name;
        }

        return orderNameText.text;
    }
}
