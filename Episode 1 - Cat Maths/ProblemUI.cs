using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

[System.Serializable]
public class TimerStartedEvent : UnityEvent<bool, float> { }
[System.Serializable]
public class ProblemCompletedEvent : UnityEvent<bool, bool> { }

public class ProblemUI : MonoBehaviour
{
    public CanvasGroup ProblemGroup;

    public Image ProblemPanel;
    public Text Problem;
    public InputField Result;

    public Image CountdownPanel;
    public Image Countdown;

    public TimerStartedEvent OnProblemTimerStarted = new TimerStartedEvent();
    public ProblemCompletedEvent OnProblemCompleted = new ProblemCompletedEvent();

    public Color CountdownStartColour = Color.green;
    public Color CountdownEndColour = Color.red;

    protected string CorrectResult = "";

    protected bool IsStarted = false;
    protected bool IsLocked = false;
    protected float InitialCountdownTime;
    protected float CountdownTime = -1;

    protected bool IsTutorial = false;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void ForceCleanup(bool sendEvents)
    {
        // don't cleanup locked problems
        if (IsLocked)
            return;

        // stop the problem and lock it so no further changes happen
        IsStarted = false;
        IsLocked = true;

        // report the problem as failed
        if (sendEvents)
            OnProblemCompleted?.Invoke(false, IsTutorial);

        // kick off delayed destruction
        StartCoroutine(DestroyProblem(false));
    }

    // Update is called once per frame
    void Update()
    {
        // has the problem started? and not paused? and not locked?
        if (IsStarted && !GameManager.Instance.IsPaused && !IsLocked)
        {
            // update the countdown time
            CountdownTime -= Time.deltaTime;

            UpdateCountdown();

            // time up?
            if (CountdownTime <= 0)
            {
                ForceCleanup(true);
            }
        }
    }

    public void BindToProblem(ProblemDefinition problemDefinition, ProblemDefinition.Configuration config, float solveTime)
    {
        Problem.text         = config.LHSText;
        InitialCountdownTime = config.SolveTime;
        CountdownTime        = config.SolveTime;
        CorrectResult        = config.RHSValue.ToString();
        IsTutorial           = config.IsTutorial;
    }

    public void OnCollisionEnter2D(Collision2D other)
    {
        // ignore if any collisions were above us
        foreach(ContactPoint2D contactPoint in other.contacts)
        {
            if (contactPoint.point.y > transform.position.y)
                return;
        }

        // start the problem if necessary
        if (!IsStarted && !IsLocked)
            StartProblem();
    }

    protected void StartProblem()
    {
        // flag as started
        IsStarted = true;

        UpdateCountdown();

        // fire the events
        OnProblemTimerStarted?.Invoke(IsTutorial, InitialCountdownTime);
    }

    protected void UpdateCountdown()
    {
        // update the percentage complete
        Countdown.fillAmount = Mathf.Clamp01(CountdownTime / InitialCountdownTime);
        Countdown.color = Color.Lerp(CountdownStartColour, CountdownEndColour, 1f - Countdown.fillAmount);
    }

    protected IEnumerator DestroyProblem(bool wasSolved)
    {
        yield return new WaitForSeconds(1f);

        // clean up all listeners
        OnProblemCompleted.RemoveAllListeners();
        OnProblemTimerStarted.RemoveAllListeners();

        Destroy(gameObject);

        yield return null;
    }

    public void OnVerifyText(string newText)
    {
        // correct result entered?
        if (Result.text == CorrectResult && !IsLocked)
        {
            // stop the problem and lock it
            IsStarted = false;
            IsLocked = true;

            // lock the result
            Result.interactable = false;

            // let any listeners know we are done
            OnProblemCompleted?.Invoke(true, IsTutorial);

            // schedule the problem for destruction
            StartCoroutine(DestroyProblem(true));
        }
    }
}
