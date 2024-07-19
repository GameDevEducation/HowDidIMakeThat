using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cleaner : MonoBehaviour
{
    protected Vector3 ImpactForce;
    protected float CleaningStrength;
    protected int MaxPenetration;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void InitCleaner(Vector3 _impactForce, float _cleaningStrength, int _maxPenetration)
    {
        ImpactForce = _impactForce;
        CleaningStrength = _cleaningStrength;
        MaxPenetration = _maxPenetration;
    }

    public void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Cleanable"))
        {
            CleanableItem item = other.gameObject.GetComponent<CleanableItem>();
            item.CleanerImpact(ImpactForce, CleaningStrength);

            // update the penetration
            --MaxPenetration;

            // Play the hit sound
            AkSoundEngine.PostEvent(item.OnHitSound, other.gameObject);

            // bubble does not penetrate
            if (MaxPenetration <= 0)
                Destroy(gameObject);
        }
        else if (other.gameObject.CompareTag("KillZone"))
        {
            Destroy(gameObject);
        }
    }
}
