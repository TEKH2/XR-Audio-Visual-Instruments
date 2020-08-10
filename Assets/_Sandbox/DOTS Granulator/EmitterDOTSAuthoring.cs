using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[DisallowMultipleComponent]
[RequiresEntityConversion]
public class EmitterDOTSAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public GrainEmissionProps _EmissionProps;

    Entity _Entity;
    EntityManager _EntityManager;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        _Entity = entity;

        dstManager.AddComponentData(entity, new EmitterComponent
        {
            _CadenceInSamples = (int)(_EmissionProps.Cadence * AudioSettings.outputSampleRate * .001f),
            _DurationInSamples = (int)(_EmissionProps.Duration * AudioSettings.outputSampleRate * .001f),
            _LastGrainEmissionDSPIndex = GrainManager.Instance._CurrentDSPSample,
            _RandomOffsetInSamples = (int)(AudioSettings.outputSampleRate * UnityEngine.Random.Range(0, .05f)),
            _Pitch = _EmissionProps.Pitch,
            _Volume = _EmissionProps.Volume,
            _PlayheadPosNormalized = _EmissionProps.Position
        });
    }

    void Start()
    {
        _EntityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
    }

    void Update()
    {
        //EmitterComponent emitter = _EntityManager.GetComponentData<EmitterComponent>(_Entity);
        //_EntityManager.SetComponentData(_Entity, new EmitterComponent
        //{
        //    _CadenceInSamples = (int)(_EmissionProps.Cadence * AudioSettings.outputSampleRate * .001f),
        //    _DurationInSamples = (int)(_EmissionProps.Duration * AudioSettings.outputSampleRate * .001f),
        //    _LastGrainEmissionDSPIndex = emitter._LastGrainEmissionDSPIndex,
        //    _RandomOffsetInSamples = emitter._RandomOffsetInSamples,
        //    _Pitch = _EmissionProps.Pitch,
        //    _Volume = _EmissionProps.Volume,
        //    _PlayheadPosNormalized = _EmissionProps.Position
        //});
    }
}
