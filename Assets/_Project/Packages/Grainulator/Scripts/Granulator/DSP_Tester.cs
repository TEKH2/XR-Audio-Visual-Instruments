using System.Collections;
using System.Collections.Generic;
using Unity.Entities.UniversalDelegates;
using UnityEngine;

public class DSP_Tester : MonoBehaviour
{
    public AudioSource _AudioClip;

    public FilterProperties _FilterProperties;
    public FilterCoefficients _FilterCoefficients;
    public FilterSignal _FilterSignal;

    // Start is called before the first frame update
    void Start()
    {
        _FilterProperties = new FilterProperties();
        _FilterCoefficients = new FilterCoefficients();
        _FilterSignal = new FilterSignal();
    }

    // Update is called once per frame
    void Update()
    {
        _FilterCoefficients = DSP_Filter.CreateCoefficents(_FilterProperties);
        _FilterSignal.fc = _FilterCoefficients;

        _FilterCoefficients.PrintFC();
    }

    private void OnAudioFilterRead(float[] data, int channels)
    {
        float outputSample = 0;

        for (int dataIndex = 0; dataIndex < data.Length; dataIndex += channels)
        {
            outputSample = _FilterSignal.Apply(data[dataIndex]);

            data[dataIndex] = outputSample;
            data[dataIndex + 1] = outputSample;
        }
    }
}