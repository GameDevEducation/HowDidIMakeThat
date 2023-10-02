using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class JournalManager : MonoBehaviour
{
    public List<Journal> Journals;
    protected List<string> VisibleJournals = new List<string>();

    protected int LastDaySeen = -100;

    private static JournalManager _Instance = null;
    public static JournalManager Instance
    {
        get
        {
            return _Instance;
        }
    }

    void Awake()
    {
        if (_Instance)
        {
            Destroy(gameObject);
            return;
        }

        _Instance = this;
    }

    public int JournalsVisible
    {
        get
        {
            return VisibleJournals.Count;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        OnNextDay();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnNextDay()
    {
        // retrieve the potential journals
        List<Journal> newJournals = Journals.Where(journal => journal.Day >= LastDaySeen && journal.Day < GameManager.Instance.CurrentDay).ToList();

        // retrieve the list of events that occurred
        List<DailyEvent> dailyEvents = GameManager.Instance.DaysEvents;

        // process the journals
        foreach(Journal journal in newJournals)
        {
            List<DailyEvent> filteredEvents = dailyEvents.Where(dailyEvent => dailyEvent.Day == journal.Day).ToList();

            string journalEntry = journal.GetContent(filteredEvents);

            VisibleJournals.Add(journalEntry);
        }

        // update the last day seen
        LastDaySeen = GameManager.Instance.CurrentDay;
    }

    public string GetJournal(int number)
    {
        if (number < 1 || number > VisibleJournals.Count)
            return "<color=#ff0000ff>Journal not found</color>";

        return VisibleJournals[number-1];
    }
}
