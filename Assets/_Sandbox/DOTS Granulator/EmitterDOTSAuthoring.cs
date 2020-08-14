using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Unity.Transforms;

[DisallowMultipleComponent]
[RequiresEntityConversion]
public class EmitterDOTSAuthoring : MonoBehaviour
{
    public GrainEmissionProps _EmissionProps;

    Entity _EmitterEntity;
    EntityManager _EntityManager;

    bool _Initialized = false;

    void Start()
    {
        _EntityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        _EmitterEntity = _EntityManager.CreateEntity();

        // Add DSP components
        _EntityManager.AddComponentData(_EmitterEntity, new DSP_BitCrush { downsampleFactor = _EmissionProps._DSP_Properties.DownsampleFactor });
        _EntityManager.AddComponentData(_EmitterEntity, new DSP_Filter
        {
            a0 = _EmissionProps._FilterCoefficients.a0,
            a1 = _EmissionProps._FilterCoefficients.a1,
            a2 = _EmissionProps._FilterCoefficients.a2,
            b1 = _EmissionProps._FilterCoefficients.b1,
            b2 = _EmissionProps._FilterCoefficients.b2
        });

        // Add emitter component
        _EntityManager.AddComponentData(_EmitterEntity, new EmitterComponent
        {
            _CadenceInSamples = (int)(_EmissionProps.Cadence * AudioSettings.outputSampleRate * .001f),
            _DurationInSamples = (int)(_EmissionProps.Duration * AudioSettings.outputSampleRate * .001f),
            _LastGrainEmissionDSPIndex = GrainManager.Instance._CurrentDSPSample,
            _RandomOffsetInSamples = (int)(AudioSettings.outputSampleRate * UnityEngine.Random.Range(0, .05f)),
            _Pitch = _EmissionProps.Pitch,
            _Volume = _EmissionProps.Volume,
            _PlayheadPosNormalized = _EmissionProps.Position,
            // Use entity manager to get the bitcrush
            _BitCrush = _EntityManager.GetComponentData<DSP_BitCrush>(_EmitterEntity),
            _Filter = _EntityManager.GetComponentData<DSP_Filter>(_EmitterEntity)
        });

        _Initialized = true;

        //_EntityManager.AddComponentData(_Entity, new Translation { Value = transform.position });
    }

    void Update()
    {
        if (!_Initialized)
            return;

        EmitterComponent emitter = _EntityManager.GetComponentData<EmitterComponent>(_EmitterEntity);

        _EntityManager.SetComponentData(_EmitterEntity, new DSP_BitCrush { downsampleFactor = _EmissionProps._DSP_Properties.DownsampleFactor});
        _EntityManager.AddComponentData(_EmitterEntity, new DSP_Filter
        {
            a0 = _EmissionProps._FilterCoefficients.a0,
            a1 = _EmissionProps._FilterCoefficients.a1,
            a2 = _EmissionProps._FilterCoefficients.a2,
            b1 = _EmissionProps._FilterCoefficients.b1,
            b2 = _EmissionProps._FilterCoefficients.b2
        });

        _EntityManager.SetComponentData(_EmitterEntity, new EmitterComponent
        {
            _CadenceInSamples = (int)(_EmissionProps.Cadence * AudioSettings.outputSampleRate * .001f),
            _DurationInSamples = (int)(_EmissionProps.Duration * AudioSettings.outputSampleRate * .001f),
            _LastGrainEmissionDSPIndex = emitter._LastGrainEmissionDSPIndex,
            _RandomOffsetInSamples = emitter._RandomOffsetInSamples,
            _Pitch = _EmissionProps.Pitch,
            _Volume = _EmissionProps.Volume,
            _PlayheadPosNormalized = _EmissionProps.Position,
            _BitCrush = _EntityManager.GetComponentData<DSP_BitCrush>(_EmitterEntity),
            _Filter = _EntityManager.GetComponentData<DSP_Filter>(_EmitterEntity)
        });
    }
}
