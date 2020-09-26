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
    public float Speed = 1;
    public Vector2 Range = new Vector2(0, 1);
    private float Phase = 0;
    private GrainSynth GrainSynth;
    private GrainEmissionProps Props;
    private bool LinkedPhase = false;

    public void SetData(GrainSynth grainSynth, GrainEmissionProps props)
    {
        GrainSynth = grainSynth;
        Props = props;
        Phase = UnityEngine.Random.value * 123.74f;
    }

    public void SetLinkedPhase(float phase)
    {
        Phase = phase;
    }


    public float UpdatePhase()
    {
        Phase += Time.deltaTime * Speed / ( GrainSynth._AudioClips[Props._ClipIndex].length / GrainSynth._AudioClips[Props._ClipIndex].channels);
        return Phase;
    }

    public float GetPhase()
    {
        return Phase;
    }

    public float Process()
    {
        float output = 0;

        switch (Type)
        {
            case EmitterPropModSimpleNoise.Automation.Off:
                break;
            case EmitterPropModSimpleNoise.Automation.Perlin:
                float norm = Mathf.PerlinNoise(Phase, Phase * 0.5f);
                output = Mathf.Lerp(Range.x, Range.y, norm);
                break;
            case EmitterPropModSimpleNoise.Automation.Straight:
                output = GrainSynthSystem.Map((Phase % 1), 0, 1, Range.x, Range.y);
                break;
            case EmitterPropModSimpleNoise.Automation.PingPong:
                if ((int)(Phase % 2) == 0)
                    output = GrainSynthSystem.Map((Phase % 1), 0, 1, Range.x, Range.y);
                else
                    output = 1 - GrainSynthSystem.Map((Phase % 1), 0, 1, Range.x, Range.y);
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
