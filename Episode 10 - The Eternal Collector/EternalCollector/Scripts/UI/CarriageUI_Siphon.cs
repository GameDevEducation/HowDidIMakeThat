using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class CarriageUI_Siphon : BaseCarriageUI
{
    [SerializeField] TextMeshProUGUI RecentItemsDisplay;
    [SerializeField] TextMeshProUGUI AcquisitionRate;

    public override EUIScreen Type() { return EUIScreen.Siphon; }

    // Start is called before the first frame update
    protected override void Start()
    {
        
    }

    // Update is called once per frame
    protected override void Update()
    {
        if (CharacterMotor.Instance.CurrentCarriage.CarriageUIType == Type())
        {
            var siphonBehaviour = (CarriageBehaviour_Siphon) CharacterMotor.Instance.CurrentCarriage.Behaviour;
            RecentItemsDisplay.text = siphonBehaviour.RecentItems;
            AcquisitionRate.text = siphonBehaviour.AcquisitionRate;
        }
    }
}
