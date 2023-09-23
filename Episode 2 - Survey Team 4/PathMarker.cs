using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Injaia
{
    public class PathMarker : MonoBehaviour
    {
        [Header("Recording Output")]
        public PathSO TargetPath;

        public enum State
        {
            NotStarted,
            Recording,
            Paused
        };

        [Header("Current State")]
        public State CurrentState = State.NotStarted;

        [Header("Recording Controls")]
        public KeyCode StartRecording = KeyCode.R;
        public KeyCode PauseRecording = KeyCode.P;
        public KeyCode ResumeRecording = KeyCode.R;
        public KeyCode StopRecording = KeyCode.E;
        public Vector3 PositionOffset;
        public bool AppendMode = true;
        public float TimeInterval = 2f;

        protected float TimeUntilNextPoint = -1f;

    #if UNITY_EDITOR
        // Start is called before the first frame update
        void Start()
        {
            TimeUntilNextPoint = TimeInterval;
        }

        // Update is called once per frame
        void Update()
        {
            // not currently recording?
            if (CurrentState == State.NotStarted)
            {
                // trying to start recording?
                if (Input.GetKeyDown(StartRecording))
                {
                    CurrentState = State.Recording;
                    if (!AppendMode)
                        TargetPath.StartPath();
                    RecordPoint();
                }
            }
            else if (CurrentState == State.Recording)
            {
                // trying to pause recording?
                if (Input.GetKeyDown(PauseRecording))
                {
                    CurrentState = State.Paused;
                }
                else if (Input.GetKeyDown(StopRecording))
                {                    
                    CurrentState = State.NotStarted;
                    RecordPoint();
                    TargetPath.EndPath();
                }
                else
                {
                    // update the time and record a new point if needed
                    TimeUntilNextPoint -= Time.deltaTime;
                    if (TimeUntilNextPoint <= 0)
                    {
                        RecordPoint();
                    }
                }
            }
            else if (CurrentState == State.Paused)
            {
                // trying to resume?
                if (Input.GetKeyDown(ResumeRecording))
                {
                    CurrentState = State.Recording;
                    RecordPoint();
                }
            }
        }

        void RecordPoint()
        {
            TimeUntilNextPoint = TimeInterval;

            TargetPath.AddPathPoint(transform.position + PositionOffset, transform.rotation);
        }

        [Header("Debugging")]
        public bool DrawPath = false;

        void OnDrawGizmos()
        {
            if(!DrawPath)
                return;

            // draw as green
            Color oldColour = Gizmos.color;
            Gizmos.color = Color.green;

            foreach(MarkerPoint marker in TargetPath.Markers)
            {
                Gizmos.DrawWireSphere(marker.Position, 1f);
            }

            // restore colour
            Gizmos.color = oldColour;
        }
        #endif // UNITY_EDITOR
    }
}