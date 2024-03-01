using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using System.Linq;
using TMPro;

[System.Serializable]
public class ServerResponse_RequestLease
{
    public string result;
    public string apartmentID;
    public string leaseGUID;
    public int realm;
    public string B1Name;
    public string B2Name;
}

public class PropertyManager : MonoBehaviour
{
    public bool IsOnline = false;

    public GameObject UI_Online;
    public GameObject UI_Offline;

    public float AnimationTime = 3f;
    public AnimationCurve TextAnimation;

    public List<Building> LeasableBuildings;
    public List<Building> RandomisedBuildings;

    public float LeaseRenewalInterval = 60f;
    float TimeUntilNextRenewal = 0f;

    public float LightSyncInterval = 5f;
    float TimeUntilNextLightSync = 0f;

    public UnityEvent FinishLoading;

    public TextMeshProUGUI Location;

    #region Server
    const string BaseURL = "";
    #endregion

    public enum IntroStage
    {
        NotStarted, 

        Selection,
        Confirmation,
        Preparation,
        WorldSync,
        TurningOnLights,
        Welcome
    }

    IntroStage CurrentStage = IntroStage.NotStarted;
    List<RectTransform> IntroUITransforms = new List<RectTransform>();
    List<Vector2> TargetUIPositions = new List<Vector2>();
    bool AnimationComplete = false;
    bool CanAdvance = false;

    Building SelectedBuilding;
    Apartment SelectedApartment;
    string LeaseGUID;
    int RealmID;

    // Start is called before the first frame update
    void Start()
    {
        TimeUntilNextRenewal = LeaseRenewalInterval;
        TimeUntilNextLightSync = LightSyncInterval;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        IsOnline = PlayerPrefs.GetString("Mode", "Offline") == "Online";
    }

    // Update is called once per frame
    void Update()
    {   
        // intro stage has completed
        if (AnimationComplete && CanAdvance)
        {
            AnimationComplete = false;
            CanAdvance = false;

            // finished and can activate the player?
            if (CurrentStage == IntroStage.Welcome)
            {
                FinishLoading?.Invoke();
                return;
            }

            CurrentStage = (CurrentStage + 1);

            StartCoroutine(PerformStageLogic());
        }

        // periodically renew the lease if online
        if (CurrentStage == IntroStage.Welcome)
        {
            if (IsOnline)
            {
                // lease renewal handling
                TimeUntilNextRenewal -= Time.deltaTime;
                if (TimeUntilNextRenewal <= 0)
                {
                    TimeUntilNextRenewal = LeaseRenewalInterval;

                    StartCoroutine(RenewLease());
                }
            }

            // light synchronisation handling
            TimeUntilNextLightSync -= Time.deltaTime;
            if (TimeUntilNextLightSync <= 0)
            {
                TimeUntilNextLightSync = LightSyncInterval;

                StartCoroutine(IsOnline ? WorldSync_Online() : WorldSync_Offline());
            }
        }
    }

    public void Begin()
    {
        // update which UI is active
        UI_Online.SetActive(IsOnline);
        UI_Offline.SetActive(!IsOnline);
        Transform workingUI = IsOnline ? UI_Online.transform : UI_Offline.transform;

        // build the list of UI stages to process
        for(int childIndex = 0; childIndex < workingUI.childCount; ++childIndex)
        {
            RectTransform childTransform = workingUI.GetChild(childIndex).transform.GetComponent<RectTransform>();

            IntroUITransforms.Add(childTransform);
            TargetUIPositions.Add(childTransform.anchoredPosition);
            
            childTransform.anchoredPosition = childTransform.anchoredPosition + Vector2.down * 500f;
            childTransform.localEulerAngles = new Vector3(90f, 0f, 0f);
        }

        CurrentStage = IntroStage.NotStarted;
        AnimationComplete = true;
        CanAdvance = true;
    }

    float AnimationProgress = 0f;
    Vector3 UIStartingPosition;

    void PrepareUIAnimation()
    {
        // reset the animation information
        AnimationProgress = 0f;
        UIStartingPosition = IntroUITransforms[(int)CurrentStage - 1].anchoredPosition;
    }

