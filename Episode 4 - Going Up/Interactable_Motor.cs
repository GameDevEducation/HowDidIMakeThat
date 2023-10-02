using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Interactable_Motor : MonoBehaviour, IInteractable
{
    public Motor LinkedMotor;
    private bool WasRepairing = false;
    private CharacterMotor Player;
    
    // Start is called before the first frame update
    void Start()
    {
        Player = FindObjectOfType<CharacterMotor>();
    }


    // Update is called once per frame
    void Update()
    {
        
    }

    // can we interact with the object?
    public bool CanInteract()
    {
        return !LinkedMotor.MotorActive && LinkedMotor.LimiterActive;
    }

    // is the interaction instant?
    public bool IsInstant()
    {
        return false;
    }

    // does the interaction need continuous input
    public bool RequiresHolding()
    {
        return true;
    }
    
    // show the highlight (ie. flag that can interact)
    public void BeginHighlight()
    {
        LinkedMotor.HighlightMotorUI();
    }

    // stop the highlight (ie. no longer interactable)
    public void StopHighlight()
    {
        LinkedMotor.UnhighlightMotorUI();
    }

    // start/update the interaction
    public void PerformInteract()
    {
        if (!WasRepairing)
        {
            WasRepairing = true;
            Player.BeginRepairing();
        }
        LinkedMotor.OnTickRepairs();

        if (!CanInteract())
            StopInteract();
    }

    // interaction has ceased
    public void StopInteract()
    {
        if (WasRepairing)
        {
            WasRepairing = false;
            Player.StopRepairing();
        }
    }
}
