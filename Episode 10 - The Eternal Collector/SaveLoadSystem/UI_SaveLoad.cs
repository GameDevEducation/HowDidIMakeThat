using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_SaveLoad : MonoBehaviour
{
    public enum EMode
    {
        Save,
        Load
    }

    [SerializeField] List<UI_SaveGameSlot> Slots;
    [SerializeField] EMode Mode = EMode.Load;
    [SerializeField] GameObject LoadButton;
    [SerializeField] GameObject SaveButton;

    SaveLoadManager.ESaveSlot SelectedSlot;
    SaveLoadManager.ESaveType SelectedType;

    // Start is called before the first frame update
    void Start()
    {
        foreach(var slot in Slots)
        {
            slot.PrepareForMode(Mode);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnEnable()
    {
        if (LoadButton != null)
            LoadButton.SetActive(false);
        if (SaveButton != null)
            SaveButton.SetActive(false);

        foreach (var slot in Slots)
        {
            slot.PrepareForMode(Mode);
        }
    }

    public void OnSetLoadSlot(SaveLoadManager.ESaveSlot slot, SaveLoadManager.ESaveType saveType)
    {
        SelectedSlot = slot;
        SelectedType = saveType;
        LoadButton.SetActive(true);

        foreach(var slotUI in Slots)
        {
            slotUI.SetSelected(slot);
        }
    }

    public void OnSetSaveSlot(SaveLoadManager.ESaveSlot slot, SaveLoadManager.ESaveType saveType)
    {
        SelectedSlot = slot;
        SelectedType = saveType;
        SaveButton.SetActive(true);

        foreach (var slotUI in Slots)
        {
            slotUI.SetSelected(slot);
        }
    }

    public void OnPerformLoad()
    {
        SaveLoadManager.Instance.RequestLoad(SelectedSlot, SelectedType);
    }

    public void OnPerformSave()
    {
        SaveLoadManager.Instance.RequestSave(SelectedSlot, SaveLoadManager.ESaveType.Manual);
    }
}
