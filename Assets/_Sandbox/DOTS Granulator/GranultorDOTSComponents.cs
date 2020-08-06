using Unity.Entities;

public struct EmitterComponent : IComponentData
{
    public float _Timer;
    public float _Cadence;
    public int _DurationInSamples;
}

public struct AudioClipDataComponent :IComponentData
{
    public BlobAssetReference<FloatBlobAsset> _ClipDataBlobAsset;
    public int _ClipIndex;
}




public struct GrainProcessor : IComponentData
{
    public AudioClipDataComponent _AudioClipDataComponent;

    public float _PlaybackHeadSamplePos;
    public int _DurationInSamples;

    public float _Pitch;
    public float _Volume;

    public int _SpeakerIndex;
   

    public bool _Populated;
}



public struct SampleProcessor : IComponentData
{
    // The index of the sample
    public int _SampleOutputArrayIndex;

    public Entity _ClipDataEntity;
    public Entity _GrainEntity;

    // Processing vars
    public float _Pitch;
    public float _Volume;
}

public struct AudioClipLibraryComponent : IComponentData
{
}

public struct FloatBufferElement : IBufferElementData
{
    public float Value;
}

public struct EntityBufferElement : IBufferElementData
{
    public Entity Value;
}



// Blob asset
public struct FloatBlobAsset
{
    public BlobArray<float> array;
}