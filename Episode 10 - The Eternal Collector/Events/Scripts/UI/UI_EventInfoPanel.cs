using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class UI_EventInfoPanel : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI EventName;
    [SerializeField] TextMeshProUGUI EventDescription;

    public void Bind(Event_Base newEvent)
    {
        EventName.text = newEvent.Name;
        EventDescription.text = newEvent.Description;

        FontManager.Bind(EventName);
        FontManager.Bind(EventDescription);
    }

    private void OnDestroy()
    {
        FontManager.Unbind(EventName);
        FontManager.Unbind(EventDescription);
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
