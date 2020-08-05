using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class GranulatorDOTS :  MonoBehaviour
{
    EntityManager _EntityManager;
    GrainManager _GrainManager;

    public void Start()
    {
        _GrainManager = GrainManager.Instance;
        _EntityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
    }

    public void ProcessGrainSample(GrainData grainData)
    {
        Entity grainEntity = _EntityManager.CreateEntity();
        _EntityManager.AddComponentData(grainEntity,
            new GrainBufferComponant()
            {
                _AudioClipIndex = grainData._ClipIndex,
                _StartSampleIndex = grainData._StartSampleIndex,
                _LengthInSamples = (int)(_GrainManager._AudioClipLibrary._Clips[grainData._ClipIndex].frequency / 1000 * grainData._Duration),

                _Pitch = grainData._Pitch,
                _Volume = grainData._Volume,

                _DSPStartSampleIndex = grainData._StartDSPSampleIndex,

                _Populated = false,
                _ReadBack = false
            });

        _EntityManager.AddBuffer<FloatBufferElement>(grainEntity);
    }
}


public class GranulatorSystem : SystemBase
{
    // Command buffer for removing tween componants once they are completed
    private EndSimulationEntityCommandBufferSystem _CommandBufferSystem;

    protected override void OnCreate()
    {
        base.OnCreate();
        _CommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        // Acquire an ECB and convert it to a concurrent one to be able to use it from a parallel job.
        EntityCommandBuffer.ParallelWriter entityCommandBuffer = _CommandBufferSystem.CreateCommandBuffer().AsParallelWriter();

        Entities.ForEach((ref GrainBufferComponant grain, in DynamicBuffer<FloatBufferElement> sampleBuffer) =>
        {
            float increment = grain._Pitch;
            float sourceIndex = grain._StartSampleIndex;

            for (int i = 0; i < grain._LengthInSamples; i++)
            {                
                // Replacement pingpong function
                if (sourceIndex + increment < 0 || sourceIndex + increment > audioClipLength - 1)
                {
                    increment = increment * -1f;
                    sourceIndex -= 1;
                }

                sourceIndex += increment;
                sourceIndexRemainder = sourceIndex % 1;

                // Interpolate sample if not integer
                if (sourceIndexRemainder != 0)
                    sourceValue = Mathf.Lerp(
                        _GrainManager._AudioClipLibrary._ClipsDataArray[gd._ClipIndex][(int)sourceIndex],
                        _GrainManager._AudioClipLibrary._ClipsDataArray[gd._ClipIndex][(int)sourceIndex + 1],
                        sourceIndexRemainder);
                else
                    sourceValue = _GrainManager._AudioClipLibrary._ClipsDataArray[gd._ClipIndex][(int)sourceIndex];

                grainPlaybackData._GrainSamples[i] = sourceValue;
            }



            grain._Populated = true;

        }).ScheduleParallel();
    }
}

public struct GrainBufferComponant : IComponentData
{
    public int _AudioClipIndex;
    public int _StartSampleIndex;
    public int _LengthInSamples;

    public float _Pitch;
    public float _Volume;

    public int _DSPStartSampleIndex;

    public bool _Populated;
    public bool _ReadBack;
}

public struct FloatBufferElement : IBufferElementData
{
    public float Value;
}