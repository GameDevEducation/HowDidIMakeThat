using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System.Linq;

public class Despawner : MonoBehaviour
{
    public float DespawnTime = 0f;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (AIManager.Instance.IsPaused)
            return;

        if (DespawnTime > 0)
        {
            DespawnTime -= Time.deltaTime;

            if (DespawnTime <= 0)
            {
                Destroy(gameObject);
            }
        }
    }
}
