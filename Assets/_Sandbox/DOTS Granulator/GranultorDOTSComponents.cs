using Unity.Entities;
using Unity.Mathematics;

#region ---------- COMPONENTS


public struct AudioClipDataComponent :IComponentData
{
    public BlobAssetReference<FloatBlobAsset> _ClipDataBlobAsset;
    public int _ClipIndex;
}

public struct WindowingDataComponent : IComponentData
{
    public BlobAssetReference<FloatBlobAsset> _WindowingArray;   
}

public struct SpeakerManagerComponent : IComponentData
{
    public float3 _ListenerPos;
    public DynamicBuffer<GrainSpeakerBufferElement> _Speakers;
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
    public bool _SamplePopulated;
}

public struct GrainSpeakerComponent : IComponentData
{
    public bool _Active;
    public int _Index;
}

public struct DSPTimerComponent : IComponentData
{
    public int _CurrentDSPSample;
    public int _EmissionLatencyInSamples;
}


public struct EmitterComponent : IComponentData
{
    public bool _Active;
    public int _SpeakerIndex;

    public int _CadenceInSamples;
    public int _DurationInSamples;
    public int _LastGrainEmissionDSPIndex;
    public int _RandomOffsetInSamples;

    public float _Pitch;
    public float _Volume;

    public float _PlayheadPosNormalized;

    public DSP_BitCrush _BitCrush;
    public DSP_Filter _Filter;

    public int _DebugCount;
}

public struct DSP_BitCrush : IComponentData
{
    public float downsampleFactor;
}

public struct DSP_Filter : IComponentData
{
    public float a0;
    public float a1;
    public float a2;
    public float b1;
    public float b2;
}


#endregion

#region ---------- BUFFER ELEMENTS

public struct GrainSampleBufferElement : IBufferElementData
{
    public float Value;
}

public struct GrainSpeakerBufferElement : IBufferElementData
{
    public GrainSpeakerComponent Value;
}

#endregion

#region ---------- BLOB ASSETS

public struct FloatBlobAsset
{
    public BlobArray<float> array;
}

#endregion
