using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class GameManager : MonoBehaviour
{
    public enum GamePhases
    {
        Unknown,
        Intro_Cleaning,
        Cleaning,
        Intro_Manufacturing,
        Manufacturing,
        End
    }

    private static GameManager _Instance = null;

    [Header("Common Settings")]
    public bool IsPaused = false;
    public GamePhases Phase = GamePhases.Intro_Cleaning;

    [Header("Cleaning Phase")]
    public float Phase1TimeLimit = 60 * 5f;

    [Header("Assembly Phase")]
    public float Phase2TimeLimit = 60 * 5f;

    [Header("Events")]
    public UnityEvent OnCleaningPhaseFinished;
    public UnityEvent OnManufacturingPhaseFinished;
    public UnityEvent OnCough;

    [Header("Coughing")]
    public float MinDirtToCough = 0.15f;
    public float MinCoughInterval = 15f;
    public float MaxCoughInterval = 60f;
    public float CoughVariance = 5f;

    protected float NormalisedDirtLevel;
    protected float TimeUntilNextCough = -1f;
    protected float CoughInterval;

    public float TimeRemaining;

    public static GameManager Instance
    {
        get
        {
            return _Instance;
        }
    }
    
    void Awake()
    {
        if (_Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        _Instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (IsPaused)
            return;

        // in the cleaning phase?
        if (Phase == GamePhases.Cleaning && TimeRemaining > 0)
        {
            TimeRemaining -= Time.deltaTime;

            // time is up?
            if (TimeRemaining <= 0)
            {
                OnCleaningPhaseFinished?.Invoke();
                return;
            }
        }
        else if (Phase == GamePhases.Manufacturing && TimeRemaining > 0)
        {
            TimeRemaining -= Time.deltaTime;

            // time is up?
            if (TimeRemaining <= 0)
            {
                OnManufacturingPhaseFinished?.Invoke();
                return;
            }

            // can cough
            if (NormalisedDirtLevel >= MinDirtToCough)
            {
                if (TimeUntilNextCough > 0)
                    TimeUntilNextCough -= Time.deltaTime;

                if (TimeUntilNextCough <= 0)
                {
                    TimeUntilNextCough = Random.Range(CoughInterval - CoughVariance, CoughInterval + CoughVariance);
                    OnCough?.Invoke();
                }
            }
        }
    }

    public void SetIsPaused(bool newValue)
    {
        IsPaused = newValue;
    }

    public void StartCleaningPhase()
    {
        if (Phase == GamePhases.Cleaning)
            return;

        // initialise the phase
        Phase = GamePhases.Cleaning;
        TimeRemaining = Phase1TimeLimit;
    }

    public void StartAssemblyPhase()
    {
        if (Phase == GamePhases.Manufacturing)
            return;

        NormalisedDirtLevel = Mathf.Clamp01(DirtManager.Instance.CurrentDirtLevel / DirtManager.Instance.MaximumDirtLevel);

        if (NormalisedDirtLevel >= MinDirtToCough)
        {
            CoughInterval = Mathf.Lerp(MinCoughInterval, MaxCoughInterval, 1f - NormalisedDirtLevel);
            TimeUntilNextCough = Random.Range(CoughInterval - CoughVariance, CoughInterval + CoughVariance);
        }

        // initialise the phase
        Phase = GamePhases.Manufacturing;
        TimeRemaining = Phase2TimeLimit;
    }
}
