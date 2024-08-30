using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeBridge_Seasons : TimeBridge_Base
{
    public override void OnTick(float CurrentTime)
    {
        SeasonManager.Instance.Tick(CurrentTime);
    }
}
