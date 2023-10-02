using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

public class MotorFailedEvent : UnityEvent<Motor> {}
public class LimiterFailedEvent : UnityEvent<Motor> {}
public class MotorRestoredEvent : UnityEvent<Motor> {}
public class LimiterRestoredEvent : UnityEvent<Motor> {}

public class Motor : MonoBehaviour, IPausable
{
    [Header("Motor")]
    public bool MotorActive;
    [Range(0f, 100f)]
    public float MotorStress;

    [Header("Limiter")]
    public bool LimiterActive;
    [Range(0f, 100f)]
    public float LimiterStress;

    [Header("Repairs")]
    public float LimiterRepairRate = 20;
    public float MotorRepairRate = 10f;

    [Header("Haptics")]
    public HapticEffect MotorFailedEffect;
    public HapticEffect LimiterFailedEffect;

    [Header("Stress")]
    public float MinStartingStress = 5f;
    public float MaxStartingStress = 15f;

    [Header("UI")]
    public Animator MotorUI;
    public TextMeshProUGUI MotorStatusText;
    public Animator LimiterUI;
    public TextMeshProUGUI LimiterStatusText;
    public float StressWarningThreshold = 0.75f;
    public float StressAlertThreshold = 0.9f;
    public Color Text_Failed;
    public Color Text_Repairing;
    public Color Text_FailureImminent;
    public Color Text_FailureWarning;
    public Color Text_Active;

    public MotorFailedEvent OnMotorFailed;
    public LimiterFailedEvent OnLimiterFailed;

    public MotorRestoredEvent OnMotorRestored;
    public LimiterRestoredEvent OnLimiterRestored;

    public const float MaximumMotorStress = 100f;
    public const float MaximumLimiterStress = 100f;
    private float RepairTextCooldown = -1f;

    // Start is called before the first frame update
    void Start()
    {
        PauseManager.Instance.RegisterPausable(this);
        
        if (!MotorActive)
            MotorStress = MaximumMotorStress;
        else
            MotorStress = Random.Range(MinStartingStress, MaxStartingStress);
        if (!LimiterActive)
            LimiterStress = MaximumLimiterStress;
        else
            LimiterStress = Random.Range(MinStartingStress, MaxStartingStress);

        MotorUI.SetBool("Usable", !MotorActive);
        LimiterUI.SetBool("Usable", !LimiterActive);
        
        AudioShim.PostEvent("Play_Motor", gameObject);
        UpdateMotorAudio();
    }

    void OnDestroy()
    {
        AudioShim.PostEvent("Stop_Motor", gameObject);
    }

    void UpdateMotorAudio()
    {
        if (MotorActive)
        {
            AudioShim.SetSwitchValue("MotorState", "Motor_Running", gameObject);
            AudioShim.SetRTPCValue("Stress", MotorStress, gameObject);
        }
        else if (LimiterActive)
        {
            AudioShim.SetSwitchValue("MotorState", "Limiter_Active", gameObject);
            AudioShim.SetRTPCValue("Stress", LimiterStress, gameObject);
        }
        else
            AudioShim.SetSwitchValue("MotorState", "Cable_Only", gameObject);        
    }

    // Update is called once per frame
    void Update()
    {
        UpdateMotorAudio();

        if (PauseManager.IsPaused)
            return;

        MotorUI.SetBool("Usable", !MotorActive && LimiterActive);
        LimiterUI.SetBool("Usable", !LimiterActive);

        if (RepairTextCooldown > 0)
            RepairTextCooldown -= Time.deltaTime;

        UpdateUI();
    }

