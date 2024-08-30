using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "TEC/Carriage Behaviours/Solar", fileName = "CarriageBehaviour_Solar")]
public class CarriageBehaviour_Solar : BaseCarriageBehaviour
{
    [SerializeField] float BaseSolarPanelArea = 72;
    [SerializeField] float BaseSolarPanelEfficiency = 0.15f;
    [SerializeField] float BaseEnergyStorage = 3600;
    [SerializeField] [Range(0f, 1f)] float InitialEnergyStored = 0.25f;

    float WorkingSolarPanelArea;
    float WorkingSolarPanelEfficiency;

    protected override void PostBind()
    {
        base.PostBind();
        _EnergyStorageUsed = _EnergyStorageAvailable * InitialEnergyStored;
    }

    public override void RefreshUpgrades()
    {
        WorkingSolarPanelArea = BaseSolarPanelArea;
        WorkingSolarPanelEfficiency = BaseSolarPanelEfficiency;
        _EnergyStorageAvailable = BaseEnergyStorage;

        foreach (var upgrade in EternalCollector.Instance.UnlockedUpgrades)
        {
            if (!upgrade.ApplicableBehaviours.Contains(TemplateBehaviour))
                continue;

            foreach (var effect in upgrade.Effects)
            {
                if (effect.Target == EUpgradeTarget.Common_EnergyStorageCapacity)
                    effect.Modify(ref _EnergyStorageAvailable);
                else if (effect.Target == EUpgradeTarget.Solar_PanelArea)
                    effect.Modify(ref WorkingSolarPanelArea);
                else if (effect.Target == EUpgradeTarget.Solar_PanelEfficiency)
                    effect.Modify(ref WorkingSolarPanelEfficiency);
            }
        }

        base.RefreshUpgrades();
    }

    public override void Tick_Power()
    {
        base.Tick_Power();

        // determine the amount of energy gained: Area (m2) * Efficiency (%) * Irradiance (W/m2)
        float energyGained = WorkingSolarPanelArea * WorkingSolarPanelEfficiency * TimeBridge_Light.SolarIrradiance;
        energyGained = EternalCollector.EffectIrradiance(energyGained);
        energyGained *= ToDManager.Instance.DeltaTime;
        energyGained /= 1000f; // convert to kW

        // update the amount of energy stored
        _EnergyStorageUsed = Mathf.Min(_EnergyStorageUsed + energyGained, EnergyStorageCapacity);
    }

    protected override float GetAmbientAudioIntensity()
    {
        if (_EnergyStorageAvailable == 0f)
            return 0f;

        return 100f * Mathf.Clamp01(_EnergyStorageUsed / _EnergyStorageAvailable);
    }
}
