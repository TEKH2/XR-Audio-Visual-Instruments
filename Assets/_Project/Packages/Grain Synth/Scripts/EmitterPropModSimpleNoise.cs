using EXPToolkit;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class AutomationData
{
    public EmitterPropModSimpleNoise.Automation Type = EmitterPropModSimpleNoise.Automation.Off;
    public float Speed = 1;
    public Vector2 Range = new Vector2(0, 1);
    private float Phase = 0;
    private float Seed = 0;
    private GrainSynth GrainSynth;
    private GrainEmissionProps Props;
    private bool Flip = false;

    public void SetData(GrainSynth grainSynth, GrainEmissionProps props)
    {
        GrainSynth = grainSynth;
        Props = props;
        Phase = UnityEngine.Random.value * 123.74f;
    }

    public void UpdatePhase()
    {
        Phase += Time.deltaTime * Speed / GrainSynth._AudioClips[Props._ClipIndex].length;
        if ((int)(Phase % 2) == 0)
            Flip = true;
        else
            Flip = false;
    }

    public float Process()
    {
        float output = 0;

        switch (Type)
        {
            case EmitterPropModSimpleNoise.Automation.Perlin:
                float norm = Mathf.PerlinNoise(Phase, Phase * 0.5f);
                output = Mathf.Lerp(Range.x, Range.y, norm);
                break;
            case EmitterPropModSimpleNoise.Automation.Straight:
                output = Phase % 1;
                break;
            case EmitterPropModSimpleNoise.Automation.PingPong:
                float ping;
                if (Flip)
                    ping = Phase % 1;
                else
                    ping = 1 - (Phase % 1);
                output = ping;
                break;
            case EmitterPropModSimpleNoise.Automation.Sine:
                break;
            default:
                break;
        }

        return output;
    }
}

[RequireComponent(typeof(GrainEmitterAuthoring))]
public class EmitterPropModSimpleNoise : MonoBehaviour
{  
    public enum Automation
    {
        Off,
        Perlin,
        Straight,
        Sine,
        PingPong
    }

    public GrainSynth _GrainSynth;
    private GrainEmitterAuthoring _Emitter;
    private GrainEmissionProps _EmissionProps;

    public AutomationData _Playhead;
    public AutomationData _Transpose;
    public AutomationData _Cadence;
    public AutomationData _Duration;


    [Space]
    [Header("Automation")]

    float _Seed;

    [Header("Playhead")]
    public Automation _PlayheadType = Automation.Off;
    public float _PlayheadSpeed = 1;
    public Vector2 _PlayheadRange = new Vector2(0, 1);
    private float _PlayheadPhase = 0;

    [Header("Transpose")]
    public Automation _TransposeType = Automation.Off;
    public float _TransposeSpeed = 1;
    public Vector2 _TransposeRange = new Vector2(-1, 1);
    private float _TransposePhase = 0;


    [Header("Timing")]
    public bool _LinkTiming = false;

    public Automation _CadenceType = Automation.Off;
    public float _CadenceSpeed = 1;
    public Vector2 _CadenceRange = new Vector2(5, 50);
    private float _CadencePhase = 0;

    public Automation _DurationType = Automation.Off;
    public float _DurationSpeed = 1;
    public Vector2 _DurationRange = new Vector2(20, 200);
    private float _DurationPhase = 0;



    private void Awake()
    {
        _Seed = UnityEngine.Random.value * 123.74f;
        _Emitter = GetComponent<GrainEmitterAuthoring>();
        _EmissionProps = _Emitter._EmissionProps;

        _Playhead = new AutomationData();
        _Playhead.SetData(_GrainSynth, _EmissionProps);

        _Transpose = new AutomationData();
        _Transpose.SetData(_GrainSynth, _EmissionProps);

        _Cadence = new AutomationData();
        _Cadence.SetData(_GrainSynth, _EmissionProps);

        _Duration = new AutomationData();
        _Duration.SetData(_GrainSynth, _EmissionProps);
    }

    private void Update()
    {
        if (_PlayheadType != Automation.Off)
            _EmissionProps.Position = Automate(_PlayheadType, _PlayheadRange, _PlayheadSpeed, ref _PlayheadPhase);

        if (_TransposeType != Automation.Off)
            _EmissionProps._Transpose = Automate(_TransposeType, _TransposeRange, _TransposeSpeed, ref _TransposePhase);

        if (_LinkTiming && _CadenceType != Automation.Off)
        {
            // Temporarily store phase so ref doesn't update CadencePhase twice per call
            float tempPhase = _CadencePhase;
            float tempAutomation = Automate(_CadenceType, _CadenceRange, _CadenceSpeed, ref tempPhase);

            _EmissionProps.Cadence = tempAutomation;
            _EmissionProps.Duration = Automate(_CadenceType, _DurationRange, _CadenceSpeed, ref _CadencePhase);
        }
        else
        {
            if (_CadenceType != Automation.Off)
                _EmissionProps._Cadence = Automate(_CadenceType, _CadenceRange, _CadenceSpeed, ref _CadencePhase);
            if (_DurationType != Automation.Off)
                _EmissionProps._Duration = Automate(_DurationType, _DurationRange, _DurationSpeed, ref _DurationPhase);
        }

        _Emitter._EmissionProps = _EmissionProps;
    }

    private float Automate(Automation type, Vector2 range, float speed, ref float phase)
    {
        float outputValue = 0;
        float automation = 0;

        switch (type)
        {
            case Automation.Perlin:
                automation = Mathf.PerlinNoise(Time.time * speed + _Seed, Time.time * speed * .5f + _Seed);
                outputValue = Mathf.Lerp(range.x, range.y, automation);
                break;
            case Automation.Straight:
                phase += Time.deltaTime * speed / _GrainSynth._AudioClips[_Emitter._EmissionProps._ClipIndex].length;
                outputValue = (_PlayheadPhase + _Seed) % 1;
                break;
            case Automation.PingPong:
                break;
            case Automation.Sine:
                break;
            default:
                break;
        }

        return outputValue;
    }
}