    bool UpdateUIAnimation()
    {   
        if (AnimationProgress < 1f)
        {
            // update the progress
            AnimationProgress = Mathf.Clamp01(AnimationProgress + (Time.deltaTime / AnimationTime));

            // update the position
            Vector3 newPosition = Vector3.Lerp(UIStartingPosition, TargetUIPositions[(int)CurrentStage - 1], AnimationProgress);
            IntroUITransforms[(int)CurrentStage - 1].anchoredPosition = newPosition;

            // update the rotation
            IntroUITransforms[(int)CurrentStage - 1].localEulerAngles = new Vector3(Mathf.Lerp(90f, 0f, AnimationProgress), 0f, 0f);

            return false;
        }

        return true;
    }

    IEnumerator PerformStageLogic()
    {
        // reset the state
        AnimationComplete = false;
        CanAdvance = true; // default to always can advance

        // setup the animation
        PrepareUIAnimation();

        // apartment selection
        if (CurrentStage == IntroStage.Selection)
        {
            // if online then manual advance
            CanAdvance = !IsOnline;

            StartCoroutine(IsOnline ? SelectApartment_Online() : SelectApartment_Offline());
        }
        else if (CurrentStage == IntroStage.WorldSync)
        {
            // if online then manual advance
            CanAdvance = !IsOnline;

            SelectedApartment.Lease();

            StartCoroutine(IsOnline ? WorldSync_Online() : WorldSync_Offline());
        }
        else if (CurrentStage == IntroStage.TurningOnLights)
        {
            Location.text = SelectedBuilding.BuildingName + System.Environment.NewLine + SelectedApartment.gameObject.name;
        }

        // run the animtion
        while(!UpdateUIAnimation())
        {
            yield return new WaitForEndOfFrame();
        }
        AnimationComplete = true;

        // wait until the stage is complete
        while(!CanAdvance)
        {
            yield return new WaitForEndOfFrame();
        }
    }

    public void OnLightsChanged()
    {
        StartCoroutine(SendLightState());
    }

    IEnumerator SendLightState()
    {   
        yield return new WaitForSeconds(0.1f);

        UnityWebRequest www = UnityWebRequest.Get(BaseURL + "action=set_lights&lease=" + LeaseGUID + "&light_state=" + (SelectedApartment.IsLightOn ? "on" : "off"));
        www.timeout = 1;
        yield return www.SendWebRequest();
 
        // check for a failure
        if(www.isNetworkError || www.isHttpError) 
        {
            // do nothing on failure at this stage
        }

        yield return null;
    }

    IEnumerator RenewLease()
    {
        UnityWebRequest www = UnityWebRequest.Get(BaseURL + "action=renew_lease&lease=" + LeaseGUID);
        www.timeout = 1;
        yield return www.SendWebRequest();
 
        // check for a failure
        if(www.isNetworkError || www.isHttpError) 
        {
            // do nothing on failure at this stage
        }

        yield return null;
    }

    IEnumerator SelectApartment_Online()
    {
        UnityWebRequest www = UnityWebRequest.Get(BaseURL + "action=request_lease");
        www.timeout = 5;
        yield return www.SendWebRequest();
 
        // go offline if there was a failure
        if(www.isNetworkError || www.isHttpError) 
        {
            IsOnline = false;
        }
        else 
        {
            ServerResponse_RequestLease response = JsonUtility.FromJson<ServerResponse_RequestLease>(www.downloadHandler.text);

            // if there was a failure then reset
            if (response.result.ToLower() != "success")
            {
                IsOnline = false;
            } // otherwise find the apartment and set the lease GUID
            else
            {
                LeaseGUID = response.leaseGUID;

                // search for the apartment
                int buildingIndex = 1;
                foreach(Building building in LeasableBuildings)
                {
                    var searchResults = building.Apartments.Where(apartment => apartment.UniqueID == response.apartmentID);

                    if (searchResults.Count() == 1)
                    {
                        SelectedBuilding = building;                        
                        SelectedApartment = searchResults.First();

                        if (buildingIndex == 1)
                            SelectedBuilding.BuildingName = response.B1Name;                        
                        else if (buildingIndex == 2)
                            SelectedBuilding.BuildingName = response.B2Name;

                        break;
                    }

                    ++buildingIndex;
                }

                RealmID = response.realm;

                if (SelectedApartment == null)
                    IsOnline = false;
            }
        }

        // online mode failed so fall back to offline mode
        if (!IsOnline)
        {
            // pick a random building
            SelectedBuilding = LeasableBuildings[Random.Range(0, LeasableBuildings.Count)];

            // select a random apartment
            SelectedApartment = SelectedBuilding.Apartments[Random.Range(0, SelectedBuilding.Apartments.Count)];
        }

        CanAdvance = true;

        yield return null;
    }

