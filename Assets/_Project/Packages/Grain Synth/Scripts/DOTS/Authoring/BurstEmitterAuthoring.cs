using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Unity.Transforms;
using Random = UnityEngine.Random;

[System.Serializable]
public class BurstEmissionProps
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

    [Header("Burst")]
    [Range(1, 100)]
    [SerializeField]
    public int _BurstCount = 10;
    [Range(10f, 1000f)]
    [SerializeField]
    public float _BurstDuration = 100f;
    [Range(0.5f, 5.0f)]
    [SerializeField]
    public float _BurstShape = 1f;

    [Header("Duration")]
    [Range(2.0f, 1000f)]
    [SerializeField]
    public float _DurationStart = 20f;
    [Range(2.0f, 1000f)]
    [SerializeField]
    public float _DurationEnd = 50f;

    [Header("Pitch")]
    [Range(-3f, 3f)]
    [SerializeField]
    public float _TransposeStart = 0;
    [Range(-3f, 3f)]
    [SerializeField]
    public float _TransposeEnd = 0;

    [Header("Volume")]
    [Range(0.0f, 2.0f)]
    [SerializeField]
    public float _VolumeStart = 1;
    [Range(0.0f, 2.0f)]
    [SerializeField]
    public float _VolumeEnd = 1;



    public float TransposeToPitch(float transpose)
    {
        return Mathf.Pow(2, Mathf.Clamp(transpose, -4f, 4f));
    }


    public BurstEmissionProps(float pos, int duration, float pitch, float volume,
        float posRand = 0, int durationRand = 0, float pitchRand = 0, float volumeRand = 0)
    {
        //_Playhead = pos;
        //_Duration = duration;
        //_Pitch = pitch;
        //_VolumeStart = volume;

        //_PlayheadRand = posRand;
        //_DurationRandom = durationRand;
        //_VolumeRandom = volumeRand;
    }
}


