using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Unity.Transforms;
using Random = UnityEngine.Random;
using System;

[System.Serializable]
public class GrainEmissionProps
{
    public bool _Playing = true;

    [Header("Clip")]
    public int _ClipIndex = 0;

    [Header("Cadence")]
    [Range(4f, 500f)]
    [SerializeField]
    public float _CadenceIdle = 20f;
    [Range(-496f, 496f)]
    [SerializeField]
    public float _CadenceInteraction = 0f;
    [Range(0.5f, 5.0f)]
    [SerializeField]
    public float _CadenceShape = 1f;
    [Range(0f, 1.0f)]
    [SerializeField]
    public float _CadenceRandom = 0f;
    [HideInInspector]
    public float _CadenceMin = 10f;
    [HideInInspector]
    public float _CadenceMax = 500f;

    [Header("Playhead")]
    [Range(0f, 1f)]
    [SerializeField]
    public float _PlayheadIdle = 0f;
    [Range(-1f, 1f)]
    [SerializeField]
    public float _PlayheadInteraction = 0f;
    [Range(0.5f, 5.0f)]
    [SerializeField]
    public float _PlayheadShape = 1f;
    [Range(0f, 1.0f)]
    [SerializeField]
    public float _PlayheadRandom = 0.01f;
    [HideInInspector]
    public float _PlayheadMin = 0f;
    [HideInInspector]
    public float _PlayheadMax = 1f;

    [Header("Grain Duration")]
    [Range(2f, 500f)]
    [SerializeField]
    public float _DurationIdle = 50f;
    [Range(-502f, 502f)]
    [SerializeField]
    public float _DurationInteraction = 0f;
    [Range(0.5f, 5.0f)]
    [SerializeField]
    public float _DurationShape = 1f;
    [Range(0f, 1.0f)]
    [SerializeField]
    public float _DurationRandom = 0.01f;
    [HideInInspector]
    public float _DurationMin = 2f;
    [HideInInspector]
    public float _DurationMax = 500f;

    [Header("Transpose")]
    [Range(-3f, 3f)]
    [SerializeField]
    public float _TransposeIdle = 0;
    [Range(-6f, 6f)]
    [SerializeField]
    public float _TransposeInteraction = 0;
    [Range(0.5f, 5.0f)]
    [SerializeField]
    public float _TransposeShape = 1f;
    [Range(0f, 1.0f)]
    [SerializeField]
    public float _TransposeRandom = 0.01f;
    [HideInInspector]
    public float _TransposeMin = -3f;
    [HideInInspector]
    public float _TransposeMax = 3f;

