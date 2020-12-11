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
    public int _SampleCount;

    public float _Pitch;
    public float _Volume;

    public int _SpeakerIndex;

    public int _DSPStartIndex;
    public bool _SamplePopulated;

    public int _DSPEffectSampleTailLength;
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
    public bool _StaticallyPaired;
    public bool _InRange;

    public int _EmitterIndex;

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

    public int _SampleRate;

    public int _DebugCount;
}

public struct ModulateParameterComponent : IComponentData
{
    public float _StartValue;
    public float _EndValue;
    public float _Random;
    public float _RandomScale;
    public float _Shape;
    public float _InteractionAmt;
}

public struct BurstEmitterComponent : IComponentData
{
    public bool _AttachedToSpeaker;
    public int _SpeakerIndex;
    public bool _StaticallyPaired;
    public bool _InRange;
    public int _EmitterIndex;
    public bool _Playing;
    public int _AudioClipIndex;
    public int _BurstCount;
    public int _BurstDuration;
    public float _BurstShape;
    public float _BurstRandom;
    public ModulateParameterComponent _Playhead;
    public ModulateParameterComponent _Duration;
    public ModulateParameterComponent _Transpose;
    public ModulateParameterComponent _Volume;

    public float _DistanceAmplitude;

    public int _LastGrainEmissionDSPIndex;
    public int _RandomOffsetInSamples;

    public float _PlayheadPosNormalized;

    public int _SampleRate;

    public int _DebugCount;
}


public struct RingBufferFiller : IComponentData
{
    public int _StartIndex;  
    public int _EndIndex;
    public int _SampleCount;
}

#endregion

#region ---------- BUFFER ELEMENTS
// Capacity set to a 1 second length by default
//[InternalBufferCapacity(44100)]
public struct GrainSampleBufferElement : IBufferElementData
{
    public float Value;
}

public struct DSPSampleBufferElement : IBufferElementData
{
    public float Value;
}

//[InternalBufferCapacity(44100)]
public struct AudioRingBufferElement : IBufferElementData
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
    public int _SampleRate;
    public int _SampleTail;
    public int _SampleStartTime;
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
    Chopper
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

