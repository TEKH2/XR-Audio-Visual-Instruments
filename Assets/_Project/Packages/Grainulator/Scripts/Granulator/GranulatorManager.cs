using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using Random = UnityEngine.Random;
using System.Linq;

public class GranulatorManager : MonoBehaviour
{
    public static GranulatorManager Instance;

    // ------------------------------------ AUDIO VARS
    public AudioClipLibrary _AudioClipLibrary;
    private int _SampleRate;

    // ------------------------------------ GRAIN POOLING
    public GameObject _GrainPrefab;
    public int _MaxGrains = 100;

    // ------------------------------------ GRAIN EMISSION  

    public FilterProperties _FilterProperties;
    private FilterCoefficients _FilterCoefficients;
    public GrainEmissionProps _EmitGrainProps;

    List<GrainEmitter> _GrainEmitters = new List<GrainEmitter>();

    public int _CurrentDSPSample = 0;

    int _SampleIndexPrevGrain = 0;      
    public float _EmissionLatencyMS = 80;
    int EmissionLatencyInSamples { get { return (int)(_EmissionLatencyMS * _SampleRate * .001f); } }

    public float _Spacing = 0;
    public AnimationCurve _WindowingCurve;
    public bool _DEBUG_TraditionalWindowing = false;

    GrainAudioSource[] _Grains;
    public int _NumberOfAudioSources = 2;
    public int _NumberOfAudioSourcesToUse = 2;

    public bool _DebugLog = false;

    [Range(0f,1f)]
    public float _DebugNorm = 0;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // Sample rate from teh audio settings
        _SampleRate = AudioSettings.outputSampleRate;

        // Generate filter coefficients
        _FilterCoefficients = new FilterCoefficients();

        // Why is this set to _SampleRate?!
        _SampleIndexPrevGrain = _SampleRate;

        // Init clip library
        _AudioClipLibrary.Initialize();

        _Grains = new GrainAudioSource[_NumberOfAudioSources];
        for (int i = 0; i < _NumberOfAudioSources; i++)
        {
            GameObject go = Instantiate(_GrainPrefab);
            _Grains[i] = go.GetComponent<GrainAudioSource>();
            _Grains[i].transform.parent = transform;
            _Grains[i].transform.localPosition = Random.onUnitSphere * 1.5f;
        }

        print("Granualtor Manager initialized. Grains created: " + _Grains.Length);

        _GrainEmitters = FindObjectsOfType<GrainEmitter>().ToList();
    }

    void Update()
    {
        // Current sample we are up to in time
        _CurrentDSPSample = _Grains[0]._CurrentDSPSampleIndex;
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
        _GrainEmitters.Add(emitter);

        // Manager audiosources here
    }

    public void RemoveGrainEmitter(GrainEmitter emitter)
    {
        _GrainEmitters.Remove(emitter);
    }
   
    public void EmitGrain(GrainData grainData, int audioSourceIndex)
    {
        Profiler.BeginSample("Emit");
        // Init grain with data
        _Grains[audioSourceIndex].AddGrainData(grainData,
            _AudioClipLibrary._ClipsDataArray[grainData._ClipIndex],
            _AudioClipLibrary._Clips[grainData._ClipIndex].frequency,
            _WindowingCurve, _DebugLog, _DEBUG_TraditionalWindowing);
        Profiler.EndSample();
    }
}