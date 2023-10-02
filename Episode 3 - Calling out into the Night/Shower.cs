using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Shower : MonoBehaviour
{
    public UnityEngine.UI.Button ActivateShowerButton;
    public TMPro.TextMeshProUGUI ActivateShowerText;

    public float WaterRequired = 4f;
    public float ShowerActiveDuration = 15f;
    public WaterTank WaterSource;

    public ParticleSystem ShowerParticles;

    protected bool ShoweringInProgress = false;
    protected bool ShowerUsedToday = false;
    protected float ShowerTimeRemaining = -1f;

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

        // currently showering?
        if (ShoweringInProgress)
        {
            ShowerTimeRemaining -= Time.deltaTime;

            // shower time is up
            if (ShowerTimeRemaining <= 0)
            {
                ShoweringInProgress = false;
                ShowerParticles.Stop();
                AkSoundEngine.PostEvent("Stop_Shower", ShowerParticles.gameObject);

                RefreshUI();
            }
        }
    }

    void RefreshUI()
    {
        if (ShoweringInProgress)
        {
            ActivateShowerButton.interactable = false;
            ActivateShowerText.text = "Shower in use";
        }
        else if (ShowerUsedToday)
        {
            ActivateShowerButton.interactable = false;
            ActivateShowerText.text = "At water quota";
        }
        else if (!WaterSource.IsWaterAvailable)
        {
            ActivateShowerButton.interactable = false;
            ActivateShowerText.text = "No clean water";
        }
        else
        {
            ActivateShowerButton.interactable = true;
            ActivateShowerText.text = "Activate Shower";
        }
    }

    void OnDestroy()
    {
        if (ShoweringInProgress)
        {
            AkSoundEngine.PostEvent("Stop_Shower", ShowerParticles.gameObject);
        }
    }

    public void OnActivateShower()
    {
        if (ShoweringInProgress || ShowerUsedToday || !WaterSource.IsWaterAvailable)
            return;

        // consume the water
        float waterConsumed = WaterSource.ConsumeFilteredWater(WaterRequired);

        // update shower used flags
        ShowerUsedToday = true;
        ShoweringInProgress = true;

        // shower particle time is based on water available
        ShowerTimeRemaining = ShowerActiveDuration * (waterConsumed / WaterRequired);
        ShowerParticles.Play();

        AkSoundEngine.PostEvent("Play_Shower", ShowerParticles.gameObject);

        RefreshUI();
    }

    public void OnNextDay(UpdateStage stage)
    {
        if (stage == UpdateStage.Plants)
        {
            // determine and apply the forced consumption
            float forcedConsumption = (GameManager.Instance.NumDaysPassed - (ShowerUsedToday ? 1 : 0)) * WaterRequired;
            WaterSource.ConsumeFilteredWater(forcedConsumption);

            // reset the shower being used
            ShowerUsedToday = false;

            // if we fell asleep while showering then stop the shower
            if (ShoweringInProgress)
            {
                ShoweringInProgress = false;
                ShowerParticles.Stop();
                AkSoundEngine.PostEvent("Stop_Shower", ShowerParticles.gameObject);
            }
        }

        if (stage == UpdateStage.UI)
            RefreshUI();
    }
}
