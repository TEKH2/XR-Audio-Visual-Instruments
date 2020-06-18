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
            float[] samples = new float[audioClip.samples * audioClip.channels];
            _Clips[i].GetData(samples, 0);
            _ClipsDataArray.Add(samples);

            Debug.Log("Clip sample: " + _ClipsDataArray[i][100]);
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
}

[System.Serializable]
public class GrainEmissionProps
{
    public int _ClipIndex = 0;

    // Position
    //---------------------------------------------------------------------
    [Range(0.0f, 1.0f)]
    [SerializeField]
    float _Position = 0;          // from 0 > 1   
    [Range(0.0f, 1.0f)]
    [SerializeField]
    public float _PositionRandom = 0;      // from 0 > 1
    public float Position
    {
        get
        {
            return Mathf.Clamp(_Position + Random.Range(0, _PositionRandom), 0f, 1f);
        }
        set
        {
            _Position = Mathf.Clamp(value, 0, 1);
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
        _Position = pos;
        _Duration = duration;
        _Pitch = pitch;
        _Volume = volume;

        _PositionRandom = posRand;
        _DurationRandom = durationRand;
        _PitchRandom = pitchRand;
        _VolumeRandom = volumeRand;
    }
}

public class Granulator1 : MonoBehaviour
{
    // ------------------------------------ AUDIO VARS
    public AudioClipLibrary _AudioClipLibrary;
    private const int _SampleRate = 44100;
    private float[] _Window;

    // ------------------------------------ GRAIN POOLING
    public GameObject _GrainParentTform;
    public GameObject _GrainPrefab;
    public int _MaxGrains = 100;

    // ------------------------------------ GRAIN EMISSION
    [Range(1.0f, 1000f)]
    public int _TimeBetweenGrains = 20;             // ms
    [Range(0.0f, 1000f)]
    public int _TimeBetweenGrainsRandom = 0;        // ms

    public GrainEmissionProps _EmitGrainProps;

    // Number of samples since last grain, used to provide an interframe offset amount
    private int _SamplesSinceLastGrain;
    private int _EmitterGrainsLastUpdate = 0;

    // Grains that are queued in between frames ready to fire at next update
    private List<GrainData> _QueuedGrainData;

    // Lists to hold the active and inactive grains
    private List<Grain> _ActiveGrainList;
    private List<Grain> _InactiveGrainList;

    float _PrevEmitTime = 0;
    float _NextEmitTime = 0;


    private void Start()
    {
        _AudioClipLibrary.Initialize();

        CreateWindowingLookupTable();

        gameObject.AddComponent<AudioSource>();

        _ActiveGrainList = new List<Grain>();
        _InactiveGrainList = new List<Grain>();
        _QueuedGrainData = new List<GrainData>();
        
        for (int i = 0; i < _MaxGrains; i++)
        {
            GameObject go = Instantiate(_GrainPrefab);
            go.SetActive(true);
            Grain grain = go.GetComponent<Grain>();
            grain.transform.parent = _GrainParentTform.transform;
            _InactiveGrainList.Add(grain);
        }

        _SamplesSinceLastGrain = 0;
    }

    float emitCounter = 0;

