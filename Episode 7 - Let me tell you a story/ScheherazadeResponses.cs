using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public class ScheherazadeResponses : MonoBehaviour, IPausable
{
    [SerializeField] CanvasGroup ResponseGroup;
    [SerializeField] float FadeSpeed = 1f;

    [SerializeField] Button Option1_Button;
    [SerializeField] TextMeshProUGUI Option1_Text;
    [SerializeField] RectTransform Option1_Progress;

    [SerializeField] Button Option2_Button;
    [SerializeField] TextMeshProUGUI Option2_Text;
    [SerializeField] RectTransform Option2_Progress;

    [SerializeField] Button Option3_Button;
    [SerializeField] TextMeshProUGUI Option3_Text;
    [SerializeField] RectTransform Option3_Progress;

    [SerializeField] float TimerWidth = 190f;

    System.Action<EResponseType> OnResponseSelected = null;
    EResponseType DefaultResponse = EResponseType.Silent;
    RectTransform DefaultResponse_Progress;

    EResponseType Option1_Type;
    EResponseType Option2_Type;
    EResponseType Option3_Type;

    float TimeUntilDefaultSelected = -1f;
    float TimeElapsed = 0f;
    float TargetAlpha = 0f;

    public bool WaitingForResponse => TargetAlpha > 0.5f;

    // Start is called before the first frame update
    void Start()
    {
        PauseManager.Instance.RegisterPausable(this);
    }

    void OnDestroy()
    {
        PauseManager.Instance.DeregisterPausable(this);
    }

    public bool OnPauseRequested()
    {
        return true;
    }

    public bool OnResumeRequested()
    {
        return true;
    }

    public void OnPause()
    {
        ResponseGroup.interactable = false;
    }

    public void OnResume()
    {
        ResponseGroup.interactable = ResponseGroup.alpha >= 0.1f;
    }

    // Update is called once per frame
    void Update()
    {
        if (PauseManager.IsPaused)
            return;

        if (ResponseGroup.alpha != TargetAlpha)
        {
            ResponseGroup.alpha = Mathf.MoveTowards(ResponseGroup.alpha, TargetAlpha, FadeSpeed * Time.deltaTime);

            ResponseGroup.interactable = ResponseGroup.alpha >= 0.1f;
        }

        // update the default countdown
        if (ResponseGroup.interactable && TimeUntilDefaultSelected > 0)
        {
            TimeElapsed += Time.deltaTime;
            if (TimeElapsed >= TimeUntilDefaultSelected)
            {
                OnSendResponse(DefaultResponse);
                TimeUntilDefaultSelected = -1f;
            }
            else
                DefaultResponse_Progress.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, TimerWidth * (1f - (TimeElapsed / TimeUntilDefaultSelected)));
        }

        if (ResponseGroup.interactable)
        {
            if (!Cursor.visible)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            if (Keyboard.current.digit1Key.wasPressedThisFrame)
                OnOption1();
            else if (Keyboard.current.digit2Key.wasPressedThisFrame)
                OnOption2();
            else if (Keyboard.current.digit3Key.wasPressedThisFrame)
                OnOption3();
        }
        else if (Cursor.visible)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public void SetFonts(TMP_FontAsset font)
    {
        Option1_Text.font = font;
        Option2_Text.font = font;
        Option3_Text.font = font;
    }

    public void ShowOptions(EResponseType option1, string option1Label,
                            EResponseType option2, string option2Label,
                            EResponseType option3, string option3Label,
                            EResponseType defaultOption, float defaultSelectionTime,
                            System.Action<EResponseType> optionSelected)
    {
        Option1_Text.text = "<sprite=\"Keyboard_Black_1\" index=0> " + option1Label;
        Option1_Type = option1;
        Option2_Text.text = "<sprite=\"Keyboard_Black_3\" index=0> " + option2Label;
        Option2_Type = option2;
        Option3_Text.text = "<sprite=\"Keyboard_Black_2\" index=0> " + option3Label;
        Option3_Type = option3;

        DefaultResponse = defaultOption;
        TimeUntilDefaultSelected = defaultSelectionTime;
        TimeElapsed = 0f;

        if (Option1_Type == defaultOption)
            DefaultResponse_Progress = Option1_Progress;
        else if (Option2_Type == defaultOption)
            DefaultResponse_Progress = Option2_Progress;
        else if (Option3_Type == defaultOption)
            DefaultResponse_Progress = Option3_Progress;

        Option1_Progress.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 0f);
        Option2_Progress.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 0f);
        Option3_Progress.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 0f);
        DefaultResponse_Progress.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, TimerWidth);

        OnResponseSelected = optionSelected;

        TargetAlpha = 1f;
    }

    public void OnOption1()
    {
        if (PauseManager.IsPaused)
            return;

        OnSendResponse(Option1_Type);
    }

    public void OnOption2()
    {
        if (PauseManager.IsPaused)
            return;

        OnSendResponse(Option2_Type);
    }

    public void OnOption3()
    {
        if (PauseManager.IsPaused)
            return;

        OnSendResponse(Option3_Type);
    }

    void OnSendResponse(EResponseType response)
    {
        TargetAlpha = 0f;
        ResponseGroup.interactable = false;
        OnResponseSelected.Invoke(response);
    }
}
