using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rock : MonoBehaviour
{
    private bool HitPlatform = false;
    private Platform ActivePlatform;

    #pragma warning disable 0649
    [SerializeField] private Rigidbody RockRB;
    #pragma warning restore 0649

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Launch(Platform activePlatform, Vector3 targetPoint, float launchForce)
    {
        ActivePlatform = activePlatform;
        
        // calculate a vector roughly to the centre of the platform
        Vector3 launchVector = (targetPoint - transform.position).normalized;
        RockRB.AddForce(launchVector * launchForce, ForceMode.Acceleration);
    }

    public void OnCollisionEnter(Collision other)
    {
        // is this the first hit?
        if (!HitPlatform && !other.gameObject.CompareTag("Player"))
        {
            AudioShim.PostEvent("Play_RockImpact", gameObject);

            ActivePlatform.OnRockHit(this);

            HitPlatform = true;
        }
    }
}
