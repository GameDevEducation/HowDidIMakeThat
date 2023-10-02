using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Platform : MonoBehaviour
{
    public GameObject PlatformGeometry;
    public List<Motor> Motors;

    [Header("Movement")]
    public float PlatformAcceleration = 0.1f;
    public AnimationCurve AccelerationModifier;
    public float CurrentSpeed {get; private set;} = 0f;
    protected float TargetSpeed = 0f;

    [Header("Orientation")]
    public float MotorFullyActive = 1f;
    public float MotorLimiterActive = 0f;
    public float MotorFullyBroken = -1f;

    public float ReorientTime = 0.5f;
    public AnimationCurve ReorientationCurve;
    protected bool Reorienting = false;
    protected float ReorientationProgress = 0f;
    protected Vector3 LastOrientation;
    protected Vector3 TargetOrientation;

    [Header("Stress")]
    public float BaseStress = 10f;
    public float SpeedStress = 10f;
    public AnimationCurve StressDirectionModifier;
    public float StressPerImpact = 10f;
    public float MaxRangeForImpactStress = 3f;

    [Header("Haptics")]
    public HapticEffect PlatformMovingEffect;
    public HapticEffect RockHitEffect;
    public AnimationCurve PlatformMovementSpeedModifier;
    private System.Guid MovingEffectID = System.Guid.Empty;
        
    [Header("Debugging")]
    public bool DEBUG_DrawPlatformVector = false;

    public UnityEvent OnPlatformFailed;

    private bool GameHalted = false;

    public float LowestPoint
    {
        get
        {
            float lowestPoint = transform.position.y;

            foreach(var motor in Motors)
            {
                if (motor.transform.position.y < lowestPoint)
                    lowestPoint = motor.transform.position.y;
            }

            return lowestPoint;
        }
    }

    public void HaltGame()
    {
        CurrentSpeed = TargetSpeed = 0f;

        GameHalted = true;
    }

    // Start is called before the first frame update
    void Start()
    {
        UpdatePlatform(true);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (PauseManager.IsPaused)
            return;

        if (GameHalted)
            return;

        UpdatePlatform(false);

        // set the speed for each motor
        foreach(var motor in Motors)
        {
            AudioShim.SetRTPCValue("PlatformSpeed", CurrentSpeed * 100f, motor.gameObject);
        }
    }

    void UpdatePlatform(bool reorientImmediately)
    {
        int numActiveUnits = 0;

        // Part 1a - Update the target speed
        TargetSpeed = 0f;
        foreach(var motor in Motors)
        {
            if (motor.MotorActive)
            {
                ++numActiveUnits;
                TargetSpeed += 1f / Motors.Count;
            }
            else if (motor.LimiterActive)
                ++numActiveUnits;
        }

        // ensure we stop abruptly
        if (TargetSpeed == 0)
            CurrentSpeed = 0;

        // Part 1b - Update the current speed
        CurrentSpeed = Mathf.MoveTowards(CurrentSpeed, TargetSpeed, PlatformAcceleration * Time.deltaTime * AccelerationModifier.Evaluate(TargetSpeed));

        // apply the haptics if we're moving
        if (CurrentSpeed > 0)
        {
            if (MovingEffectID == System.Guid.Empty)
                HapticsManager.Instance.PlayEffect(PlatformMovingEffect, out MovingEffectID);

            HapticsManager.Instance.SetEffectDuration(MovingEffectID, PlatformMovingEffect.Duration * PlatformMovementSpeedModifier.Evaluate(CurrentSpeed));
        }
        else
        {
            if (MovingEffectID != System.Guid.Empty)
            {
                HapticsManager.Instance.StopEffect(MovingEffectID);
                MovingEffectID = System.Guid.Empty;
            }
        }

        // Part 2 - Update the incline of the platform
        Vector3 platformVector = Vector3.zero;
        foreach(var motor in Motors)
        {
            // Retrieve the vector to the motor and eliminate the y component (for now)
            Vector3 vectorToMotor = motor.transform.position - transform.position;
            vectorToMotor.y = 0;
            vectorToMotor.Normalize();

            // Calculate the y component of the vector based on the state of the motor
            if (motor.MotorActive)
                vectorToMotor.y = MotorFullyActive;
            else if (motor.LimiterActive)
                vectorToMotor.y = MotorLimiterActive;
            else    
                vectorToMotor.y = MotorFullyBroken;

            platformVector += vectorToMotor.normalized;
        }

        platformVector.Normalize();

        if (numActiveUnits == 0 || platformVector.sqrMagnitude < float.Epsilon)
        {
            platformVector = transform.up;

            OnPlatformFailed?.Invoke();
        }

        if (DEBUG_DrawPlatformVector)
        {
            Debug.DrawLine(transform.position, transform.position + platformVector, Color.magenta);
            Debug.DrawLine(transform.position + platformVector, transform.position + Vector3.up * platformVector.y, Color.cyan);
        }

        // handle if reorienting immediately
        if (reorientImmediately)
        {
            transform.up = LastOrientation = TargetOrientation = platformVector;
        }
        else
        {
            // Part 3 - Update the target platform rotation

            // do we have a new target orientation?
            if ((TargetOrientation - platformVector).sqrMagnitude > float.Epsilon)
            {
                LastOrientation = transform.up;
                TargetOrientation = platformVector;
                ReorientationProgress = 0f;
                Reorienting = true;
            }

            // update orientation
            if (Reorienting)
            {
                ReorientationProgress += Time.deltaTime;
                transform.up = Vector3.Lerp(LastOrientation, TargetOrientation, ReorientationCurve.Evaluate(ReorientationProgress / ReorientTime));

                // reorientation complete
                if (ReorientationProgress >= ReorientTime)
                    Reorienting = false;
            }
        }

        // Part 4 - Update the stress on each motor

        // determine the total stress on the platform
        float totalStress = (BaseStress + SpeedStress * CurrentSpeed) * Time.deltaTime;
        
        // determine the stress per active unit
        float stressPerUnit = totalStress / numActiveUnits;

        // apply the stress
        foreach(Motor motor in Motors)
        {
            // Retrieve the vector to the motor and eliminate the y component (for now)
            Vector3 vectorToMotor = motor.transform.position - transform.position;
            vectorToMotor.y = 0;
            vectorToMotor.Normalize();

            float projection = Vector3.Dot(transform.up, vectorToMotor);

            motor.AddStress(stressPerUnit * StressDirectionModifier.Evaluate((projection + 1) / 1f));
        }
    }

    public void OnRockHit(Rock rock)
    {
        HapticsManager.Instance.PlayEffect(RockHitEffect);

        // apply the stress to each motor
        for (int index = 0; index < Motors.Count; ++index)
        {
            // get the vector to the motor in 2D
            Vector3 vectorToMotor = Motors[index].transform.position - rock.transform.position;
            vectorToMotor.y = 0;

            // get the distance
            float distance = vectorToMotor.magnitude;

            // out of range - do nothing
            if (distance > MaxRangeForImpactStress)
                continue;

            // apply the stress
            float stressToApply = StressPerImpact * (1f - (distance / MaxRangeForImpactStress));
            Motors[index].AddStress(stressToApply);
        }
    }
}
