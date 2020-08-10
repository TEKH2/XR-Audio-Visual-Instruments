using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[DisallowMultipleComponent]
[RequiresEntityConversion]
public class EmitterDOTSAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public float _CadenceInMS = 20;
    public float _DurationInMS = 20;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new EmitterComponent
        {
            _CadenceInSamples = (int)(_CadenceInMS * AudioSettings.outputSampleRate * .001f),
            _DurationInSamples = (int)(_DurationInMS * AudioSettings.outputSampleRate * .001f)
        });
    }
}
