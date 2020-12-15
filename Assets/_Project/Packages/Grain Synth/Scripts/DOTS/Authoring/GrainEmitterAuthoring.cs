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

    public GameObject _InteractionObject;

    [Header("Cadence")]
    [Range(4f, 500f)]
    [SerializeField]
    public float _CadenceStart = 20f;
    [Range(4f, 500f)]
    [SerializeField]
    public float _CadenceEnd = 50f;
    [Range(0f, 1.0f)]
    [SerializeField]
    public float _CadenceRandom = 0f;
    [Range(0.5f, 5.0f)]
    [SerializeField]
    public float _CadenceShape = 1f;
    [Range(-1.0f, 1.0f)]
    [SerializeField]
    public float _CadenceInteraction = 0f;
    [HideInInspector]
    public float _CadenceMin = 10f;
    [HideInInspector]
    public float _CadenceMax = 1000f;

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
public class GrainEmitterAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    [Header("Debug")]
    public bool _AttachedToSpeaker = false;
    public int _AttachedSpeakerIndex;
    public GrainSpeakerAuthoring _PairedSpeaker;
    public Transform _HeadPosition;
    public Rigidbody _RigidBody;
    public float _ObjectSpeed;
    
    [Header("Emission Properties")]
    public GrainEmissionProps _EmissionProps;

    [Header("DSP Effects")]
    public DSPBase[] _DSPChainParams;

    Entity _EmitterEntity;
    EntityManager _EntityManager;

    bool _Initialized = false;
    bool _StaticallyPaired = false;
    bool _InRangeTemp = false;

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
        int samplesPerMS = (int)(AudioSettings.outputSampleRate * .001f);

        // Add emitter component
        dstManager.AddComponentData(_EmitterEntity, new EmitterComponent
        {
            _Playing = _EmissionProps._Playing,
            _AttachedToSpeaker = _StaticallyPaired,
            _StaticallyPaired = _StaticallyPaired,

            _InteractionInput = 0f,

            _Cadence = new ModulateParameterComponent
            {
                _StartValue = _EmissionProps._CadenceStart * samplesPerMS,
                _EndValue = _EmissionProps._CadenceEnd * samplesPerMS,
                _Shape = _EmissionProps._CadenceShape,
                _Random = _EmissionProps._CadenceRandom,
                _Interaction = _EmissionProps._CadenceInteraction,
                _Min = _EmissionProps._CadenceMin * samplesPerMS,
                _Max = _EmissionProps._CadenceMax * samplesPerMS
            },
            _Playhead = new ModulateParameterComponent
            {
                _StartValue = _EmissionProps._PlayheadStart,
                _EndValue = _EmissionProps._PlayheadEnd,
                _Shape = _EmissionProps._PlayheadShape,
                _Random = _EmissionProps._PlayheadRandom,
                _Interaction = _EmissionProps._PlayheadInteraction,
                _Min = _EmissionProps._PlayheadMin,
                _Max = _EmissionProps._PlayheadMax
            },
            _Duration = new ModulateParameterComponent
            {
                _StartValue = _EmissionProps._DurationStart * samplesPerMS,
                _EndValue = _EmissionProps._DurationEnd * samplesPerMS,
                _Shape = _EmissionProps._DurationShape,
                _Random = _EmissionProps._DurationRandom,
                _Interaction = _EmissionProps._DurationInteraction,
                _Min = _EmissionProps._DurationMin * samplesPerMS,
                _Max = _EmissionProps._DurationMax * samplesPerMS
            },
            _Transpose = new ModulateParameterComponent
            {
                _StartValue = _EmissionProps._TransposeStart,
                _EndValue = _EmissionProps._TransposeEnd,
                _Shape = _EmissionProps._TransposeShape,
                _Random = _EmissionProps._TransposeRandom,
                _Interaction = _EmissionProps._TransposeInteraction,
                _Min = _EmissionProps._TransposeMin,
                _Max = _EmissionProps._TransposeMax
            },
            _Volume = new ModulateParameterComponent
            {
                _StartValue = _EmissionProps._VolumeStart,
                _EndValue = _EmissionProps._VolumeEnd,
                _Shape = _EmissionProps._VolumeShape,
                _Random = _EmissionProps._VolumeRandom,
                _Interaction = _EmissionProps._VolumeInteraction,
                _Min = _EmissionProps._VolumeMin,
                _Max = _EmissionProps._VolumeMax
            },

            _DistanceAmplitude = 1,
            _AudioClipIndex = _EmissionProps._ClipIndex,
            _SpeakerIndex = attachedSpeakerIndex,
            _EmitterIndex = index,
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
        _RigidBody = _EmissionProps._InteractionObject.GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (!_Initialized)
            return;

        float samplesPerMS = AudioSettings.outputSampleRate * 0.001f;

        EmitterComponent data = _EntityManager.GetComponentData<EmitterComponent>(_EmitterEntity);

        int attachedSpeakerIndex = _StaticallyPaired ? _PairedSpeaker._SpeakerIndex : data._SpeakerIndex;
        float distanceAmplitude = 1;

        if (data._AttachedToSpeaker)
        {
            distanceAmplitude = AudioUtils.DistanceAttenuation(
                _HeadPosition.position,
                GrainSynth.Instance._GrainSpeakers[attachedSpeakerIndex].gameObject.transform.position,
                transform.position);
        }

        EmitterComponent emitter = _EntityManager.GetComponentData<EmitterComponent>(_EmitterEntity);

        data._Playing = _EmissionProps._Playing;
        data._SpeakerIndex = attachedSpeakerIndex;
        data._AudioClipIndex = _EmissionProps._ClipIndex;

        if (_RigidBody != null)
            data._InteractionInput = Mathf.Clamp(_RigidBody.velocity.magnitude / 2f, 0f, 1f);
        else
            data._InteractionInput = 0f;

        _ObjectSpeed = data._InteractionInput;

        data._Cadence = new ModulateParameterComponent
        {
            _StartValue = _EmissionProps._CadenceStart * samplesPerMS,
            _EndValue = _EmissionProps._CadenceEnd * samplesPerMS,
            _Shape = _EmissionProps._CadenceShape,
            _Random = _EmissionProps._CadenceRandom,
            _Interaction = _EmissionProps._CadenceInteraction,
            _Min = _EmissionProps._CadenceMin * samplesPerMS,
            _Max = _EmissionProps._CadenceMax * samplesPerMS
        };
        data._Playhead = new ModulateParameterComponent
        {
            _StartValue = _EmissionProps._PlayheadStart,
            _EndValue = _EmissionProps._PlayheadEnd,
            _Shape = _EmissionProps._PlayheadShape,
            _Random = _EmissionProps._PlayheadRandom,
            _Interaction = _EmissionProps._PlayheadInteraction,
            _Min = _EmissionProps._PlayheadMin,
            _Max = _EmissionProps._PlayheadMax
        };
        data._Duration = new ModulateParameterComponent
        {
            _StartValue = _EmissionProps._DurationStart * samplesPerMS,
            _EndValue = _EmissionProps._DurationEnd * samplesPerMS,
            _Shape = _EmissionProps._DurationShape,
            _Random = _EmissionProps._DurationRandom,
            _Interaction = _EmissionProps._DurationInteraction,
            _Min = _EmissionProps._DurationMin * samplesPerMS,
            _Max = _EmissionProps._DurationMax * samplesPerMS
        };
        data._Transpose = new ModulateParameterComponent
        {
            _StartValue = _EmissionProps._TransposeStart,
            _EndValue = _EmissionProps._TransposeEnd,
            _Shape = _EmissionProps._TransposeShape,
            _Random = _EmissionProps._TransposeRandom,
            _Interaction = _EmissionProps._TransposeInteraction,
            _Min = _EmissionProps._TransposeMin,
            _Max = _EmissionProps._TransposeMax
        };
        data._Volume = new ModulateParameterComponent
        {
            _StartValue = _EmissionProps._VolumeStart,
            _EndValue = _EmissionProps._VolumeEnd,
            _Shape = _EmissionProps._VolumeShape,
            _Random = _EmissionProps._VolumeRandom,
            _Interaction = _EmissionProps._VolumeInteraction,
            _Min = _EmissionProps._VolumeMin,
            _Max = _EmissionProps._VolumeMax
        };

        data._DistanceAmplitude = distanceAmplitude;

        _EntityManager.SetComponentData(_EmitterEntity, data);

        _InRangeTemp = data._InRange;

        _AttachedSpeakerIndex = data._SpeakerIndex;
        _AttachedToSpeaker = data._AttachedToSpeaker;

        Translation trans = _EntityManager.GetComponentData<Translation>(_EmitterEntity);
        _EntityManager.SetComponentData(_EmitterEntity, new Translation
        {
            Value = transform.position
        });
    }

    void OnDrawGizmos()
    {
        Gizmos.color = _InRangeTemp ? Color.yellow : Color.blue;
        Gizmos.DrawSphere(transform.position, .1f);
    }
}