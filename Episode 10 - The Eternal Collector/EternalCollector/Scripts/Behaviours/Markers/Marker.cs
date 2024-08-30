using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Marker : MonoBehaviour
{
    public enum EMarkerType
    {
        Unknown,
        Siphon_Small,
        Siphon_Medium,
        Siphon_Large,

        EmergencyRetrieval_Siphon,
        EmergencyRetrieval_RespawnPoint,

        Lightning_Rod,
        Lightning_Target,

        Retirement_Chair,
        Retirement_Camera
    }

    [SerializeField] EMarkerType _Type;

    public EMarkerType Type => _Type;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
