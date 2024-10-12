using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public string SceneToLoad;
    private AsyncOperation LevelLoadAO;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    IEnumerator PerformAsyncLoad(string sceneName)
    {
        yield return null;

        // start the scene loading but disable it being activated once done
        LevelLoadAO = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        LevelLoadAO.allowSceneActivation = false;

        // wait until the scene is done
        while (!LevelLoadAO.isDone)
        {
            // remap the progress from 0 to 90 to 0 to 100%
            float progress = Mathf.Clamp01(LevelLoadAO.progress / 0.9f);

            // load finished?
            if (LevelLoadAO.progress == 0.9f)
            {
                LevelLoadAO.allowSceneActivation = true;
            }

            yield return null;
        }
    }

    public void OnPlay()
    {
        StartCoroutine(PerformAsyncLoad(SceneToLoad));
    }

    public void OnExit()
    {
        Application.Quit();
    }
}
