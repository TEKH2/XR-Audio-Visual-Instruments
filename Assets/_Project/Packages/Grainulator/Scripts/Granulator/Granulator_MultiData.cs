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

    public GrainEmissionProps _EmitGrainProps;

    int _PrevGrainSampleIndex = 0;      
    public float _EmissionLatencyMS = 80;
    int EmissionLatencyInSamples { get { return (int)(_EmissionLatencyMS * _SampleRate * .001f); } }

    public float _Spacing = 0;
    public AnimationCurve _WindowingCurve;
    public bool _DEBUG_TraditionalWindowing = false;

    // Grains that are queued in between frames ready to fire at next update
    private List<GrainData> _QueuedGrainData;

    double _StartDSPTime;

    Grain_MultiData _Grain;

    private List<GrainData> _GrainDataPool = new List<GrainData>();

    public bool _DebugLog = false;
    int _DebugFrameCounter = 0;

    [Range(0f,1f)]
    public float _DebugNorm = 0;

    private void Start()
    {
        _StartDSPTime = AudioSettings.dspTime;

        _SampleRate = AudioSettings.outputSampleRate;

        _PrevGrainSampleIndex = _SampleRate;

        _AudioClipLibrary.Initialize();

        _QueuedGrainData = new List<GrainData>();
        
        //for (int i = 0; i < _MaxGrains; i++)
        //{
            GameObject go = Instantiate(_GrainPrefab);
            _Grain = go.GetComponent<Grain_MultiData>();
            _Grain.transform.parent = transform;
            _Grain.transform.localPosition = Vector3.zero;
        //}
    }

    void Update()
    {
        // Limit audio clip selection to available clips
        _EmitGrainProps._ClipIndex = Mathf.Clamp(_EmitGrainProps._ClipIndex, 0, _AudioClipLibrary._Clips.Length - 1);

        //------------------------------------------ UPDATE GRAIN SPAWN LIST
        // Current sample we are up to in time
        int dspSampleIndexThisFrame = _Grain._CurrentDSPSampleIndex;// (AudioSettings.dspTime - _StartDSPTime) * _SampleRate;
        int sampleRangeMax = dspSampleIndexThisFrame + EmissionLatencyInSamples;

        //print("DSP Sample Index: " + dspSampleIndexThisFrame);
        // Calculate random sample rate
        int randomSampleBetweenGrains = (int)(_SampleRate * ((_Cadence + Random.Range(0, _CadenceRandom)) * .001f));
        // Find sample that next grain is emitted at
        int nextEmitSampleIndex = _PrevGrainSampleIndex + randomSampleBetweenGrains;

        int emitted = 0;

        // fill the spawn sample time list while the next emit sample index 
        while(nextEmitSampleIndex <= sampleRangeMax)
        {
            GrainData tempGrainData = new GrainData
            (
                transform.position + (Random.insideUnitSphere * _Spacing),
                Vector3.zero,
                0,
                _EmitGrainProps._ClipIndex,
                _EmitGrainProps.Duration,
                _EmitGrainProps.Position,
                _EmitGrainProps.Pitch,
                _EmitGrainProps.Volume,
                nextEmitSampleIndex
            );

            EmitGrain(tempGrainData);

            // Find next sample start time
            _PrevGrainSampleIndex = nextEmitSampleIndex;
            randomSampleBetweenGrains = (int)(_SampleRate * ((_Cadence + Random.Range(0, _CadenceRandom)) * .001f));
            nextEmitSampleIndex = _PrevGrainSampleIndex + randomSampleBetweenGrains;

            emitted++;
        }

        if (_DebugLog)
        {
            print("Prev: " + _PrevGrainSampleIndex + "   start frame:  " + dspSampleIndexThisFrame + "   max sample range: " + sampleRangeMax + "  Emitted: " + emitted);
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
        _Grain.AddGrainData(grainData, _AudioClipLibrary._ClipsDataArray[grainData._ClipIndex], _AudioClipLibrary._Clips[grainData._ClipIndex].frequency, _WindowingCurve, _DebugLog, _DEBUG_TraditionalWindowing);        
    }
}