using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class VehicleEvent : MonoBehaviour, IGameplayEvent
{
    public List<GameObject> Vehicles;
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
        // pick which of the vehicles will be active
        int activeVehicle = Random.Range(0, Vehicles.Count);
        for (int index = 0; index < Vehicles.Count; ++index)
        {
            Vehicles[index].SetActive(index == activeVehicle);
        }

        // run the activation event
        OnActivate?.Invoke();
    }

    public void PerformCleanup()
    {
        foreach(var vehicle in Vehicles)
        {
            vehicle.SetActive(false);
        }
    }
}
