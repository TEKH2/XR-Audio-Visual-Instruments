using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChorusTEST : MonoBehaviour
{
    public AudioSource audioSource;
    public DSP_Properties DSP_Properties;
    public ChorusProperties chorusProperties;
    private ChorusMono chorusMono;

    // Start is called before the first frame update
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        chorusProperties = new ChorusProperties();
        chorusMono = new ChorusMono();
    }

    // Update is called once per frame
    void Update()
    {
        chorusProperties.mod = DSP_Properties.ChorusMod;
        chorusProperties.delay = DSP_Properties.ChorusDelay;
        chorusProperties.fb = DSP_Properties.ChorusFB;
        chorusProperties.frequency = DSP_Properties.ChorusFreq;

        chorusMono.SetProperties(chorusProperties);

    }

    private void OnAudioFilterRead(float[] data, int channels)
    {
        float outputSample;

        for (int dataIndex = 0; dataIndex < data.Length; dataIndex += channels)
        {
            outputSample = chorusMono.Apply(data[dataIndex]);
            data[dataIndex] = outputSample;
            data[dataIndex+1] = outputSample;
        }
    }
}
