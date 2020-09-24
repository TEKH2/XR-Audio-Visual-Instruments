using EXPToolkit;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YamlDotNet.Core;

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

    [Space]
    [Header("Automation")]

    float _Seed;
    public float _AutomationSpeed = 1;

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
        _Seed = Random.value * 123.74f;
        _Emitter = GetComponent<GrainEmitterAuthoring>();
        _EmissionProps = _Emitter._EmissionProps;
    }

    private void Update()
    {
        float automation = Mathf.PerlinNoise(Time.time * _AutomationSpeed + _Seed, Time.time * _AutomationSpeed * .5f + _Seed);

        if (_PlayheadType != Automation.Off)
            _EmissionProps.Position = Automate(_PlayheadType, _PlayheadRange, _PlayheadSpeed, ref _PlayheadPhase);

        if (_TransposeType != Automation.Off)
            _EmissionProps._Transpose = Automate(_TransposeType, _TransposeRange, _TransposeSpeed, ref _TransposePhase);

        if (_LinkTiming && _CadenceType != Automation.Off)
        {
            float tempAutomation = Automate(_CadenceType, _CadenceRange, _CadenceSpeed, ref _CadencePhase);
            float timingRange = (Mathf.Abs(_DurationRange.x / _CadenceRange.x) + Mathf.Abs(_DurationRange.y / _CadenceRange.y)) / 2;
            _EmissionProps.Cadence = tempAutomation;
            _EmissionProps.Duration = tempAutomation * timingRange;
            Debug.Log(timingRange);
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
                automation = Mathf.PerlinNoise(Time.time * speed, Time.time * speed * .5f);
                outputValue = Mathf.Lerp(range.x, range.y, automation);
                break;
            case Automation.Straight:
                phase += Time.deltaTime * speed / _GrainSynth._AudioClips[_Emitter._EmissionProps._ClipIndex].length;
                outputValue = _PlayheadPhase % 1;
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
