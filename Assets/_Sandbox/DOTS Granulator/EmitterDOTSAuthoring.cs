using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Unity.Transforms;

[DisallowMultipleComponent]
[RequiresEntityConversion]
public class EmitterDOTSAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public GrainEmissionProps _EmissionProps;

    Entity _EmitterEntity;
    EntityManager _EntityManager;

    bool _Initialized = false;

    float _Timer = 0;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        _EmitterEntity = entity;

        // Add DSP components
        dstManager.AddComponentData(_EmitterEntity, new DSP_BitCrush { downsampleFactor = _EmissionProps._DSP_Properties.DownsampleFactor });

        dstManager.AddComponentData(_EmitterEntity, new DSP_Filter
        {
            a0 = _EmissionProps._FilterCoefficients.a0,
            a1 = _EmissionProps._FilterCoefficients.a1,
            a2 = _EmissionProps._FilterCoefficients.a2,
            b1 = _EmissionProps._FilterCoefficients.b1,
            b2 = _EmissionProps._FilterCoefficients.b2
        });

        // Add emitter component
        dstManager.AddComponentData(_EmitterEntity, new EmitterComponent
        {
            _Active = false,
            _CadenceInSamples = (int)(_EmissionProps.Cadence * AudioSettings.outputSampleRate * .001f),
            _DurationInSamples = (int)(_EmissionProps.Duration * AudioSettings.outputSampleRate * .001f),
            _LastGrainEmissionDSPIndex = GranulatorDOTS.Instance._CurrentDSPSample,
            _RandomOffsetInSamples = (int)(AudioSettings.outputSampleRate * UnityEngine.Random.Range(0, .05f)),
            _Pitch = _EmissionProps.Pitch,
            _Volume = _EmissionProps.Volume,
            _SpeakerIndex = 0,
            _PlayheadPosNormalized = _EmissionProps.Position,
            // Use entity manager to get the bitcrush
            _BitCrush = _EntityManager.GetComponentData<DSP_BitCrush>(_EmitterEntity),
            _Filter = _EntityManager.GetComponentData<DSP_Filter>(_EmitterEntity)
        });

        _Initialized = true;
    }

    void Start()
    {
        _EntityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
    }

    void Update()
    {
        if (!_Initialized)
            return;

        _Timer += Time.deltaTime;
       

        EmitterComponent emitter = _EntityManager.GetComponentData<EmitterComponent>(_EmitterEntity);

        _EntityManager.SetComponentData(_EmitterEntity, new DSP_BitCrush { downsampleFactor = _EmissionProps._DSP_Properties.DownsampleFactor});
        _EntityManager.SetComponentData(_EmitterEntity, new DSP_Filter
        {
            a0 = _EmissionProps._FilterCoefficients.a0,
            a1 = _EmissionProps._FilterCoefficients.a1,
            a2 = _EmissionProps._FilterCoefficients.a2,
            b1 = _EmissionProps._FilterCoefficients.b1,
            b2 = _EmissionProps._FilterCoefficients.b2
        });

        EmitterComponent data = _EntityManager.GetComponentData<EmitterComponent>(_EmitterEntity);

        _EntityManager.SetComponentData(_EmitterEntity, new EmitterComponent
        {
            _Active = data._Active,
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

//EmitterComponent data = _EntityManager.GetComponentData<EmitterComponent>(_EmitterEntity);

//_EntityManager.SetComponentData(_EmitterEntity, new EmitterComponent
//        {
//            _Active = data._Active,
//            _CadenceInSamples = (int) (_EmissionProps.Cadence* AudioSettings.outputSampleRate* .001f),
//            _DurationInSamples = (int) (_EmissionProps.Duration* AudioSettings.outputSampleRate* .001f),
//            _LastGrainEmissionDSPIndex = emitter._LastGrainEmissionDSPIndex,
//            _RandomOffsetInSamples = emitter._RandomOffsetInSamples,
//            _Pitch = _EmissionProps.Pitch,
//            _Volume = _EmissionProps.Volume,
//            _PlayheadPosNormalized = _EmissionProps.Position,
//            _BitCrush = _EntityManager.GetComponentData<DSP_BitCrush>(_EmitterEntity),
//            _Filter = _EntityManager.GetComponentData<DSP_Filter>(_EmitterEntity)
//        });