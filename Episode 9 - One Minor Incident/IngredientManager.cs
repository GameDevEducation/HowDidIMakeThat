using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IngredientManager : MonoBehaviour
{
    public GameObject SoundEmitter;
    
    public IngredientUI[] IngredientDisplays;
    public Sprite[] IngredientSprites;
    public TMPro.TextMeshProUGUI StatusText;
    public int MinAmount = 1;
    public int MaxAmount = 6;

    public int PenaltyPerIncorrectIngredient = 2;
    public int PenaltyPerTooMuchIngredient = 1;
    public int PenaltyPerMissingIngredient = 1;

    public int TotalPenalty = 0;

    public float CycleTime = 60f;
    protected float CycleTimeRemaining = -1;

    protected List<Barrel.IngredientType> Ingredients = new List<Barrel.IngredientType>();
    protected List<int> IngredientAmounts = new List<int>();

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // cycle the recipe
        if (CycleTimeRemaining > 0)
        {
            CycleTimeRemaining -= Time.deltaTime;

            if (CycleTimeRemaining <= 0)
            {
                CycleRecipe();
            }
        }

        if (TotalPenalty == 0)
            StatusText.text = "On Track";
        else if (TotalPenalty < 40)
            StatusText.text = "Minor Issues Detected";
        else if (TotalPenalty < 80)
            StatusText.text = "Major Issues Detected";
        else if (TotalPenalty > 200)
            StatusText.text = "What have you done?!?";
        else 
            StatusText.text = "Critical Issues Detected";
    }

    public void CycleRecipe()
    {
        // don't cycle in the final 20 s
        if (GameManager.Instance.TimeRemaining < 20f && GameManager.Instance.Phase == GameManager.GamePhases.Manufacturing)
            return;
            
        // penalise any missing ingredients
        for (int index = 0; index < IngredientAmounts.Count; ++index)
        {
            TotalPenalty += PenaltyPerMissingIngredient * IngredientAmounts[index];
        }

        // clear the ingredients
        Ingredients.Clear();
        IngredientAmounts.Clear();

        // pick the ingredients
        while (Ingredients.Count < IngredientDisplays.Length)
        {
            Barrel.IngredientType ingredient = (Barrel.IngredientType) Random.Range((int)Barrel.IngredientType.First, (int)Barrel.IngredientType.Last + 1);

            // already picked this ingredient?
            if (Ingredients.Contains(ingredient))
                continue;

            Ingredients.Add(ingredient);
        }

        // Refresh the displays
        for (int index = 0; index < Ingredients.Count; ++index)
        {
            Barrel.IngredientType ingredient = Ingredients[index];

            int amount = Random.Range(MinAmount, MaxAmount + 1);
            IngredientAmounts.Add(amount);

            IngredientDisplays[index].SetIngredient(ingredient, IngredientSprites[(int)ingredient], amount);
        }

        CycleTimeRemaining = CycleTime;

        AkSoundEngine.PostEvent("Play_New_Ingredient_List", SoundEmitter);
    }

    protected void AddIngredient(Barrel.IngredientType ingredient, int ingredientIndex, int amount)
    {
        // ingredient matches?
        if (ingredient == Ingredients[ingredientIndex])
        {
            // update the amount needed
            IngredientAmounts[ingredientIndex] -= amount;

            // correct amount added? (ie. some still remaining to add)
            if (IngredientAmounts[ingredientIndex] > 0)
            {
                // nothing to do
            }
            else
            {
                TotalPenalty += PenaltyPerTooMuchIngredient * Mathf.Abs(IngredientAmounts[ingredientIndex]);

                IngredientAmounts[ingredientIndex] = 0;
            }

            // refresh the ingredient display
            IngredientDisplays[ingredientIndex].SetIngredient(ingredient, IngredientSprites[(int)ingredient], IngredientAmounts[ingredientIndex]);
        }
        else
        {
            TotalPenalty += PenaltyPerIncorrectIngredient * amount;
        }
    }

    public void OnChute1_AddIngredient(int amount, int ingredientType)
    {
        Barrel.IngredientType ingredient = (Barrel.IngredientType)ingredientType;

        AddIngredient(ingredient, 0, amount);
    }

    public void OnChute2_AddIngredient(int amount, int ingredientType)
    {
        Barrel.IngredientType ingredient = (Barrel.IngredientType)ingredientType;

        AddIngredient(ingredient, 1, amount);
    }
    
    public void OnChute3_AddIngredient(int amount, int ingredientType)
    {
        Barrel.IngredientType ingredient = (Barrel.IngredientType)ingredientType;

        AddIngredient(ingredient, 2, amount);
    }    
}
