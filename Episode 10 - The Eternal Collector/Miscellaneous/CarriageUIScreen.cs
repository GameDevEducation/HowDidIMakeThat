using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarriageUIScreen : MonoBehaviour
{
    TimeBridge_Light LinkedLightBridge;
    MeshRenderer LinkedMR;
    MaterialPropertyBlock PropBlock;
    float PreviousIntensity = -1f;

    private void Awake()
    {
        PropBlock = new MaterialPropertyBlock();
        LinkedMR = GetComponent<MeshRenderer>();
        LinkedMR.GetPropertyBlock(PropBlock);
    }

    // Start is called before the first frame update
    void Start()
    {
        LinkedLightBridge = FindObjectOfType<TimeBridge_Light>();
    }

    // Update is called once per frame
    void Update()
    {
        float NewIntensity = LinkedLightBridge.IsDayTime ? 50f : 1f;
        if (PreviousIntensity != NewIntensity)
        {
            PreviousIntensity = NewIntensity;
            PropBlock.SetColor("_EmissiveColor", Color.white * NewIntensity);
            LinkedMR.SetPropertyBlock(PropBlock);
        }
    }
}
