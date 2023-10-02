using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[CreateAssetMenu(fileName = "Journal", menuName = "Injaia/Journal", order = 1)]
public class Journal : ScriptableObject
{
    [Header("Common Options")]
    public int Day;

    [Header("Content")]
    [TextArea(5, 30)]
    public string Entry;

    [Header("Plants")]
    [TextArea(2, 5)]
    public string CouldNotWaterPlants;
    [TextArea(2, 5)]
    public string DidNotWaterPlants;

    [Header("Water")]
    [TextArea(2, 5)]
    public string CouldNotFilterWater;
    [TextArea(2, 5)]
    public string DidNotFilterWater;

    [Header("Drinking")]
    [TextArea(2, 5)]
    public string CouldNotDrink;
    [TextArea(2, 5)]
    public string DidNotDrink;
    
    [Header("Eating")]
    [TextArea(2, 5)]
    public string CouldNotEat;
    [TextArea(2, 5)]
    public string DidNotEat;

    [Header("Sleeping")]
    [TextArea(2, 5)]
    public string Exhaustion;
    
    protected Dictionary<string, string> TranslateEventsIntoKeywords(List<DailyEvent> dailyEvents)
    {
        // nothing to do if there were no entry
        if (dailyEvents == null || dailyEvents.Count == 0)
            return null;

        Dictionary<string, string> keywords = new Dictionary<string, string>();

        // check for could not water plants
        if (dailyEvents.Where(dailyEvent => dailyEvent.GetType() == typeof(DailyEvent_CouldNotWaterPlants)).Count() > 0)
        {
            keywords["CouldNotWaterPlants"] = CouldNotWaterPlants;
            keywords["DidNotWaterPlants"] = null;
        }
        else if (dailyEvents.Where(dailyEvent => dailyEvent.GetType() == typeof(DailyEvent_DidNotWaterPlants)).Count() > 0)
        {
            keywords["CouldNotWaterPlants"] = null;
            keywords["DidNotWaterPlants"] = DidNotWaterPlants;
        }

        // check for could not water filter water
        if (dailyEvents.Where(dailyEvent => dailyEvent.GetType() == typeof(DailyEvent_CouldNotFilterWater)).Count() > 0)
        {
            keywords["CouldNotFilterWater"] = CouldNotFilterWater;
            keywords["DidNotFilterWater"] = null;
        }
        else if (dailyEvents.Where(dailyEvent => dailyEvent.GetType() == typeof(DailyEvent_DidNotFilterWater)).Count() > 0)
        {
            keywords["CouldNotFilterWater"] = null;
            keywords["DidNotFilterWater"] = DidNotFilterWater;
        }

        // check for could not drink water
        if (dailyEvents.Where(dailyEvent => dailyEvent.GetType() == typeof(DailyEvent_CouldNotDrinkWater)).Count() > 0)
        {
            keywords["CouldNotDrink"] = CouldNotDrink;
            keywords["DidNotDrink"] = null;
        }
        else if (dailyEvents.Where(dailyEvent => dailyEvent.GetType() == typeof(DailyEvent_DidNotDrinkWater)).Count() > 0)
        {
            keywords["CouldNotDrink"] = null;
            keywords["DidNotDrink"] = DidNotDrink;
        }

        // check for could not eat
        if (dailyEvents.Where(dailyEvent => dailyEvent.GetType() == typeof(DailyEvent_CouldNotEat)).Count() > 0)
        {
            keywords["CouldNotEat"] = CouldNotEat;
            keywords["DidNotEat"] = null;
        }
        else if (dailyEvents.Where(dailyEvent => dailyEvent.GetType() == typeof(DailyEvent_DidNotEat)).Count() > 0)
        {
            keywords["CouldNotEat"] = null;
            keywords["DidNotEat"] = DidNotEat;
        }

        // check for exhaustion
        if (dailyEvents.Where(dailyEvent => dailyEvent.GetType() == typeof(DailyEvent_Exhaustion)).Count() > 0)
        {
            keywords["Exhaustion"] = Exhaustion;
        }
        else
        {
            keywords["Exhaustion"] = null;
        }
       
        return keywords;
    }

    public string GetContent(List<DailyEvent> dailyEvents)
    {
        Dictionary<string, string> keywords = TranslateEventsIntoKeywords(dailyEvents);
        string journalContent = Entry;

        // evaluate any of the keywords
        if (keywords != null)
        {
            foreach(string keyword in keywords.Keys)
            {
                string newText = keywords[keyword];

                // skip if null or empty
                if (string.IsNullOrEmpty(newText))
                {
                    continue;
                }

                // otherwise replace in journal content
                journalContent = journalContent.Replace(keyword, newText);
            }
        }

        // strip any remaining keywords
        string[] allKeywords = {"CouldNotWaterPlants", "DidNotWaterPlants", "CouldNotFilterWater", "DidNotFilterWater", "CouldNotDrink", "DidNotDrink", "CouldNotEat", "DidNotEat", "Exhaustion"};
        foreach(string keyword in allKeywords)
        {
            journalContent = journalContent.Replace(keyword, "");
        }

        // now reformat based on redundant newlines
        List<string> contentFragments = journalContent.Split(new[] { System.Environment.NewLine }, System.StringSplitOptions.RemoveEmptyEntries).ToList();

        // trim all fragments and remove redundant ones
        for (int fragmentIndex = 0; fragmentIndex < contentFragments.Count; ++fragmentIndex)
        {
            // trim the fragment
            contentFragments[fragmentIndex] = contentFragments[fragmentIndex].Trim();

            // if now empty then remove
            if (contentFragments[fragmentIndex].Length == 0)
            {
                contentFragments.RemoveAt(fragmentIndex);
                --fragmentIndex;
            }
        }

        // reconstruct the entry
        journalContent = "";
        foreach(string fragment in contentFragments)
        {
            if (journalContent.Length > 0)
                journalContent += System.Environment.NewLine + System.Environment.NewLine;

            journalContent += fragment;
        }

        return journalContent;
    }
}
