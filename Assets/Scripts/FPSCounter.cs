using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Text))]
public class FPSCounter : MonoBehaviour {

    private Text counterText;
    private double frameDurationAverage = -1;
    private double frameDurationEstimatedAverage = -1;
    private double t = 0.825;
    private ulong frameCount = 0;

    void Start()
    {
        counterText = GetComponent<Text>();
    }

    void Update ()
    {
        double frameDuration = Time.deltaTime;
        ++frameCount;
        
        if(frameDurationAverage < 0)
        {
            frameDurationAverage = frameDuration;
            frameDurationEstimatedAverage = frameDuration;
        }	
        else
        {
            frameDurationEstimatedAverage = t * frameDurationEstimatedAverage + (1 - t) * frameDuration;
            frameDurationAverage = (((double)(frameCount - 1)) / frameCount) * (frameDurationAverage + frameDuration / (frameCount - 1));
        }

        counterText.text = "FPS Counter\n" +
            "Average: " + (1.0 / frameDurationAverage).ToString("F2") + "\n" +
            "Estimated Average: " + (1.0 / frameDurationEstimatedAverage).ToString("F2") + "\n";

    }
}
