using Ludiq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class GrainSynthVisualizer : MonoBehaviour
{
    ParticleSystem _PS;
    ParticleSystem.EmissionModule _Emission;
    ParticleSystem.MainModule _Main;

    public float _Scale = .1f;
    public float _Distance = 3;
    public float _Lifetime = 3;

    float _SampleRate;
    public int _HeightCount = 4;
    int _Increment = 0;

    public Transform _XAxisPivot;
    public Transform _XAxisPivot_Frametime;
    public Transform _YAxisPivot;

    public GrainSpeakerAuthoring _GrainSpeaker;

    
    

    // Start is called before the first frame update
    void Start()
    {
        _PS = GetComponent<ParticleSystem>();
        _Emission = _PS.emission;
        _Main = _PS.main;
        _Main.startLifetime = _Lifetime;
        _Emission.rateOverTime = 0;

        _SampleRate = AudioSettings.outputSampleRate;
        _GrainSpeaker.OnGrainEmitted += EmitGrain;
    }

    // Update is called once per frame
    void Update()
    {
        if (_YAxisPivot != null)
            _YAxisPivot.SetScaleY(_Scale * _HeightCount);

        if (_XAxisPivot != null)
            _XAxisPivot.SetScaleX(_Distance);

        if (Application.isPlaying && _XAxisPivot_Frametime != null)
            _XAxisPivot_Frametime.SetScaleX(-GrainSynth.Instance._EmissionLatencyMS * .001f * (_Distance / _Lifetime));
    }

    int prevEmitSample;
    int sampleGap;
    public void EmitGrain(GrainPlaybackData grainData, int currentDSPSample)
    {
        // DEBUG
        sampleGap = (int)(_SampleRate * .001f * 20);
        int sampleTIming = grainData._DSPStartIndex - prevEmitSample;
        if (sampleTIming != sampleGap)
            print(sampleTIming + "   " + sampleGap);

        prevEmitSample = grainData._DSPStartIndex;

        ParticleSystem.EmitParams emit = new ParticleSystem.EmitParams();

        // Position based on start index
        int sampleDiff = grainData._DSPStartIndex - currentDSPSample;
        Vector3 worldPos = transform.position + transform.right * (sampleDiff / _SampleRate) * (_Distance / _Lifetime);
        worldPos.y += (_Increment % _HeightCount) * _Scale;
        worldPos.y += _Scale * .5f;
        emit.position = worldPos;

        // Scale based on duration
        float durationInSeconds = grainData._PlaybackSampleCount / _SampleRate;
        durationInSeconds *= _Distance / _Lifetime;
        Vector3 size = new Vector3(durationInSeconds, _Scale, .001f);
        emit.startSize3D = size;

        emit.startColor = Color.white;

        if (grainData._DSPStartIndex <= currentDSPSample)
            emit.startColor = Color.yellow;

        // Velocity based on lifetime/dist
        emit.velocity = -transform.right * (_Distance/_Lifetime);

        _PS.Emit(emit, 1);

        _Increment++;
    }



    private void OnDrawGizmos()
    {
        Gizmos.DrawLine(transform.position, transform.position +  -transform.right * _Distance);
        Gizmos.DrawLine(transform.position, transform.position + transform.up * _Scale * _HeightCount);
    }
}
