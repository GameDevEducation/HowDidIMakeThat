using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class MaterialPropertyData
{
    public MeshRenderer Renderer;
    public MaterialPropertyBlock[] PropertyBlocks;
}

public class InteractableObject : MonoBehaviour
{
    public UnityEvent OnInteract;

    public bool AutoDeactivate = true;

    protected bool _IsInteractable = false;

    protected List<MaterialPropertyData> MaterialProperties;

    public Color EmissionColour = Color.white;
    public float EmissionPulseSpeed = 0.25f;
    public float MinEmission = 0.3f;
    public float MaxEmission = 0.8f;

    public bool IsInteractable
    {
        get
        {
            return _IsInteractable;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        // build up the material property data so we can update it
        MaterialProperties = new List<MaterialPropertyData>();
        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>();
        foreach(MeshRenderer renderer in renderers)
        {
            MaterialPropertyData propertyData = new MaterialPropertyData();
            propertyData.Renderer = renderer;

            // create the property blocks
            propertyData.PropertyBlocks = new MaterialPropertyBlock[renderer.sharedMaterials.Length];
            for (int materialIndex = 0; materialIndex < renderer.sharedMaterials.Length; ++materialIndex)
            {
                propertyData.PropertyBlocks[materialIndex] = new MaterialPropertyBlock();
                renderer.sharedMaterials[materialIndex].EnableKeyword("_EMISSION");
            }

            MaterialProperties.Add(propertyData);
        }

        // // make sure we include this object
        // MeshRenderer rendererSelf = GetComponent<MeshRenderer>();
        // if (rendererSelf)
        // {
        //     MaterialPropertyData propertyData = new MaterialPropertyData();
        //     propertyData.Renderer = rendererSelf;

        //     // create the property blocks
        //     propertyData.PropertyBlocks = new MaterialPropertyBlock[rendererSelf.sharedMaterials.Length];
        //     for (int materialIndex = 0; materialIndex < rendererSelf.sharedMaterials.Length; ++materialIndex)
        //     {
        //         propertyData.PropertyBlocks[materialIndex] = new MaterialPropertyBlock();
        //     }

        //     MaterialProperties.Add(propertyData);
        // }
    }

    // Update is called once per frame
    void Update()
    {
        // is it interactable?
        if (IsInteractable)
            UpdateHighlight();
    }

    protected void UpdateHighlight()
    {
        float emissionAmount = MinEmission + Mathf.PingPong(Time.time * EmissionPulseSpeed, MaxEmission - MinEmission);
        Color emissionColour = EmissionColour * Mathf.LinearToGammaSpace(emissionAmount);

        // synchronise the property blocks
        foreach (MaterialPropertyData propertyData in MaterialProperties)
        {
            for(int materialIndex = 0; materialIndex < propertyData.PropertyBlocks.Length; ++materialIndex)
            {
                // read the property block
                propertyData.Renderer.GetPropertyBlock(propertyData.PropertyBlocks[materialIndex], materialIndex);

                // update the highlight
                if (IsInteractable)
                {
                    propertyData.PropertyBlocks[materialIndex].SetColor("_EmissionColor", emissionColour);
                }
                else
                    propertyData.PropertyBlocks[materialIndex].SetColor("_EmissionColor", Color.black);

                // apply the property block
                propertyData.Renderer.SetPropertyBlock(propertyData.PropertyBlocks[materialIndex], materialIndex);
            }
        }
    }

    public void InteractionHappened()
    {
        OnInteract?.Invoke();

        if (AutoDeactivate)
            DisableInteractions();
    }

    public void EnableInteractions()
    {
        _IsInteractable = true;
    }

    public void DisableInteractions()
    {
        _IsInteractable = false;
        UpdateHighlight();
    }
}
