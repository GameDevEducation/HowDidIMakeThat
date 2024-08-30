using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InGameMenu : MonoBehaviour
{
    [SerializeField] GameObject LoadButton;

    public void RefreshLoadVisibility()
    {
        LoadButton.SetActive(SaveLoadManager.Instance.HasAnySaveGames);
    }
}
