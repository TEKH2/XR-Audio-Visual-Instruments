using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using Random = UnityEngine.Random;
using System.Linq;

[RequireComponent(typeof(AudioSource))]
public class GranulatorManager : MonoBehaviour
{
    #region VARIABLES
    public static GranulatorManager Instance;
    AudioListener _AudioListener;

    // ------------------------------------ AUDIO VARS
    public AudioClipLibrary _AudioClipLibrary;
    private int _SampleRate;
    //public FilterProperties _FilterProperties;
    //private FilterCoefficients _FilterCoefficients;

    // ------------------------------------ GRAIN AUDIO SOURCES

    public GrainAudioOutput _GrainAudioSourcePrefab;

    public List<GrainAudioOutput> _ActiveOutputs = new List<GrainAudioOutput>();
    public List<GrainAudioOutput> _InactiveOutputs = new List<GrainAudioOutput>();

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
                GrainAudioOutput output = _ActiveOutputs[i];
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
        GrainAudioOutput audioSource = _GrainAudioSourcePrefab;
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

    GrainAudioOutput InstantiateNewAudioSource(Vector3 pos, bool addToActiveList = true)
    {
        if (TotalAudioSourceCount == _MaxAudioSources)
            return null;

        GrainAudioOutput audioSource = Instantiate(_GrainAudioSourcePrefab, transform);
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