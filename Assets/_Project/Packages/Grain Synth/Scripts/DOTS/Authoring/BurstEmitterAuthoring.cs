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

    [Header("Burst Density")]
    [Range(1, 100)]
    [SerializeField]
    public int _BurstCount = 10;
    [Range(0f, 1f)]
    [SerializeField]
    public float _BurstCountRandom = 0f;
    [Range(-1.0f, 1.0f)]
    [SerializeField]
    public float _CountInteraction = 0f;
    [HideInInspector]
    public int _CountMin = 1;
    [HideInInspector]
    public int _CountMax = 100;

    [Header("Burst Timing")]
    [Range(10f, 1000f)]
    [SerializeField]
    public float _BurstDuration = 250f;
    [Range(0.5f, 5.0f)]
    [SerializeField]
    public float _BurstShape = 1f;
    [Range(0f, 1.0f)]
    [SerializeField]
    public float _TimingRandom = 0.01f;
    [Range(-1.0f, 1.0f)]
    [SerializeField]
    public float _TimingInteraction = 0f;
    [HideInInspector]
    public float _TimingMin = 10f;
    [HideInInspector]
    public float _TimingMax = 1000f;

    [Header("Playhead")]
    [Range(0f, 1f)]
    [SerializeField]
    public float _PlayheadStart = 0;
    [Range(0f, 1f)]
    [SerializeField]
    public float _PlayheadEnd = 0.1f;
    [Range(0.5f, 5.0f)]
    [SerializeField]
    public float _PlayheadShape = 1f;
    [Range(0f, 1.0f)]
    [SerializeField]
    public float _PlayheadRandom = 0.01f;
    [Range(-1.0f, 1.0f)]
    [SerializeField]
    public float _PlayheadInteraction = 0f;
    [HideInInspector]
    public float _PlayheadMin = 0f;
    [HideInInspector]
    public float _PlayheadMax = 1f;

    [Header("Grain Duration")]
    [Range(2f, 500f)]
    [SerializeField]
    public float _DurationStart = 20f;
    [Range(2f, 500f)]
    [SerializeField]
    public float _DurationEnd = 50f;
    [Range(0.5f, 5.0f)]
    [SerializeField]
    public float _DurationShape = 2f;
    [Range(0f, 1.0f)]
    [SerializeField]
    public float _DurationRandom = 0.01f;
    [Range(-1.0f, 1.0f)]
    [SerializeField]
    public float _DurationInteraction = 0f;
    [HideInInspector]
    public float _DurationMin = 2f;
    [HideInInspector]
    public float _DurationMax = 500f;

    [Header("Transpose")]
    [Range(-3f, 3f)]
    [SerializeField]
    public float _TransposeStart = 0;
    [Range(-3f, 3f)]
    [SerializeField]
    public float _TransposeEnd = 0;
    [Range(0.5f, 5.0f)]
    [SerializeField]
    public float _TransposeShape = 2f;
    [Range(0f, 1.0f)]
    [SerializeField]
    public float _TransposeRandom = 0.01f;
    [Range(-1.0f, 1.0f)]
    [SerializeField]
    public float _TransposeInteraction = 0f;
    [HideInInspector]
    public float _TransposeMin = -3f;
    [HideInInspector]
    public float _TransposeMax = 3f;

    [Header("Volume")]
    public bool _VolumeLockEndValue = true;
    [Range(0f, 2f)]
    [SerializeField]
    public float _VolumeStart = 1;
    [Range(0f, 2f)]
    [SerializeField]
    public float _VolumeEnd = 1;
    [Range(0.5f, 5.0f)]
    [SerializeField]
    public float _VolumeShape = 2f;
    [Range(0f, 1.0f)]
    [SerializeField]
    public float _VolumeRandom = 0.01f;
    [Range(-1.0f, 1.0f)]
    [SerializeField]
    public float _VolumeInteraction = 0f;
    [HideInInspector]
    public float _VolumeMin = 0f;
    [HideInInspector]
    public float _VolumeMax = 2f;
}


