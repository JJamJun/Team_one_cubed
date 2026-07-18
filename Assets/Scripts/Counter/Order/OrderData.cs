using System.Collections.Generic;

public class OrderData
{
    public CustomerSpawner.ParsedRecipe Recipe { get; private set; }

    public float MaxPatience { get; private set; }
    public float CurrentPatience { get; set; }

    public OrderData(CustomerSpawner.ParsedRecipe recipe, float patienceDuration)
    {
        Recipe = recipe;
        MaxPatience = patienceDuration;
        CurrentPatience = patienceDuration;
    }

    public string GetOrderDescription()
    {
        return $"{Recipe.MenuName} 하나 주세요!";
    }
}