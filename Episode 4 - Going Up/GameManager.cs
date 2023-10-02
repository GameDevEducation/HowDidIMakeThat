using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class GameManager : MonoBehaviour
{
    public CharacterMotor Player;
    public Platform LiftPlatform;

    public float DeathBuffer = 2f;

    protected bool HasDied = false;

    public UnityEvent OnHaltGame;
    public UnityEvent OnPlayerFell;
    public UnityEvent OnPlatformFailed;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (PauseManager.IsPaused)
            return;

        if (HasDied)
            return;

        if (Player.transform.position.y < (LiftPlatform.LowestPoint - DeathBuffer))
        {
            HasDied = true;

            EndGame_PlayerFell();
        }
    }

    public void EndGame_PlayerFell()
    {
        OnHaltGame?.Invoke();
        OnPlayerFell?.Invoke();
    }

    public void EndGame_PlatformFailed()
    {
        OnHaltGame?.Invoke();
        OnPlatformFailed?.Invoke();
    }

    public void GoToMainMenu()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu", UnityEngine.SceneManagement.LoadSceneMode.Single);
    }
}
