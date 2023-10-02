using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Toilet : MonoBehaviour
{
    public UnityEngine.UI.Button ActivateToiletButton;
    public TMPro.TextMeshProUGUI ActivateToiletText;

    public float WaterRequired = 6f;
    public WaterTank WaterSource;

    public float ToiletUseLockoutTime = 180;
    protected float LockoutTimeRemaining = 0f;

    public GameObject SoundEmitter;

    public UnityEvent OnUseToilet;

    protected int ToiletUsesToday = 0;

    // Start is called before the first frame update
    void Start()
    {
        RefreshUI();

        GameManager.Instance.OnNextDay.AddListener(OnNextDay);             
    }

    // Update is called once per frame
    void Update()
    {
        if (GameManager.Instance.IsPaused)   
            return;

        // update the lockout time
        if (LockoutTimeRemaining > 0)
        {
            LockoutTimeRemaining -= Time.deltaTime;
        }
    }

    void RefreshUI()
    {
        if (LockoutTimeRemaining > 0)
        {
            ActivateToiletButton.interactable = false;
            ActivateToiletText.text = "At water quota";
        }
        else if (!WaterSource.IsAnyWaterAvailable)
        {
            ActivateToiletButton.interactable = false;
            ActivateToiletText.text = "No water";
        }
        else
        {
            ActivateToiletButton.interactable = true;
            ActivateToiletText.text = "Use Toilet";
        }
    } 

    public void OnActivateToilet()
    {
        if (LockoutTimeRemaining > 0 || !WaterSource.IsAnyWaterAvailable)
            return;

        // consume the water
        float waterConsumed = WaterSource.ConsumeUnfilteredWater(WaterRequired);

        // didn't consume enough? use some filtered
        if (waterConsumed < WaterRequired)
            waterConsumed += WaterSource.ConsumeFilteredWater(WaterRequired - waterConsumed);

        // update used flags
        ++ToiletUsesToday;
        LockoutTimeRemaining = ToiletUseLockoutTime;

        AkSoundEngine.PostEvent("Play_Toilet", SoundEmitter);

        RefreshUI();

        OnUseToilet?.Invoke();
    }

    public void OnNextDay(UpdateStage stage)
    {
        if (stage == UpdateStage.Plants)
        {
            // calculate the maximum uses per day
            int numPossibleUsesPerDay = Mathf.CeilToInt((GameManager.Instance.DayConfiguration.Duration + GameManager.Instance.NightConfiguration.Duration) / ToiletUseLockoutTime);
            int totalNumUses = GameManager.Instance.NumDaysPassed * numPossibleUsesPerDay;
            
            // determine and apply the forced consumption
            float forcedConsumption = (totalNumUses - ToiletUsesToday) * WaterRequired;
            float amountConsumed = WaterSource.ConsumeUnfilteredWater(forcedConsumption);

            // forced consumption might need to carry over to clean water
            if (amountConsumed < forcedConsumption)
                WaterSource.ConsumeFilteredWater(forcedConsumption);

            // reset the toilet being used
            LockoutTimeRemaining = 0;
            ToiletUsesToday = 0;
        }

        if (stage == UpdateStage.UI)
            RefreshUI();
    }    
}
