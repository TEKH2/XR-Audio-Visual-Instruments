using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

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
    public float _EmitterToListenerActivationRange;
    public float _EmitterToSpeakerAttachRadius;
}


public struct GrainProcessor : IComponentData
{
    public AudioClipDataComponent _AudioClipDataComponent;

    public float _PlayheadNorm;
    public int _DurationInSamples;

    public float _Pitch;
    public float _Volume;

    public int _SpeakerIndex;

    public int _DSPSamplePlaybackStart;
    public bool _SamplePopulated;
}

public struct GrainSpeakerComponent : IComponentData
{
    public int _SpeakerIndex;
}

public struct PooledObjectComponent : IComponentData
{
    public PooledObjectState _State;
}

public enum PooledObjectState
{
    Pooled,
    Active
}

public struct StaticallyPairedTag : IComponentData
{
}

public struct DSPTimerComponent : IComponentData
{
    public int _CurrentDSPSample;
    public int _GrainQueueDuration;
}


public struct EmitterComponent : IComponentData
{
    public bool _AttachedToSpeaker;
    public int _SpeakerIndex;
    public bool _InRange;

    public int _Index;

    public bool _Playing;   

    public int _AudioClipIndex;
    public int _CadenceInSamples;
    public int _DurationInSamples;
    public int _LastGrainEmissionDSPIndex;
    public int _RandomOffsetInSamples;
    public float _Pitch;
    public float _Volume;
    public float _DistanceAmplitude;

    public float _PlayheadPosNormalized;

    public int _DebugCount;
}

public struct Dots_DSP_BitCrush : IComponentData
{
    public float mix;
    public float downsampleFactor;
}

public struct Dots_DSP_Filter : IComponentData
{
    public float mix;
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

[System.Serializable]
public struct DSPParametersElement : IBufferElementData
{
    public DSPTypes _DSPType;
    public float _Mix;
    public float _Value0;
    public float _Value1;
    public float _Value2;
    public float _Value3;
    public float _Value4;
    public float _Value5;
    public float _Value6;
    public float _Value7;
    public float _Value8;
    public float _Value9;
    public float _Value10;
}

public enum DSPTypes
{
    Bitcrush,
    Flange,
    Delay,
    Filter,
}


#endregion

#region ---------- BLOB ASSETS

public struct FloatBlobAsset
{
    public BlobArray<float> array;
}

#endregion



public struct TestComp : IComponentData
{
    public float testVar;
}

