using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class CarriageUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI _HeaderText;
    [SerializeField] Camera LinkedCamera;
    [SerializeField] float UIFPS = 10f;
    [SerializeField] List<BaseCarriageUI> Screens;

    public TextMeshProUGUI HeaderText => _HeaderText;

    float NextUpdateTime = 0f;
    float UpdateInterval = 0f;
    EUIScreen CurrentScreen;

    public static CarriageUI Instance { get; private set; } = null;

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError($"Found duplicate CarriageUI on {gameObject.name} destroying the newest one");
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        UpdateInterval = 1f / UIFPS;

        SwitchScreen_Overview();
    }

    // Update is called once per frame
    void Update()
    {
        NextUpdateTime -= Time.deltaTime;
        if (NextUpdateTime <= 0f)
        {
            NextUpdateTime += UpdateInterval;
            LinkedCamera.Render();
        }
    }

    public void OnChangedCarriage(Carriage previousCarriage, Carriage currentCarriage)
    {
        if (previousCarriage != null && CurrentScreen == previousCarriage.CarriageUIType)
            SwitchScreen(currentCarriage.CarriageUIType);
    }

    public void SwitchScreen_Overview()
    {
        SwitchScreen(EUIScreen.Overview);
    }

    public void SwitchScreen_CurrentCarriage()
    {
        SwitchScreen(CharacterMotor.Instance.CurrentCarriage.CarriageUIType);
    }

    public void SwitchScreen_Upgrades()
    {
        SwitchScreen(EUIScreen.Upgrades);
    }

    void SwitchScreen(EUIScreen newScreen)
    {
        CurrentScreen = newScreen;

        // update which screen is visible
        foreach(var screen in Screens)
        {
            if (screen.Type() == newScreen)
            {
                screen.gameObject.SetActive(true);
                screen.OnSwitchToScreen();
            }
            else
                screen.gameObject.SetActive(false);
        }
    }
}
