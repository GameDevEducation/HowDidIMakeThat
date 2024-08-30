using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "TEC/Events/Shop Prices", fileName = "Event_ShopPrices")]
public class Event_ShopPrices : Event_Base, IShopModifier
{
    [SerializeField, Range(0.5f, 2f)] protected float CostMultiplier = 1f;
    [SerializeField, Range(0.5f, 2f)] protected float DeliveryTimeMultiplier = 1f;

    float WorkingCostMultiplier;
    float WorkingDeliveryTimeMultiplier;

    protected override void OnStart_Internal()
    {
        WorkingCostMultiplier = CostMultiplier * Variation;
        WorkingDeliveryTimeMultiplier = DeliveryTimeMultiplier * Variation;

        EternalCollector.RegisterModifier(this);
    }

    protected override void Tick_Internal()
    {
    }

    protected override void OnFinish_Internal()
    {
        EternalCollector.DeregisterModifier(this);
    }

    public int EffectPrice(int currentPrice)
    {
        return Mathf.RoundToInt(currentPrice * Mathf.Lerp(1f, WorkingCostMultiplier, Intensity));
    }

    public float EffectDeliveryTime(float currentDeliveryTime)
    {
        return currentDeliveryTime * Mathf.Lerp(1f, WorkingDeliveryTimeMultiplier, Intensity);
    }
}

