using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugGUI_Granulator : MonoBehaviour
{
   GrainManager _Granulator;


    void Awake()
    {
        // Set up graph properties using our graph keys
        DebugGUI.SetGraphProperties("smoothFrameRate", "SmoothFPS", 0, 120, 0, new Color(0, 1, 1), false);
        DebugGUI.SetGraphProperties("avLayering", "Av. Layering", 0, 20, 0, new Color(1, 0, 0), true);

        DebugGUI.SetGraphProperties("activeEmitters", "Active emitters", 0, 20, 1, new Color(0, 1, 1), true);
        DebugGUI.SetGraphProperties("activeAudioOutputs", "Audio Outputs", 0, 20, 1, new Color(1, 1, 0), true);

        DebugGUI.SetGraphProperties("grainLatency", "Latency per grains", -20, 20, 2, new Color(1, 0, 0), false);
        DebugGUI.SetGraphProperties("grainLatencyCenter", "", -20, 20, 2, new Color(0, 1, 1), false);
    }

    private void Start()
    {
        _Granulator = GrainManager.Instance;
    }

    void Update()
    {
        //// Manual persistent logging
        DebugGUI.LogPersistent("smoothFrameRate", "SmoothFPS: " + (1f / Time.deltaTime).ToString("F1"));
        DebugGUI.LogPersistent("avLayering", "Av. Layering: " + _Granulator._AvLayeredSamples.ToString("F1"));

        if (Time.smoothDeltaTime != 0)
            DebugGUI.Graph("smoothFrameRate", 1 / Time.smoothDeltaTime);

        DebugGUI.Graph("avLayering", _Granulator._AvLayeredSamples);

        DebugGUI.Graph("activeEmitters", _Granulator._ActiveEmitters);
        DebugGUI.Graph("activeAudioOutputs", _Granulator.ActiveSpeakers.Count);
    }

    public void LogLatency(float latency)
    {
        DebugGUI.Graph("grainLatency", latency);
        DebugGUI.Graph("grainLatencyCenter", 0);
    }

    void OnDestroy()
    {
        // Clean up our logs and graphs when this object is destroyed
        DebugGUI.RemoveGraph("smoothFrameRate");
    }
}
