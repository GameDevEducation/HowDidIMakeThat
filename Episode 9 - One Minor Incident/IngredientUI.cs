using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IngredientUI : MonoBehaviour
{
    public UnityEngine.UI.Image IngredientImage;
    public GameObject[] AmountIndicators;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetIngredient(Barrel.IngredientType ingredient, Sprite newSprite, int amount)
    {
        IngredientImage.sprite = newSprite;

        // change which indicators are active
        for (int index = 0; index < AmountIndicators.Length; ++index)
        {
            AmountIndicators[index].SetActive(index < amount);
        }
    }
}
