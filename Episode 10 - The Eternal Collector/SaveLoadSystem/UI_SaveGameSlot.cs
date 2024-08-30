using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Events;

public class UI_SaveGameSlot : MonoBehaviour
{
    [SerializeField] SaveLoadManager.ESaveSlot Slot;
    [SerializeField] TextMeshProUGUI SlotLabel;
    [SerializeField] TextMeshProUGUI ManualSave_Date;
    [SerializeField] TextMeshProUGUI AutomaticSave_Date;

    [SerializeField] UnityEngine.UI.Image ManualBackground;
    [SerializeField] UnityEngine.UI.Image AutomaticBackground;

    [SerializeField] Color SelectedColour = Color.green;
    [SerializeField] Color DefaultColour = Color.black;

    [SerializeField] UnityEvent<SaveLoadManager.ESaveSlot, SaveLoadManager.ESaveType> OnSlotSelected = new UnityEvent<SaveLoadManager.ESaveSlot, SaveLoadManager.ESaveType>();

    UI_SaveLoad.EMode Mode;
    bool HasManualSave;
    bool HasAutomaticSave;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void PrepareForMode(UI_SaveLoad.EMode newMode)
    {
        Mode = newMode;

        SlotLabel.text = $"Slot {((int)Slot)}";

        HasManualSave = SaveLoadManager.Instance.IsSavePresent(Slot, SaveLoadManager.ESaveType.Manual);
        HasAutomaticSave = SaveLoadManager.Instance.IsSavePresent(Slot, SaveLoadManager.ESaveType.Automatic);

        if (HasManualSave)
            ManualSave_Date.text = SaveLoadManager.Instance.GetSaveDate(Slot, SaveLoadManager.ESaveType.Manual);
        else
            ManualSave_Date.text = Mode == UI_SaveLoad.EMode.Save ? "None" : "Empty";

        if (HasAutomaticSave)
            AutomaticSave_Date.text = SaveLoadManager.Instance.GetSaveDate(Slot, SaveLoadManager.ESaveType.Automatic);
        else
            AutomaticSave_Date.text = Mode == UI_SaveLoad.EMode.Save ? "None" : "Empty";

        if (Mode == UI_SaveLoad.EMode.Load && !HasManualSave && !HasAutomaticSave)
            gameObject.SetActive(false);
        else
            gameObject.SetActive(true);
    }

    public void OnSelectManualSave()
    {
        if (!HasManualSave && Mode == UI_SaveLoad.EMode.Load)
            return;

        AutomaticBackground.color = DefaultColour;
        ManualBackground.color = SelectedColour;

        OnSlotSelected.Invoke(Slot, SaveLoadManager.ESaveType.Manual);
    }

    public void OnSelectAutomaticSave()
    {
        if (!HasAutomaticSave && Mode == UI_SaveLoad.EMode.Load)
            return;

        AutomaticBackground.color = SelectedColour;
        ManualBackground.color = DefaultColour;

        OnSlotSelected.Invoke(Slot, SaveLoadManager.ESaveType.Automatic);
    }

    public void SetSelected(SaveLoadManager.ESaveSlot slot)
    {
        if (slot != Slot)
        {
            AutomaticBackground.color = DefaultColour;
            ManualBackground.color = DefaultColour;
        }
    }
}
