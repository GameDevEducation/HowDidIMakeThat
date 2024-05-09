using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using Fungus;
using UnityEngine.Events;
using TMPro;

public class ScheherazadeManager : MonoBehaviour, IPausable
{
    [SerializeField] Writer LinkedWriter;
    [SerializeField] WriterAudio LinkedWriterAudio;
    [SerializeField] ScheherazadeResponses ResponseSelector;
    [SerializeField] TextMeshProUGUI DialogueDisplay;
    [SerializeField] int EndThreshold = 3;
    [SerializeField] UnityEvent OnWithinEndThreshold = new UnityEvent();
    [SerializeField] UnityEvent OnStartedFinalLine = new UnityEvent();
    [SerializeField] UnityEvent OnTaleFinished = new UnityEvent();

    [SerializeField] TMP_FontAsset DefaultDialogueFont;
    [SerializeField] TMP_FontAsset DefaultResponseFont;
    [SerializeField] TMP_FontAsset OpenDyslexicFont;

    [SerializeField] TextMeshProUGUI MoreStoriesLeftMessage;
    [SerializeField] TextMeshProUGUI NoMoreStoriesMessage;

    [SerializeField] float ThunderInterval = 45f;
    [SerializeField] float ThunderVariation = 15f;
    [SerializeField] float ThunderHeight = 50f;
    [SerializeField] float ThunderRange = 50f;
    [SerializeField] GameObject ThunderPrefab;
    float NextThunderTime = 0f;

    [SerializeField] [Range(0f, 1f)] float MinRainIntensity = 0.25f;
    [SerializeField] [Range(0f, 1f)] float MaxRainIntensity = 0.75f;
    [SerializeField] int MinRainSpawnRate = 250;
    [SerializeField] int MaxRainSpawnRate = 1000;
    [SerializeField] UnityEngine.VFX.VisualEffect RainEffect;
    [SerializeField] AnimationCurve StormCurve;
    [SerializeField] float StormDuration = 5 * 60f;
    float StormProgress = 0f;
    float CurrentRainIntensity = 0.25f;

    [SerializeField] GameObject DriverSoundEmitter;

    [SerializeField] bool TestMode = false;
    [SerializeField] string DEBUG_ForceTaleID = "1";

    Dictionary<string, Tale> TaleRegistry = new Dictionary<string, Tale>();
    Tale ActiveTale;
    int CurrentLine = -1;

#if UNITY_EDITOR
    int DEBUG_CurrentResponse = 0;
#endif // UNITY_EDITOR

    static string _ConversationRoot = null;
    static string ConversationRoot
    {
        get
        {
            if (_ConversationRoot == null)
                _ConversationRoot = System.IO.Path.Combine(Application.streamingAssetsPath, "Conversations");

            return _ConversationRoot;
        }
    }

    public static string GetTaleKey(Tale tale)
    {
        return $"Conversation_{tale.UniqueID}";
    }

    public static bool IsStoryInProgress
    {
        get
        {
            int numComplete = 0;
            int numIncomplete = 0;

            foreach (var candidate in Directory.EnumerateFiles(ConversationRoot))
            {
                if (Path.GetExtension(candidate).ToUpper() != ".XML")
                    continue;

                var tale = LoadTale(candidate);
                if (tale.Type == ETaleType.Template)
                    continue;

                var taleKey = GetTaleKey(tale);
                if (PlayerPrefs.GetInt(taleKey, 0) == 0)
                    ++numIncomplete;
                else
                    ++numComplete;
            }       

            return numComplete > 0 && numIncomplete > 0;
        }
    }

    public static bool IsStoryComplete
    {
        get
        {
            foreach (var candidate in Directory.EnumerateFiles(ConversationRoot))
            {
                if (Path.GetExtension(candidate).ToUpper() != ".XML")
                    continue;

                var tale = LoadTale(candidate);
                if (tale.Type == ETaleType.Template)
                    continue;

                var taleKey = GetTaleKey(tale);
                if (PlayerPrefs.GetInt(taleKey, 0) == 0)
                    return false;
            }

            return true;
        }
    }

