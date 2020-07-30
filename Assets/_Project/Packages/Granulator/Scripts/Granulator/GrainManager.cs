using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using Random = UnityEngine.Random;
using System.Linq;

[RequireComponent(typeof(AudioSource))]
public class GrainManager : MonoBehaviour
{
    #region VARIABLES
    public static GrainManager Instance;
    AudioListener _AudioListener;

    // ------------------------------------ AUDIO VARS
    public AudioClipLibrary _AudioClipLibrary;
    private int _SampleRate;
    //public FilterProperties _FilterProperties;
    //private FilterCoefficients _FilterCoefficients;

    // ------------------------------------ GRAIN AUDIO SOURCES

    public GrainSpeaker _GrainAudioSourcePrefab;

    public List<GrainSpeaker> _ActiveOutputs = new List<GrainSpeaker>();
    public List<GrainSpeaker> _InactiveOutputs = new List<GrainSpeaker>();

    // ------------------------------------ GRAIN EMITTER PROPS  
    public List<GrainEmitter> _AllGrainEmitters = new List<GrainEmitter>();

    int _CurrentDSPSample = 0;
    public float _EmissionLatencyMS = 80;
    int EmissionLatencyInSamples { get { return (int)(_EmissionLatencyMS * _SampleRate * .001f); } }

    // maximum distance an emitter can be from an audio source
    public float _MaxDistBetweenEmitterAndSource = 1;
    // Maximum allowed audio sources
    public int _MaxAudioSources = 10;
    int TotalAudioSourceCount { get { return _InactiveOutputs.Count + _ActiveOutputs.Count;} }

    public AnimationCurve _WindowingCurve;

    [Header("DEBUG")]
    public bool _DebugLog = false;

    public bool _DEBUG_TraditionalWindowing = false;

    [Range(0f, 1f)]
    public float _DebugNorm = 0;

    public Transform _DebugTransform;
    public float _AudioOutputDeactivationDistance = 5;

    public float _LayeredSamples;
    public float _AvLayeredSamples;
    public int _ActiveEmitters;
    #endregion

    private void Awake()
    {
        Instance = this;

        _AudioListener = FindObjectOfType<AudioListener>();

        // Sample rate from teh audio settings
        _SampleRate = AudioSettings.outputSampleRate;

        // Init clip library
        _AudioClipLibrary.Initialize();

        for (int i = 0; i < 1; i++)
        {
            InstantiateNewAudioSource(Vector3.zero, false);
        }

        print("Granualtor Manager initialized. Grains created: " + _InactiveOutputs.Count);

        _AllGrainEmitters = FindObjectsOfType<GrainEmitter>().ToList();
    }

    void Update()
    {
        _ActiveEmitters = 0;
        float layeredSamplesThisFrame = 0;

        // UPDATE ACTIVE OUTPUTS AND CHECK IF OUT OF RANGE
        int sampleIndexMax = _CurrentDSPSample + EmissionLatencyInSamples;
       
        for (int i = _ActiveOutputs.Count - 1; i >= 0; i--)
        {
            float dist = Vector3.Distance(_AudioListener.transform.position, _ActiveOutputs[i].transform.position);

            // If out of range remove from the active output list
            if (dist > _AudioOutputDeactivationDistance)
            {
                GrainSpeaker output = _ActiveOutputs[i];
                output.Deactivate();
                _ActiveOutputs.Remove(output);
                _InactiveOutputs.Add(output);
            }
            //  else update the output
            else
            {
                _ActiveOutputs[i].ManualUpdate(sampleIndexMax, _SampleRate);
                layeredSamplesThisFrame += _ActiveOutputs[i]._LayeredSamples;
                _ActiveEmitters += _ActiveOutputs[i]._AttachedGrainEmitters.Count;
            }
        }

        // Performance metrics
        _LayeredSamples = Mathf.Lerp(_LayeredSamples, layeredSamplesThisFrame, Time.deltaTime * 4);
        if (_ActiveOutputs.Count == 0)
            _AvLayeredSamples = 0;
        else
            _AvLayeredSamples = _LayeredSamples / (float)_ActiveOutputs.Count;

        if (_DebugLog)
            print("Active outputs: " + _ActiveOutputs.Count + "   Layered Samples: " + _LayeredSamples + "   Av layered Samples: " + _LayeredSamples/(float)_ActiveOutputs.Count);


        // CHECK IF INACTIVE SOURCES ARE IN RANGE
        // Order by distance
        _AllGrainEmitters = _AllGrainEmitters.OrderBy(x => Vector3.SqrMagnitude(_AudioListener.transform.position - x.transform.position)).ToList();
        for (int i = 0; i < _AllGrainEmitters.Count; i++)
        {
            if (_AllGrainEmitters[i]._Active)
                continue;

            float dist = Vector3.Distance(_AudioListener.transform.position, _AllGrainEmitters[i].transform.position);

            if (dist < _AudioOutputDeactivationDistance)
            {
                TryAssignEmitterToSource(_AllGrainEmitters[i]);
            }
        }
    }

