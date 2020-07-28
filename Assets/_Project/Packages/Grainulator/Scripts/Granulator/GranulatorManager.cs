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
    public List<GrainEmitter> _ActiveGrainEmitters = new List<GrainEmitter>();
    public List<GrainEmitter> _InactiveGrainEmitters = new List<GrainEmitter>();

    int _CurrentDSPSample = 0;
    public float _EmissionLatencyMS = 80;
    int EmissionLatencyInSamples { get { return (int)(_EmissionLatencyMS * _SampleRate * .001f); } }

    // maximum distance an emitter can be from an audio source
    public float _MaxDistBetweenEmitterAndSource = 1;
    // Maximum allowed audio sources
    public int _MaxAudioSources = 10;
    int TotalAudioSourceCount { get { return _InactiveOutputs.Count + _ActiveOutputs.Count;} }

    public AnimationCurve _WindowingCurve;
    public bool _DEBUG_TraditionalWindowing = false;

    public bool _DebugLog = false;

    [Range(0f, 1f)]
    public float _DebugNorm = 0;

    public Transform _DebugTransform;
    public float _AudioOutputDeactivationDistance = 5;
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

        _InactiveGrainEmitters = FindObjectsOfType<GrainEmitter>().ToList();
    }

    void Update()
    {
        // CHECK IF INACTIVE SOURCES ARE IN RANGE
        for (int i = 0; i < _InactiveGrainEmitters.Count; i++)
        {
            float dist = Vector3.Distance(_AudioListener.transform.position, _InactiveGrainEmitters[i].transform.position);

            if (dist < _AudioOutputDeactivationDistance)
            {
                TryAssignEmitterToSource(_InactiveGrainEmitters[i]);
            }
        }

        // CHECK IF ACTIVE SOURCES ARE OUT OF RANGE
        for (int i = _ActiveOutputs.Count - 1; i >= 0; i--)
        {
            float dist = Vector3.Distance(_AudioListener.transform.position, _ActiveOutputs[i].transform.position);
            if (dist > _AudioOutputDeactivationDistance)
            {
                GrainAudioOutput output = _ActiveOutputs[i];
                output.Deactivate();
               
                _ActiveOutputs.Remove(output);
                _InactiveOutputs.Add(output);
            }
        }

        // UPDATE EMITTERS
        int sampleIndexMax = _CurrentDSPSample + EmissionLatencyInSamples;
        // Update all emitters
        for (int i = 0; i < _ActiveGrainEmitters.Count; i++)
        {
            _ActiveGrainEmitters[i].ManualUpdate(this, sampleIndexMax, _SampleRate);
        }
    }


    public void TryAssignEmitterToSource(GrainEmitter emitter)
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

                print("Found audio source in inactive list");
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
            emitter.Init(_CurrentDSPSample, audioSource);
            _ActiveGrainEmitters.Add(emitter);
            _InactiveGrainEmitters.Remove(emitter);
        }
    }

    public void RemoveActiveGrainEmitter(GrainEmitter emitter)
    {
        _ActiveGrainEmitters.Remove(emitter);
        _InactiveGrainEmitters.Add(emitter);

        bool stillInUse = false;

        for (int i = 0; i < _ActiveGrainEmitters.Count; i++)
        {
            if (_ActiveGrainEmitters[i]._AudioSource == emitter._AudioSource)
                stillInUse = true;
        }

        if (!stillInUse)
        {
            _ActiveOutputs.Remove(emitter._AudioSource);
            _InactiveOutputs.Add(emitter._AudioSource);
            emitter._AudioSource.gameObject.SetActive(false);
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


    public void EmitGrain(GrainData grainData, GrainAudioOutput audioSource)
    {
        Profiler.BeginSample("Emit");
        // Init grain with data
        audioSource.AddGrainData(grainData,
            _AudioClipLibrary._ClipsDataArray[grainData._ClipIndex],
            _AudioClipLibrary._Clips[grainData._ClipIndex].frequency,
            _WindowingCurve, _DebugLog, _DEBUG_TraditionalWindowing);
        Profiler.EndSample();
    }


    void OnAudioFilterRead(float[] data, int channels)
    {
        for (int dataIndex = 0; dataIndex < data.Length; dataIndex += channels)
        {
            _CurrentDSPSample++;
        }
    }

    private void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(_AudioListener.transform.position, _AudioOutputDeactivationDistance);

            for (int i = 0; i < _ActiveOutputs.Count; i++)
            {
                Gizmos.DrawLine(_AudioListener.transform.position, _ActiveOutputs[i].transform.position);
            }

            for (int i = 0; i < _ActiveGrainEmitters.Count; i++)
            {
                Gizmos.DrawLine(_AudioListener.transform.position, _ActiveGrainEmitters[i].transform.position);
            }
        }
    }
}