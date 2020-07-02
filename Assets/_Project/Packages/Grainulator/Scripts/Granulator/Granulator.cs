using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using Random = UnityEngine.Random;
using System.Linq;


[System.Serializable]
public class AudioClipLibrary
{
    public AudioClip[] _Clips;
    public List<float[]> _ClipsDataArray = new List<float[]>();

    public void Initialize()
    {
        Debug.Log("Initializing clip library.");
        for (int i = 0; i < _Clips.Length; i++)
        {
            AudioClip audioClip = _Clips[i];

            if(audioClip.channels > 1)
            {
                Debug.LogError("Audio clip not mono");
            }

            float[] samples = new float[audioClip.samples];
            _Clips[i].GetData(samples, 0);
            _ClipsDataArray.Add(samples);

            Debug.Log(String.Format("Clip {0}      Samples: {1}        Time length: {2} ", _Clips[i].name, _ClipsDataArray[i].Length, _ClipsDataArray[i].Length / (float)_Clips[i].frequency));
        }
    }
}


public class GrainData
{
    public Vector3 _WorldPos;
    public Transform _ParentTransform;
    public Vector3 _Velocity;
    public float _Mass;

    public int _SampleOffset;
    public float _Duration;
    public float _PlayheadPos;
    public float _Pitch;
    public float _Volume;

    public int _ClipIndex;

    public GrainData() { }
    public GrainData(Vector3 position, Transform parent, Vector3 velocity, float mass, int grainAudioClipIndex,
        float durationInMS, int grainOffsetInSamples, float playheadPosition, float pitch, float volume)
    {
        _WorldPos = position;
        _ParentTransform = parent;
        _Velocity = velocity;
        _Mass = mass;
        _ClipIndex = grainAudioClipIndex;
        _SampleOffset = grainOffsetInSamples;
        _Duration = durationInMS;
        _PlayheadPos = playheadPosition;
        _Pitch = pitch;
        _Volume = volume;
    }

    public void Initialize(Vector3 position, Transform parent, Vector3 velocity, float mass, int grainAudioClipIndex,
        float durationInMS, int grainOffsetInSamples, float playheadPosition, float pitch, float volume)
    {
        _WorldPos = position;
        _ParentTransform = parent;
        _Velocity = velocity;
        _Mass = mass;
        _ClipIndex = grainAudioClipIndex;
        _SampleOffset = grainOffsetInSamples;
        _Duration = durationInMS;
        _PlayheadPos = playheadPosition;
        _Pitch = pitch;
        _Volume = volume;
    }
}

[System.Serializable]
public class GrainEmissionProps
{
    public int _ClipIndex = 0;

    // Position
    //---------------------------------------------------------------------
    [Range(0.0f, 1.0f)]
    [SerializeField]
    float _PlayheadPos = 0;          // from 0 > 1   
    [Range(0.0f, 1.0f)]
    [SerializeField]
    public float _PositionRandom = 0;      // from 0 > 1
    public float Position
    {
        get
        {
            return Mathf.Clamp(_PlayheadPos + Random.Range(0, _PositionRandom), 0f, 1f);
        }
        set
        {
            _PlayheadPos = Mathf.Clamp(value, 0, 1);
        }
    }

    // Duration
    //---------------------------------------------------------------------
    [Range(2.0f, 1000f)]
    [SerializeField]
    int _Duration = 100;       // ms
    [Range(0.0f, 1000f)]
    [SerializeField]
    int _DurationRandom = 0;     // ms
    public float Duration
    {
        get
        {
            return Mathf.Clamp(_Duration + Random.Range(0, _DurationRandom), 2, 1000);
        }
        set
        {
            _Duration = (int)Mathf.Clamp(value, 2, 1000);
        }
    }

    // Pitch
    //---------------------------------------------------------------------
    [Range(0.1f, 5f)]
    [SerializeField]
    float _Pitch = 1;
    [Range(0.0f, 1f)]
    [SerializeField]
    float _PitchRandom = 0;
    public float Pitch
    {
        get
        {
            return Mathf.Clamp(_Pitch + Random.Range(-_PitchRandom, _PitchRandom), 0.1f, 5f);
        }
        set
        {
            _Pitch = (int)Mathf.Clamp(value, 0.1f, 5f);
        }
    }