    void TryAssignEmitterToSource(GrainEmitter emitter)
    {
        // had to initialize it to something TODO fix pattern
        GrainSpeaker audioSource = _GrainAudioSourcePrefab;
        bool sourceFound = false;

        // ------------------------------  FIND CLOSE ACTIVE SOURCE
        float closestDist = _MaxDistBetweenEmitterAndSource;      
        for (int i = 0; i < _ActiveOutputs.Count; i++)
        {
            float dist = Vector3.Distance(emitter.transform.position, _ActiveOutputs[i].transform.position);
            if (dist <= closestDist)
            {
                closestDist = dist;
                audioSource = _ActiveOutputs[i];
                sourceFound = true;
            }
        }
        
        if (!sourceFound)
        {
            // ------------------------------  FIND INACTIVE SOURCE
            if (_InactiveOutputs.Count > 0)
            {
                audioSource = _InactiveOutputs[0];
                audioSource.transform.position = emitter.transform.position;
                audioSource.gameObject.SetActive(true);
                audioSource._CurrentDSPSampleIndex = _CurrentDSPSample;

                _InactiveOutputs.RemoveAt(0);
                _ActiveOutputs.Add(audioSource);

                sourceFound = true;
            }
            // ------------------------------  CREATE NEW SOURCE
            else
            {
                audioSource = InstantiateNewAudioSource(emitter.transform.position);

                if (audioSource != null)
                    sourceFound = true;
            }
        }

        if(sourceFound)
        {
            audioSource.AttachEmitter(emitter);
        }
    }

    GrainSpeaker InstantiateNewAudioSource(Vector3 pos, bool addToActiveList = true)
    {
        if (TotalAudioSourceCount == _MaxAudioSources)
            return null;

        GrainSpeaker audioSource = Instantiate(_GrainAudioSourcePrefab, transform);
        audioSource.name = "Grain audio source " + TotalAudioSourceCount;
        audioSource.transform.position = pos;
        audioSource._CurrentDSPSampleIndex = _CurrentDSPSample;

        if (addToActiveList)
        {
            _ActiveOutputs.Add(audioSource);
            audioSource.gameObject.SetActive(true);
        }
        else
        {
            _InactiveOutputs.Add(audioSource);
            audioSource.gameObject.SetActive(false);
        }

        print("New grain audio source added - Total: " + TotalAudioSourceCount + " / " + _MaxAudioSources);

        return audioSource;
    }

    public void AddNewEmitter(GrainEmitter emitter)
    {
        if (!_AllGrainEmitters.Contains(emitter))
            _AllGrainEmitters.Add(emitter);
    }

    void OnAudioFilterRead(float[] data, int channels)
    {
        for (int dataIndex = 0; dataIndex < data.Length; dataIndex += channels)
        {
            _CurrentDSPSample++;
        }
    }

    void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(_AudioListener.transform.position, _AudioOutputDeactivationDistance);

            for (int i = 0; i < _ActiveOutputs.Count; i++)
            {
                Gizmos.DrawLine(_AudioListener.transform.position, _ActiveOutputs[i].transform.position);
            }
        }
    }
}


#region Grain data classes
public class GrainData
{
    public int _StartDSPSampleIndex;

    // Optimum 10ms - 60ms
    public float _Duration;
    public float _PlayheadPos;
    public float _Pitch;
    public float _Volume;
    public FilterCoefficients _Coefficients;

    public int _ClipIndex;

    public int _StartSampleIndex;

    public GrainData() { }
    public GrainData(int grainAudioClipIndex,
        float durationInMS, float playheadPosition, float pitch, float volume, FilterCoefficients fc, int startSampleIndex)
    {
        _ClipIndex = grainAudioClipIndex;
        _Duration = durationInMS;
        _PlayheadPos = playheadPosition;
        _Pitch = pitch;
        _Volume = volume;
        _Coefficients = fc;
        _StartSampleIndex = startSampleIndex;
    }

