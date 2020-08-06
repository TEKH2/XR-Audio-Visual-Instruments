using Unity.Burst;
using Unity.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Profiling;

public class GranulatorDOTS :  MonoBehaviour
{
    EntityManager _EntityManager;
    GrainManager _GrainManager;

    public AudioClip[] _AudioClips;
    Entity[] _AudioClipEntities;

    List<Entity> _GrainEntities = new List<Entity>();



    public void Start()
    {
        _GrainManager = GrainManager.Instance;
        _EntityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        _AudioClipEntities = new Entity[_AudioClips.Length];        
        // Create audio clip entities
        for (int i = 0; i < _AudioClips.Length; i++)
        {
            // Create entity
            Entity clipEntity = _EntityManager.CreateEntity();

            // Add it to local array to it can be added to the library buffer later
            _AudioClipEntities[i] = clipEntity;

            // Assign a dynamic buffer to hold all the clip samples
            DynamicBuffer<ClipDataBufferElement> buffer = _EntityManager.AddBuffer<ClipDataBufferElement>(clipEntity);
            float[] samples = new float[_AudioClips[i].samples];
            _AudioClips[i].GetData(samples, 0);
            for (int s = 0; s < samples.Length; s++)
            {
                buffer.Add( new ClipDataBufferElement { Value = samples[s] });
            }
        }

        // Create entity
        Entity clipLibraryEntity = _EntityManager.CreateEntity();

        // Add AudioClipLibraryComponent component so we can look it up in the system
        _EntityManager.AddComponentData(clipLibraryEntity, new AudioClipLibraryComponent());

        // Add a buffer that holds referecnes to all the entities with audio clip data
        DynamicBuffer<EntityBufferElement> libraryBuffer = _EntityManager.AddBuffer<EntityBufferElement>(clipLibraryEntity);
        for (int i = 0; i < _AudioClipEntities.Length; i++)
        {
            libraryBuffer.Add(new EntityBufferElement { Value = _AudioClipEntities[i] });
        }
    }

    public void ProcessGrainSample(GrainData grainData, int speakerIndex)
    {
        Profiler.BeginSample("Process grain");
        Entity grainEntity = _EntityManager.CreateEntity();

        _GrainEntities.Add(grainEntity);

        _EntityManager.AddComponentData(grainEntity,
            new GrainProcessor()
            {
                _AudioClipIndex = grainData._ClipIndex,
                _LengthInSamples = (int)(_GrainManager._AudioClipLibrary._Clips[grainData._ClipIndex].frequency / 1000 * grainData._Duration),
                _StartSampleIndex = (int)((_GrainManager._AudioClipLibrary._Clips[grainData._ClipIndex].frequency / 1000 * grainData._Duration) *  grainData._PlayheadPos),
                _Pitch = grainData._Pitch,
                _Volume = grainData._Volume,

                _DSPStartSampleIndex = grainData._StartSampleIndex,

                _Populated = false,
                _ReadBack = false,
                _ClipDataEntity = _AudioClipEntities[grainData._ClipIndex],
                _SpeakerIndex = speakerIndex
            });

        Profiler.EndSample();

        _EntityManager.AddBuffer<ClipDataBufferElement>(grainEntity);
    }

