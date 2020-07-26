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
    public int _Debug_NumberOfAudioSourcesToUse = 2;

    // ------------------------------------ GRAIN EMITTER PROPS  
    List<GrainEmitter> _GrainEmitters = new List<GrainEmitter>();
    int _CurrentDSPSample = 0;
    public float _EmissionLatencyMS = 80;
    int EmissionLatencyInSamples { get { return (int)(_EmissionLatencyMS * _SampleRate * .001f); } }

    public float _EmitterToSourceMaxDist = 0;



    public AnimationCurve _WindowingCurve;
    public bool _DEBUG_TraditionalWindowing = false;



    public bool _DebugLog = false;

    [Range(0f, 1f)]
    public float _DebugNorm = 0;

    private void Awake()
    {
        Instance = this;

        // Sample rate from teh audio settings
        _SampleRate = AudioSettings.outputSampleRate;

        // Init clip library
        _AudioClipLibrary.Initialize();

        for (int i = 0; i < 5; i++)
        {
            GrainAudioSource grainSource = Instantiate(_GrainAudioSourcePrefab, transform);
            grainSource.transform.position = Random.insideUnitSphere * 3;
            grainSource.gameObject.SetActive(false);
            _IdleGrainAudioSources.Add(grainSource);
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
    }

    public void AddGrainEmitter(GrainEmitter emitter)
    {
        GrainAudioSource audioSource;
        bool sourceFound = false;
        // try find a source close to the emitter
        for (int i = 0; i < _ActiveGrainAudioSources.Count; i++)
        {
            if (Vector3.Distance(emitter.transform.position, _ActiveGrainAudioSources[i].transform.position) < _EmitterToSourceMaxDist)
            {
                audioSource = _ActiveGrainAudioSources[i];
                sourceFound = true;
                print("Found audio source in active list");
            }
        }

        //  if no audio source is close enough try find an idle source
        if (!sourceFound && _IdleGrainAudioSources.Count > 0)
        {
            audioSource = _IdleGrainAudioSources[0];
            audioSource.transform.position = emitter.transform.position;
            audioSource.gameObject.SetActive(true);
            audioSource._CurrentDSPSampleIndex = _CurrentDSPSample;

            _IdleGrainAudioSources.RemoveAt(0);
            _ActiveGrainAudioSources.Add(audioSource);

            print("Found audio source in inactive list");
        }
        // ... else add new audio source
        else
        {
            audioSource = Instantiate(_GrainAudioSourcePrefab, transform);
            audioSource.transform.position = emitter.transform.position;
            audioSource._CurrentDSPSampleIndex = _CurrentDSPSample;
            _ActiveGrainAudioSources.Add(audioSource);
            print("Made new audio source");
        }        

        emitter.Init(_CurrentDSPSample, audioSource);
        _GrainEmitters.Add(emitter);
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