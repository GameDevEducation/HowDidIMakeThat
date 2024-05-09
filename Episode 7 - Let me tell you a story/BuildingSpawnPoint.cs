using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EBuildingType
{
    Unknown,

    Small   = 10,
    Medium  = 20,
    Large   = 30,

    LampPost    = 40,
    PhoneBooth  = 50,
    Bench       = 60,
    Bins        = 70,
}

public class BuildingSpawnPoint : MonoBehaviour
{
    [SerializeField] EBuildingType _BuildingType;

    public EBuildingType BuildingType => _BuildingType;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