    [Header("Volume")]
    [Range(0f, 2f)]
    [SerializeField]
    public float _VolumeIdle = 1;
    [Range(-2f, 2f)]
    [SerializeField]
    public float _VolumeInteraction = 0f;
    [Range(0.5f, 5.0f)]
    [SerializeField]
    public float _VolumeShape = 1f;
    [Range(0f, 1.0f)]
    [SerializeField]
    public float _VolumeRandom = 0f;
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
    public bool _AttachedToSpeaker;
    public int _AttachedSpeakerIndex;
    public GrainSpeakerAuthoring _PairedSpeaker;
    public Transform _HeadPosition;
    public GameObject _InteractionObject;
    private Rigidbody _RigidBody;
    [Range(0, 10)]
    public float _InteractionSmoothing = 4f;
    public float _ObjectSpeed = 0f;

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
            _PairedSpeaker.AddPairedEmitter(gameObject);
            _StaticallyPaired = true;
            dstManager.AddComponentData(_EmitterEntity, new StaticallyPairedTag { });
            attachedSpeakerIndex =_PairedSpeaker.GetRegisterAndGetIndex();
        }

        int index = GrainSynth.Instance.RegisterEmitter(entity);
        int samplesPerMS = (int)(AudioSettings.outputSampleRate * .001f);

        #region ADD EMITTER COMPONENT
        dstManager.AddComponentData(_EmitterEntity, new EmitterComponent
        {
            _Playing = _EmissionProps._Playing,
            _AttachedToSpeaker = _StaticallyPaired,
            _StaticallyPaired = _StaticallyPaired,

            _InteractionInput = 0f,

            _Cadence = new ModulateParameterComponent
            {
                _StartValue = _EmissionProps._CadenceIdle * samplesPerMS,
                _EndValue = _EmissionProps._CadenceInteraction * samplesPerMS,
                _Shape = _EmissionProps._CadenceShape,
                _Random = _EmissionProps._CadenceRandom,
                _Min = _EmissionProps._CadenceMin * samplesPerMS,
                _Max = _EmissionProps._CadenceMax * samplesPerMS
            },
            _Playhead = new ModulateParameterComponent
            {
                _StartValue = _EmissionProps._PlayheadIdle,
                _EndValue = _EmissionProps._PlayheadInteraction,
                _Shape = _EmissionProps._PlayheadShape,
                _Random = _EmissionProps._PlayheadRandom,
                _Min = _EmissionProps._PlayheadMin,
                _Max = _EmissionProps._PlayheadMax
            },
            _Duration = new ModulateParameterComponent
            {
                _StartValue = _EmissionProps._DurationIdle * samplesPerMS,
                _EndValue = _EmissionProps._DurationInteraction * samplesPerMS,
                _Shape = _EmissionProps._DurationShape,
                _Random = _EmissionProps._DurationRandom,
                _Min = _EmissionProps._DurationMin * samplesPerMS,
                _Max = _EmissionProps._DurationMax * samplesPerMS
            },
            _Transpose = new ModulateParameterComponent
            {
                _StartValue = _EmissionProps._TransposeIdle,
                _EndValue = _EmissionProps._TransposeInteraction,
                _Shape = _EmissionProps._TransposeShape,
                _Random = _EmissionProps._TransposeRandom,
                _Interaction = _EmissionProps._TransposeInteraction,
                _Min = _EmissionProps._TransposeMin,
                _Max = _EmissionProps._TransposeMax
            },
            _Volume = new ModulateParameterComponent
            {
                _StartValue = _EmissionProps._VolumeIdle,
                _EndValue = _EmissionProps._VolumeInteraction,
                _Shape = _EmissionProps._VolumeShape,
                _Random = _EmissionProps._VolumeRandom,
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
        #endregion


        //---   DSP CHAIN
        dstManager.AddBuffer<DSPParametersElement>(_EmitterEntity);
        DynamicBuffer<DSPParametersElement> dspBuffer = dstManager.GetBuffer<DSPParametersElement>(_EmitterEntity);
        for (int i = 0; i < _DSPChainParams.Length; i++)        
            dspBuffer.Add(_DSPChainParams[i].GetDSPBufferElement());     

        dstManager.AddComponentData(entity, new QuadEntityType { _Type = QuadEntityType.QuadEntityTypeEnum.Emitter });

        _Initialized = true;
    }

    void Start()
    {
        _EntityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        _HeadPosition = FindObjectOfType<Camera>().transform;

        if (_InteractionObject == null)
            _InteractionObject = gameObject;

        _RigidBody = _InteractionObject.GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (!_Initialized)
            return;

        float samplesPerMS = AudioSettings.outputSampleRate * 0.001f;

        // Get object speed and smooth
        if (_RigidBody != null)
        {
            _ObjectSpeed = Mathf.Lerp(_ObjectSpeed, _RigidBody.velocity.magnitude / 8f, Time.deltaTime * _InteractionSmoothing);
            if (Mathf.Abs(_ObjectSpeed - _RigidBody.velocity.magnitude / 8f) < .005f)
                _ObjectSpeed = _RigidBody.velocity.magnitude / 8f;
        }

        EmitterComponent emitterData = _EntityManager.GetComponentData<EmitterComponent>(_EmitterEntity);

        int attachedSpeakerIndex = _StaticallyPaired ? _PairedSpeaker._SpeakerIndex : emitterData._SpeakerIndex;
        float distanceAmplitude = 1;

        if (emitterData._AttachedToSpeaker)
        {
            distanceAmplitude = AudioUtils.DistanceAttenuation(
                _HeadPosition.position,
                GrainSynth.Instance._GrainSpeakers[attachedSpeakerIndex].gameObject.transform.position,
                transform.position);
        }

        EmitterComponent emitter = _EntityManager.GetComponentData<EmitterComponent>(_EmitterEntity);

        emitterData._Playing = _EmissionProps._Playing;
        emitterData._SpeakerIndex = attachedSpeakerIndex;
        emitterData._AudioClipIndex = _EmissionProps._ClipIndex;

        if (_RigidBody != null)
            emitterData._InteractionInput = Mathf.Clamp(_ObjectSpeed, 0f, 1f);
        else
            emitterData._InteractionInput = 0f;

        emitterData._Cadence = new ModulateParameterComponent
        {
            _StartValue = _EmissionProps._CadenceIdle * samplesPerMS,
            _EndValue = _EmissionProps._CadenceInteraction * samplesPerMS,
            _Shape = _EmissionProps._CadenceShape,
            _Random = _EmissionProps._CadenceRandom,
            _Min = _EmissionProps._CadenceMin * samplesPerMS,
            _Max = _EmissionProps._CadenceMax * samplesPerMS
        };
        emitterData._Playhead = new ModulateParameterComponent
        {
            _StartValue = _EmissionProps._PlayheadIdle,
            _EndValue = _EmissionProps._PlayheadInteraction,
            _Shape = _EmissionProps._PlayheadShape,
            _Random = _EmissionProps._PlayheadRandom,
            _Min = _EmissionProps._PlayheadMin,
            _Max = _EmissionProps._PlayheadMax
        };
        emitterData._Duration = new ModulateParameterComponent
        {
            _StartValue = _EmissionProps._DurationIdle * samplesPerMS,
            _EndValue = _EmissionProps._DurationInteraction * samplesPerMS,
            _Shape = _EmissionProps._DurationShape,
            _Random = _EmissionProps._DurationRandom,
            _Min = _EmissionProps._DurationMin * samplesPerMS,
            _Max = _EmissionProps._DurationMax * samplesPerMS
        };
        emitterData._Transpose = new ModulateParameterComponent
        {
            _StartValue = _EmissionProps._TransposeIdle,
            _EndValue = _EmissionProps._TransposeInteraction,
            _Shape = _EmissionProps._TransposeShape,
            _Random = _EmissionProps._TransposeRandom,
            _Min = _EmissionProps._TransposeMin,
            _Max = _EmissionProps._TransposeMax
        };
        emitterData._Volume = new ModulateParameterComponent
        {
            _StartValue = _EmissionProps._VolumeIdle,
            _EndValue = _EmissionProps._VolumeInteraction,
            _Shape = _EmissionProps._VolumeShape,
            _Random = _EmissionProps._VolumeRandom,
            _Min = _EmissionProps._VolumeMin,
            _Max = _EmissionProps._VolumeMax
        };

        emitterData._DistanceAmplitude = distanceAmplitude;

        _EntityManager.SetComponentData(_EmitterEntity, emitterData);


        //---   DSP CHAIN        
        UpdateDSPBuffer();

        _InRangeTemp = emitterData._InRange;

        _AttachedSpeakerIndex = emitterData._SpeakerIndex;
        _AttachedToSpeaker = emitterData._AttachedToSpeaker;

        Translation trans = _EntityManager.GetComponentData<Translation>(_EmitterEntity);
        _EntityManager.SetComponentData(_EmitterEntity, new Translation
        {
            Value = transform.position
        });
    }

    void UpdateDSPBuffer(bool clear = true)
    {
       // Debug.Log("UpdateDSPBuffer...");
        //--- TODO not sure if clearing and adding again is the best way to do this
        DynamicBuffer<DSPParametersElement> dspBuffer = _EntityManager.GetBuffer<DSPParametersElement>(_EmitterEntity);
        
        if(clear) dspBuffer.Clear();

        for (int i = 0; i < _DSPChainParams.Length; i++)
        {
            dspBuffer.Add(_DSPChainParams[i].GetDSPBufferElement());
        }
        //Debug.Log("...UpdateDSPBuffer complete.");
    }

    void OnDrawGizmos()
    {
        Gizmos.color = _InRangeTemp ? Color.yellow : Color.blue;
        Gizmos.DrawSphere(transform.position, .1f);
    }
}