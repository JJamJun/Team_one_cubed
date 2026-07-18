using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class CustomerController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private Slider patienceSlider;

    public OrderData ActiveOrder { get; private set; }
    private bool isInitialized = false;

    //event to notify the manager if this customer ran out of patience
    public static event Action<CustomerController> OnCustomerLeftAngry;

    public void Initialize(OrderData order)
    {
        ActiveOrder = order;

        //update the dialogue bubble text
        if (dialogueText != null)
        {
            dialogueText.text = order.GetOrderDescription();
        }

        //set up the patience bar slider
        if (patienceSlider != null)
        {
            patienceSlider.maxValue = order.MaxPatience;
            patienceSlider.value = order.MaxPatience;
        }

        isInitialized = true;
    }

    private void Update()
    {
        if (!isInitialized || ActiveOrder == null) return;

        //tick down patience over time
        ActiveOrder.CurrentPatience -= Time.deltaTime;

        if (patienceSlider != null)
        {
            patienceSlider.value = ActiveOrder.CurrentPatience;
        }

        //if time runs out, the customer leaves
        if (ActiveOrder.CurrentPatience <= 0f)
        {
            OnCustomerLeftAngry?.Invoke(this);
            Destroy(gameObject);
        }
    }
}