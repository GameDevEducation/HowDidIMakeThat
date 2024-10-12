using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class CameraTransitioner : MonoBehaviour
{
    [System.Serializable]
    public class Transition
    {
        public string ID;

        [Header("Markers")]
        public Transform StartTransform;
        public Transform EndTransform;

        [Header("Target")]
        public Transform TargetObject;

        [Header("Movement Control")]
        public AnimationCurve PositionCurve;
        public AnimationCurve RotationCurve;
        public float TransitionTime = 5f;

        [Header("Events")]
        public UnityEvent OnFinishedTransition;

        [System.NonSerialized]
        public float TransitionSpeed;
        [System.NonSerialized]
        public float CurrentProgress = 0f;

        [System.NonSerialized]
        public bool IsActive = false;

        private EventSystem activeEventSystem;

        public void StartTransition()
        {
            // mark as active
            IsActive = true;
            TransitionSpeed = 1f / TransitionTime;
            CurrentProgress = 0f;

            // turn off the event system until the transition is complete
            activeEventSystem = EventSystem.current;
            EventSystem.current.enabled = false;
        }

        public void Update()
        {
            // not active - exit
            if (!IsActive)
                return;

            // update progress if needed
            if (CurrentProgress < 1f)
            {
                // Update the progress
                CurrentProgress += Time.deltaTime * TransitionSpeed;

                // calculate the new position and rotation
                TargetObject.position = Vector3.Lerp(StartTransform.position, EndTransform.position, PositionCurve.Evaluate(CurrentProgress));
                TargetObject.rotation = Quaternion.Lerp(StartTransform.rotation, EndTransform.rotation, RotationCurve.Evaluate(CurrentProgress));

                // reached the end?
                if (CurrentProgress >= 1f)
                {
                    // re-enable the event system
                    activeEventSystem.enabled = true;
                    activeEventSystem = null;

                    OnFinishedTransition?.Invoke();

                    IsActive = false;
                }
            }            
        }
    }

    public List<Transition> Transitions;

    // Start is called before the first frame update
    void Start()
    {
    }

    public void StartTransition(string transitionID)
    {
        // start the appropriate transition
        foreach(Transition transition in Transitions)
        {
            if (transition.ID == transitionID)
                transition.StartTransition();
        }
    }

    // Update is called once per frame
    void Update()
    {
        // update all transitions
        foreach(Transition transition in Transitions)
        {
            transition.Update();
        }
    }
}
