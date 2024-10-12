using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AspectRatioHelper : MonoBehaviour
{
    [System.Serializable]
    public class Config
    {
        public float AspectRatio;
        public Vector3 Position;
    }

    public List<Config> AspectRatioConfigs;

    // Start is called before the first frame update
    void Start()
    {
        float aspectRatio = (float)Screen.width / (float)Screen.height;

        // find the config with the closest aspect ratio
        Config bestConfig = null;
        float bestDelta = float.MaxValue;
        foreach(Config config in AspectRatioConfigs)
        {
            float delta = Mathf.Abs(config.AspectRatio - aspectRatio);

            if (delta < bestDelta)
            {
                bestDelta = delta;
                bestConfig = config;
            }
        }

        // update the position
        if (bestConfig != null)
        {
            transform.position = bestConfig.Position;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
