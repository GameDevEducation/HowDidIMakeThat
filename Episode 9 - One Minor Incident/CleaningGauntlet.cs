using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CleaningGauntlet : MonoBehaviour
{
    [Header("Activation")]
    public KeyCode ActivationKey;

    [Header("Emission")]
    public Transform Emitter;
    public GameObject ProjectilePrefab;
    public GameObject ProjectilePrefab_Inferno;
    public float MinLaunchForce = 50f;
    public float MaxLaunchForce = 100f;
    public float MaxLaunchSpread = 5f;
    public float OverheatedLaunchSpread = 15f;

    [Header("Normal")]
    public float MinEmissionInterval = 0.2f;
    public float MaxEmissionInterval = 0.5f;
    protected float TimeUntilNextEmission = -1f;

    [Header("Overheated")]
    public float OverheatedMinEmissionInterval = 0.01f;
    public float OverheatedMaxEmissionInterval = 0.2f;
    protected float OverheatedTimeUntilNextEmission = -1f;
    
    [Header("Heat Buildup")]
    public float HeatGainPerProjectile = 0.05f;
    public float HeatLostPerSecond = 0.2f;
    protected float CurrentHeat = 0f;

    [Header("Cleaning")]
    public float NormalImpactScale = 0.2f;
    public float OverheatedImpactScale = 2f;
    public float OverheatedCleaningStrength = 0.1f;
    public float CleaningStrength = 0.1f;

    [Header("UI")]
    public RectTransform OverheatIndicatorPanel;
    public UnityEngine.UI.Image OverheatIndicatorImage;
    public Color NormalTemperatureColour;
    public Color OverheatTemperatureColour;

    [Range(1, 10)]
    public int OverheatedPenetration = 2;
    [Range(1, 10)]
    public int NormalPenetration = 1;

    protected bool IsFiring = false;
    protected bool IsOverheated = false;
    protected bool IsPaused = false;

    // Start is called before the first frame update
    void Start()
    {
    }

    public void SetIsPaused(bool newIsPaused)
    {
        IsPaused = newIsPaused;
    }

    public void Deactivate()
    {
        if (IsFiring)
            AkSoundEngine.PostEvent("Stop_Gauntlet_Loop", gameObject);

        IsFiring = false;
    }

    // Update is called once per frame
    void Update()
    {
        // paused?
        if (IsPaused)
            return;

        // update the gauntlet heat (unless overheated)
        if (!IsOverheated)
            AkSoundEngine.SetRTPCValue("Gauntlet_Heat", 100f * CurrentHeat, gameObject);
            
        // detect if we are firing or not
        if (Input.GetKeyDown(ActivationKey))
        {
            if (!IsFiring)
                AkSoundEngine.PostEvent("Play_Gauntlet_Loop", gameObject);

            IsFiring = true;
            TimeUntilNextEmission = 0f;
        }
        else if (Input.GetKeyUp(ActivationKey))
        {
            if (IsFiring)
                AkSoundEngine.PostEvent("Stop_Gauntlet_Loop", gameObject);

            IsFiring = false;
            return;
        }

        // are we firing? and not overheated
        if (IsFiring && !IsOverheated)
        {
            // update the emission time
            TimeUntilNextEmission -= Time.deltaTime;

            // time to fire?
            if (TimeUntilNextEmission <= 0)
            {
                // launch the projectile
                LaunchProjectile();

                // update the heat
                CurrentHeat = Mathf.Clamp01(CurrentHeat + HeatGainPerProjectile);

                // roll a new emission time
                TimeUntilNextEmission = Random.Range(MinEmissionInterval, MaxEmissionInterval);

                // overheated?
                if (CurrentHeat >= 1f)
                {
                    OverheatedTimeUntilNextEmission = 0f;
                    IsOverheated = true;
                }
            }
        }
        else
        {
            // apply cooling
            if (CurrentHeat > 0)
            {
                // apply cooling
                CurrentHeat = Mathf.Clamp01(CurrentHeat - HeatLostPerSecond * Time.deltaTime);

                // cooled down?
                if (CurrentHeat <= 0f)
                    IsOverheated = false;
            }

            // currently overheated?
            if (IsOverheated)
            {
                // update the emission time
                OverheatedTimeUntilNextEmission -= Time.deltaTime;

                // time to fire?
                if (OverheatedTimeUntilNextEmission <= 0)
                {
                    // launch the projectile
                    LaunchProjectile();

                    // roll a new emission time
                    TimeUntilNextEmission = Random.Range(OverheatedMinEmissionInterval, OverheatedMaxEmissionInterval);
                }
            }
        }

        // update the UI
        float temperatureIndicatorScale = Mathf.Clamp01(IsOverheated ? 1f : CurrentHeat);
        OverheatIndicatorPanel.localScale = new Vector3(1f, temperatureIndicatorScale, 1f);
        OverheatIndicatorImage.color = Color.Lerp(NormalTemperatureColour, OverheatTemperatureColour, temperatureIndicatorScale);
    }

    protected void LaunchProjectile()
    {
        // spawn the new projectile
        GameObject newProjectile = IsOverheated ? GameObject.Instantiate(ProjectilePrefab_Inferno) : GameObject.Instantiate(ProjectilePrefab);

        // position the projectile
        newProjectile.transform.position = Emitter.transform.position;
        newProjectile.transform.rotation = Emitter.transform.rotation;

        // get the projectile's rigid body and cleaner logic
        Rigidbody projectileRB = newProjectile.GetComponent<Rigidbody>();
        Cleaner cleanerScript = newProjectile.GetComponent<Cleaner>();

        // randomise the amount of force
        float launchForce = Random.Range(MinLaunchForce, MaxLaunchForce);

        float launchSpread = IsOverheated ? OverheatedLaunchSpread : MaxLaunchSpread;

        // determine the launch direction
        Vector3 launchDirection = Emitter.up;
        launchDirection += Emitter.forward * Mathf.Sin(Random.Range(-launchSpread, launchSpread) * Mathf.Deg2Rad);
        launchDirection += Emitter.right * Mathf.Sin(Random.Range(-launchSpread, launchSpread) * Mathf.Deg2Rad);

        cleanerScript.InitCleaner(launchDirection * launchForce * (IsOverheated ? OverheatedImpactScale : NormalImpactScale), 
                                  IsOverheated ? OverheatedCleaningStrength : CleaningStrength,
                                  IsOverheated ? OverheatedPenetration : NormalPenetration);

        // launch the projectile
        projectileRB.AddForce(launchForce * launchDirection.normalized, ForceMode.Impulse);

        AkSoundEngine.PostEvent("Play_Gauntlet_Fire", Emitter.gameObject);
    }
}
