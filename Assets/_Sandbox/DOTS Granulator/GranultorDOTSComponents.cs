using Unity.Entities;

public struct EmitterComponent : IComponentData
{
    public float _Timer;
    public float _Cadence;
}

public struct AudioClipDataComponent :IComponentData
{
    public BlobAssetReference<FloatBlobAsset> _ClipDataBlobAsset;
    public int _ClipIndex;
}




public struct GrainProcessor : IComponentData
{
    public int _SampleStartIndex;
    public int _PlaybackStartIndex;
    public int _LengthInSamples;
    public bool _Populated;
    public int _SpeakerIndex;
    public AudioClipDataComponent _AudioClipDataComponent;

    //public BlobAssetReference<FloatBlobAsset> _ClipDataBlobAsset;
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