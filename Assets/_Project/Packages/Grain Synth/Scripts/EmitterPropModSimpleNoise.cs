using EXPToolkit;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YamlDotNet.Core;

[System.Serializable]
public class AutomationData
{
    public EmitterPropModSimpleNoise.Automation Type = EmitterPropModSimpleNoise.Automation.Off;
    public float _Speed = 1;
    public Vector2 _Range = new Vector2(0, 1);
    private float _Phase = 0;
    private GrainSynth _GrainSynth;
    private GrainEmissionProps _Props;
    private bool LinkedPhase = false;

    public void SetData(GrainSynth grainSynth, GrainEmissionProps props)
    {
        _GrainSynth = grainSynth;
        _Props = props;
        _Phase = UnityEngine.Random.value * 123.74f;
    }

    public void SetLinkedPhase(float phase)
    {
        _Phase = phase;
    }

    public float UpdatePhase()
    {
        _Phase += Time.deltaTime * _Speed / ( _GrainSynth._AudioClips[_Props._ClipIndex].length / _GrainSynth._AudioClips[_Props._ClipIndex].channels);
        return _Phase;
    }

    public float Process()
    {
        float output = 0;

        switch (Type)
        {
            case EmitterPropModSimpleNoise.Automation.Off:
                break;
            case EmitterPropModSimpleNoise.Automation.Perlin:
                output = Mathf.Lerp(_Range.x, _Range.y, Mathf.PerlinNoise(_Phase, _Phase * 0.5f));
                break;
            case EmitterPropModSimpleNoise.Automation.Straight:
                output = GrainSynthSystem.Map((_Phase % 1), 0, 1, _Range.x, _Range.y);
                break;
            case EmitterPropModSimpleNoise.Automation.PingPong:
                if ((int)(_Phase % 2) == 0)
                    output = Mathf.Lerp(_Range.x, _Range.y, _Phase % 1);
                else
                    output = 1 - Mathf.Lerp(_Range.x, _Range.y, _Phase % 1);
                break;
            case EmitterPropModSimpleNoise.Automation.Sine:
                output = Mathf.Lerp(_Range.x, _Range.y, (1 + Mathf.Sin(_Phase * 2)) / 2);
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
        Straight,
        PingPong,
        Sine,
        Perlin
    }

    public GrainSynth _GrainSynth;
    private GrainEmitterAuthoring _Emitter;
    private GrainEmissionProps _EmissionProps;

    public AutomationData _Playhead;
    public AutomationData _Transpose;
    public AutomationData _Cadence;
    public bool _LinkTiming = false;
    public AutomationData _Duration;

    private void Awake()
    {
        _Emitter = GetComponent<GrainEmitterAuthoring>();
        _EmissionProps = _Emitter._EmissionProps;

        _Playhead.SetData(_GrainSynth, _EmissionProps);
        _Transpose.SetData(_GrainSynth, _EmissionProps);
        _Cadence.SetData(_GrainSynth, _EmissionProps);
        _Duration.SetData(_GrainSynth, _EmissionProps);
    }

    private void Update()
    {
        _EmissionProps = _Emitter._EmissionProps;

        if (_Playhead.Type != Automation.Off)
        {
            _Playhead.UpdatePhase();
            _EmissionProps._Playhead = _Playhead.Process();
        }

        if (_Transpose.Type != Automation.Off)
        {
            _Transpose.UpdatePhase();
            _EmissionProps._Transpose = _Transpose.Process();
        }

        if (_LinkTiming && _Cadence.Type != Automation.Off)
        {
            _Duration.SetLinkedPhase(_Cadence.UpdatePhase());
            _EmissionProps._Cadence = _Cadence.Process();
            _EmissionProps._Duration = _Duration.Process();
        }
        else
        {
            if (_Cadence.Type != Automation.Off)
            {
                _Cadence.UpdatePhase();
                _EmissionProps._Cadence = _Cadence.Process();
            }
            if (_Duration.Type != Automation.Off)
            {
                _Duration.UpdatePhase();
                _EmissionProps._Duration = _Duration.Process();
            }
        }

        _Emitter._EmissionProps = _EmissionProps;
    }
}