    public static void ClearAllPlayed()
    {
        foreach (var candidate in Directory.EnumerateFiles(ConversationRoot))
        {
            if (Path.GetExtension(candidate).ToUpper() != ".XML")
                continue;

            var tale = LoadTale(candidate);
            if (tale.Type == ETaleType.Template)
                continue;

            var taleKey = GetTaleKey(tale);
            if (PlayerPrefs.HasKey(taleKey))
                PlayerPrefs.DeleteKey(taleKey);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        AudioShim.StartRain();

        NextThunderTime = Random.Range(0f, ThunderInterval + Random.Range(-ThunderVariation, ThunderVariation));

        RefreshSettings();

        SyncRainIntensity();

        string nextTaleToPlay = null;
        string firstTaleID = null;

        foreach (var candidate in Directory.EnumerateFiles(ConversationRoot))
        {
            if (Path.GetExtension(candidate).ToUpper() != ".XML")
                continue;

            var tale = LoadTale(candidate);
            if (tale.Type == ETaleType.Template)
                continue;

            TaleRegistry.Add(tale.UniqueID, tale);

            if (nextTaleToPlay == null && PlayerPrefs.GetInt(GetTaleKey(tale), 0) == 0)
                nextTaleToPlay = tale.UniqueID;

            if (firstTaleID == null)
                firstTaleID = tale.UniqueID;
        }

        // all tales played
        if (nextTaleToPlay == null)
            nextTaleToPlay = firstTaleID;

#if UNITY_EDITOR
        if (TestMode)
        {
            nextTaleToPlay = DEBUG_ForceTaleID;
            DEBUG_CurrentResponse = 0;
        }
#endif // UNITY_EDITOR

        LinkedWriter.AttachedWriterAudio = LinkedWriterAudio;

        ActiveTale = TaleRegistry[nextTaleToPlay];

        PauseManager.Instance.RegisterPausable(this);
    }

    void OnDestroy()
    {
        PauseManager.Instance.DeregisterPausable(this);
    }

    public bool OnPauseRequested()
    {
        return true;
    }

    public bool OnResumeRequested()
    {
        return true;
    }

    public void OnPause()
    {
        LinkedWriter.Paused = true;
    }

    public void OnResume()
    {
        LinkedWriter.Paused = false;

        RefreshSettings();
    }

    void SyncRainIntensity()
    {
        AkSoundEngine.SetRTPCValue(AK.GAME_PARAMETERS.RAININTENSITY, CurrentRainIntensity * 100f);
        RainEffect.SetInt("Spawn Rate", Mathf.RoundToInt(Mathf.Lerp(MinRainSpawnRate, MaxRainSpawnRate, CurrentRainIntensity)));
    }

    void RefreshSettings()
    {
        LinkedWriter.SetWritingSpeed(SettingsManager.Settings.Gameplay.TypingSpeed);
        if (SettingsManager.Settings.Gameplay.UseOpenDyslexic)
        {
            DialogueDisplay.font = OpenDyslexicFont;
            ResponseSelector.SetFonts(OpenDyslexicFont);
        }
        else
        {
            DialogueDisplay.font = DefaultDialogueFont;
            ResponseSelector.SetFonts(DefaultResponseFont);
        }
    }

    // Update is called once per frame
    void Update()
    {
        StormProgress = (StormProgress + Time.deltaTime / StormDuration) % 1;
        CurrentRainIntensity = Mathf.Lerp(MinRainIntensity, MaxRainIntensity, StormCurve.Evaluate(StormProgress));
        SyncRainIntensity();

        #if UNITY_EDITOR
        if (UnityEngine.InputSystem.Keyboard.current.tabKey.wasPressedThisFrame)
            ScreenCapture.CaptureScreenshot("Screenshot_" + Time.frameCount + ".png", 2);

        if (TestMode)
        {
            if (UnityEngine.InputSystem.Keyboard.current.enterKey.wasPressedThisFrame)
            {
                StartCoroutine(PerformLine(ActiveTale.Lines[CurrentLine]));
            }
        }
        #endif // UNITY_EDITOR

        // generate thunder?
        NextThunderTime -= Time.deltaTime;
        if (NextThunderTime <= 0f)
        {
            NextThunderTime = ThunderInterval + Random.Range(-ThunderVariation, ThunderVariation);

            // pick a location
            float angle = Random.Range(0f, 2f * Mathf.PI);
            Vector3 thunderPosition = new Vector3(ThunderRange * Mathf.Cos(angle),
                                                  ThunderHeight,
                                                  ThunderRange * Mathf.Sin(angle));

            // spawn the thunder prefab
            Instantiate(ThunderPrefab, thunderPosition + Camera.main.transform.position, Quaternion.identity);
        }
    }

    static Tale LoadTale(string taleName)
    {
        XmlSerializer serializer = new XmlSerializer(typeof(Tale));

        using (FileStream stream = File.OpenRead(System.IO.Path.Combine(ConversationRoot, taleName)))
        {
            return serializer.Deserialize(stream) as Tale;
        }
    }

    public void StartTale()
    {
        CurrentLine = 0;

        StartCoroutine(PerformLine(ActiveTale.Lines[CurrentLine]));
    }

    IEnumerator PerformLine(TaleLine line)
    {
#if UNITY_EDITOR
        if (TestMode)
        {
            DialogueDisplay.text = line.Text;
            OnPerformedDriverLine(line);

            if (DEBUG_CurrentResponse == 0)
                ResponseSelector.OnOption1();
            else if (DEBUG_CurrentResponse == 1)
                ResponseSelector.OnOption2();
            else
                ResponseSelector.OnOption3();

            yield break;
        }
#endif // UNITY_EDITOR

        AkSoundEngine.PostEvent($"Play_C{ActiveTale.UniqueID}L{(CurrentLine + 1)}", DriverSoundEmitter);

        string lineText = line.Text + "{w=" + line.PromptDelay + "}";
        yield return StartCoroutine(LinkedWriter.Write(lineText, true, false, true, false, null, delegate { OnPerformedDriverLine(line); }));
    }

    void OnPerformedDriverLine(TaleLine line)
    {
        string agreeText = string.Empty;
        string disagreeText = string.Empty;
        string silentText = string.Empty;
        EResponseType defaultResponse = EResponseType.Silent;
        float defaultSelectionTime = -1f;

        // extract the line data
        foreach(var response in line.Responses)
        {
            if (response.Type == EResponseType.Agree)
                agreeText = response.Prompt;
            else if (response.Type == EResponseType.Disagree)
                disagreeText = response.Prompt;
            else if (response.Type == EResponseType.Silent)
                silentText = response.Prompt;

            if (response.IsDefault)
            {
                defaultResponse = response.Type;
                defaultSelectionTime = response.WaitTime;
            }
        }

        ResponseSelector.ShowOptions(EResponseType.Agree, agreeText, 
                                     EResponseType.Disagree, disagreeText, 
                                     EResponseType.Silent, silentText,
                                     defaultResponse, defaultSelectionTime,
                                     OnPassengerSelectedPrompt);
    }

    void OnPassengerSelectedPrompt(EResponseType responseType)
    {
        // attempt to find the drivers response
        string driverResponse = "{w=" + ActiveTale.Lines[CurrentLine].ResponseDelay + "}";
        int responseIndex = 1;
        foreach(var response in ActiveTale.Lines[CurrentLine].Responses)
        {
            if (response.Type == responseType)
            {
                driverResponse += response.Response;
                break;
            }
            ++responseIndex;
        }

#if UNITY_EDITOR
        if (TestMode)
        {
            DialogueDisplay.text += driverResponse;
            DEBUG_CurrentResponse = (DEBUG_CurrentResponse + 1) % 3;

            if (DEBUG_CurrentResponse == 0)
                ++CurrentLine;

            if (CurrentLine >= ActiveTale.Lines.Length)
                Debug.Log("Tale Completed");

            return;
        }
#endif // UNITY_EDITOR

        AkSoundEngine.PostEvent($"Play_C{ActiveTale.UniqueID}L{(CurrentLine + 1)}R{responseIndex}", DriverSoundEmitter);

        StartCoroutine(LinkedWriter.Write(driverResponse, false, false, true, false, null, delegate { OnPerformedDriverResponse(); }));
    }


    void OnPerformedDriverResponse()
    {
#if UNITY_EDITOR
        if (TestMode)
            return;
#endif // UNITY_EDITOR

        ++CurrentLine;
        if (CurrentLine < ActiveTale.Lines.Length)
        {
            Invoke("StartNextLine", ActiveTale.Lines[CurrentLine - 1].NextLineDelay);

            // send the end notification
            int linesRemaining = ActiveTale.Lines.Length - CurrentLine;
            if (linesRemaining == EndThreshold)
                OnWithinEndThreshold.Invoke();
        }
        else
            TaleCompleted();
    }

    void StartNextLine()
    {
        if (CurrentLine == (ActiveTale.Lines.Length - 1))
            OnStartedFinalLine.Invoke();

        StartCoroutine(PerformLine(ActiveTale.Lines[CurrentLine]));
    }

    void TaleCompleted()
    {
        // mark as played
        PlayerPrefs.SetInt(GetTaleKey(ActiveTale), 1);
        PlayerPrefs.Save();

        if (IsStoryComplete)
        {
            NoMoreStoriesMessage.gameObject.SetActive(true);
            MoreStoriesLeftMessage.gameObject.SetActive(false);
        }
        else
        {
            NoMoreStoriesMessage.gameObject.SetActive(false);
            MoreStoriesLeftMessage.gameObject.SetActive(true);            
        }

        ResponseSelector.gameObject.SetActive(false);
        DialogueDisplay.gameObject.SetActive(false);

        OnTaleFinished.Invoke();
    }
}
