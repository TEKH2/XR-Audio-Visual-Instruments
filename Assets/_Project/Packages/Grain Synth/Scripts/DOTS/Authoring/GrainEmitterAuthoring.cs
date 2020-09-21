using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Unity.Transforms;
using Random = UnityEngine.Random;

[System.Serializable]
public class GrainEmissionProps
{
    public bool _Playing = true;

    [Header("Clip")]
    public int _ClipIndex = 0;

    // Position (normalised)
    //---------------------------------------------------------------------
    [Range(0.0f, 1.0f)]
    [SerializeField]
    public float _Playhead = 0;
    [Range(0.0f, 1f)]
    [SerializeField]
    public float _PlayheadRand = 0;
    public float Position
    {
        get
        {
            return Mathf.Clamp(_Playhead + Random.Range(0, _PlayheadRand), 0f, 1f);
        }
        set
        {
            _Playhead = Mathf.Clamp(value, 0f, 1f);
        }
    }

    [Header("Timing")]
    [Range(3.0f, 1000f)]
    public float _Cadence = 20;             // ms
    [Range(0f, 1000f)]
    public float _CadenceRandom = 0;        // ms
    public float Cadence
    {
        get
        {
            return Mathf.Clamp(_Cadence + Random.Range(0, _CadenceRandom), 3f, 1000f);
        }
        set
        {
            _Cadence = Mathf.Clamp(value, 3f, 1000f);
        }
    }



    // Duration (ms)
    //---------------------------------------------------------------------
    [Range(2.0f, 1000f)]
    [SerializeField]
    public float _Duration = 100;
    [Range(0.0f, 500f)]
    [SerializeField]
    public float _DurationRandom = 0;
    public float Duration
    {
        get
        {
            return Mathf.Clamp(_Duration + Random.Range(0, _DurationRandom), 2, 1000);
        }
        set
        {
            _Duration = Mathf.Clamp(value, 2, 1000);
        }
    }

    [Header("Effects")]
    // Volume
    //---------------------------------------------------------------------
    [Range(0.0f, 2.0f)]
    [SerializeField]
    public float _Volume = 1;          // from 0 > 1
    [Range(0.0f, 1.0f)]
    [SerializeField]
    public float _VolumeRandom = 0;      // from 0 > 1
    public float Volume
    {
        get
        {
            return Mathf.Clamp(_Volume + Random.Range(-_VolumeRandom, _VolumeRandom), 0f, 3f);
        }
        set
        {
            _Volume = (int)Mathf.Clamp(value, 0f, 3f);
        }
    }


    // Transpose
    //---------------------------------------------------------------------
    [Range(-3f, 3f)]
    [SerializeField]
    public float _Transpose = 0;
    [Range(0f, 1f)]
    [SerializeField]
    public float _TransposeRandom = 0;

    float _Pitch = 1;
    public float Pitch
    {
        get
        {
            _Pitch = Mathf.Pow(2, Mathf.Clamp(_Transpose + Random.Range(-_TransposeRandom, _TransposeRandom), -4f, 4f));
            return Mathf.Clamp(_Pitch, 0.06f, 16f);
        }
    }

    public GrainEmissionProps(float pos, int duration, float pitch, float volume,
        float posRand = 0, int durationRand = 0, float pitchRand = 0, float volumeRand = 0)
    {
        _Playhead = pos;
        _Duration = duration;
        _Pitch = pitch;
        _Volume = volume;

        _PlayheadRand = posRand;
        _DurationRandom = durationRand;
        _VolumeRandom = volumeRand;
    }
}


[DisallowMultipleComponent]
[RequiresEntityConversion]
public class GrainEmitterAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{  
    public GrainEmissionProps _EmissionProps;

    Entity _EmitterEntity;
    EntityManager _EntityManager;

    bool _Initialized = false;

    bool _StaticallyPaired = false;
    GrainSpeakerAuthoring _PairedSpeaker;

    float _Timer = 0;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        _EmitterEntity = entity;

        // If this emitter has a speaker componenet then it is statically paired        
        if (gameObject.GetComponent<GrainSpeakerAuthoring>() != null)
        {
            _PairedSpeaker = gameObject.GetComponent<GrainSpeakerAuthoring>();
            _PairedSpeaker._StaticallyPaired = true;
            _StaticallyPaired = true;
            dstManager.AddComponentData(_EmitterEntity, new StaticallyPairedTag { });
        }

        // Add emitter component
        dstManager.AddComponentData(_EmitterEntity, new EmitterComponent
        {
            _Playing = _EmissionProps._Playing,
            _AttachedToSpeaker = _StaticallyPaired,
            _InRange = _StaticallyPaired,
            _CadenceInSamples = (int)(_EmissionProps.Cadence * AudioSettings.outputSampleRate * .001f),
            _DurationInSamples = (int)(_EmissionProps.Duration * AudioSettings.outputSampleRate * .001f),
            _LastGrainEmissionDSPIndex = GrainSynth.Instance._CurrentDSPSample,
            _RandomOffsetInSamples = (int)(AudioSettings.outputSampleRate * UnityEngine.Random.Range(0, .05f)),
            _Pitch = _EmissionProps.Pitch,
            _Volume = _EmissionProps.Volume,
            _AudioClipIndex = _EmissionProps._ClipIndex,
            _SpeakerIndex = 0,
            _PlayheadPosNormalized = _EmissionProps.Position,
        });

        _Initialized = true;
    }

    void Start()
    {
        _EntityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
    }

    void Update()
    {
        if (!_Initialized)
            return;

        _Timer += Time.deltaTime;

        // TODO, only do when updated
        //_EmissionProps._FilterCoefficients = FilterConstruction.CreateCoefficents(_EmissionProps._DSP_Properties);


        EmitterComponent data = _EntityManager.GetComponentData<EmitterComponent>(_EmitterEntity);

        int attachedSpeakerIndex = _StaticallyPaired ? _PairedSpeaker._SpeakerIndex : data._SpeakerIndex;
        _EntityManager.SetComponentData(_EmitterEntity, new EmitterComponent
        {
            _Playing = _EmissionProps._Playing,
            _SpeakerIndex = attachedSpeakerIndex,
            _AttachedToSpeaker = data._AttachedToSpeaker,
            _AudioClipIndex = _EmissionProps._ClipIndex,
            _InRange = data._InRange,
            _CadenceInSamples = (int)(_EmissionProps.Cadence * AudioSettings.outputSampleRate * .001f),
            _DurationInSamples = (int)(_EmissionProps.Duration * AudioSettings.outputSampleRate * .001f),
            _LastGrainEmissionDSPIndex = data._LastGrainEmissionDSPIndex,
            _RandomOffsetInSamples = data._RandomOffsetInSamples,
            _Pitch = _EmissionProps.Pitch,
            _Volume = _EmissionProps.Volume,
            _PlayheadPosNormalized = _EmissionProps.Position
        });

        Translation trans = _EntityManager.GetComponentData<Translation>(_EmitterEntity);
        _EntityManager.SetComponentData(_EmitterEntity, new Translation
        {
            Value = transform.position
        });
    }
}