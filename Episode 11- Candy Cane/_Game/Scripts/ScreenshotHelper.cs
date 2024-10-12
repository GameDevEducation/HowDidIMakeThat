using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScreenshotHelper : MonoBehaviour
{
    public Transform CameraMarker;
    public bool TakeScreenshot = false;

    // Start is called before the first frame update
    void Start()
    {
        TakeScreenshot = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ScreenCapture.CaptureScreenshot("Screenshot" + Time.frameCount + ".png", 2);
        }

        if (TakeScreenshot)
        {
            TakeScreenshot = false;

            Camera.main.transform.position = CameraMarker.position;
            Camera.main.transform.rotation = CameraMarker.rotation;

            ScreenCapture.CaptureScreenshot("Screenshot" + Time.frameCount + ".png", 2);
        }
    }
}
