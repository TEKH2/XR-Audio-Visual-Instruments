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

    // Holds a reference to the audio listener on the camera
    AudioListener _AudioListener;

    // ------------------------------------ AUDIO VARS
    public AudioClipLibrary _AudioClipLibrary;
    private int _SampleRate;
    //public FilterProperties _FilterProperties;   //TODO reimpliment
    //private FilterCoefficients _FilterCoefficients;

    // ------------------------------------ GRAIN AUDIO SPEAKERS
    public GrainSpeaker _GrainSpeakerPrefab;

    List<GrainSpeaker> _ActiveSpeakers = new List<GrainSpeaker>();
    public List<GrainSpeaker> ActiveSpeakers { get { return _ActiveSpeakers; }  }
    List<GrainSpeaker> _InactiveSpeakers = new List<GrainSpeaker>();

    // ------------------------------------ GRAIN EMITTER PROPS  
    List<GrainEmitter> _AllGrainEmitters = new List<GrainEmitter>();

    public int _CurrentDSPSample = 0; // TODO see if this is any different to calcing from the DSP time * sample rate

    [Range(0,100)]
    public float _EmissionLatencyMS = 80;
    int EmissionLatencyInSamples { get { return (int)(_EmissionLatencyMS * _SampleRate * .001f); } }

    // maximum distance an emitter can be from a speaker
    public float _MaxDistBetweenEmitterAndSpeaker = 1;

    // Maximum allowed speakers
    public int _MaxGrainSpeakers = 10;
    int TotalSpeakerCount { get { return _InactiveSpeakers.Count + _ActiveSpeakers.Count;} }

    public AnimationCurve _WindowingCurve;

    [Header("DEBUG")]
    public bool _DebugLog = false;

    public bool _DEBUG_TraditionalWindowing = false;

    [Range(0f, 1f)]
    public float _DebugNorm = 0;

    public Transform _DebugTransform;
    public float _GrainSpeakerDeactivationDistance = 5;

    // Performance metrics
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
               
        InstantiateGrainSpeaker(Vector3.zero, false);  

        _AllGrainEmitters = FindObjectsOfType<GrainEmitter>().ToList();

        print("Grain Manager initialized. Grains created: " + _InactiveSpeakers.Count);
    }

    void Update()
    {
        _ActiveEmitters = 0;
        float layeredSamplesThisFrame = 0;

        Profiler.BeginSample("Updating active speakers");
        #region UPDATE ACTIVE SPEAKERS AND CHECKS IF THE LISTENER IS OUT OF RANGE
        
        int sampleIndexMax = _CurrentDSPSample + EmissionLatencyInSamples;
       
        for (int i = _ActiveSpeakers.Count - 1; i >= 0; i--)
        {
            float dist = Vector3.Distance(_AudioListener.transform.position, _ActiveSpeakers[i].transform.position);

            // If out of range remove from the active output list
            if (dist > _GrainSpeakerDeactivationDistance)
            {
                GrainSpeaker output = _ActiveSpeakers[i];
                output.Deactivate();
                _ActiveSpeakers.Remove(output);
                _InactiveSpeakers.Add(output);
            }
            //  else update the output
            else
            {
                _ActiveSpeakers[i].ManualUpdate(sampleIndexMax, _SampleRate);
                layeredSamplesThisFrame += _ActiveSpeakers[i]._LayeredSamples;
                _ActiveEmitters += _ActiveSpeakers[i]._AttachedGrainEmitters.Count;
            }
        }

        #endregion
        Profiler.EndSample();

        Profiler.BeginSample("Speaker range test");
        #region CHECK IF LISTENER IS IN RANGE OF INACTIVE EMITTERS
        // Order by distance  // TODO this is the cause of allocation
        _AllGrainEmitters = _AllGrainEmitters.OrderBy(x => Vector3.SqrMagnitude(_AudioListener.transform.position - x.transform.position)).ToList();
        for (int i = 0; i < _AllGrainEmitters.Count; i++)
        {
            if (_AllGrainEmitters[i]._Active)
                continue;

            float dist = Vector3.Distance(_AudioListener.transform.position, _AllGrainEmitters[i].transform.position);

            if (dist < _GrainSpeakerDeactivationDistance)
            {
                GrainEmitter emitter = _AllGrainEmitters[i];

                // had to initialize it to something TODO fix pattern
                GrainSpeaker grainSpeaker = _GrainSpeakerPrefab;
                bool activeSpeakerFound = false;

                // ------------------------------  FIND CLOSE ACTIVE SPEAKER
                float closestDist = _MaxDistBetweenEmitterAndSpeaker;
                for (int j = 0; j < _ActiveSpeakers.Count; j++)
                {
                    float speakerDist = Vector3.Distance(emitter.transform.position, _ActiveSpeakers[j].transform.position);
                    if (speakerDist <= closestDist)
                    {
                        closestDist = speakerDist;
                        grainSpeaker = _ActiveSpeakers[j];
                        activeSpeakerFound = true;
                    }
                }

                if (!activeSpeakerFound)
                {
                    // ------------------------------  FIND INACTIVE SPEAKER
                    if (_InactiveSpeakers.Count > 0)
                    {
                        grainSpeaker = _InactiveSpeakers[0];
                        grainSpeaker.transform.position = emitter.transform.position;
                        grainSpeaker.gameObject.SetActive(true);
                        grainSpeaker._CurrentDSPSampleIndex = _CurrentDSPSample;

                        _InactiveSpeakers.RemoveAt(0);
                        _ActiveSpeakers.Add(grainSpeaker);

                        activeSpeakerFound = true;
                    }
                    // ------------------------------  CREATE NEW SPEAKER
                    else
                    {
                        grainSpeaker = InstantiateGrainSpeaker(emitter.transform.position);

                        if (grainSpeaker != null)
                            activeSpeakerFound = true;
                    }
                }

                if (activeSpeakerFound)
                {
                    grainSpeaker.AttachEmitter(emitter);
                }
            }
        }
        #endregion
        Profiler.EndSample();

        #region PERFORMANCE METRICS
        // Performance metrics
        _LayeredSamples = Mathf.Lerp(_LayeredSamples, layeredSamplesThisFrame, Time.deltaTime * 4);
        if (_ActiveSpeakers.Count == 0)
            _AvLayeredSamples = 0;
        else
            _AvLayeredSamples = _LayeredSamples / (float)_ActiveSpeakers.Count;

        if (_DebugLog)
            print("Active outputs: " + _ActiveSpeakers.Count + "   Layered Samples: " + _LayeredSamples + "   Av layered Samples: " + _LayeredSamples / (float)_ActiveSpeakers.Count);
        #endregion
    }

    GrainSpeaker InstantiateGrainSpeaker(Vector3 pos, bool addToActiveList = true)
    {
        if (TotalSpeakerCount == _MaxGrainSpeakers)
            return null;

        GrainSpeaker speaker = Instantiate(_GrainSpeakerPrefab, transform);
        speaker.name = "Grain speaker " + TotalSpeakerCount;
        speaker.transform.position = pos;
        speaker._CurrentDSPSampleIndex = _CurrentDSPSample;

        if (addToActiveList)
        {
            _ActiveSpeakers.Add(speaker);
            speaker.gameObject.SetActive(true);
        }
        else
        {
            _InactiveSpeakers.Add(speaker);
            speaker.gameObject.SetActive(false);
        }

        print("New grain speaker added - Total: " + TotalSpeakerCount + " / " + _MaxGrainSpeakers);

        return speaker;
    }

    public void AddGrainEmitterToList(GrainEmitter emitter)
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
            Gizmos.DrawWireSphere(_AudioListener.transform.position, _GrainSpeakerDeactivationDistance);

            for (int i = 0; i < _ActiveSpeakers.Count; i++)
            {
                Gizmos.DrawLine(_AudioListener.transform.position, _ActiveSpeakers[i].transform.position);
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
    [Header("Speaker")]
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
            return Mathf.Clamp(_PlayheadPos + Random.Range(0, _PositionRandom), 0f, 1f);
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
            return Mathf.Clamp(_Cadence + Random.Range(0, _CadenceRandom), 2f, 1000f);
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
            return Mathf.Clamp(_Duration + Random.Range(0, _DurationRandom), 2, 1000);
        }
        set
        {
            _Duration = (int)Mathf.Clamp(value, 2, 1000);
        }
    }

    [Header("Effects")]
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
            return Mathf.Clamp(_Volume + Random.Range(-_VolumeRandom, _VolumeRandom), 0f, 3f);
        }
        set
        {
            _Volume = (int)Mathf.Clamp(value, 0f, 3f);
        }
    }


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
            _Pitch = Mathf.Pow(2, Mathf.Clamp(_Transpose + Random.Range(-_TransposeRandom, _TransposeRandom), -5f, 5f));
            return Mathf.Clamp(_Pitch, 0.1f, 5f);
        }
        set
        {
            _Pitch = Mathf.Clamp(value, 0.1f, 5f);
        }
    }

    public DSP_Properties _FilterProperties;


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