    void Update()
    {
        // Remove finished grains from Playing List and add them to Finished list
        for (int i = _ActiveGrainList.Count - 1; i >= 0; i--)
        {
            Grain playingGrain = _ActiveGrainList[i];

            if (!playingGrain._IsPlaying)
            {
                _ActiveGrainList.RemoveAt(i);
                _InactiveGrainList.Add(playingGrain);
                playingGrain.gameObject.SetActive(false);
            }
        }

        int samplesSinceLastUpdate = (int)(Time.deltaTime * _SampleRate);
        int numberOfGrainsToPlay = 0;
        int firstGrainOffset = 0;
        int densityInSamples = (_TimeBetweenGrains + Random.Range(0, _TimeBetweenGrainsRandom)) * (_SampleRate / 1000);

        // testing
        float grainsPerSecond = 1000f / (_TimeBetweenGrains + Random.Range(0, _TimeBetweenGrainsRandom));
        emitCounter += grainsPerSecond * Time.deltaTime;

        //print(timeBetweenGrains + "   " + emitCounter + "   " + Time.time);


        /// If no sample was played last update, adding the previous update's samples count,
        /// AFTER the update is complete, should correctly accumulate the samples since the
        /// last grain playback. Otherwise, if a sample WAS played last update, the sample
        /// offset of that grain is subtracted from the total samples of the previous update.
        /// This provides the correct number of samples since the most recent grain was started.
        if (_EmitterGrainsLastUpdate == 0)
            _SamplesSinceLastGrain += samplesSinceLastUpdate;
        else
            _SamplesSinceLastGrain = samplesSinceLastUpdate - _SamplesSinceLastGrain;

        // If the density of grains minus samples since last grain fits within the
        // estimated time for the this update, calculate number of grains to play this update
        if (densityInSamples - _SamplesSinceLastGrain < samplesSinceLastUpdate)
        {
            // Should always equal one or more
            // TODO: Not sure if the + 1 is correct here. Potentially introducing rounding errors?
            numberOfGrainsToPlay = samplesSinceLastUpdate / densityInSamples + 1;
            
            // Create initial grain offset for this update
            firstGrainOffset = densityInSamples - _SamplesSinceLastGrain;
            
            // Hacky check to avoid offsets lower than 0 (if this occurs, something
            // isn't handled correctly. This is a precaution. Haven't properly checked this yet.
            if (firstGrainOffset < 0)
                firstGrainOffset = 0;
        }

        _EmitterGrainsLastUpdate = numberOfGrainsToPlay;

        int emitted = 0;
        for (int i = 0; i < emitCounter; i++)
        {              
            // Store duration locally because it's used twice
            float duration = _EmitGrainProps.Duration;

            // Calculate timing offset for grain
            int offset = firstGrainOffset + i * densityInSamples;

            // Create temporary grain data object and add it to the playback queue
            GrainData tempGrainData = new GrainData(transform.position, _GrainParentTform.transform, Vector3.right * 2, 1,
                _EmitGrainProps._ClipIndex, duration, offset, _EmitGrainProps.Position, _EmitGrainProps.Pitch, _EmitGrainProps.Volume);

            _QueuedGrainData.Add(tempGrainData);
            emitted++;
        }

        emitCounter -= emitted;

        // If a grain is going to be played this update, set the samples since last grain
        // counter to the sample offset value of the final grain
        if (_QueuedGrainData.Count > 0)
            _SamplesSinceLastGrain = _QueuedGrainData[_QueuedGrainData.Count - 1]._SampleOffset;

        foreach (GrainData grainData in _QueuedGrainData)        
            EmitGrain(grainData);

        _QueuedGrainData.Clear();

        DebugGUI.LogPersistent("Grains per second", "Grains per second: " + grainsPerSecond );
        DebugGUI.LogPersistent("Grains Active/Inactive", "Grains Active/Inactive: " + _ActiveGrainList.Count + "/"+ _MaxGrains);
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
        _InactiveGrainList.Remove(grain);
        // Add grain to active list
        _ActiveGrainList.Add(grain);

        grain.gameObject.SetActive(true);

        // Init grain with data
        grain.Initialise(this, grainData, _AudioClipLibrary._ClipsDataArray[grainData._ClipIndex], _AudioClipLibrary._Clips[grainData._ClipIndex].channels, _AudioClipLibrary._Clips[grainData._ClipIndex].frequency);
        
        _PrevEmitTime = Time.time;
    }

    void CreateWindowingLookupTable()
    {
        _Window = new float[512];

        for (int i = 0; i < _Window.Length; i++)
        {
            _Window[i] = 0.5f * (1 - Mathf.Cos(2 * Mathf.PI * i / _Window.Length));
        }
    }

}