    IEnumerator SelectApartment_Offline()
    {
        // initialise the RNG to a set value
        Random.InitState(System.DateTime.UtcNow.Millisecond * System.DateTime.UtcNow.Hour);

        // pick a random building
        SelectedBuilding = LeasableBuildings[Random.Range(0, LeasableBuildings.Count)];

        // select a random apartment
        SelectedApartment = SelectedBuilding.Apartments[Random.Range(0, SelectedBuilding.Apartments.Count)];

        yield return null;
    }

    void UpdateLights_RandomisedBuildings()
    {
        int currentUTCHour = System.DateTime.UtcNow.Hour;

        // loop through each building
        for (int buildingIndex = 0; buildingIndex < RandomisedBuildings.Count; ++buildingIndex)
        {
            // initialise the RNG to a set value
            Random.InitState((buildingIndex + 1) * (currentUTCHour + 1) * (currentUTCHour + 1));

            RandomisedBuildings[buildingIndex].RandomiseApartmentLights();
        }
    }

    IEnumerator WorldSync_Online()
    {
        UpdateLights_RandomisedBuildings();

        UnityWebRequest www = UnityWebRequest.Get(BaseURL + "action=get_lights&realm=" + RealmID);
        www.timeout = 2;
        yield return www.SendWebRequest();
 
        // do nothing on failure at this stage
        if(www.isNetworkError || www.isHttpError) 
        {
        }
        else 
        {
            string getLightResponse = www.downloadHandler.text;

            // received a valid v1 packet
            if (getLightResponse.Length == 53 && getLightResponse[0] == 'V' && getLightResponse[1] == '1')
            {
                int startPos = 3;

                // process the packet
                int[] lightBytes = new int[25];
                for (int byteIndex = 0; byteIndex < 25; ++byteIndex)                
                {
                    lightBytes[byteIndex] = System.Convert.ToInt32(getLightResponse.Substring(startPos, 2), 16);                    
                    startPos += 2;
                }

                // determine the light state
                int buildingIndex = 0;
                int apartmentIndex = 0;
                for (int byteIndex = 0; byteIndex < lightBytes.Length; ++byteIndex)                
                {
                    // process each bit
                    for (int bit = 0; bit < 8; ++bit)
                    {
                        int testValue = 1 << bit;

                        // determine if the light is on
                        bool lightOn = (lightBytes[byteIndex] & testValue) == testValue;

                        // update the apartment
                        if (!LeasableBuildings[buildingIndex].Apartments[apartmentIndex].IsLeased &&
                            (LeasableBuildings[buildingIndex].Apartments[apartmentIndex].IsLightOn != lightOn))
                        {
                            //Debug.Log("Updating light for " + LeasableBuildings[buildingIndex].Apartments[apartmentIndex].UniqueID + " to " + lightOn);
                            LeasableBuildings[buildingIndex].Apartments[apartmentIndex].UpdateLight(lightOn);
                        }
                        
                        ++apartmentIndex;

                        // moved to the next building?
                        if (apartmentIndex >= LeasableBuildings[buildingIndex].Apartments.Count)
                        {
                            apartmentIndex = 0;
                            ++buildingIndex;
                        }
                    }
                }
            }
        }

        CanAdvance = true;

        yield return null;
    }

    IEnumerator WorldSync_Offline()
    {
        int currentUTCHour = System.DateTime.UtcNow.Hour;
        int buildingOffset = RandomisedBuildings.Count;

        UpdateLights_RandomisedBuildings();

        // randomise the lights for the buildings
        for (int buildingIndex = 0; buildingIndex < LeasableBuildings.Count; ++buildingIndex)
        {
            // initialise the RNG to a set value
            Random.InitState((buildingOffset + buildingIndex + 1) * (currentUTCHour + 1) * (currentUTCHour + 1));

            LeasableBuildings[buildingIndex].RandomiseApartmentLights();
        }

        yield return null;
    }    
}
