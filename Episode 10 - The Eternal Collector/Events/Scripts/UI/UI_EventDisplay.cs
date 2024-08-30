using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_EventDisplay : MonoBehaviour
{
    [SerializeField] CanvasGroup EventCanvasGroup;
    [SerializeField] Transform EventRoot;
    [SerializeField] GameObject EventPrefab;

    Dictionary<Event_Base, UI_EventInfoPanel> ActiveEventUI = new Dictionary<Event_Base, UI_EventInfoPanel>();

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void StartEvent(Event_Base newEvent)
    {
        var newEventUIGO = Instantiate(EventPrefab, EventRoot);
        var eventUIScript = newEventUIGO.GetComponent<UI_EventInfoPanel>();

        eventUIScript.Bind(newEvent);

        ActiveEventUI[newEvent] = eventUIScript;
    }

    public void StopEvent(Event_Base eventToDestroy)
    {
        Destroy(ActiveEventUI[eventToDestroy].gameObject);
        ActiveEventUI.Remove(eventToDestroy);
    }
}
