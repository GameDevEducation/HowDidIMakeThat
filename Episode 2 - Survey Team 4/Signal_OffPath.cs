using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.Events;

namespace Injaia
{
    [System.Serializable]
    public class DistanceToPathUpdated : UnityEvent<float> {}

    public class Signal_OffPath : MonoBehaviour
    {
        [Header("Reference Path")]
        public PathSO ReferencePath;

        [Header("Reference Character")]
        [Tooltip("This is the character who's proximity to the path will be monitored.")]
        public Transform ReferenceCharacter;

        [Header("Detection Behaviour")]
        public bool IsPaused = false;
        public float ScanInterval = 2f;
        public float OffPathThreshold = 30f;
        protected float TimeUntilNextScan = -1f;

        public DistanceToPathUpdated OnUpdateDistanceToPath;

        protected bool WasWithinThreshold = true;

        // Start is called before the first frame update
        void Start()
        {
            // Default to on path
            //AkSoundEngine.SetState("Player_Location", "OnPath");
        }

        // Update is called once per frame
        void Update()
        {
            // exit if paused
            if (IsPaused)
                return;

            // update the time
            TimeUntilNextScan -= Time.deltaTime;

            // time to perform a scan
            if (TimeUntilNextScan < 0)
            {
                TimeUntilNextScan = ScanInterval;

                PerformScan();
            }
        }

        public void PerformScan()
        {
            MarkerPoint closestMarker = ReferencePath.GetClosestMarker2D(ReferenceCharacter);

            // found the closest marker
            if (closestMarker != null)
            {
                // calculate the distance in 2D
                Vector3 distance = closestMarker.Position - ReferenceCharacter.position;
                distance.y = 0;

                float ratio = distance.magnitude / OffPathThreshold;

                if (WasWithinThreshold && ratio >= 1)
                {
                    WasWithinThreshold = false;
                    // Tell the sound engine we are off path
                    //AkSoundEngine.SetState("Player_Location", "OffPath");
                }
                else if (!WasWithinThreshold && ratio < 1)
                {
                    WasWithinThreshold = true;
                    // Tell the sound engine we are on path
                    AkSoundEngine.SetState("Player_Location", "OnPath");
                }

                OnUpdateDistanceToPath?.Invoke(ratio);
                //Debug.Log(Mathf.Sqrt(closestMarker.DistSquared2D(ReferenceCharacter)));
            }
        }
    }
}
