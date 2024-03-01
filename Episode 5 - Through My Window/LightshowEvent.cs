using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightshowEvent : MonoBehaviour, IGameplayEvent
{
    const int ApartmentsPerLevel = 10;
    const int NumLevels = 10;

    [Multiline(10)]
    [TextArea(10, 11)]
    public string LightPattern;
    public Building LinkedBuilding;
    public float MinTimeToApply = 5f;
    public float MaxTimeToApply = 10f;

    public float MinTimeToHold = 30f;
    public float MaxTimeToHold = 90f;

    public float MinTimeToReturn = 5f;
    public float MaxTimeToReturn = 5f;

    protected bool IsActive = false;

    protected List<bool> TargetConfiguration;
    protected List<int> ApplicationOrder;
    protected List<int> ReturnOrder;

    protected float TimeToApply;
    protected float TimeToHold;
    protected float TimeToReturn;
    protected float Progress;

    // Start is called before the first frame update
    void Start()
    {
        // turn the pattern into the boolean array
        string[] configRows = LightPattern.Split(new [] {System.Environment.NewLine, "\n", "\r"}, System.StringSplitOptions.RemoveEmptyEntries);
        TargetConfiguration = new List<bool>(NumLevels * ApartmentsPerLevel);
        for (int row = NumLevels - 1; row >= 0; --row)
        {
            for (int column = 0; column < ApartmentsPerLevel; ++column)
            {
                TargetConfiguration.Add(configRows[row][column] == '1');
            }
        }
    }

    // Update is called once per frame
    void Update()
    {        
        if (!IsActive)
            return;

        // advance the time
        Progress += Time.deltaTime;

        // still applying?
        if (Progress <= TimeToApply)
        {
            int applyUpTo = Mathf.RoundToInt(ApplicationOrder.Count * (Progress / TimeToApply));

            // override the lights
            for (int index = 0; index < applyUpTo; ++index)
            {
                int apartment = ApplicationOrder[index];

                LinkedBuilding.Apartments[apartment].OverrideLights(TargetConfiguration[apartment]);
            }
        }
        else if (Progress < TimeToHold)
        {
            // make sure all of the values are applied
            foreach(int apartment in ApplicationOrder)
            {
                LinkedBuilding.Apartments[apartment].OverrideLights(TargetConfiguration[apartment]);
            }            
        }
        else if (Progress < TimeToReturn)
        {
            int returnUpTo = Mathf.RoundToInt(ReturnOrder.Count * ((Progress - TimeToHold) / (TimeToReturn - TimeToHold)));

            // override the lights
            for (int index = 0; index < returnUpTo; ++index)
            {
                int apartment = ReturnOrder[index];

                LinkedBuilding.Apartments[apartment].ClearOverride();
            }
        }
        else if (Progress > TimeToReturn)
        {
            IsActive = false;

            // reset the lights
            foreach(var apartment in LinkedBuilding.Apartments)
            {
                apartment.ClearOverride();
            }
        }
    }

    public void ActivateEvent()
    {
        if (IsActive)
            return;

        // Enable the event
        IsActive = true;

        // roll the random values
        TimeToApply = Random.Range(MinTimeToApply, MaxTimeToApply);
        TimeToHold = TimeToApply + Random.Range(MinTimeToHold, MaxTimeToHold);
        TimeToReturn = TimeToHold + Random.Range(MinTimeToReturn, MaxTimeToReturn);
        Progress = 0f;

        // determine the application order
        List<int> workingList = new List<int>(NumLevels * ApartmentsPerLevel);
        for (int index = 0; index < (NumLevels * ApartmentsPerLevel); ++index)
        {
            workingList.Add(index);
        }

        // populate the application and return lists
        ApplicationOrder = new List<int>(NumLevels * ApartmentsPerLevel);
        ReturnOrder = new List<int>(NumLevels * ApartmentsPerLevel);
        while (workingList.Count > 0)
        {
            int index = Random.Range(0, workingList.Count);
            ApplicationOrder.Add(workingList[index]);
            ReturnOrder.Insert(0, workingList[index]);
            workingList.RemoveAt(index);
        }
    }
}
