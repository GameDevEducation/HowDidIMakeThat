using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Injaia
{
    [CreateAssetMenu(fileName = "TimeKeeperconfig", menuName = "Injaia/TimeKeeper Config", order = 1)]
    public class TimeKeeperConfig : ScriptableObject
    {
        public TimeDefinition[] TimePeriods;

        public int InitialTimePeriodIndex = 0;

        protected int CurrentTimePeriodIndex;

        public TimeDefinition CurrentTimePeriod
        {
            get
            {
                return TimePeriods[CurrentTimePeriodIndex];
            }
        }

        // Start is called before the first frame update
        public void Reset()
        {
            CurrentTimePeriodIndex = InitialTimePeriodIndex;

            // reset all of the timeperiods
            foreach(TimeDefinition timeDef in TimePeriods)
            {
                timeDef.Reset();
            }
        }

        public float NormalisedTime
        {
            get
            {
                return CurrentTimePeriod.NormalisedTime;
            }
        }

        public bool AdvanceTime(float amount, bool isOnPath)
        {
            // advance the time
            CurrentTimePeriod.AdvanceTime(amount, isOnPath);

            // reached the end of the time period?
            if (CurrentTimePeriod.NormalisedTime >= 1f)
            {
                // move to the next time period
                CurrentTimePeriodIndex = TimePeriods.ToList().IndexOf(CurrentTimePeriod.NextTimePeriod);

                // reset the next time period
                CurrentTimePeriod.Reset();

                Debug.Log("Time period is now " + CurrentTimePeriod.Name);

                return true;
            }

            return false;
        }
    }
}