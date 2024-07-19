using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Barrel : InteractableItemBase
{
    public enum IngredientType
    {
        Unknown, 

        Circle,
        CircleWithDots,
        Pentagon,
        PentagonWithDots,
        Square,
        SquareWithDots,
        Triangle,

        First = Circle,
        Last = Triangle
    }

    public IngredientType Ingredient;
    public int Capacity;

    public int LargeAmount;
    public int SmallAmount;

    protected int AmountRemaining;

    protected void Awake()
    {
        AmountRemaining = Capacity;
    }

    protected override void DidPickup(bool isPrimaryActivation)
    {
        
    }

    public int UseAmount(bool isLargePour)
    {
        int amountToUse = isLargePour ? LargeAmount : SmallAmount;

        // clamp the amount to use
        if (amountToUse >= AmountRemaining)
            amountToUse = AmountRemaining;
        
        AmountRemaining -= amountToUse;
        return amountToUse;
    }
}
