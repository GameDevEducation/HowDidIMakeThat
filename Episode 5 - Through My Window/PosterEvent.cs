using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PosterEvent : MonoBehaviour, IGameplayEvent
{
    public UnityEvent OnActivate;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
    }

    public void ActivateEvent()
    {
        // run the activation event
        OnActivate?.Invoke();
    }

    public void PerformCleanup()
    {
    }
}
