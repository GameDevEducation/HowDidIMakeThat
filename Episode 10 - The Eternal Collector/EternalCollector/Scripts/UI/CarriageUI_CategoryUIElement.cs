using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

public class CarriageUI_CategoryUIElement : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI LinkedText;
    [SerializeField] Image BackgroundImage;
    protected UnityAction<string> OnCategorySelected;

    [SerializeField] Color DefaultColour;
    [SerializeField] Color SelectedColour;

    protected bool UsingDefaultFont;
    protected float CurrentFontSize;

    // Start is called before the first frame update
    void Start()
    {
        UsingDefaultFont = !SettingsManager.Settings.Gameplay.UseOpenDyslexic;

        CurrentFontSize = LinkedText.fontSize;

        UpdateFonts();
    }

    void UpdateFonts()
    {
        float fontSizeDelta;
        LinkedText.font = FontManager.GetCurrentFont(out fontSizeDelta);
        LinkedText.fontSize = CurrentFontSize + fontSizeDelta;
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

    public void SetCategory(string category, UnityAction<string> onSelected)
    {
        LinkedText.text = category;
        OnCategorySelected = onSelected;
    }

    public void SetSelected(bool isSelected)
    {
        BackgroundImage.color = isSelected ? SelectedColour : DefaultColour;
    }

    public void OnClicked()
    {
        OnCategorySelected.Invoke(LinkedText.text);
    }
}
