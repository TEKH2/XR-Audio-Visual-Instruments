using Unity.Entities;

public struct EmitterComponent : IComponentData
{
    public float _Timer;
    public float _Cadence;
    public int _DurationInSamples;
    public int _PreviousEmissionDSPSampleIndex;

    public int _SampleIndexMax;
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

    public int _DSPSamplePlaybackStart;
    public bool _Populated;
}

public struct DSPTimerComponent : IComponentData
{
    public int _CurrentDSPSample;
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