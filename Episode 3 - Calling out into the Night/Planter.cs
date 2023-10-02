using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Planter : MonoBehaviour
{
    public TMPro.TextMeshProUGUI PlantType;
    public TMPro.TextMeshProUGUI HydrationStatus;

    public UnityEngine.UI.Button BeginWateringButton;
    public TMPro.TextMeshProUGUI BeginWateringButtonText;

    public float WaterRequired = 4f;
    public WaterTank WaterSource;

    public List<ParticleSystem> Sprinklers;

    public float HydrationDropPerDay = 0.2f;
    public float CanWaterThreshold = 0.6f; // cannot water if hydration is above this
    protected float CurrentHydrationLevel = 0f;

    protected bool WateringInProgress = false;

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

    void RefreshUI()
    {
        if (CurrentHydrationLevel <= CanWaterThreshold)
            HydrationStatus.text = "Watering Needed";
        else
            HydrationStatus.text = "Optimal";

        // update the watering button
        if (!WaterSource.IsWaterAvailable)
        {
            BeginWateringButton.interactable = false;
            BeginWateringButtonText.text = "No Water Available";
        }
        else if (WateringInProgress)
        {
            BeginWateringButton.interactable = false;
            BeginWateringButtonText.text = "Watering in Progress";
        }
        else if (CurrentHydrationLevel > CanWaterThreshold)
        {
            BeginWateringButton.interactable = false;
            BeginWateringButtonText.text = "Hydration Optimal";            
        }
        else
        {
            BeginWateringButton.interactable = true;
            BeginWateringButtonText.text = "Begin Watering";
        }
    }

    public void BeginWatering()
    {
        // no watering needed
        if (CurrentHydrationLevel > CanWaterThreshold || WateringInProgress)
            return;

        float waterProvided = WaterSource.ConsumeFilteredWater(WaterRequired);

        // update the hydration level
        CurrentHydrationLevel = Mathf.Clamp01(CurrentHydrationLevel + (waterProvided / WaterRequired));

        // if water was provided then start the particles
        if (waterProvided > 0)
        {
            foreach(ParticleSystem ps in Sprinklers)
            {
                AkSoundEngine.PostEvent("Play_Sprinkler", ps.gameObject);
                ps.Play();
            }

            WateringInProgress = true;
        }

        RefreshUI();
    }

    public void OnDestroy()
    {
        if (WateringInProgress)
        {
            // Stop all sprinklers
            foreach(ParticleSystem ps in Sprinklers)
            {
                AkSoundEngine.PostEvent("Stop_Sprinkler", ps.gameObject);
            }        
        }
    }

    public void OnNextDay(UpdateStage stage)
    {
        if (stage == UpdateStage.Plants)
        {
            // plant was not watered
            if (!WateringInProgress)
            {
                // add the daily event depending on if we could water or not
                if (WaterSource.IsWaterAvailable)
                    GameManager.Instance.RegisterDailyEvent(new DailyEvent_DidNotWaterPlants(GameManager.Instance.PreviousDay));
                else
                    GameManager.Instance.RegisterDailyEvent(new DailyEvent_CouldNotWaterPlants(GameManager.Instance.PreviousDay));
            }
            else
            {
                // Stop all sprinklers
                foreach(ParticleSystem ps in Sprinklers)
                {
                    AkSoundEngine.PostEvent("Stop_Sprinkler", ps.gameObject);
                    ps.Stop();
                }

                WateringInProgress = false;
            }
            
            // apply water usage
            for (int day = 0; day < GameManager.Instance.NumDaysPassed; ++day)
                CurrentHydrationLevel = Mathf.Clamp01(CurrentHydrationLevel - HydrationDropPerDay);
        }

        if (stage == UpdateStage.UI)
            RefreshUI();
    }
}
