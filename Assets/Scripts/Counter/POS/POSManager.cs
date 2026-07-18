using UnityEngine;
using TMPro;

public class POSManager : MonoBehaviour
{
    [SerializeField] private CustomerSpawner spawner;
    [SerializeField] private PrinterController printer;
    [SerializeField] private Transform receiptArea;
    [SerializeField] private TMP_Text currentSelectionText;
    [SerializeField] private int maxReceipts = 4;

    private string currentSelectedDrink = "";

    private void Start() => ClearInput();

    public void SelectDrink(string drinkName)
    {
        currentSelectedDrink = drinkName;
        if (currentSelectionText != null) currentSelectionText.text = $"선택된 음료: {drinkName}";
    }

    public void ClearInput()
    {
        currentSelectedDrink = "";
        if (currentSelectionText != null) currentSelectionText.text = "선택된 음료: 없음";
    }

    public void PrintReceipt()
    {
        if (string.IsNullOrEmpty(currentSelectedDrink)) return;
        if (receiptArea.childCount >= maxReceipts) return;

        CustomerController customer = spawner.GetCurrentCustomer();
        if (customer == null) return;

        // Compare strings!
        if (customer.ActiveOrder.Recipe.MenuName == currentSelectedDrink)
        {
            printer.QueueReceipt(customer.ActiveOrder.Recipe);
            spawner.ClearCurrentCustomer();
            ClearInput();
        }
        else
        {
            customer.ActiveOrder.CurrentPatience -= 5f;
            ClearInput();
        }
    }
}