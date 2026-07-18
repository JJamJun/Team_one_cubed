using UnityEngine;
using UnityEngine.UI;

public class PrinterController : MonoBehaviour
{
    [SerializeField] private UIStationScoller cameraTransition;
    [SerializeField] private Transform receiptArea;
    [SerializeField] private GameObject receiptPrefab;
    [SerializeField] private GameObject paperSprite;
    [SerializeField] private Button printerButton;

    private CustomerSpawner.ParsedRecipe pendingRecipe;
    private bool hasPendingReceipt = false;

    private void Awake()
    {
        if (printerButton != null) printerButton.onClick.AddListener(OnPrinterClicked);
        if (paperSprite != null) paperSprite.SetActive(false);
    }

    public void QueueReceipt(CustomerSpawner.ParsedRecipe recipe)
    {
        pendingRecipe = recipe;
        hasPendingReceipt = true;
        if (paperSprite != null) paperSprite.SetActive(true);
    }

    private void OnPrinterClicked()
    {
        if (!hasPendingReceipt) return;

        if (cameraTransition != null) cameraTransition.MoveToKitchen();

        GameObject newReceipt = Instantiate(receiptPrefab, receiptArea);
        Receipt receiptScript = newReceipt.GetComponent<Receipt>();
        if (receiptScript != null) receiptScript.Initialize(pendingRecipe);

        hasPendingReceipt = false;
        if (paperSprite != null) paperSprite.SetActive(false);
    }
}