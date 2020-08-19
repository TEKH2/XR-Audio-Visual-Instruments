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
        chorusProperties.bw = DSP_Properties.ChorusBW;
        chorusProperties.centre = DSP_Properties.ChorusCentre;
        chorusProperties.fb = DSP_Properties.ChorusFB;
        chorusProperties.rate = DSP_Properties.ChorusRate;

        chorusMono.SetProperties(chorusProperties);

    }

    private void OnAudioFilterRead(float[] data, int channels)
    {
        float outputSample;

        for (int dataIndex = 0; dataIndex < data.Length; dataIndex += channels)
        {
            outputSample = chorusMono.Apply(data[dataIndex]);
            data[dataIndex] = outputSample;
        }
    }
}
