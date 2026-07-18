using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Linq;
using TMPro; 
public class Receipt : MonoBehaviour, IDropHandler
{
    [SerializeField] private TMP_Text orderNameText; 

    private CustomerSpawner.ParsedRecipe requiredRecipe;

    public void Initialize(CustomerSpawner.ParsedRecipe recipe)
    {
        requiredRecipe = recipe;

        if (orderNameText != null)
        {
            orderNameText.text = requiredRecipe.MenuName;
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag != null)
        {
            CupDragController draggedCup = eventData.pointerDrag.GetComponent<CupDragController>();
            if (draggedCup != null) TryFulfillReceipt(draggedCup);
        }
    }

    public void TryFulfillReceipt(CupDragController cup)
    {
        if (cup == null || cup.ContentStates == null) return;

        bool isCorrect = true;

        int validCupIngredients = 0;
        foreach (var state in cup.ContentStates)
        {
            if (state != CupContentState.Normal) validCupIngredients++;
        }

        if (validCupIngredients != requiredRecipe.Ingredients.Count)
        {
            isCorrect = false;
        }
        else
        {
            foreach (var reqIngredient in requiredRecipe.Ingredients)
            {
                if (!cup.ContentStates.Contains(reqIngredient))
                {
                    isCorrect = false;
                    break;
                }
            }
        }

        if (isCorrect)
        {
            Debug.Log($"Drink '{requiredRecipe.MenuName}' was served successfully!");
            Destroy(gameObject);
        }
        else
        {
            Debug.LogWarning($"Wrong drink! This receipt needs {requiredRecipe.MenuName}.");
        }
    }
}