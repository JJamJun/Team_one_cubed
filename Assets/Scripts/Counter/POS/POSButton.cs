using UnityEngine;
using UnityEngine.UI;

public class POSButton : MonoBehaviour
{
    [SerializeField] private POSManager posManager;
    [SerializeField] private string menuName; //type "아이스티" in the inspector due to korean issue

    private void Awake()
    {
        Button btn = GetComponent<Button>();
        if (btn != null) btn.onClick.AddListener(OnButtonClicked);
    }

    private void OnButtonClicked()
    {
        if (posManager != null) posManager.SelectDrink(menuName);
    }
}