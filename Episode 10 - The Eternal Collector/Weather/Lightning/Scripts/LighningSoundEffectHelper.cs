using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LighningSoundEffectHelper : MonoBehaviour
{
    [SerializeField] float DeathClock = 5f;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (DeathClock > 0)
        {
            DeathClock -= Time.deltaTime;
            if (DeathClock <= 0)
                Destroy(gameObject);
        }
    }
}
