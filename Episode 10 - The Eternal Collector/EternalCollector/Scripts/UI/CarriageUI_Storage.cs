using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class CarriageUI_Storage : BaseCarriageUI
{
    [SerializeField] TextMeshProUGUI LocalStorage;
    [SerializeField] TextMeshProUGUI GlobalStorage;

    public override EUIScreen Type() { return EUIScreen.Storage; }

    // Start is called before the first frame update
    protected override void Start()
    {
        
    }

    // Update is called once per frame
    protected override void Update()
    {
        if (CharacterMotor.Instance.CurrentCarriage.CarriageUIType == Type())
        {
            var storageBehaviour = (CarriageBehaviour_Storage)CharacterMotor.Instance.CurrentCarriage.Behaviour;

            LocalStorage.text = $"{storageBehaviour.ScrapStorageUsed:n0} of {storageBehaviour.ScrapStorageAvailable:n0} kg scrap stored";
            GlobalStorage.text = $"{EternalCollector.Instance.ScrapStorageUsed:n0} of {EternalCollector.Instance.ScrapStorageAvailable:n0} kg scrap stored";
        }
    }
}
