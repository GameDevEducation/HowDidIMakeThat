using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Injaia
{
    [System.Serializable] public class DeathclockStatusEvent : UnityEvent<bool> {};

    public class TimeKeeper : MonoBehaviour
    {
        public TimeKeeperConfig Configuration;

        public bool IsPaused = false;
        public float TimeScale = 1f;
        public float StartingTime = 0f;

        public DeathclockStatusEvent OnDeathclockStatusChanged;

        protected bool IsOnPath = true;

        public float NormalisedTime
        {
            get 
            {
                return Configuration.NormalisedTime;
            }
        }

        // Start is called before the first frame update
        void Start()
        {
            // Reset the time keeper
            Configuration?.Reset();

            if(Configuration.AdvanceTime(StartingTime, IsOnPath))
            {
                OnDeathclockStatusChanged?.Invoke(Configuration.CurrentTimePeriod.HasDeathclock);
            }
        }

        // Update is called once per frame
        void Update()
        {
            // do nothing if paused
            if (IsPaused)
                return;

            if (Configuration.AdvanceTime(Time.deltaTime * TimeScale, IsOnPath))
            {
                OnDeathclockStatusChanged?.Invoke(Configuration.CurrentTimePeriod.HasDeathclock);
            }
        }

        public void OnUpdateDistanceToPath(float ratio)
        {
            IsOnPath = ratio < 1f;
        }
    }
}