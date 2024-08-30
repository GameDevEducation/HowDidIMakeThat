using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "TEC/Carriage Behaviours/Battery", fileName = "CarriageBehaviour_Battery")]
public class CarriageBehaviour_Battery : BaseCarriageBehaviour
{
    [SerializeField] float BaseEnergyStorage = 4800;
    [SerializeField] [Range(0f, 1f)] float InitialEnergyStored = 0.25f;

    protected override void PostBind()
    {
        base.PostBind();
        _EnergyStorageUsed = _EnergyStorageAvailable * InitialEnergyStored;
    }

    public override void RefreshUpgrades()
    {
        _EnergyStorageAvailable = BaseEnergyStorage;

        foreach (var upgrade in EternalCollector.Instance.UnlockedUpgrades)
        {
            if (!upgrade.ApplicableBehaviours.Contains(TemplateBehaviour))
                continue;

            foreach (var effect in upgrade.Effects)
            {
                if (effect.Target == EUpgradeTarget.Common_EnergyStorageCapacity)
                    effect.Modify(ref _EnergyStorageAvailable);
            }
        }

        base.RefreshUpgrades();
    }

    protected override float GetAmbientAudioIntensity()
    {
        if (_EnergyStorageAvailable == 0f)
            return 0f;

        return 100f * Mathf.Clamp01(_EnergyStorageUsed / _EnergyStorageAvailable);
    }
}
