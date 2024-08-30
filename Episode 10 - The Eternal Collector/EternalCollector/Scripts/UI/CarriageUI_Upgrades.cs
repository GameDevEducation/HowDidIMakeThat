using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class CarriageUI_Upgrades : BaseCarriageUI
{
    [SerializeField] GameObject CategoryUIPrefab;
    [SerializeField] GameObject UpgradeUIPrefab;
    [SerializeField] GameObject CarriageUIPrefab;
    [SerializeField] GameObject NoUpgrades_AllPurchasedUIPrefab;
    [SerializeField] GameObject NoUpgrades_NoCarriageUIPrefab;
    [SerializeField] Transform CategoryRoot;
    [SerializeField] Transform UpgradeRoot;
    [SerializeField] string DefaultCategory = "Carriages";

    [SerializeField] GameObject PurchaseUIGO;
    [SerializeField] GameObject UpgradeInProgressUIGO;

    [SerializeField] GameObject PurchaseButtonGO;
    [SerializeField] TextMeshProUGUI PurchaseButtonText;
    [SerializeField] GameObject InsufficientScrapGO;
    [SerializeField] TextMeshProUGUI InsufficientScrapText;

    [SerializeField] List<CarriageUpgrade> AllUpgrades;
    [SerializeField] List<BaseCarriageBehaviour> AllCarriageTypes;

    [System.NonSerialized] Dictionary<string, List<CarriageUpgrade>> UpgradesByCategory;
    [System.NonSerialized] Dictionary<string, List<BaseCarriageBehaviour>> CarriagesByCategory;

    [System.NonSerialized] Dictionary<string, CarriageUI_CategoryUIElement> CategoryUIElements;
    [System.NonSerialized] Dictionary<CarriageUpgrade, CarriageUI_UpgradeUIElement> UpgradeUIElements;
    [System.NonSerialized] Dictionary<BaseCarriageBehaviour, CarriageUI_CarriageUIElement> CarriageUIElements;

    string SelectedCategory;
    CarriageUpgrade SelectedUpgrade;
    BaseCarriageBehaviour SelectedCarriage;
    bool PurchaseWasInProgress = false;

    public override EUIScreen Type() { return EUIScreen.Upgrades; }

    public override void OnSwitchToScreen()
    {
        OnCategorySelected(SelectedCategory);
    }

    protected override void Start()
    {
        SelectedCategory = DefaultCategory;

        List<string> allCategories = new List<string>();

        // build the maps
        UpgradesByCategory = new Dictionary<string, List<CarriageUpgrade>>();
        foreach (var upgrade in AllUpgrades)
        {
            // add in the category if not yet present
            if (!UpgradesByCategory.ContainsKey(upgrade.Category))
            {
                allCategories.Add(upgrade.Category);
                UpgradesByCategory[upgrade.Category] = new List<CarriageUpgrade>();
            }

            UpgradesByCategory[upgrade.Category].Add(upgrade);
        }
        allCategories.Sort();

        CarriagesByCategory = new Dictionary<string, List<BaseCarriageBehaviour>>();
        foreach (var carriage in AllCarriageTypes)
        {
            // add in the category if not yet present
            if (!CarriagesByCategory.ContainsKey(carriage.Category))
            {
                CarriagesByCategory[carriage.Category] = new List<BaseCarriageBehaviour>();

                if (!allCategories.Contains(carriage.Category))
                    allCategories.Insert(0, carriage.Category);
            }

            CarriagesByCategory[carriage.Category].Add(carriage);
        }

        // sort the categories and add in the UI elements
        CategoryUIElements = new Dictionary<string, CarriageUI_CategoryUIElement>();
        foreach (var category in allCategories)
        {
            var categoryUIGO = Instantiate(CategoryUIPrefab, CategoryRoot);
            var categoryUI = categoryUIGO.GetComponent<CarriageUI_CategoryUIElement>();
            categoryUI.SetCategory(category, OnCategorySelected);

            CategoryUIElements[category] = categoryUI;
        }
    }

    protected override void Update()
    {
        if (PurchaseWasInProgress && !EternalCollector.Instance.PurchaseInProgress)
        {
            PurchaseWasInProgress = false;
            OnCategorySelected(SelectedCategory);
        }
    }

    protected void OnCategorySelected(string category)
    {
        SelectedUpgrade = null;
        SelectedCarriage = null;
        SelectedCategory = category;
        InsufficientScrapGO.SetActive(false);
        PurchaseButtonGO.SetActive(false);

        // check if an upgrade is in progress
        if (EternalCollector.Instance.PurchaseInProgress)
        {
            UpgradeInProgressUIGO.SetActive(true);
            PurchaseUIGO.SetActive(false);
            return;
        }
        else
        {
            UpgradeInProgressUIGO.SetActive(false);
            PurchaseUIGO.SetActive(true);
        }

        // update which category is selected
        foreach (var kvp in CategoryUIElements)
            kvp.Value.SetSelected(kvp.Key == category);

        // clear any existing items
        for (int index = UpgradeRoot.childCount - 1; index >= 0; index--)
        {
            var childGO = UpgradeRoot.GetChild(index).gameObject;
            Destroy(childGO);
        }

        // spawn the new items if a carriage was selected
        CarriageUIElements = new Dictionary<BaseCarriageBehaviour, CarriageUI_CarriageUIElement>();
        if (CarriagesByCategory.ContainsKey(category))
        {
            // add the UI for any available carriages
            var carriageList = CarriagesByCategory[category];
            foreach (var carriage in carriageList)
            {
                var carriageUIGO = Instantiate(CarriageUIPrefab, UpgradeRoot);
                var carriageUI = carriageUIGO.GetComponent<CarriageUI_CarriageUIElement>();
                carriageUI.SetCarriage(carriage, OnCarriageSelected);

                CarriageUIElements[carriage] = carriageUI;
            }
        }

        // spawn the new items if an upgrade was selected
        UpgradeUIElements = new Dictionary<CarriageUpgrade, CarriageUI_UpgradeUIElement>();
        if (UpgradesByCategory.ContainsKey(category))
        {
            // add the UI for any unlocked upgrades
            var upgradeList = UpgradesByCategory[category];
            bool allUpgradesUnlocked = true;
            foreach(var upgrade in upgradeList)
            {
                // if the upgrade is unlocked then skip it
                if (EternalCollector.IsUpgradeUnlocked(upgrade))
                    continue;

                allUpgradesUnlocked = false;

                // check if we have prerequisites
                bool prerequisitesMet = true;
                if (upgrade.Prerequisites != null && upgrade.Prerequisites.Count > 0)
                {
                    foreach(var prerequisite in upgrade.Prerequisites)
                    {
                        if (!EternalCollector.IsUpgradeUnlocked(prerequisite))
                        {
                            prerequisitesMet = false;
                            break;
                        }
                    }
                }

                // check if there are any carriages available that support this upgrade
                if (upgrade.ApplicableBehaviours != null && upgrade.ApplicableBehaviours.Count > 0)
                {
                    if (EternalCollector.GetNumApplicableCarriages(upgrade) == 0)
                        prerequisitesMet = false;
                }

                // prerequisites not met
                if (!prerequisitesMet)
                    continue;

                var upgradeUIGO = Instantiate(UpgradeUIPrefab, UpgradeRoot);
                var upgradeUI = upgradeUIGO.GetComponent<CarriageUI_UpgradeUIElement>();
                upgradeUI.SetUpgrade(upgrade, OnUpgradeSelected);

                UpgradeUIElements[upgrade] = upgradeUI;
            }

            // check if there are no UI elements added
            if (UpgradeUIElements.Count == 0)
            {
                if (allUpgradesUnlocked)
                    Instantiate(NoUpgrades_AllPurchasedUIPrefab, UpgradeRoot);
                else
                    Instantiate(NoUpgrades_NoCarriageUIPrefab, UpgradeRoot);
            }
        }
    }

    protected void OnUpgradeSelected(CarriageUpgrade upgrade)
    {
        SelectedUpgrade = upgrade;

        // update which upgrade is selected
        foreach (var kvp in UpgradeUIElements)
            kvp.Value.SetSelected(kvp.Key == upgrade);

        UpdateControls(EternalCollector.CalculateCost(upgrade));
    }

    protected void OnCarriageSelected(BaseCarriageBehaviour behaviour)
    {
        SelectedCarriage = behaviour;

        // update which carriage is selected
        foreach (var kvp in CarriageUIElements)
            kvp.Value.SetSelected(kvp.Key == behaviour);

        UpdateControls(EternalCollector.CalculateCost(behaviour));
    }

    protected void UpdateControls(float itemCost)
    {
        float scrapStored = EternalCollector.Instance.ScrapStorageUsed;

        if (scrapStored >= itemCost)
        {
            InsufficientScrapGO.SetActive(false);
            PurchaseButtonGO.SetActive(true);
            PurchaseButtonText.text = $"Purchase for {itemCost:n0} scrap";
        }
        else
        {
            InsufficientScrapGO.SetActive(true);
            PurchaseButtonGO.SetActive(false);
            InsufficientScrapText.text = $"Need {(itemCost - scrapStored):n0} more scrap to purchase";
        }
    }

    public void OnPurchase()
    {
        AkSoundEngine.PostEvent(AK.EVENTS.PLAY_INTERFACE_PURCHASE, Camera.main.gameObject);

        if (SelectedCarriage != null)
            EternalCollector.PurchaseCarriage(SelectedCarriage);
        else if (SelectedUpgrade != null)
            EternalCollector.PurchaseUpgrade(SelectedUpgrade);

        PurchaseWasInProgress = true;
        OnCategorySelected(SelectedCategory);
    }
}
