using Unity.Entities;

public struct AudioClipDataComponent :IComponentData
{
    public BlobAssetReference<FloatBlobAsset> _ClipDataBlobAsset;
    public int _ClipIndex;
}

public struct WindowingDataComponent : IComponentData
{
    public BlobAssetReference<FloatBlobAsset> _WindowingArray;   
}

public struct GrainProcessor : IComponentData
{
    public AudioClipDataComponent _AudioClipDataComponent;

    public float _PlaybackHeadNormPos;
    public int _DurationInSamples;

    public float _Pitch;
    public float _Volume;

    public int _SpeakerIndex;

    public int _DSPSamplePlaybackStart;
    public bool _Populated;
}

public struct DSPTimerComponent : IComponentData
{
    public int _CurrentDSPSample;
    public int _EmissionLatencyInSamples;
}

public struct FloatBufferElement : IBufferElementData
{
    public float Value;
}

// Blob asset
public struct FloatBlobAsset
{
    public BlobArray<float> array;
}

public struct EmitterComponent : IComponentData
{
    public int _CadenceInSamples;
    public int _DurationInSamples;
    public int _LastGrainEmissionDSPIndex;
    public int _RandomOffsetInSamples;

    public float _Pitch;
    public float _Volume;

    public float _PlayheadPosNormalized;
}

// ---------- DSPS
public struct DSP_VolumeScalar : IComponentData
{
    public float _VolumeScalar;
}