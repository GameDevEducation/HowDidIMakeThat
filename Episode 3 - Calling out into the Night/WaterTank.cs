using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterTank : MonoBehaviour
{
    public TMPro.TextMeshProUGUI IntakeStatus;
    public TMPro.TextMeshProUGUI UnfilteredWaterDisplay;
    public TMPro.TextMeshProUGUI FilteredWaterDisplay;

    public UnityEngine.UI.Button BeginFiltrationButton;
    public TMPro.TextMeshProUGUI FiltrationButtonText;

    public float FilteredLeakPerDay = 1f;
    public float UnfilteredLeakPerDay = 1f;

    public float MaximumTankCapacity = 1200f;

    public float FilteredWaterLevel = 200f;
    public float UnfilteredWaterLevel = 1000f;

    public float WaterFilteredPerCycle = 50f;

    public int ForcedDropToZeroDay = 20;

    protected bool IsFiltering = false;

    // Start is called before the first frame update
    void Start()
    {
        RefreshUI();

        GameManager.Instance.OnNextDay.AddListener(OnNextDay);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    protected void RefreshUI()
    {
        UnfilteredWaterDisplay.text = Mathf.FloorToInt(UnfilteredWaterLevel).ToString() + " Litres";
        FilteredWaterDisplay.text = Mathf.FloorToInt(FilteredWaterLevel).ToString() + " Litres";

        // update the filtration button status
        if (IsFiltering || UnfilteredWaterLevel <= 0)
        {            
            BeginFiltrationButton.interactable = false;
            FiltrationButtonText.text = IsFiltering ? "Filtration in Progress" : "Filtration Unavailable";
        }
        else
        {
            BeginFiltrationButton.interactable = true;
            FiltrationButtonText.text = "Begin Filtration Cycle";
        }
    }

    public bool IsWaterAvailable
    {
        get
        {
            return FilteredWaterLevel > 0f;
        }
    }

    public bool IsAnyWaterAvailable
    {
        get
        {
            return FilteredWaterLevel > 0f || UnfilteredWaterLevel > 0f;
        }
    }

    public void BeginFiltrationCycle()
    {
        IsFiltering = true;

        AkSoundEngine.PostEvent("Play_Water_Filter", gameObject);

        RefreshUI();
    }

    public float ConsumeUnfilteredWater(float amountRequested)
    {
        // calculate the amount consumed
        float actualAmountConsumed = Mathf.Min(amountRequested, UnfilteredWaterLevel);

        // update the water level
        UnfilteredWaterLevel -= actualAmountConsumed;

        // update the displays
        RefreshUI();

        return actualAmountConsumed;
    }

    public float ConsumeFilteredWater(float amountRequested)
    {
        // calculate the amount consumed
        float actualAmountConsumed = Mathf.Min(amountRequested, FilteredWaterLevel);

        // update the water level
        FilteredWaterLevel -= actualAmountConsumed;

        // update the displays
        RefreshUI();

        return actualAmountConsumed;
    }

    public void OnDestroy()
    {
        if (IsFiltering)
        {
            AkSoundEngine.PostEvent("Stop_Water_Filter", gameObject);
            IsFiltering = false;
        }
    }

    public void OnNextDay(UpdateStage stage)
    {
        if (stage == UpdateStage.Purifier)
        {
            if (GameManager.Instance.CurrentDay >= ForcedDropToZeroDay)
                UnfilteredWaterLevel = FilteredWaterLevel = 0;

            // water was not filtered
            if (!IsFiltering)
            {
                // could water have been filtered?
                if (UnfilteredWaterLevel > 0)
                    GameManager.Instance.RegisterDailyEvent(new DailyEvent_DidNotFilterWater(GameManager.Instance.PreviousDay));
                else
                    GameManager.Instance.RegisterDailyEvent(new DailyEvent_CouldNotFilterWater(GameManager.Instance.PreviousDay));
            }

            // Apply leaking
            for (int day = 0; day < GameManager.Instance.NumDaysPassed; ++day)
            {
                UnfilteredWaterLevel = Mathf.Max(0, UnfilteredWaterLevel - UnfilteredLeakPerDay);
                FilteredWaterLevel = Mathf.Max(0, FilteredWaterLevel - FilteredLeakPerDay);
            }

            // filtering was requested?
            if (IsFiltering)
            {
                // determine the amount filtered
                float amountFiltered = Mathf.Min(WaterFilteredPerCycle, UnfilteredWaterLevel);

                // update the water levels
                UnfilteredWaterLevel -= amountFiltered;
                FilteredWaterLevel += amountFiltered;

                AkSoundEngine.PostEvent("Stop_Water_Filter", gameObject);

                IsFiltering = false;
            }
        }

        if (stage == UpdateStage.UI)
            RefreshUI();
    }
}