    void UpdateUI()
    {
        // update the motor text
        if (MotorActive)
        {
            if (MotorStress >= (StressAlertThreshold * MaximumMotorStress))
            {
                MotorStatusText.text = "Failure Imminent";
                MotorStatusText.color = Text_FailureImminent;
            }
            else if (MotorStress >= (StressWarningThreshold * MaximumMotorStress))
            {
                MotorStatusText.text = "Failure Approaching";
                MotorStatusText.color = Text_FailureWarning;
            }
            else
            {
                MotorStatusText.text = "Active";
                MotorStatusText.color = Text_Active;
            }
        }
        else
        {
            if (RepairTextCooldown > 0 && LimiterActive)
            {
                MotorStatusText.text = "Repairing";
                MotorStatusText.color = Text_Repairing;
            }
            else
            {
                MotorStatusText.text = "Failed";
                MotorStatusText.color = Text_Failed;
            }
        }

        // Update the limiter text
        if (LimiterActive)
        {
            if (LimiterStress >= (StressAlertThreshold * MaximumLimiterStress))
            {
                LimiterStatusText.text = "Failure Imminent";
                LimiterStatusText.color = Text_FailureImminent;
            }
            else if (LimiterStress >= (StressWarningThreshold * MaximumLimiterStress))
            {
                LimiterStatusText.text = "Failure Approaching";
                LimiterStatusText.color = Text_FailureWarning;
            }
            else
            {
                LimiterStatusText.text = "Active";
                LimiterStatusText.color = Text_Active;
            }
        }
        else
        {
            if (RepairTextCooldown <= 0)
            {
                LimiterStatusText.text = "Failed";
                LimiterStatusText.color = Text_Failed;
            }
            else
            {
                LimiterStatusText.text = "Repairing";
                LimiterStatusText.color = Text_Repairing;
            }
        }
    }

    public void AddStress(float amount)
    {
        // is the motor active
        if (MotorActive)
        {
            MotorStress += amount;

            // reached the maximum stress
            if (MotorStress >= MaximumMotorStress)
            {
                AudioShim.PostEvent("Play_MotorFailed", gameObject);

                MotorActive = false;
                MotorStress = MaximumMotorStress;
                OnMotorFailed?.Invoke(this);
                HapticsManager.Instance.PlayEffect(MotorFailedEffect);
            }

            return;
        }

        // is the limiter active
        if (LimiterActive)
        {
            LimiterStress += amount;

            // reached the maximum stress
            if (LimiterStress >= MaximumLimiterStress)
            {
                AudioShim.PostEvent("Play_LimiterFailed", gameObject);

                LimiterActive = false;
                LimiterStress = MaximumLimiterStress;
                OnLimiterFailed?.Invoke(this);
                HapticsManager.Instance.PlayEffect(LimiterFailedEffect);
            }

            return;
        }
    }

    public void OnTickRepairs()
    {
        // does the limiter need repairing?
        if (!LimiterActive)
        {
            LimiterStress -= LimiterRepairRate * Time.deltaTime;
            RepairTextCooldown = 0.2f;

            // reached full repairs
            if (LimiterStress <= 0)
            {
                LimiterStress = 0f;
                LimiterActive = true;
                OnLimiterRestored?.Invoke(this);
            }

            return;
        }

        // does the motor need repairing?
        if (!MotorActive)
        {
            MotorStress -= MotorRepairRate * Time.deltaTime;
            RepairTextCooldown = 0.2f;

            // reached full repairs
            if (MotorStress <= 0)
            {
                MotorStress = 0f;
                LimiterStress = 0f;
                MotorActive = true;
                OnMotorRestored?.Invoke(this);
            }

            return;
        }
    }

    public void HighlightMotorUI()
    {
        MotorUI.SetBool("Highlighted", true);
    }

    public void UnhighlightMotorUI()
    {
        MotorUI.SetBool("Highlighted", false);
    }

    public void HighlightLimiterUI()
    {
        LimiterUI.SetBool("Highlighted", true);
    }
    
    public void UnhighlightLimiterUI()
    {
        LimiterUI.SetBool("Highlighted", false);
    }

    #region IPausable
    public bool OnPauseRequested()  { return true; }
    public bool OnResumeRequested() { return true; }

    public void OnPause()  
    { 
        AudioShim.PostEvent("Pause_Motor", gameObject);
    }

    public void OnResume() 
    { 
        AudioShim.PostEvent("Resume_Motor", gameObject);
    }
    #endregion 
}
