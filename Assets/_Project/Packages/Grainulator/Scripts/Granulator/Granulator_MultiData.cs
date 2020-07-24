using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using Random = UnityEngine.Random;
using System.Linq;

public class Granulator_MultiData : MonoBehaviour
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
    [Range(0.002f, 1000f)]
    public int _CadenceRandom = 0;        // ms

    public FilterProperties _FilterProperties;
    private FilterCoefficients _FilterCoefficients;
    public GrainEmissionProps _EmitGrainProps;
    

    int _SampleIndexPrevGrain = 0;      
    public float _EmissionLatencyMS = 80;
    int EmissionLatencyInSamples { get { return (int)(_EmissionLatencyMS * _SampleRate * .001f); } }

    public float _Spacing = 0;
    public AnimationCurve _WindowingCurve;
    public bool _DEBUG_TraditionalWindowing = false;

    Grain_MultiData _Grain;

    public bool _DebugLog = false;
    int _DebugFrameCounter = 0;

    [Range(0f,1f)]
    public float _DebugNorm = 0;

    private void Start()
    {
        _SampleRate = AudioSettings.outputSampleRate;

        _FilterCoefficients = new FilterCoefficients();

        // Why is this set to _SampleRate?!
        _SampleIndexPrevGrain = _SampleRate;

        _AudioClipLibrary.Initialize();

        GameObject go = Instantiate(_GrainPrefab);
        _Grain = go.GetComponent<Grain_MultiData>();
        _Grain.transform.parent = transform;
        _Grain.transform.localPosition = Vector3.zero;
    }

    void Update()
    {
        // Currently generating coefficents each frame from the GUI filter props
        // Will want to move this over to a per-grain method in the near future
        // And restrict updates to OnChange for efficency
        _FilterCoefficients = DSP_Filter.CreateCoefficents(_FilterProperties);

        // Limit audio clip selection to available clips
        _EmitGrainProps._ClipIndex = Mathf.Clamp(_EmitGrainProps._ClipIndex, 0, _AudioClipLibrary._Clips.Length - 1);

        //------------------------------------------ UPDATE GRAIN SPAWN LIST
        // Current sample we are up to in time
        int sampleIndexFrameStart = _Grain._CurrentSampleIndex;// (AudioSettings.dspTime - _StartDSPTime) * _SampleRate;
        int sampleIndexMax = sampleIndexFrameStart + EmissionLatencyInSamples;

        // Calculate random sample rate
        int currentCadence = (int)(_SampleRate * ((_Cadence + Random.Range(0, _CadenceRandom)) * .001f));
        // Find sample that next grain is emitted at
        int sampleIndexNextGrainStart = _SampleIndexPrevGrain + currentCadence;

        int emitted = 0;

        // Emit grain if it starts within the sample range
        while(sampleIndexNextGrainStart <= sampleIndexMax)
        {
            GrainData tempGrainData = new GrainData
            (
                transform.position + (Random.insideUnitSphere * _Spacing),
                Vector3.zero, 0,
                _EmitGrainProps._ClipIndex,
                _EmitGrainProps.Duration,
                _EmitGrainProps.Position,
                _EmitGrainProps.Pitch,
                _EmitGrainProps.Volume,
                _FilterCoefficients,
                sampleIndexNextGrainStart
            );

            EmitGrain(tempGrainData);
            _SampleIndexPrevGrain = sampleIndexNextGrainStart;

            currentCadence = (int)(_SampleRate * ((_Cadence + Random.Range(0, _CadenceRandom)) * .001f));
            sampleIndexNextGrainStart = _SampleIndexPrevGrain + currentCadence;

            emitted++;
        }

        if (_DebugLog)
        {
            print("Prev: " + _SampleIndexPrevGrain + "   start frame:  " + sampleIndexFrameStart + "   max sample range: " + sampleIndexMax + "  Emitted: " + emitted);
        }

        if (_DebugLog)
        {
            //------------------------------------------ DEBUG
            DebugGUI.LogPersistent("Grains per second", "Grains per second: " + 1000 / _Cadence);
            DebugGUI.LogPersistent("Samples per grain", "Samples per grain: " + _SampleRate * (_EmitGrainProps.Duration * .001f));
            DebugGUI.LogPersistent("Samples bewteen grain", "Samples between grains: " + _SampleRate * (_Cadence * .001f));
            DebugGUI.LogPersistent("CurrentSample", "Current Sample: " + Time.time * _SampleRate);

            _DebugFrameCounter++;
        }
    }

    public void EmitGrain(GrainData grainData)
    {
        // Init grain with data
        _Grain.AddGrainData(grainData,
            _AudioClipLibrary._ClipsDataArray[grainData._ClipIndex],
            _AudioClipLibrary._Clips[grainData._ClipIndex].frequency,
            _WindowingCurve, _DebugLog, _DEBUG_TraditionalWindowing);        
    }
}