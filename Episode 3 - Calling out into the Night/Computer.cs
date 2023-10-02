using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Computer : MonoBehaviour
{
    public TMPro.TextMeshProUGUI StatusText;
    public TMPro.TextMeshProUGUI BatteryText;

    public float BatteryChargeTime = 300f;
    protected float BatteryCharge = 0;
    protected bool IsCharging = true;

    protected bool WasPerformingRadioScan = false;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (GameManager.Instance.IsPaused)
            return;

        // charging currently?
        if (IsCharging)
        {
            // update the charge
            BatteryCharge = Mathf.Clamp01(BatteryCharge + (Time.deltaTime / BatteryChargeTime));
        }

        // update the status text
        if (GameManager.Instance.IsDayTime)
        {
            StatusText.text = "Terminal in low power mode";
        }
        else if (GameManager.Instance.IsPerformingRadioScan)
        {
            StatusText.text = "Scanning for activity";
        }
        else
        {
            StatusText.text = "Terminal ready for use";
        }

        BatteryText.text = "Battery Status: " + Mathf.RoundToInt(100 * BatteryCharge).ToString() + "%";
    }

    public void OnBeginRadioScan()
    {
        WasPerformingRadioScan = true;
        AkSoundEngine.PostEvent("Play_Radio_Scan", gameObject);
    }

    public void OnSunrise()
    {
        if (WasPerformingRadioScan)
            AkSoundEngine.PostEvent("Stop_Radio_Scan", gameObject);

        BatteryCharge = 0;
        IsCharging = true;
        WasPerformingRadioScan = false;
    }

    public void OnDestroy()
    {
        if (WasPerformingRadioScan)
            AkSoundEngine.PostEvent("Stop_Radio_Scan", gameObject);
    }

    public void OnSunset()
    {
        IsCharging = false;
    }
}
