using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class LoadoutUI : MonoBehaviour
{
    public TMPro.TextMeshProUGUI MessageContent;
    public UnityEngine.UI.Button CloseButton;
    public UnityEvent OnShowLoadoutUI;
    public Launcher LauncherLogic;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
    }

    public void ShowWeapon(AmmoSO weapon)
    {
        LauncherLogic.SetAmmunition(weapon);

        MessageContent.text = "<b>" + weapon.Name + "</b>" + System.Environment.NewLine +
                              "<i>" + weapon.FlavourText + "</i>" + System.Environment.NewLine + System.Environment.NewLine +
                              "<b>Primary Fire</b>" + System.Environment.NewLine + weapon.PrimaryFire.FlavourText + System.Environment.NewLine + System.Environment.NewLine + 
                              "<b>Secondary Fire</b>" + System.Environment.NewLine + weapon.SecondaryFire.FlavourText;

        gameObject.SetActive(true);

        OnShowLoadoutUI?.Invoke();
    }
}
