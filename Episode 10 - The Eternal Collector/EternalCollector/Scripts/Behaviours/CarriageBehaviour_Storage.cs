using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "TEC/Carriage Behaviours/Storage", fileName = "CarriageBehaviour_Storage")]
public class CarriageBehaviour_Storage : BaseCarriageBehaviour
{
    [SerializeField] float BaseStorageCapacity = 1000f;

    public override void RefreshUpgrades()
    {
        _ScrapStorageAvailable = BaseStorageCapacity;

        foreach (var upgrade in EternalCollector.Instance.UnlockedUpgrades)
        {
            if (!upgrade.ApplicableBehaviours.Contains(TemplateBehaviour))
                continue;

            foreach (var effect in upgrade.Effects)
            {
                if (effect.Target == EUpgradeTarget.Common_ScrapStorageCapacity)
                    effect.Modify(ref _ScrapStorageAvailable);
            }
        }

        base.RefreshUpgrades();
    }
}