[DisallowMultipleComponent]
[RequiresEntityConversion]
public class BurstEmitterAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    [Header("Debug")]
    public bool _AttachedToSpeaker = false;
    public int _AttachedSpeakerIndex;
    public GrainSpeakerAuthoring _PairedSpeaker;
    public Transform _HeadPosition;
    public float _CollisionImpact = 0f;
    public bool _Triggered = false;

    [Header("Burst Properties")]
    public BurstEmissionProps _BurstEmissionProps;

    [Header("DSP Effects")]
    public DSPBase[] _DSPChainParams;

    Entity _BurstEntity;
    EntityManager _EntityManager;

    bool _Initialized = false;
    bool _StaticallyPaired = false;
    bool _InRangeTemp = false;
    
    Collision _Collision;

    public GrainSpeakerAuthoring DynamicallyAttachedSpeaker { get { return GrainSynth.Instance._GrainSpeakers[_AttachedSpeakerIndex]; } }

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        _BurstEntity = entity;

        // If this emitter has a speaker componenet then it is statically paired        
        if (_PairedSpeaker == null && gameObject.GetComponent<GrainSpeakerAuthoring>() != null)
        {
            _PairedSpeaker = gameObject.GetComponent<GrainSpeakerAuthoring>();          
        }

        int attachedSpeakerIndex = int.MaxValue;

        if(_PairedSpeaker != null)
        {
            _PairedSpeaker.AddPairedEmitter(gameObject);
            _StaticallyPaired = true;
            dstManager.AddComponentData(_BurstEntity, new StaticallyPairedTag { });
            attachedSpeakerIndex =_PairedSpeaker.GetRegisterAndGetIndex();
        }

        int index = GrainSynth.Instance.RegisterEmitter(entity);
        int samplesPerMS = (int)(AudioSettings.outputSampleRate * .001f);

        // Add emitter component
        dstManager.AddComponentData(_BurstEntity, new BurstEmitterComponent
        {
            _Playing = false,
            _AttachedToSpeaker = _StaticallyPaired,
            _StaticallyPaired = _StaticallyPaired,

            _InteractionInput = 0f,

            _Density = new ModulateParameterComponent
            {
                _StartValue = _BurstEmissionProps._BurstCount,
                _Random = _BurstEmissionProps._BurstCountRandom,
                _Interaction = _BurstEmissionProps._CountInteraction,
                _Min = _BurstEmissionProps._CountMin,
                _Max = _BurstEmissionProps._CountMax,
                _LockEndValue = false
            },
            _Timing = new ModulateParameterComponent
            {
                _StartValue = _BurstEmissionProps._BurstDuration * samplesPerMS,
                _Shape = _BurstEmissionProps._BurstShape,
                _Random = _BurstEmissionProps._TimingRandom,
                _Interaction = _BurstEmissionProps._TimingInteraction,
                _Min = _BurstEmissionProps._TimingMin * samplesPerMS,
                _Max = _BurstEmissionProps._TimingMax * samplesPerMS,
                _LockEndValue = false
            },
            _Playhead = new ModulateParameterComponent
            {
                _StartValue = _BurstEmissionProps._PlayheadStart,
                _EndValue = _BurstEmissionProps._PlayheadEnd,
                _Shape = _BurstEmissionProps._PlayheadShape,
                _Random = _BurstEmissionProps._PlayheadRandom,
                _Interaction = _BurstEmissionProps._PlayheadInteraction,
                _Min = _BurstEmissionProps._PlayheadMin,
                _Max = _BurstEmissionProps._PlayheadMax,
                _LockEndValue = false
            },
            _Duration = new ModulateParameterComponent
            {
                _StartValue = _BurstEmissionProps._DurationStart * samplesPerMS,
                _EndValue = _BurstEmissionProps._DurationEnd * samplesPerMS,
                _Shape = _BurstEmissionProps._DurationShape,
                _Random = _BurstEmissionProps._DurationRandom,
                _Interaction = _BurstEmissionProps._DurationInteraction,
                _Min = _BurstEmissionProps._DurationMin * samplesPerMS,
                _Max = _BurstEmissionProps._DurationMax * samplesPerMS,
                _LockEndValue = false
            },
            _Transpose = new ModulateParameterComponent
            {
                _StartValue = _BurstEmissionProps._TransposeStart,
                _EndValue = _BurstEmissionProps._TransposeEnd,
                _Shape = _BurstEmissionProps._TransposeShape,
                _Random = _BurstEmissionProps._TransposeRandom,
                _Interaction = _BurstEmissionProps._TransposeInteraction,
                _Min = _BurstEmissionProps._TransposeMin,
                _Max = _BurstEmissionProps._TransposeMax,
                _LockEndValue = false
            },
            _Volume = new ModulateParameterComponent
            {
                _StartValue = _BurstEmissionProps._VolumeStart,
                _EndValue = _BurstEmissionProps._VolumeEnd,
                _Shape = _BurstEmissionProps._VolumeShape,
                _Random = _BurstEmissionProps._VolumeRandom,
                _Interaction = _BurstEmissionProps._VolumeInteraction,
                _Min = _BurstEmissionProps._VolumeMin,
                _Max = _BurstEmissionProps._VolumeMax,
                _LockEndValue = _BurstEmissionProps._VolumeLockEndValue
            },


            _DistanceAmplitude = 1,
            _AudioClipIndex = _BurstEmissionProps._ClipIndex,
            _SpeakerIndex = attachedSpeakerIndex,
            _EmitterIndex = index,
            _SampleRate = AudioSettings.outputSampleRate
        });

        dstManager.SetName(entity, "Emitter");
        dstManager.AddBuffer<DSPParametersElement>(_BurstEntity);
        dstManager.AddComponentData(entity, new QuadEntityType { _Type = QuadEntityType.QuadEntityTypeEnum.Emitter });

        _Initialized = true;
    }

    void Start()
    {
        _EntityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        _HeadPosition = FindObjectOfType<Camera>().transform;
    }

    public void Collided(Collision collision)
    {
        _Collision = collision;
        _Triggered = true;
        _CollisionImpact = collision.relativeVelocity.magnitude;
    }

    void Update()
    {
        if (!_Initialized)
            return;

        float samplesPerMS = AudioSettings.outputSampleRate * 0.001f;

        if (_Triggered)
        {
            BurstEmitterComponent data = _EntityManager.GetComponentData<BurstEmitterComponent>(_BurstEntity);

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

            data._InteractionInput = Mathf.Clamp(_Collision.relativeVelocity.magnitude / 10f, 0f, 1f);

            data._Density = new ModulateParameterComponent
            {
                _StartValue = _BurstEmissionProps._BurstCount,
                _Random = _BurstEmissionProps._BurstCountRandom,
                _Interaction = _BurstEmissionProps._CountInteraction,
                _Min = _BurstEmissionProps._CountMin,
                _Max = _BurstEmissionProps._CountMax
            };
            data._Timing = new ModulateParameterComponent
            {
                _StartValue = _BurstEmissionProps._BurstDuration * samplesPerMS,
                _Shape = _BurstEmissionProps._BurstShape,
                _Random = _BurstEmissionProps._TimingRandom,
                _Interaction = _BurstEmissionProps._TimingInteraction,
                _Min = _BurstEmissionProps._TimingMin * samplesPerMS,
                _Max = _BurstEmissionProps._TimingMax * samplesPerMS
            };
            data._Playhead = new ModulateParameterComponent
            {
                _StartValue = _BurstEmissionProps._PlayheadStart,
                _EndValue = _BurstEmissionProps._PlayheadEnd,
                _Shape = _BurstEmissionProps._PlayheadShape,
                _Random = _BurstEmissionProps._PlayheadRandom,
                _Interaction = _BurstEmissionProps._PlayheadInteraction,
                _Min = _BurstEmissionProps._PlayheadMin,
                _Max = _BurstEmissionProps._PlayheadMax
            };
            data._Duration = new ModulateParameterComponent
            {
                _StartValue = _BurstEmissionProps._DurationStart * samplesPerMS,
                _EndValue = _BurstEmissionProps._DurationEnd * samplesPerMS,
                _Shape = _BurstEmissionProps._DurationShape,
                _Random = _BurstEmissionProps._DurationRandom,
                _Interaction = _BurstEmissionProps._DurationInteraction,
                _Min = _BurstEmissionProps._DurationMin * samplesPerMS,
                _Max = _BurstEmissionProps._DurationMax * samplesPerMS
            };
            data._Transpose = new ModulateParameterComponent
            {
                _StartValue = _BurstEmissionProps._TransposeStart,
                _EndValue = _BurstEmissionProps._TransposeEnd,
                _Shape = _BurstEmissionProps._TransposeShape,
                _Random = _BurstEmissionProps._TransposeRandom,
                _Interaction = _BurstEmissionProps._TransposeInteraction,
                _Min = _BurstEmissionProps._TransposeMin,
                _Max = _BurstEmissionProps._TransposeMax
            };
            data._Volume = new ModulateParameterComponent
            {
                _StartValue = _BurstEmissionProps._VolumeStart,
                _EndValue = _BurstEmissionProps._VolumeEnd,
                _Shape = _BurstEmissionProps._VolumeShape,
                _Random = _BurstEmissionProps._VolumeRandom,
                _Interaction = _BurstEmissionProps._VolumeInteraction,
                _Min = _BurstEmissionProps._VolumeMin,
                _Max = _BurstEmissionProps._VolumeMax,
                _LockEndValue = _BurstEmissionProps._VolumeLockEndValue
            };


            data._DistanceAmplitude = distanceAmplitude;

            _EntityManager.SetComponentData(_BurstEntity, data);

            _InRangeTemp = data._InRange;

            _AttachedSpeakerIndex = data._SpeakerIndex;
            _AttachedToSpeaker = data._AttachedToSpeaker;

            Translation trans = _EntityManager.GetComponentData<Translation>(_BurstEntity);
            _EntityManager.SetComponentData(_BurstEntity, new Translation
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