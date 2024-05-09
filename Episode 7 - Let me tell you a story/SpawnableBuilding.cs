using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnableBuilding : MonoBehaviour
{
    [SerializeField] EBuildingType _BuildingType;
    [SerializeField] bool _IsEasterEgg;

    public EBuildingType BuildingType => _BuildingType;
    public bool IsEasterEgg => _IsEasterEgg;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
