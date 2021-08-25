using UnityEngine;
using UnityEngine.UI;

// Simply drag this script on a GameObject (preferable on a canvas child object)
[RequireComponent(typeof(Text))]
public class FPSCounter : MonoBehaviour
{
    const float fpsMeasurePeriod = 0.5f;
    private int fpsAccumulator = 0;
    private float fpsNextPeriod = 0;
    private int currentFps;
    const string display = "{0} FPS";
    private Text fpsLabel;


    private void Start()
    {
        fpsNextPeriod = Time.realtimeSinceStartup + fpsMeasurePeriod;
        fpsLabel = GetComponent<Text>();
    }


    private void Update()
    {
        // Measure average frames per second
        fpsAccumulator++;
        if (Time.realtimeSinceStartup > fpsNextPeriod)
        {
            currentFps = (int)(fpsAccumulator / fpsMeasurePeriod);
            fpsAccumulator = 0;
            fpsNextPeriod += fpsMeasurePeriod;
            fpsLabel.text = string.Format(display, currentFps);
        }
    }
}