[DisallowMultipleComponent]
[RequiresEntityConversion]
public class BurstEmitterAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    [Header("Debug")]
    public bool _AttachedToSpeaker = false;
    public int _AttachedSpeakerIndex;
    public bool _Triggered = false;
    public GrainSpeakerAuthoring _PairedSpeaker;
    public Transform _HeadPosition;

    [Header("Properties")]
    public BurstEmissionProps _BurstEmissionProps;

    [Header("DSP Effects")]
    public DSPBase[] _DSPChainParams;

    Entity _EmitterEntity;
    EntityManager _EntityManager;

    bool _Initialized = false;
    bool _StaticallyPaired = false;
    bool _InRangeTemp = false;
    
    Collision _Collision;



    public GrainSpeakerAuthoring DynamicallyAttachedSpeaker { get { return GrainSynth.Instance._GrainSpeakers[_AttachedSpeakerIndex]; } }


    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        _EmitterEntity = entity;

        // If this emitter has a speaker componenet then it is statically paired        
        if (_PairedSpeaker == null && gameObject.GetComponent<GrainSpeakerAuthoring>() != null)
        {
            _PairedSpeaker = gameObject.GetComponent<GrainSpeakerAuthoring>();          
        }

        int attachedSpeakerIndex = int.MaxValue;
        if(_PairedSpeaker != null)
        {
            _PairedSpeaker._StaticallyPairedToEmitter = true;
            _StaticallyPaired = true;
            dstManager.AddComponentData(_EmitterEntity, new StaticallyPairedTag { });
            attachedSpeakerIndex =_PairedSpeaker.GetRegisterAndGetIndex();
        }

        int index = GrainSynth.Instance.RegisterEmitter(entity);


        // Add emitter component
        dstManager.AddComponentData(_EmitterEntity, new BurstEmitterComponent
        {
            _Playing = false,
            _AttachedToSpeaker = _StaticallyPaired,
            _StaticallyPaired = _StaticallyPaired,
            _BurstCount = _BurstEmissionProps._BurstCount,
            _BurstDuration = (int)(_BurstEmissionProps._BurstDuration * AudioSettings.outputSampleRate * .001f),
            _BurstShape = _BurstEmissionProps._BurstShape,
            _DurationStart = (int)(_BurstEmissionProps._DurationStart * AudioSettings.outputSampleRate * .001f),
            _DurationEnd = (int)(_BurstEmissionProps._DurationEnd * AudioSettings.outputSampleRate * .001f),
            _PitchStart = _BurstEmissionProps.TransposeToPitch(_BurstEmissionProps._TransposeStart),
            _PitchEnd = _BurstEmissionProps.TransposeToPitch(_BurstEmissionProps._TransposeEnd),
            _VolumeStart = _BurstEmissionProps._VolumeStart,
            _VolumeEnd = _BurstEmissionProps._VolumeEnd,
            _DistanceAmplitude = 1,
            _LastGrainEmissionDSPIndex = GrainSynth.Instance._CurrentDSPSample,
            _RandomOffsetInSamples = (int)(AudioSettings.outputSampleRate * UnityEngine.Random.Range(0, .05f)),
            _AudioClipIndex = _BurstEmissionProps._ClipIndex,
            _SpeakerIndex = attachedSpeakerIndex,
            _EmitterIndex = index,
            _PlayheadPosNormalized = _BurstEmissionProps.Position,
            _SampleRate = AudioSettings.outputSampleRate
        });

        dstManager.SetName(entity, "Emitter");
        dstManager.AddBuffer<DSPParametersElement>(_EmitterEntity);
        dstManager.AddComponentData(entity, new QuadEntityType { _Type = QuadEntityType.QuadEntityTypeEnum.Emitter });

        _Initialized = true;
    }

    void Start()
    {
        _EntityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        _HeadPosition = FindObjectOfType<Camera>().transform;
    }

    // BRAD: Is this possible to detect collisions on a script with IConvertGameObjectToEntity and "Convert to Entity" component?
    void OnCollisionEnter(Collision collision)
    {
        _Collision = collision;
        _Triggered = true;
    }

    public void Collided(Collision collision)
    {
        _Collision = collision;
        _Triggered = true;
    }

    void Update()
    {
        if (!_Initialized)
            return;

        if (_Triggered)
        {
            // ----   Update DSP chain  // TODO dont think this is used
            //DynamicBuffer<DSPParametersElement> dspTypes = _EntityManager.GetBuffer<DSPParametersElement>(_EmitterEntity);
            //dspTypes.Clear();
            //for (int i = 0; i < _DSPChainParams.Length; i++)
            //{
            //    dspTypes.Add(_DSPChainParams[i].GetDSPBufferElement());
            //}

            BurstEmitterComponent data = _EntityManager.GetComponentData<BurstEmitterComponent>(_EmitterEntity);

            int attachedSpeakerIndex = _StaticallyPaired ? _PairedSpeaker._SpeakerIndex : data._SpeakerIndex;
            float distanceAmplitude = 1;
            if (data._AttachedToSpeaker)
            {
                distanceAmplitude = AudioUtils.DistanceAttenuation(
                    _HeadPosition.position,
                    GrainSynth.Instance._GrainSpeakers[attachedSpeakerIndex].gameObject.transform.position,
                    transform.position);
            }

            data._Playing = true;
            data._SpeakerIndex = attachedSpeakerIndex;
            data._AudioClipIndex = _BurstEmissionProps._ClipIndex;
            data._BurstCount = _BurstEmissionProps._BurstCount;
            data._BurstDuration = (int)(_BurstEmissionProps._BurstDuration * AudioSettings.outputSampleRate * .001f);
            data._BurstShape = _BurstEmissionProps._BurstShape;
            data._DurationStart = (int)(_BurstEmissionProps._DurationStart * AudioSettings.outputSampleRate * .001f);
            data._DurationEnd = (int)(_BurstEmissionProps._DurationEnd * AudioSettings.outputSampleRate * .001f);
            data._PitchStart = _BurstEmissionProps.TransposeToPitch(_BurstEmissionProps._TransposeStart);
            data._PitchEnd = _BurstEmissionProps.TransposeToPitch(_BurstEmissionProps._TransposeEnd);
            data._VolumeStart = _BurstEmissionProps._VolumeStart;
            data._VolumeEnd = _BurstEmissionProps._VolumeEnd;
            data._DistanceAmplitude = distanceAmplitude;
            data._PlayheadPosNormalized = _BurstEmissionProps.Position;

            _EntityManager.SetComponentData(_EmitterEntity, data);

            _InRangeTemp = data._InRange;

            _AttachedSpeakerIndex = data._SpeakerIndex;
            _AttachedToSpeaker = data._AttachedToSpeaker;

            Translation trans = _EntityManager.GetComponentData<Translation>(_EmitterEntity);
            _EntityManager.SetComponentData(_EmitterEntity, new Translation
            {
                Value = transform.position
            });

            _Triggered = false;
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = _InRangeTemp ? Color.yellow : Color.blue;
        Gizmos.DrawSphere(transform.position, .1f);
    }
}