using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class CarriageUI_Overview : BaseCarriageUI
{
    [SerializeField] TextMeshProUGUI    Speed_Label;
    [SerializeField] Slider             Speed_ValueSlider;
    [SerializeField] TextMeshProUGUI    Speed_Value;

    [SerializeField] TextMeshProUGUI    Scrap_Label;
    [SerializeField] Slider             Scrap_ValueSlider;
    [SerializeField] TextMeshProUGUI    Scrap_Value;

    [SerializeField] TextMeshProUGUI    Power_Label;
    [SerializeField] Slider             Power_ValueSlider;
    [SerializeField] TextMeshProUGUI    Power_Value;

    [SerializeField] TextMeshProUGUI    PowerDelta_Label;
    [SerializeField] TextMeshProUGUI    PowerDelta_Value;

    public override EUIScreen Type() { return EUIScreen.Overview; }

    // Start is called before the first frame update
    protected override void Start()
    {
        
    }

    // Update is called once per frame
    protected override void Update()
    {
        Speed_ValueSlider.value = EternalCollector.Instance.RequestedSpeedFactor;
        Speed_Value.text = $"{EternalCollector.Instance.CurrentSpeedKPH:0} kph";

        float scrapLevel = 0f;
        if (EternalCollector.Instance.ScrapStorageAvailable > 0)
            scrapLevel = EternalCollector.Instance.ScrapStorageUsed / EternalCollector.Instance.ScrapStorageAvailable;
        Scrap_ValueSlider.value = scrapLevel;
        Scrap_Value.text = $"{EternalCollector.Instance.ScrapStorageUsed:n0} kg";

        float powerLevel = 0f;
        if (EternalCollector.Instance.EnergyStorageCapacity > 0f)
            powerLevel = EternalCollector.Instance.EnergyPowerStored / EternalCollector.Instance.EnergyStorageCapacity;
        Power_ValueSlider.value = powerLevel;
        Power_Value.text = $"{EternalCollector.Instance.EnergyPowerStored:n0} kWh";

        PowerDelta_Value.text = $"{EternalCollector.Instance.CurrentPowerDelta:0.0} kWh";
    }
}
