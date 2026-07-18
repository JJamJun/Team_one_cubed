using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class CustomerSpawner : MonoBehaviour
{
    [Serializable]
    public struct ParsedRecipe
    {
        public string MenuName;
        public List<CupContentState> Ingredients;
    }

    [Header("Prefabs & Spawn Points")]
    [SerializeField] private GameObject customerPrefab;
    [SerializeField] private Transform spawnPoint;

    [Header("Spawner Settings")]
    [SerializeField] private string recipeResourcePath = "temp_recipe";
    [SerializeField] private float minSpawnDelay = 5f;
    [SerializeField] private float maxSpawnDelay = 15f;
    [SerializeField] private float baseCustomerPatience = 45f;

    private CustomerController currentActiveCustomer;
    private bool isSpawning = true;
    private List<ParsedRecipe> availableRecipes = new List<ParsedRecipe>();

    private void Start()
    {
        LoadRecipesFromText();
        StartCoroutine(SpawnLoop());
    }

    private void LoadRecipesFromText()
    {
        TextAsset recipeAsset = Resources.Load<TextAsset>(recipeResourcePath);
        if (recipeAsset == null) return;

        string[] lines = recipeAsset.text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (string rawLine in lines)
        {
            string line = rawLine.Trim();
            if (string.IsNullOrEmpty(line)) continue;

            char separator = line.Contains("\t") ? '\t' : ',';
            string[] cells = line.Split(separator);
            if (cells.Length < 2) continue;

            string menuName = cells[0].Trim();
            List<CupContentState> ingredients = new List<CupContentState>();

            // Loop through all remaining columns to get ingredients
            for (int i = 1; i < cells.Length; i++)
            {
                string ingredientName = cells[i].Trim();
                if (string.IsNullOrEmpty(ingredientName)) continue;

                if (TryParseIngredientState(ingredientName, out CupContentState state))
                {
                    ingredients.Add(state);
                }
            }

            availableRecipes.Add(new ParsedRecipe { MenuName = menuName, Ingredients = ingredients });
        }
    }

    private IEnumerator SpawnLoop()
    {
        yield return new WaitForSeconds(2f);
        while (isSpawning)
        {
            if (currentActiveCustomer == null && availableRecipes.Count > 0) SpawnCustomer();
            yield return new WaitForSeconds(UnityEngine.Random.Range(minSpawnDelay, maxSpawnDelay));
        }
    }

    private void SpawnCustomer()
    {
        if (customerPrefab == null || spawnPoint == null) return;

        GameObject spawnedObj = Instantiate(customerPrefab, spawnPoint.position, Quaternion.identity, spawnPoint);
        currentActiveCustomer = spawnedObj.GetComponent<CustomerController>();

        ParsedRecipe randomRecipe = availableRecipes[UnityEngine.Random.Range(0, availableRecipes.Count)];
        currentActiveCustomer.Initialize(new OrderData(randomRecipe, baseCustomerPatience));
    }

    public CustomerController GetCurrentCustomer() => currentActiveCustomer;

    public void ClearCurrentCustomer()
    {
        if (currentActiveCustomer != null)
        {
            Destroy(currentActiveCustomer.gameObject);
            currentActiveCustomer = null;
        }
    }

    private bool TryParseIngredientState(string ingredientName, out CupContentState state)
    {
        string normalized = ingredientName.Replace(" ", "").Replace("_", "").Replace("-", "").ToLowerInvariant();
        switch (normalized)
        {
            case "icetea": case "iceteaed": state = CupContentState.IceTeaEd; return true;
            case "waterpot": case "waterpotted": state = CupContentState.WaterPotEd; return true;
            case "icemachine": case "icemachineed": state = CupContentState.IceMachineEd; return true;
            case "coffeemachine": case "coffeemachineed": state = CupContentState.CoffeeMachineEd; return true;
            case "syrup": case "syruped": state = CupContentState.SyrupEd; return true;
            default: state = CupContentState.Normal; return false;
        }
    }
}