    public void Initialize(int grainAudioClipIndex,
        float durationInMS, float playheadPosition, float pitch, float volume, FilterCoefficients fc, int startSampleIndex)
    {
        _ClipIndex = grainAudioClipIndex;
        _Duration = durationInMS;
        _PlayheadPos = playheadPosition;
        _Pitch = pitch;
        _Volume = volume;
        _Coefficients = fc;
        _StartSampleIndex = startSampleIndex;
    }
}

[System.Serializable]
public class GrainEmissionProps
{
    [Header("Source")]
    public int _ClipIndex = 0;

    // Position (normalised)
    //---------------------------------------------------------------------
    [Range(0.0f, 1.0f)]
    [SerializeField]
    float _PlayheadPos = 0;
    [Range(0.0f, .1f)]
    [SerializeField]
    public float _PositionRandom = 0;
    public float Position
    {
        get
        {
            return Mathf.Clamp(_PlayheadPos + UnityEngine.Random.Range(0, _PositionRandom), 0f, 1f);
        }
        set
        {
            _PlayheadPos = Mathf.Clamp(value, 0f, 1f);
        }
    }

    [Header("Timing")]
    [Range(2.0f, 1000f)]
    public int _Cadence = 20;             // ms
    [Range(0.002f, 1000f)]
    public int _CadenceRandom = 0;        // ms
    public float Cadence
    {
        get
        {
            return Mathf.Clamp(_Cadence + UnityEngine.Random.Range(0, _CadenceRandom), 2f, 1000f);
        }
        set
        {
            _Cadence = (int)Mathf.Clamp(value, 2f, 1000f);
        }
    }



    // Duration (ms)
    //---------------------------------------------------------------------
    [Range(2.0f, 1000f)]
    [SerializeField]
    int _Duration = 100;
    [Range(0.0f, 500f)]
    [SerializeField]
    int _DurationRandom = 0;
    public float Duration
    {
        get
        {
            return Mathf.Clamp(_Duration + UnityEngine.Random.Range(0, _DurationRandom), 2, 1000);
        }
        set
        {
            _Duration = (int)Mathf.Clamp(value, 2, 1000);
        }
    }

    [Header("Effects")]
    // Transpose
    //---------------------------------------------------------------------
    [Range(-4f, 4f)]
    [SerializeField]
    float _Transpose = 0;
    [Range(0f, 1f)]
    [SerializeField]
    float _TransposeRandom = 0;

    float _Pitch = 1;
    public float Pitch
    {
        get
        {
            _Pitch = TransposeToPitch(Mathf.Clamp(_Transpose + UnityEngine.Random.Range(-_TransposeRandom, _TransposeRandom), -5f, 5f));
            return Mathf.Clamp(_Pitch, 0.1f, 5f);
        }
        set
        {
            _Pitch = Mathf.Clamp(value, 0.1f, 5f);
        }
    }

    // Converts the more human-readable value of transpose to pitch values for the grains
    private float TransposeToPitch(float transpose)
    {
        float pitch = 1;

        if (transpose < 0)
            pitch = (1 / (1 + Mathf.Abs(transpose)));
        else if (transpose > 0)
            pitch = transpose + 1;

        return pitch;
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
            return Mathf.Clamp(_Volume + UnityEngine.Random.Range(-_VolumeRandom, _VolumeRandom), 0f, 3f);
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
        //_PitchRandom = pitchRand;
        _VolumeRandom = volumeRand;
    }
}

[System.Serializable]
public class AudioClipLibrary
{
    public AudioClip[] _Clips;
    public List<float[]> _ClipsDataArray = new List<float[]>();

    public void Initialize()
    {
        if (_Clips.Length == 0)
            Debug.LogError("No clips in clip library");
        else
            Debug.Log("Initializing clip library.");

        for (int i = 0; i < _Clips.Length; i++)
        {
            AudioClip audioClip = _Clips[i];

            if (audioClip.channels > 1)
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

public class GrainPlaybackData
{
    public bool _IsPlaying = true;
    public float[] _GrainSamples;
    public float[] _TempSampleBuffer;
    public int _PlaybackIndex = 0;
    public int _PlaybackSampleCount;

    // The DSP sample that the grain starts at
    public int _StartSampleIndex;

    public GrainPlaybackData()
    {
        // instantiate the grain samples at the max length of a grain of 1 second worth of samples
        _GrainSamples = new float[44 * 1000];
        _TempSampleBuffer = new float[44 * 1000];
    }
}
#endregion