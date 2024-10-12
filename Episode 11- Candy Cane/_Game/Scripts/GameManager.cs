using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class PauseHandler : UnityEvent<bool> {}

public class GameManager : MonoBehaviour
{
    public enum State
    {
        Introduction,
        Playing,
        ShowWeapon,
        Paused,
        EndGame
    };

    public State CurrentState = State.Introduction;

    public PauseHandler OnPauseChanged;

    public LoadoutUI LoadoutDisplay;
    public AmmoSO DefaultWeapon;

    public GameObject Player;
    public GameObject DeathLine;

    public UnityEvent OnGameOver;
    protected bool EndTriggered = false;

    public List<BuildingPiece> Foundations;

    // Start is called before the first frame update
    void Start()
    {
        LoadoutDisplay.ShowWeapon(DefaultWeapon);
    }

    // Update is called once per frame
    void Update()
    {
        if (!EndTriggered && Player.transform.position.y < DeathLine.transform.position.y)
        {
            EndTriggered = true;
            OnGameOver?.Invoke();
        }
    }

    public void OnPause()
    {
        Physics.autoSimulation = false;

        OnPauseChanged?.Invoke(true);
    }

    public void OnResume()
    {
        Physics.autoSimulation = true;

        OnPauseChanged?.Invoke(false);
    }

    public void LoadMainMenu()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }

    public void DestroyHouse()
    {
        foreach(BuildingPiece buildingPiece in Foundations)
        {
            if (buildingPiece.CanBeDamaged)
            {
                buildingPiece.OnTakeDamage(buildingPiece.CurrentHealth);
            }
        }
    }
}
