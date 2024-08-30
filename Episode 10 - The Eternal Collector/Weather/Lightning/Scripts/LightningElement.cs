using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightningElement : MonoBehaviour
{
    [SerializeField] MeshRenderer LinkedMesh;

    public float StartTime { get; private set; }
    public float EndTime { get; private set; }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public bool SyncToTime(float time)
    {
        if (time < StartTime || time >= EndTime)
        {
            LinkedMesh.enabled = false;
            return false;
        }

        LinkedMesh.enabled = true;
        return true;
    }

    public void SetTimes(float startTime, float persistenceTime)
    {
        StartTime = startTime;
        EndTime = startTime + persistenceTime;
    }
}
