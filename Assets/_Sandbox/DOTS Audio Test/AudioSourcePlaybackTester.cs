using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioSourcePlaybackTester : MonoBehaviour
{
    class GrainTiming
    {
        public float[] _Sample;
        public int _StartSampleIndex;
        public int _PlaybackSampleCounter;
        public bool _IsPlaying = true;
    }

    AudioSource _AudioSource;
    public AudioClip _Clip;
    List<GrainTiming> _Grains = new List<GrainTiming>();
    public int _NumberOfGrains = 20;

    float _SampleLengthInSeconds = 1f;
    float _StartTime = 1;

    int _CurrentDSPSample = 0;
    int _SampleRate;
    

    // Start is called before the first frame update
    void Start()
    {
        _AudioSource = GetComponent<AudioSource>();
        _SampleRate = AudioSettings.outputSampleRate;
        int sampleCount = (int)(_SampleLengthInSeconds * _SampleRate);

        for (int i = 0; i < _NumberOfGrains; i++)
        {           
            float[] sample = new float[sampleCount];
            _Clip.GetData(sample, (int)(_StartTime * _SampleRate));
            _Grains.Add(new GrainTiming() { _Sample = sample, _StartSampleIndex = (int)(i * _SampleLengthInSeconds * _SampleRate * .01f) });
        }

        print(_Grains.Count + "     " + _Grains[4]._Sample[10000] + "     " + sampleCount);
    }

    private void Update()
    {
       // print(_CurrentDSPSample);
    }

    private void OnAudioFilterRead(float[] data, int channels)
    {
        for (int dataIndex = 0; dataIndex < data.Length; dataIndex += channels)
        {
            for (int i = 0; i < _Grains.Count; i++)
            {
                GrainTiming grainData = _Grains[i];

                if (grainData == null)
                    continue;

                if (_CurrentDSPSample >= grainData._StartSampleIndex)
                {
                    //print(_CurrentDSPSample + "    " + grainData._PlaybackSampleIndex);

                    if (grainData._PlaybackSampleCounter >= grainData._Sample.Length)
                    {
                        //print("here");
                        grainData._PlaybackSampleCounter = 0;
                        grainData._StartSampleIndex += grainData._Sample.Length;
                    }
                    else
                    {                        
                        data[dataIndex] += grainData._Sample[grainData._PlaybackSampleCounter];
                        grainData._PlaybackSampleCounter++;
                    }
                }
            }

            _CurrentDSPSample++;
        }
    }
}
