using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class CarHandler : MonoBehaviour
{
    [SerializeField] float MaxSpeed = 7.5f;
    [SerializeField] AnimationCurve SpeedVariation;
    [SerializeField] float Acceleration = 1f;
    [SerializeField] float Deceleration = 3f;
    [SerializeField] float StoppingDeceleration = 1f;
    [SerializeField] float RotationSlewRate = 30f;
    [SerializeField] bool CanMove = false;
    [SerializeField] float TurningSampleDistance = 2f;
    [SerializeField] List<Transform> WheelObjects;
    [SerializeField] float MaxSteeringWheelAngle = 15f;

    [SerializeField] GameObject EngineSoundEmitter;
    [SerializeField] GameObject DoorSoundEmitter;
    [SerializeField] GameObject BuckleSoundEmitter;
    [SerializeField] GameObject WiperSoundEmitter;

    CinemachineDollyCart LinkedCart;

    float CurrentSpeed = 0f;
    float TargetSpeed = 0f;
    bool JustPlaced = false;
    bool IsStopping = false;

    // Start is called before the first frame update
    void Start()
    {
        AkSoundEngine.SetRTPCValue(AK.GAME_PARAMETERS.SPEED, 0);
    }

    // Update is called once per frame
    void Update()
    {
        if (!CanMove)
            return;

        TargetSpeed = MaxSpeed * SpeedVariation.Evaluate(GetTurnSteepness());

        if (IsStopping)
            TargetSpeed = 0f;

        if (TargetSpeed >= CurrentSpeed)
            CurrentSpeed = Mathf.MoveTowards(CurrentSpeed, TargetSpeed, Acceleration * Time.deltaTime);
        else if (!IsStopping)
            CurrentSpeed = Mathf.MoveTowards(CurrentSpeed, TargetSpeed, Deceleration * Time.deltaTime);
        else
            CurrentSpeed = Mathf.MoveTowards(CurrentSpeed, TargetSpeed, StoppingDeceleration * Time.deltaTime);

        // update the engine sounds
        AkSoundEngine.SetRTPCValue(AK.GAME_PARAMETERS.SPEED, 100f * CurrentSpeed / MaxSpeed);

        // reached a stop?
        if (IsStopping && CurrentSpeed < float.Epsilon)
        {
            AkSoundEngine.PostEvent(AK.EVENTS.STOP_ENGINE, EngineSoundEmitter);
            AkSoundEngine.PostEvent(AK.EVENTS.STOP_WIPERS, WiperSoundEmitter);
            CanMove = false;
        }
        
        float newPosition = LinkedCart.m_Position + CurrentSpeed * Time.deltaTime;

        // reached the end of this path?
        if (newPosition >= LinkedCart.m_Path.PathLength)
        {
            // update how far into the next path we'll be and request a new path
            newPosition -= LinkedCart.m_Path.PathLength;
            LinkedCart = InfiniteRoadsManager.Instance.ReachedEndOfSegment();

            // update the cart position
            LinkedCart.m_Position = newPosition;
        }
        else
            LinkedCart.m_Position = newPosition;
    }

    public void StartMoving()
    {
        CanMove = true;
        AkSoundEngine.PostEvent(AK.EVENTS.PLAY_ENGINE, EngineSoundEmitter);
        AkSoundEngine.PostEvent(AK.EVENTS.PLAY_WIPERS, WiperSoundEmitter);
    }

    public void ComeToStop()
    {
        IsStopping = true;
    }

    public void OnOpenDoor()
    {
        AkSoundEngine.PostEvent(AK.EVENTS.PLAY_OPENDOOR, DoorSoundEmitter);
    }

    public void OnCloseDoor()
    {
        AkSoundEngine.PostEvent(AK.EVENTS.PLAY_CLOSEDOOR, DoorSoundEmitter);
    }

    public void OnBuckle()
    {
        AkSoundEngine.PostEvent(AK.EVENTS.PLAY_BUCKLE, BuckleSoundEmitter);
    }

    public void OnUnbuckle()
    {
        AkSoundEngine.PostEvent(AK.EVENTS.PLAY_UNBUCKLE, BuckleSoundEmitter);
    }

    public void PlaceCar(CinemachineDollyCart trackDolly, float initialProgress)
    {
        LinkedCart = trackDolly;

        // set the initial position
        LinkedCart.m_Position = LinkedCart.m_Path.PathLength * initialProgress;
        JustPlaced = true;
    }

    void LateUpdate()
    {
        if (JustPlaced)
        {
            transform.rotation = LinkedCart.transform.rotation;
            JustPlaced = false;
        }

        Quaternion oldRotation = transform.rotation;

        transform.position = LinkedCart.transform.position;
        transform.rotation = Quaternion.RotateTowards(transform.rotation, LinkedCart.transform.rotation, RotationSlewRate * Time.deltaTime);

        // calculate the signed angle delta
        float signedAngleDelta = transform.eulerAngles.y - oldRotation.eulerAngles.y;
        if (signedAngleDelta >= 360f)
            signedAngleDelta -= 360f;
        else if (signedAngleDelta <= -360f)
            signedAngleDelta += 360f;

        // rotate the steering wheel based on the turn
        float targetAngle = -Mathf.Clamp(signedAngleDelta, -1f, 1f) * MaxSteeringWheelAngle;
        CurrentSteeringWheelAngle = Mathf.MoveTowards(CurrentSteeringWheelAngle, targetAngle, SteeringWheelSlewRate * Time.deltaTime);
        for (int index = 0; index < WheelObjects.Count; ++index)
        {
            WheelObjects[index].transform.localEulerAngles = new Vector3(0f, 0f, CurrentSteeringWheelAngle);
        }
    }
    float CurrentSteeringWheelAngle = 0f;
    [SerializeField] float SteeringWheelSlewRate = 30f;

    float GetTurnSteepness()
    {
        var currentPosition = transform.position;
        var nextPosition = LinkedCart.m_Path.EvaluatePositionAtUnit(LinkedCart.m_Position + TurningSampleDistance, LinkedCart.m_PositionUnits);

        var vecToNext = nextPosition - currentPosition;

        return Mathf.Abs(Vector3.Dot(transform.right, vecToNext.normalized));
    }
}
