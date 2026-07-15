using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public enum IngredientType
{
    Cup,
    IceTea,
    WaterPot,
    Syrup,
    CoffeeMachine,
    IceMachine
}

public class CookingIngredientManager : MonoBehaviour
{
    [Header("Ingredient Buttons")]
    [SerializeField] private Button cupButton;
    [SerializeField] private Button iceTeaButton;
    [SerializeField] private Button waterPotButton;
    [SerializeField] private Button syrupButton;
    [SerializeField] private Button coffeeMachineButton;
    [SerializeField] private Button iceMachineButton;
    [SerializeField] private Button trashCanButton;

    private readonly List<IngredientType> clickedIngredients = new List<IngredientType>();
    private readonly Dictionary<Button, UnityAction> buttonListeners = new Dictionary<Button, UnityAction>();

    public IReadOnlyList<IngredientType> ClickedIngredients => clickedIngredients;
    public event UnityAction<bool> IngredientSelectionChanged;

    private void OnEnable()
    {
        RegisterButton(cupButton, IngredientType.Cup);
        RegisterButton(iceTeaButton, IngredientType.IceTea);
        RegisterButton(waterPotButton, IngredientType.WaterPot);
        RegisterButton(syrupButton, IngredientType.Syrup);
        RegisterButton(coffeeMachineButton, IngredientType.CoffeeMachine);
        RegisterButton(iceMachineButton, IngredientType.IceMachine);
        RegisterClearButton(trashCanButton);

        NotifyIngredientSelectionChanged();
    }

    private void OnDisable()
    {
        foreach (KeyValuePair<Button, UnityAction> buttonListener in buttonListeners)
        {
            if (buttonListener.Key != null)
            {
                buttonListener.Key.onClick.RemoveListener(buttonListener.Value);
            }
        }

        buttonListeners.Clear();
    }

    private void RegisterButton(Button button, IngredientType ingredientType)
    {
        if (button == null)
        {
            Debug.LogWarning($"{nameof(CookingIngredientManager)}: {ingredientType} button is not assigned.");
            return;
        }

        if (buttonListeners.ContainsKey(button))
        {
            return;
        }

        UnityAction listener = () => RecordIngredient(ingredientType);
        buttonListeners.Add(button, listener);
        button.onClick.AddListener(listener);
    }

    private void RegisterClearButton(Button button)
    {
        if (button == null)
        {
            Debug.LogWarning($"{nameof(CookingIngredientManager)}: TrashCan button is not assigned.");
            return;
        }

        if (buttonListeners.ContainsKey(button))
        {
            return;
        }

        UnityAction listener = ClearIngredients;
        buttonListeners.Add(button, listener);
        button.onClick.AddListener(listener);
    }

    private void RecordIngredient(IngredientType ingredientType)
    {
        clickedIngredients.Add(ingredientType);

        LogCurrentIngredients();
        NotifyIngredientSelectionChanged();
    }

    private void ClearIngredients()
    {
        clickedIngredients.Clear();
        Debug.Log("Ingredient input cleared.");
        LogCurrentIngredients();
        NotifyIngredientSelectionChanged();
    }

    private void LogCurrentIngredients()
    {
        string ingredientLog = string.Join(", ", clickedIngredients.Select(ingredient => ingredient.ToString()));
        Debug.Log($"Current Ingredients: [{ingredientLog}]");
    }

    private void NotifyIngredientSelectionChanged()
    {
        IngredientSelectionChanged?.Invoke(clickedIngredients.Count > 0);
    }
}
