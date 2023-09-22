using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ProblemDB", menuName = "Databases/Problem Database", order = 1)]
public class ProblemDatabase : ScriptableObject
{
    public List<ProblemDefinition> Problems;

    [Tooltip("This is the intial time (in seconds) to solve a problem")]
    public float BaseSolveTime = 10f;

    [Tooltip("This is the smallest solve time permitted")]
    public float MinimumSolveTime = 5f;

    [Tooltip("This is the percentage the current solve time is reduced by per loop. One loop = one full cycle of all defined problems.")]
    public float SolveTimeReductionPerLoop = 0.1f;

    [Tooltip("How many problems will be shown at each increment before advancing to the next")]
    public int ProblemRepeatsPerIncrement = 3;

    [System.NonSerialized]
    public float CurrentSolveTime = -1f;

    [System.NonSerialized]
    public int CurrentProblemIndex = 0;

    [System.NonSerialized]
    public int CurrentProblemRepeatCount = 0;

    [System.NonSerialized]
    public int LoopCount = 0;

    [System.NonSerialized]
    public int CurrentLevel = 0;

    public void Initialise()
    {
        CurrentSolveTime = BaseSolveTime;
        CurrentProblemIndex = 0;
        CurrentProblemRepeatCount = 0;
        LoopCount = 0;

        // seed the random number generate based on time
        Random.InitState(System.DateTime.Now.Millisecond);
    }

    protected ProblemDefinition NextProblem
    {
        get
        {
            ProblemDefinition problemInfo = Problems[CurrentProblemIndex];

            // advance the repeat count
            ++CurrentProblemRepeatCount;

            // at the repeat limit?
            if (CurrentProblemRepeatCount >= ProblemRepeatsPerIncrement)
            {
                // reset the counter
                CurrentProblemRepeatCount = 0;

                // advance the problem
                ++CurrentProblemIndex;

                // advance the level
                ++ CurrentLevel;
            }

            // covered all problems this loop?
            if (CurrentProblemIndex >= Problems.Count)
            {
                // reset the problem index
                CurrentProblemIndex = 0;

                // update the loop count
                ++LoopCount;

                // update the solve time
                CurrentSolveTime *= 1f -  SolveTimeReductionPerLoop;
                
                // constrain the solve time
                if (CurrentSolveTime < MinimumSolveTime)
                    CurrentSolveTime = MinimumSolveTime;
            }

            return problemInfo;
        }
    }

    protected ProblemDefinition.Configuration InstantiateProblem(ProblemDefinition definition)
    {
        List<int> problemValues = new List<int>();
        List<bool> problemPlusMinusOperators = new List<bool>();

        // random roll the plus minus operators
        while (problemPlusMinusOperators.Count < definition.NumPlusMinusOperators)
        {
            problemPlusMinusOperators.Add(Random.Range(0, 2) == 1);
        }

        // random roll all of the digits
        while (problemValues.Count < definition.NumValues)
        {
            // even numbered loop counts use a wider range for all values
            if ((LoopCount % 2) == 0)
                problemValues.Add(Random.Range(1, 10 + 10 * LoopCount));
            else // otherwise 50/50 chance of using larger range
            {
                if (Random.Range(0, 2) == 0)
                    problemValues.Add(Random.Range(1, 10 + 10 * LoopCount));
                else
                    problemValues.Add(Random.Range(1, 10));
            }
        }

        return new ProblemDefinition.Configuration(problemValues, problemPlusMinusOperators, CurrentSolveTime, definition);
    }

    public GameObject GenerateProblem(GameObject spawnPoint)
    {
        // Retrieve the next problem
        ProblemDefinition problemDefinition = NextProblem;

        // instantiate the problem prefab and retrieve the UI
        GameObject problemObject = GameObject.Instantiate(problemDefinition.ProblemUIPrefab, 
                                                          spawnPoint.transform.position, spawnPoint.transform.rotation, spawnPoint.transform);
        ProblemUI problemUI = problemObject.GetComponent<ProblemUI>();

        // bind the UI to the prefab
        problemUI.BindToProblem(problemDefinition, InstantiateProblem(problemDefinition), CurrentSolveTime);

        return problemObject;
    }
}
