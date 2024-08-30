using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EUIScreen
{
    None            = 0,

    Overview        = 1,
    Upgrades        = 2,

    Siphon          = 10,
    Storage         = 20,
    Engine          = 30,
    EngineBooster   = 40,
    Solar           = 50,
    Battery         = 60,
}

public abstract class BaseCarriageUI : MonoBehaviour
{
    public abstract EUIScreen Type();

    // Start is called before the first frame update
    protected virtual void Start()
    {
    }

    // Update is called once per frame
    protected virtual void Update()
    {
        
    }

    public virtual void OnSwitchToScreen()
    {

    }
}
