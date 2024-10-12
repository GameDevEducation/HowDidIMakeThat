using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public UnityEngine.UI.Text ProgressText;

    public string SceneToLoad;

    public float ActivationDelay = 1f;
    public int ActivationDelayCycles = 5;

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(PerformAsyncLoad(SceneToLoad));
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    IEnumerator PerformAsyncLoad(string sceneName)
    {
        yield return null;

        // start the scene loading but disable it being activated once done
        AsyncOperation loadAO = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        loadAO.allowSceneActivation = false;

        float previousProgress = 0f;

        // wait until the scene is done
        while (!loadAO.isDone)
        {
            // Remap the progress from 0 to 100%
            float progress = Mathf.Clamp01(loadAO.progress / 0.9f);

            // progress has increased
            if (progress > previousProgress)
            {
                previousProgress = progress;

                ProgressText.text += ".";
            }

            // load finished?
            if (loadAO.progress == 0.9f)
            {
                // slight delay for activation
                for (int delayCycle = 0; delayCycle < ActivationDelayCycles; ++delayCycle)
                {
                    ProgressText.text += ".";
                    yield return new WaitForSeconds(ActivationDelay);
                }

                loadAO.allowSceneActivation = true;
            }

            yield return null;
        }
    }
}
