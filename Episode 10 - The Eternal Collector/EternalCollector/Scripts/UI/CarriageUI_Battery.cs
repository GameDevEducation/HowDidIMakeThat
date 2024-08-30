using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class CarriageUI_Battery : BaseCarriageUI
{
    public override EUIScreen Type() { return EUIScreen.Battery; }

    [SerializeField] TextMeshProUGUI EnergyStoredDisplay;
    [SerializeField] TextMeshProUGUI SolarIrradianceDisplay;
    [SerializeField] TextMeshProUGUI PowerUsageDisplay;

    // Start is called before the first frame update
    protected override void Start()
    {

    }

    // Update is called once per frame
    protected override void Update()
    {
        if (CharacterMotor.Instance.CurrentCarriage.CarriageUIType == Type())
        {
            EnergyStoredDisplay.text    = $"{EternalCollector.Instance.EnergyPowerStored:n0} kWh";
            SolarIrradianceDisplay.text = $"{TimeBridge_Light.SolarIrradiance:0.0²} W/m";
            PowerUsageDisplay.text      = $"{EternalCollector.Instance.CurrentPowerRequests:0.0} kW";
        }
    }

}
