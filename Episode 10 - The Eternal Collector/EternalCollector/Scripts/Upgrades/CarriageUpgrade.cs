using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EUpgradeTarget
{
    Unknown                         = 0,

    Common_Weight                   = 1,
    Common_EnergyStorageCapacity    = 2,
    Common_ScrapStorageCapacity     = 3,
    Common_PowerUsage               = 4,

    Siphon_CollectionChance         = 10,
    Siphon_ScanAngle                = 11,
    Siphon_SiphonSpeed              = 12,

    // Scrap Storage - starts at 20

    Engine_Power                    = 30,
    Engine_Efficiency               = 31,

    Solar_PanelArea                 = 50,
    Solar_PanelEfficiency           = 51,
}

public enum EModificationEffect
{
    Add,
    Scale,
    Set
}

[System.Serializable]
public class UpgradeEffect
{
    public EUpgradeTarget Target;
    public EModificationEffect Effect;
    public float Value;

    public void Modify(ref float currentValue)
    {
        if (Effect == EModificationEffect.Add)
            currentValue += Value;
        else if (Effect == EModificationEffect.Scale)
            currentValue *= Value;
        else if (Effect == EModificationEffect.Set)
            currentValue = Value;
    }
}

[CreateAssetMenu(menuName = "TEC/Upgrade", fileName = "Upgrade")]
public class CarriageUpgrade : ScriptableObject
{
    public string Category;
    public string DisplayName;
    public int Cost;
    [TextArea(3, 5)] public string Description;
    public List<CarriageUpgrade> Prerequisites;

    public List<UpgradeEffect> Effects;
    public List<BaseCarriageBehaviour> ApplicableBehaviours;
    public bool AddsUpperDeck = false;
    public bool IsPerCarriage = true;
}