    // Volume
    //---------------------------------------------------------------------
    [Range(0.0f, 2.0f)]
    [SerializeField]
    float _Volume = 1;          // from 0 > 1
    [Range(0.0f, 1.0f)]
    [SerializeField]
    float _VolumeRandom = 0;      // from 0 > 1
    public float Volume
    {
        get
        {
            return Mathf.Clamp(_Volume + Random.Range(-_VolumeRandom, _VolumeRandom),0f, 3f);
        }
        set
        {
            _Volume = (int)Mathf.Clamp(value, 0f, 3f);
        }
    }

    public GrainEmissionProps(float pos, int duration, float pitch, float volume, 
        float posRand = 0, int durationRand = 0, float pitchRand = 0, float volumeRand = 0)
    {
        _PlayheadPos = pos;
        _Duration = duration;
        _Pitch = pitch;
        _Volume = volume;

        _PositionRandom = posRand;
        _DurationRandom = durationRand;
        _PitchRandom = pitchRand;
        _VolumeRandom = volumeRand;
    }
}

public class Granulator : MonoBehaviour
{
    // ------------------------------------ AUDIO VARS
    public AudioClipLibrary _AudioClipLibrary;
    private int _SampleRate;

    // ------------------------------------ GRAIN POOLING
    public GameObject _GrainPrefab;
    public int _MaxGrains = 100;

    // ------------------------------------ GRAIN EMISSION
    [Range(1.0f, 1000f)]
    public int _Cadence = 20;             // ms
    [Range(0.0f, 1000f)]
    public int _CadenceRandom = 0;        // ms

    public GrainEmissionProps _EmitGrainProps;

    double _PrevEmissionSampleIndex = 0;
    List<double> _SpawnAtSampleTimes = new List<double>();

    // Grains that are queued in between frames ready to fire at next update
    private List<GrainData> _QueuedGrainData;

    // Lists to hold the active and inactive grains
    private List<Grain> _ActiveGrainList;
    private List<Grain> _InactiveGrainList;

    private List<GrainData> _ActiveGrainDataList = new List<GrainData>();
    private List<GrainData> _InactiveGrainDataList = new List<GrainData>();

    public bool _DebugLog = false;
    int _DebugFrameCounter = 0;

    private void Start()
    {
        _SampleRate = AudioSettings.outputSampleRate;
        _AudioClipLibrary.Initialize();

        _ActiveGrainList = new List<Grain>();
        _InactiveGrainList = new List<Grain>();
        _QueuedGrainData = new List<GrainData>();
        
        for (int i = 0; i < _MaxGrains; i++)
        {
            GameObject go = Instantiate(_GrainPrefab);
            go.SetActive(true);
            Grain grain = go.GetComponent<Grain>();
            grain.transform.parent = transform;
            grain.transform.localPosition = Vector3.zero;

            int index = i;
            grain._Index = index;

            _InactiveGrainList.Add(grain);

            _InactiveGrainDataList.Add(new GrainData());
            grain.Activate(false);
        }
    }

