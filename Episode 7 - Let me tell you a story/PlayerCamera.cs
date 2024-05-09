using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;

public class PlayerCamera : MonoBehaviour
{
    [SerializeField] float MinYaw = -50f;
    [SerializeField] float MaxYaw = 75f;
    [SerializeField] float DefaultYaw = -10f;

    [SerializeField] float MinPitch = -30f;
    [SerializeField] float MaxPitch = 12f;
    [SerializeField] float DefaultPitch = 0f;

    [SerializeField] UnityEvent OnToggleSettingsMenu = new UnityEvent();

    ScheherazadeResponses ResponseSelector;

    float CurrentYaw;
    float CurrentPitch;

    // Start is called before the first frame update
    void Start()
    {
        CurrentYaw = DefaultYaw;
        CurrentPitch = DefaultPitch;

        ResponseSelector = FindObjectOfType<ScheherazadeResponses>();
    }

    public void OnSettings(InputValue value)
    {
        if (!PauseManager.IsPaused)
            OnToggleSettingsMenu?.Invoke();
    }

    protected Vector2 _Internal_LookInput;
    public void OnLook(InputValue value)
    {
        _Internal_LookInput = value.Get<Vector2>();
        
        // adjust based on sensitivity and axis inversion
        _Internal_LookInput.x *= SettingsManager.Settings.Camera.Sensitivity_X;
        _Internal_LookInput.y *= SettingsManager.Settings.Camera.Sensitivity_Y * (SettingsManager.Settings.Camera.Invert_YAxis ? 1f : -1f);
    }

    // Update is called once per frame
    void Update()
    {
        if (PauseManager.IsPaused || ResponseSelector.WaitingForResponse)
            return;

        // retrieve the camera input (already has sensitivity and inversion applied)
        Vector2 cameraInput = _Internal_LookInput * Time.deltaTime;

        // determine the new rotations
        CurrentYaw = Mathf.Clamp(CurrentYaw + cameraInput.x, MinYaw, MaxYaw);
        CurrentPitch = Mathf.Clamp(CurrentPitch + cameraInput.y, MinPitch, MaxPitch);

        transform.localEulerAngles = new Vector3(CurrentPitch, CurrentYaw, 0f);
    }
}