    private void Update()
    {      
        for (int i = _GrainEntities.Count - 1; i > 0; i--)
        {
            GrainProcessor processedGrain = _EntityManager.GetComponentData<GrainProcessor>(_GrainEntities[i]);
            if (processedGrain._Populated)
            {
                DynamicBuffer<ClipDataBufferElement> sampleBuffer = _EntityManager.GetBuffer<ClipDataBufferElement>(_GrainEntities[i]);
                NativeArray<float> sampleBufferFloats = sampleBuffer.Reinterpret<float>().ToNativeArray(Allocator.Temp);
                GrainPlaybackData playbackData = _GrainManager._AllSpeakers[processedGrain._SpeakerIndex].GetGrainPlaybackDataFromPool();

                playbackData._IsPlaying = true;
                playbackData._PlaybackIndex = 0;
                playbackData._PlaybackSampleCount = processedGrain._LengthInSamples;
                playbackData._StartSampleIndex = processedGrain._StartSampleIndex;
                playbackData._GrainSamples = sampleBufferFloats.ToArray();

                _GrainManager._AllSpeakers[processedGrain._SpeakerIndex].AddGrainPlaybackData(playbackData);

                _EntityManager.DestroyEntity(_GrainEntities[i]);
                _GrainEntities.RemoveAt(i);
            }
            
        }
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

    bool _UseBurst = false;
    protected override void OnUpdate()
    {
        // Acquire an ECB and convert it to a concurrent one to be able to use it from a parallel job.
        EntityCommandBuffer.ParallelWriter entityCommandBuffer = _CommandBufferSystem.CreateCommandBuffer().AsParallelWriter();

        // Get the buffer that holds the references to the entitys containing the audio clip data
        //DynamicBuffer<EntityBufferElement> audioClipLibBuffer = GetBufferFromEntity<EntityBufferElement>(true)[GetSingletonEntity<AudioClipLibraryComponent>()];       

        if (!_UseBurst)
        {
           
            Entities.WithoutBurst().ForEach((Entity entity, ref GrainProcessor grain) =>
            {
                if (!grain._Populated)
                {
                    float increment = grain._Pitch;
                    float sourceIndex = grain._StartSampleIndex;

                    DynamicBuffer<ClipDataBufferElement> audioClipData = GetBufferFromEntity<ClipDataBufferElement>(true)[grain._ClipDataEntity];
                    NativeArray<float> audioClipDataArray = audioClipData.Reinterpret<float>().ToNativeArray(Allocator.TempJob);

                    DynamicBuffer<ClipDataBufferElement> sampleBuffer = GetBufferFromEntity<ClipDataBufferElement>(false)[entity];

                    int audioClipLength = audioClipData.Length;

                    float sourceValue = 0f;
                    float sourceIndexRemainder = 0f;

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

                        // PITCHING - Interpolate sample if not integer to create 
                        if (sourceIndexRemainder != 0)
                            sourceValue = math.lerp(
                                audioClipDataArray[(int)sourceIndex],
                                audioClipDataArray[(int)sourceIndex + 1],
                                sourceIndexRemainder);
                        else
                            sourceValue = audioClipDataArray[(int)sourceIndex];

                        sourceValue = 1;
                        sampleBuffer.Add(new ClipDataBufferElement { Value = sourceValue });
                    }

                    audioClipDataArray.Dispose();
                    grain._Populated = true;
                }
            }).Run();                                       
        }
        else
        {
            // Example using burst
            Entities.ForEach((Entity entity, ref GrainProcessor grain) =>
            {
                if (!grain._Populated)
                {
                    float increment = grain._Pitch;
                    float sourceIndex = grain._StartSampleIndex;

                    DynamicBuffer<ClipDataBufferElement> audioClipData = GetBufferFromEntity<ClipDataBufferElement>(true)[grain._ClipDataEntity];
                    NativeArray<float> audioClipDataArray = audioClipData.Reinterpret<float>().ToNativeArray(Allocator.TempJob);

                    DynamicBuffer<ClipDataBufferElement> sampleBuffer = GetBufferFromEntity<ClipDataBufferElement>(false)[entity];

                    int audioClipLength = audioClipData.Length;

                    float sourceValue = 0f;
                    float sourceIndexRemainder = 0f;

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

                        // PITCHING - Interpolate sample if not integer to create 
                        if (sourceIndexRemainder != 0)
                            sourceValue = math.lerp(
                                audioClipDataArray[(int)sourceIndex],
                                audioClipDataArray[(int)sourceIndex + 1],
                                sourceIndexRemainder);
                        else
                            sourceValue = audioClipDataArray[(int)sourceIndex];

                        sampleBuffer.Add(new ClipDataBufferElement { Value = sourceValue });
                    }

                    audioClipDataArray.Dispose();
                    grain._Populated = true;
                }

            }).ScheduleParallel();
        }
    }
}


public struct GrainProcessor : IComponentData
{
    public int _AudioClipIndex;
    public int _StartSampleIndex;
    public int _LengthInSamples;

    public float _Pitch;
    public float _Volume;

    public int _DSPStartSampleIndex;

    public bool _Populated;
    public bool _ReadBack;

    public int _SpeakerIndex;

    public Entity _ClipDataEntity;
}

public struct AudioClipLibraryComponent : IComponentData
{
}

public struct ClipDataBufferElement : IBufferElementData
{
    public float Value;
}

public struct EntityBufferElement : IBufferElementData
{
    public Entity Value;
}