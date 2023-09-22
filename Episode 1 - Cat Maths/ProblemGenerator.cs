using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class UnityEvent_Bool : UnityEvent<bool> { }

public class ProblemGenerator : MonoBehaviour
{
    public ProblemDatabase ProblemDB;
    public GameObject SpawnPoint;

    public UnityEvent OnProblemSpawned;
    public UnityEvent_Bool OnProblemSolved;
    public UnityEvent_Bool OnProblemFailed; 
    public TimerStartedEvent OnProblemTimerStarted;

    // Start is called before the first frame update
    void Start()
    {
        OnReset();
    }

    // Update is called once per frame
    void Update()
    {
    }

    public void OnReset()
    {
        ProblemDB.Initialise();

        // find all active problems
        ProblemUI[] activeProblems = FindObjectsOfType<ProblemUI>();
        foreach(ProblemUI problem in activeProblems)
        {
            problem?.ForceCleanup(false);
        }
    }

    public void SpawnProblem()
    {
        // instantiate the problem
        GameObject newProblem = ProblemDB.GenerateProblem(SpawnPoint);

        // link up the completion events
        ProblemUI problemUI = newProblem.GetComponent<ProblemUI>();
        problemUI.OnProblemTimerStarted.AddListener(OnProblemTimerStarted_Internal);
        problemUI.OnProblemCompleted.AddListener(OnProblemCompleted);

        // on spawned event
        OnProblemSpawned?.Invoke();
    }

    void OnProblemTimerStarted_Internal(bool isTutorial, float amount)
    {
        OnProblemTimerStarted?.Invoke(isTutorial, amount);
    }

    public void OnProblemCompleted(bool successful, bool isTutorial)
    {
        if (successful)
            OnProblemSolved?.Invoke(isTutorial);
        else
            OnProblemFailed?.Invoke(isTutorial);
    }
}