    void Update()
    {
        //------------------------------------------ CLEAN UP GRAINS THAT ARE FINISHED
        // Remove finished grains from Playing List and add them to Finished list
        for (int i = _ActiveGrainList.Count - 1; i >= 0; i--)
        {
            Grain playingGrain = _ActiveGrainList[i];

            if (!playingGrain._IsPlaying)
            {
                _ActiveGrainList.RemoveAt(i);
                _InactiveGrainList.Add(playingGrain);

                _ActiveGrainDataList.Remove(playingGrain._GrainData);
                _InactiveGrainDataList.Add(playingGrain._GrainData);

                playingGrain.Activate(false);
            }
        }

        //------------------------------------------ UPDATE GRAIN SPAWN LIST
        // Current sample we are up to in time
        double frameSampleIndex = AudioSettings.dspTime;
        // Calculate random sample rate
        float randomSampleBetweenGrains = _SampleRate * ((_Cadence + Random.Range(0, _CadenceRandom)) * .001f);
        // Find sample that next grain is emitted at
        double nextEmitSampleIndex = _PrevEmissionSampleIndex + randomSampleBetweenGrains;

        // fill the spawn sample time list while the next emit sample index 
        while(nextEmitSampleIndex <= frameSampleIndex)
        {
            // add to spawn sample list
            _SpawnAtSampleTimes.Add(nextEmitSampleIndex);

            // recalculate random sample rate
            _PrevEmissionSampleIndex = nextEmitSampleIndex;
            randomSampleBetweenGrains = _SampleRate * ((_Cadence + Random.Range(0, _CadenceRandom)) * .001f);
            nextEmitSampleIndex = _PrevEmissionSampleIndex + randomSampleBetweenGrains;
        }


        //------------------------------------------ GENERATE GRAIN DATA      
        for (int i = 0; i < _SpawnAtSampleTimes.Count; i++)
        {
            // Calculate timing offset for grain
            int offset = (int)(frameSampleIndex - _SpawnAtSampleTimes[i]);

            if (_InactiveGrainDataList.Count > 0)
            {
                GrainData tempGrainData = _InactiveGrainDataList[0];
                _InactiveGrainDataList.Remove(tempGrainData);
                _ActiveGrainDataList.Add(tempGrainData);

                // Create temporary grain data object and add it to the playback queue
                tempGrainData.Initialize(transform.position, transform, Vector3.zero, 0,
                    _EmitGrainProps._ClipIndex, _EmitGrainProps.Duration, offset, _EmitGrainProps.Position, _EmitGrainProps.Pitch, _EmitGrainProps.Volume);

                _QueuedGrainData.Add(tempGrainData);
            }
            else
            {
                if(_DebugLog)
                    print("Not enough grain datas to spawn a new grain.");
            }
        }

        //------------------------------------------ SPAWN GRAINS 
        foreach (GrainData grainData in _QueuedGrainData)        
            EmitGrain(grainData);

        //------------------------------------------ CLEAN UP
        _QueuedGrainData.Clear();
        _SpawnAtSampleTimes.Clear();

        //------------------------------------------ DEBUG
        DebugGUI.LogPersistent("Grains per second", "Grains per second: " + 1000 / _Cadence);
        DebugGUI.LogPersistent("Grains Active/Inactive", "Grains Active/Inactive: " + _ActiveGrainList.Count + "/"+ _MaxGrains);
        DebugGUI.LogPersistent("Samples per grain", "Samples per grain: " + _SampleRate * (_EmitGrainProps.Duration * .001f));
        DebugGUI.LogPersistent("Samples bewteen grain", "Samples between grains: " + _SampleRate * (_Cadence * .001f));
        DebugGUI.LogPersistent("CurrentSample", "Current Sample: " + Time.time * _SampleRate);

        _DebugFrameCounter++;
    }

    public void EmitGrain(GrainData grainData)
    {
        if (_InactiveGrainList.Count <= 0)
        {
            print("No inactive grains, trying to spawn too quickly. Potentially boost max grains. Active/Inactive: " + _ActiveGrainList.Count + " / " + _InactiveGrainList.Count);
            return;
        }

        // Get grain from inactive list and remove from list
        Grain grain = _InactiveGrainList[0];
        grain.transform.position = grainData._WorldPos;
        _InactiveGrainList.Remove(grain);
        // Add grain to active list
        _ActiveGrainList.Add(grain);

        //grain.gameObject.SetActive(true);
        if (_DebugLog)
        {
            Debug.Log(String.Format("Frame: {0}  Offset: {1}", _DebugFrameCounter, grainData._SampleOffset));
        }

        grain.Activate(true);
        // Init grain with data
        grain.Initialise(grainData, _AudioClipLibrary._ClipsDataArray[grainData._ClipIndex], _AudioClipLibrary._Clips[grainData._ClipIndex].frequency, _DebugLog, Time.time * _SampleRate);        
    }
}