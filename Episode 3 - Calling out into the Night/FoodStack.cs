using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class FoodStack : MonoBehaviour
{
    private List<CannedFood> AllFood;

    void Start()
    {
        AllFood = FindObjectsOfType<CannedFood>().ToList();
    }

    public bool IsFoodAvailable
    {
        get
        {
            return AllFood.Count > 0;
        }
    }

    public void ConsumeFood(int amount)
    {
        for (int foodConsumed = 0; foodConsumed < amount; ++foodConsumed)
        {
            // no food left
            if (AllFood.Count == 0)
                return;

            // pick a random food
            int randomFoodIndex = Random.Range(0, AllFood.Count);
            Destroy(AllFood[randomFoodIndex].gameObject);

            // remove from the list
            AllFood.RemoveAt(randomFoodIndex);
        }
    }

    public void ConsumeFood(CannedFood food)
    {
        AllFood.Remove(food);
    }
}
