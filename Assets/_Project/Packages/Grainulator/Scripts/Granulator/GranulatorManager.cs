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
    public static GranulatorManager Instance;

    // ------------------------------------ AUDIO VARS
    public AudioClipLibrary _AudioClipLibrary;
    private int _SampleRate;
    //public FilterProperties _FilterProperties;
    //private FilterCoefficients _FilterCoefficients;

    // ------------------------------------ GRAIN AUDIO SOURCES
    public GrainAudioSource _GrainAudioSourcePrefab;
    List<GrainAudioSource> _ActiveGrainAudioSources = new List<GrainAudioSource>();
    List<GrainAudioSource> _IdleGrainAudioSources = new List<GrainAudioSource>();
    //public int _Debug_NumberOfAudioSourcesToUse = 2;

    // ------------------------------------ GRAIN EMITTER PROPS  
    List<GrainEmitter> _GrainEmitters = new List<GrainEmitter>();
    int _CurrentDSPSample = 0;
    public float _EmissionLatencyMS = 80;
    int EmissionLatencyInSamples { get { return (int)(_EmissionLatencyMS * _SampleRate * .001f); } }

    // maximum distance an emitter can be from an audio source
    public float _MaxDistBetweenEmitterAndSource = 1;
    // Maximum allowed audio sources
    public int _MaxAudioSources = 10;
    int TotalAudioSources { get { return _IdleGrainAudioSources.Count + _ActiveGrainAudioSources.Count;} }

    public AnimationCurve _WindowingCurve;
    public bool _DEBUG_TraditionalWindowing = false;

    public bool _DebugLog = false;

    [Range(0f, 1f)]
    public float _DebugNorm = 0;

    public Transform _DebugTransform;
    public float _DeactivateDistance = 5;

    private void Awake()
    {
        Instance = this;

        // Sample rate from teh audio settings
        _SampleRate = AudioSettings.outputSampleRate;

        // Init clip library
        _AudioClipLibrary.Initialize();

        for (int i = 0; i < 1; i++)
        {
            InstantiateNewAudioSource(Vector3.zero, false);
        }

        print("Granualtor Manager initialized. Grains created: " + _IdleGrainAudioSources.Count);

        _GrainEmitters = FindObjectsOfType<GrainEmitter>().ToList();
    }

    void Update()
    {
        // Max sample index to spawn before
        int sampleIndexMax = _CurrentDSPSample + EmissionLatencyInSamples;

        // Update all emitters
        for (int i = 0; i < _GrainEmitters.Count; i++)
        {
            _GrainEmitters[i].ManualUpdate(this, sampleIndexMax, _SampleRate);
        }


        //// Check to see if you are far enough away from audio sources, if so disable
        //for (int i = _ActiveGrainAudioSources.Count - 1; i >= 0; i--)
        //{
        //    float dist = Vector3.Distance(Camera.main.transform.position, _ActiveGrainAudioSources[i].transform.position);
        //    if(dist > _DeactivateDistance)
        //    {
        //        //_ActiveGrainAudioSources[i]
        //    }
        //}        
    }

    public void AssignEmitterToSource(GrainEmitter emitter)
    {
        // had to initialize it to something TODO fix pattern
        GrainAudioSource audioSource = _GrainAudioSourcePrefab;
        bool sourceFound = false;

        print("------------------------------   Looking for grain audio source....");
        float closestDist = _MaxDistBetweenEmitterAndSource;
        // FIND CLOSE ACTIVE SOUCRE
        for (int i = 0; i < _ActiveGrainAudioSources.Count; i++)
        {
            float dist = Vector3.Distance(emitter.transform.position, _ActiveGrainAudioSources[i].transform.position);
            if (dist <= closestDist)
            {
                closestDist = dist;
                audioSource = _ActiveGrainAudioSources[i];
                sourceFound = true;
                print("Found audio source in active list");
            }
        }

        if (sourceFound)
        {
            print("Found withing range : " + sourceFound + "   Closest found: " + closestDist);
            emitter.Init(_CurrentDSPSample, audioSource);
            _GrainEmitters.Add(emitter);
        }
        else
            print("None in range");

        
        if (!sourceFound)
        {
            // FIND IDLE SOURCE
            if (_IdleGrainAudioSources.Count > 0)
            {
                audioSource = _IdleGrainAudioSources[0];
                audioSource.transform.position = emitter.transform.position;
                audioSource.gameObject.SetActive(true);
                audioSource._CurrentDSPSampleIndex = _CurrentDSPSample;

                _IdleGrainAudioSources.RemoveAt(0);
                _ActiveGrainAudioSources.Add(audioSource);

                print("Found audio source in inactive list");
            }
            // CREATE NEW SOURCE
            else
            {
                audioSource = InstantiateNewAudioSource(emitter.transform.position);              
            }

            if (audioSource != null)
            {
                emitter.Init(_CurrentDSPSample, audioSource);
                _GrainEmitters.Add(emitter);
            }
        }
    }

    GrainAudioSource InstantiateNewAudioSource(Vector3 pos, bool addToActiveList = true)
    {
        if (TotalAudioSources == _MaxAudioSources)
            return null;

        GrainAudioSource audioSource = Instantiate(_GrainAudioSourcePrefab, transform);
        audioSource.transform.position = pos;
        audioSource._CurrentDSPSampleIndex = _CurrentDSPSample;

        if (addToActiveList)
        {
            _ActiveGrainAudioSources.Add(audioSource);
            audioSource.gameObject.SetActive(true);
        }
        else
        {
            _IdleGrainAudioSources.Add(audioSource);
            audioSource.gameObject.SetActive(false);
        }

        print("New grain audio source added - Total: " + TotalAudioSources + " / " + _MaxAudioSources);

        return audioSource;
    }

    public void RemoveGrainEmitter(GrainEmitter emitter)
    {
        _GrainEmitters.Remove(emitter);

        bool stillInUse = false;

        for (int i = 0; i < _GrainEmitters.Count; i++)
        {
            if (_GrainEmitters[i]._AudioSource == emitter._AudioSource)
                stillInUse = true;
        }

        if(!stillInUse)
        {
            _ActiveGrainAudioSources.Remove(emitter._AudioSource);
            _IdleGrainAudioSources.Add(emitter._AudioSource);
            emitter._AudioSource.gameObject.SetActive(false);
        }
    }

    public void EmitGrain(GrainData grainData, GrainAudioSource audioSource)
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
}