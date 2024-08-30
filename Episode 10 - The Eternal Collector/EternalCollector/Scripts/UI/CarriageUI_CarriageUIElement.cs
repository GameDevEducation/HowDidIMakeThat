using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

public class CarriageUI_CarriageUIElement : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI NameLabel;
    [SerializeField] TextMeshProUGUI CostLabel;
    [SerializeField] TextMeshProUGUI DescriptionLabel;
    [SerializeField] Image BackgroundImage;

    [SerializeField] Color DefaultColour;
    [SerializeField] Color SelectedColour;

    protected BaseCarriageBehaviour LinkedBehaviour;
    protected UnityAction<BaseCarriageBehaviour> OnCarriageSelected;

    protected bool UsingDefaultFont;
    protected float CurrentFontSize_Name;
    protected float CurrentFontSize_Cost;
    protected float CurrentFontSize_Description;

    // Start is called before the first frame update
    void Start()
    {
        UsingDefaultFont = !SettingsManager.Settings.Gameplay.UseOpenDyslexic;

        CurrentFontSize_Name = NameLabel.fontSize;
        CurrentFontSize_Cost = CostLabel.fontSize;
        CurrentFontSize_Description = DescriptionLabel.fontSize;

        UpdateFonts();
    }

    void UpdateFonts()
    {
        float fontSizeDelta;
        NameLabel.font = CostLabel.font = DescriptionLabel.font = FontManager.GetCurrentFont(out fontSizeDelta);
        NameLabel.fontSize = CurrentFontSize_Name + (UsingDefaultFont ? 0 : -10f);
        CostLabel.fontSize = CurrentFontSize_Cost + (UsingDefaultFont ? 0f : 0f);
        DescriptionLabel.fontSize = CurrentFontSize_Description + (UsingDefaultFont ? 0f : -8f);
    }

    // Update is called once per frame
    void Update()
    {
        if (UsingDefaultFont != !SettingsManager.Settings.Gameplay.UseOpenDyslexic)
        {
            UsingDefaultFont = !SettingsManager.Settings.Gameplay.UseOpenDyslexic;
            UpdateFonts();
        }
    }

    public void SetCarriage(BaseCarriageBehaviour behaviour, UnityAction<BaseCarriageBehaviour> onSelected)
    {
        LinkedBehaviour = behaviour;
        OnCarriageSelected = onSelected;

        NameLabel.text = behaviour.DisplayName;
        CostLabel.text = $"{EternalCollector.CalculateCost(behaviour):n0}";
        DescriptionLabel.text = behaviour.Description;
    }

    public void SetSelected(bool isSelected)
    {
        BackgroundImage.color = isSelected ? SelectedColour : DefaultColour;
    }

    public void OnClicked()
    {
        OnCarriageSelected.Invoke(LinkedBehaviour);
    }
}
