using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CarriageUI_Engine : BaseCarriageUI
{
    public override EUIScreen Type() { return EUIScreen.Engine; }

    [SerializeField] TextMeshProUGUI EnginePowerDisplay;
    [SerializeField] TextMeshProUGUI P2WDisplay;
    [SerializeField] TextMeshProUGUI CurrentSpeedDisplay;
    [SerializeField] Slider ThrottleSlider;
    [SerializeField] List<Image> SpeedButtons;
    [SerializeField] Color SpeedActiveColour;
    [SerializeField] Color SpeedInactiveColour;

    // Start is called before the first frame update
    protected override void Start()
    {

    }

    // Update is called once per frame
    float LastSpeedFactor = -1f;
    protected override void Update()
    {
        if (CharacterMotor.Instance.CurrentCarriage == null)
            return;

        if (CharacterMotor.Instance.CurrentCarriage.CarriageUIType == Type())
        {
            EnginePowerDisplay.text = $"{EternalCollector.Instance.TotalTrainPower:n0} kW";
            P2WDisplay.text = $"{EternalCollector.Instance.CurrentPowerToWeightRatio:0.0} W/kg";
            CurrentSpeedDisplay.text = $"{EternalCollector.Instance.CurrentSpeedKPH:0} kPH";
            ThrottleSlider.SetValueWithoutNotify(EternalCollector.Instance.RequestedSpeedFactor);

            if (LastSpeedFactor != EternalCollector.Instance.RequestedSpeedFactor)
            {
                LastSpeedFactor = EternalCollector.Instance.RequestedSpeedFactor;
                int activeSpeedButton = 0;
                if (EternalCollector.Instance.RequestedSpeedFactor >= 0.99f)
                    activeSpeedButton = SpeedButtons.Count - 1;
                else if (EternalCollector.Instance.RequestedSpeedFactor >= 0.74f)
                    activeSpeedButton = SpeedButtons.Count - 2;
                else if (EternalCollector.Instance.RequestedSpeedFactor >= 0.49f)
                    activeSpeedButton = SpeedButtons.Count - 3;
                else if (EternalCollector.Instance.RequestedSpeedFactor >= 0.24f)
                    activeSpeedButton = SpeedButtons.Count - 4;
                for (int index = 0; index < SpeedButtons.Count; index++)
                {
                    SpeedButtons[index].color = index == activeSpeedButton ? SpeedActiveColour : SpeedInactiveColour;
                }
            }
        }
    }

    public void OnSetThrottle(float value)
    {
        EternalCollector.Instance.SetSpeedFactor(value);
    }
}
