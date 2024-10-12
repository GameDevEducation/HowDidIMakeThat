using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System.Linq;

public class BuildingSegment : MonoBehaviour
{
    public BuildingPiece Foundation;
    public GameObject BlockerWhenDestroyed;

    protected List<BuildingPiece> AllSegmentPieces = new List<BuildingPiece>();

    [Header("Debug")]
    public bool Debug_DestroyFoundation;

    // Start is called before the first frame update
    void Start()
    {
        // get all of the building pieces
        GetComponentsInChildren<BuildingPiece>(false, AllSegmentPieces);

        // remove the foundation from the list
        AllSegmentPieces.Remove(Foundation);

        // listen for the foundation being destroyed
        Foundation.OnPieceDestroyed.AddListener(OnFoundationDestroyed);

        // sort the segments by height
        AllSegmentPieces = AllSegmentPieces.OrderBy(piece => piece.transform.position.y).ToList();
    }

    // Update is called once per frame
    void Update()
    {
        // foundation has been destroyed
        if (Debug_DestroyFoundation)
        {
            // apply full damage
            Foundation.OnTakeDamage(Foundation.CurrentHealth);

            // prevent re-triggering
            Debug_DestroyFoundation = false;
        }
    }

    protected void OnFoundationDestroyed(BuildingPiece foundation)
    {
        // get rid of the foundation
        Foundation.gameObject.SetActive(false);

        // switch off any lights
        Light[] allLights = GetComponentsInChildren<Light>();
        foreach(Light light in allLights)
        {
            light.gameObject.SetActive(false);
        }

        // give everything a rigid body
        foreach(BuildingPiece piece in AllSegmentPieces)
        {
            piece.CanEmitSound = true;
            
            Rigidbody rb = piece.gameObject.AddComponent<Rigidbody>();
            rb.mass = piece.Config.Mass;
        }

        // enable the safety blocker
        BlockerWhenDestroyed.SetActive(true);
    }
}
