using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CounterMenuButton : MonoBehaviour
{
    [SerializeField] private CounterOrderController orderController;
    [SerializeField] private string menuName;

    private void Awake()
    {
        Button button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(OnClicked);
        }
    }

    public void Initialize(CounterOrderController targetOrderController, string targetMenuName)
    {
        orderController = targetOrderController;
        menuName = targetMenuName;
        SetLabel(menuName);
    }

    private void OnClicked()
    {
        if (orderController == null)
        {
            orderController = FindFirstObjectByType<CounterOrderController>();
        }

        if (orderController != null)
        {
            orderController.RegisterMenuClick(menuName);
        }
    }

    private void SetLabel(string labelText)
    {
        string displayText = FormatMenuNameForButton(labelText);

        TMP_Text tmpLabel = GetComponentInChildren<TMP_Text>(true);
        if (tmpLabel != null)
        {
            tmpLabel.text = displayText;
            return;
        }

        Text legacyLabel = GetComponentInChildren<Text>(true);
        if (legacyLabel != null)
        {
            legacyLabel.text = displayText;
        }
    }

    private static string FormatMenuNameForButton(string labelText)
    {
        return string.IsNullOrEmpty(labelText) ? string.Empty : labelText.Replace(" ", "\n");
    }
}
