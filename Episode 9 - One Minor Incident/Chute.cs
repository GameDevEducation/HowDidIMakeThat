using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class UseIngredientEvent : UnityEvent<int, int> {}

public class Chute : InteractableItemBase
{
    protected InteractableChecker Player;
    public UseIngredientEvent OnIngredientUsed;

    protected override void Start()
    {
        base.Start();

        Player = FindObjectOfType<InteractableChecker>();
    }

    protected override void DidPickup(bool isPrimaryActivation)
    {
        // does the player have an attached barrel?
        if (Player.AttachedItem && Player.AttachedItem is Barrel)
        {
            Barrel ingredient = Player.AttachedItem as Barrel;

            int amountUsed = ingredient.UseAmount(isPrimaryActivation);

            OnIngredientUsed?.Invoke(amountUsed, (int)ingredient.Ingredient);

            if (amountUsed > 0)
                AkSoundEngine.PostEvent(isPrimaryActivation ? "Play_Large_Barrel_Pour" : "Play_Small_Barrel_Pour", Player.AttachedItem.gameObject);
        }
    }
}
