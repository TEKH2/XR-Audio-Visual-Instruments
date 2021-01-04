using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Unity.Transforms;
using Random = UnityEngine.Random;

[System.Serializable]
public class BurstEmissionProps
{
    public bool _Playing = true;
    public int _ClipIndex = 0;

    public BurstPropPlayhead _Playhead;
    public BurstPropDuration _BurstDuration;
    public BurstPropDensity _Density;
    public BurstPropGrainDuration _GrainDuration;
    public BurstPropTranspose _Transpose;
    public BurstPropVolume _Volume;
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

        #region ADD EMITTER COMPONENT
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
        #endregion


        dstManager.AddBuffer<DSPParametersElement>(_BurstEntity);
        DynamicBuffer<DSPParametersElement> dspParams = dstManager.GetBuffer<DSPParametersElement>(_BurstEntity);
        for (int i = 0; i < _DSPChainParams.Length; i++)
        {
            dspParams.Add(_DSPChainParams[i].GetDSPBufferElement());
        }

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

        _BurstEmissionProps._Playhead._InteractionInput.CollisionData(collision);
        _BurstEmissionProps._Duration._InteractionInput.CollisionData(collision);
        _BurstEmissionProps._Density._InteractionInput.CollisionData(collision);
        _BurstEmissionProps._GrainDuration._InteractionInput.CollisionData(collision);
        _BurstEmissionProps._Transpose._InteractionInput.CollisionData(collision);
        _BurstEmissionProps._Volume._InteractionInput.CollisionData(collision);
    }

    void Update()
    {
        if (!_Initialized)
            return;

        float samplesPerMS = AudioSettings.outputSampleRate * 0.001f;

        if (_Triggered)
        {
            BurstEmitterComponent burstData = _EntityManager.GetComponentData<BurstEmitterComponent>(_BurstEntity);

            int attachedSpeakerIndex = _StaticallyPaired ? _PairedSpeaker._SpeakerIndex : burstData._SpeakerIndex;
            float distanceAmplitude = 1;

            if (burstData._AttachedToSpeaker)
            {
                distanceAmplitude = AudioUtils.DistanceAttenuation(
                    _HeadPosition.position,
                    GrainSynth.Instance._GrainSpeakers[attachedSpeakerIndex].gameObject.transform.position,
                    transform.position);
            }

            burstData._Playing = true;
            burstData._SpeakerIndex = attachedSpeakerIndex;
            burstData._AudioClipIndex = _BurstEmissionProps._ClipIndex;

            burstData._InteractionInput = Mathf.Clamp(_Collision.relativeVelocity.magnitude / 10f, 0f, 1f);

            burstData._Density = new ModulateParameterComponent
            {
                _StartValue = _BurstEmissionProps._BurstCount,
                _Random = _BurstEmissionProps._BurstCountRandom,
                _Interaction = _BurstEmissionProps._CountInteraction,
                _Min = _BurstEmissionProps._CountMin,
                _Max = _BurstEmissionProps._CountMax
            };
            burstData._Timing = new ModulateParameterComponent
            {
                _StartValue = _BurstEmissionProps._BurstDuration * samplesPerMS,
                _Shape = _BurstEmissionProps._BurstShape,
                _Random = _BurstEmissionProps._TimingRandom,
                _Interaction = _BurstEmissionProps._TimingInteraction,
                _Min = _BurstEmissionProps._TimingMin * samplesPerMS,
                _Max = _BurstEmissionProps._TimingMax * samplesPerMS
            };
            burstData._Playhead = new ModulateParameterComponent
            {
                _StartValue = _BurstEmissionProps._PlayheadStart,
                _EndValue = _BurstEmissionProps._PlayheadEnd,
                _Shape = _BurstEmissionProps._PlayheadShape,
                _Random = _BurstEmissionProps._PlayheadRandom,
                _Interaction = _BurstEmissionProps._PlayheadInteraction,
                _Min = _BurstEmissionProps._PlayheadMin,
                _Max = _BurstEmissionProps._PlayheadMax
            };
            burstData._Duration = new ModulateParameterComponent
            {
                _StartValue = _BurstEmissionProps._DurationStart * samplesPerMS,
                _EndValue = _BurstEmissionProps._DurationEnd * samplesPerMS,
                _Shape = _BurstEmissionProps._DurationShape,
                _Random = _BurstEmissionProps._DurationRandom,
                _Interaction = _BurstEmissionProps._DurationInteraction,
                _Min = _BurstEmissionProps._DurationMin * samplesPerMS,
                _Max = _BurstEmissionProps._DurationMax * samplesPerMS
            };
            burstData._Transpose = new ModulateParameterComponent
            {
                _StartValue = _BurstEmissionProps._TransposeStart,
                _EndValue = _BurstEmissionProps._TransposeEnd,
                _Shape = _BurstEmissionProps._TransposeShape,
                _Random = _BurstEmissionProps._TransposeRandom,
                _Interaction = _BurstEmissionProps._TransposeInteraction,
                _Min = _BurstEmissionProps._TransposeMin,
                _Max = _BurstEmissionProps._TransposeMax
            };
            burstData._Volume = new ModulateParameterComponent
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


            burstData._DistanceAmplitude = distanceAmplitude;

            _EntityManager.SetComponentData(_BurstEntity, burstData);

            //---   DSP CHAIN        
            UpdateDSPBuffer();

            _InRangeTemp = burstData._InRange;

            _AttachedSpeakerIndex = burstData._SpeakerIndex;
            _AttachedToSpeaker = burstData._AttachedToSpeaker;

            Translation trans = _EntityManager.GetComponentData<Translation>(_BurstEntity);
            _EntityManager.SetComponentData(_BurstEntity, new Translation
            {
                Value = transform.position
            });

            _Triggered = false;
        }
    }

    void UpdateDSPBuffer(bool clear = true)
    {
        //--- TODO not sure if clearing and adding again is the best way to do this
        DynamicBuffer<DSPParametersElement> dspBuffer = _EntityManager.GetBuffer<DSPParametersElement>(_BurstEntity);

        if (clear) dspBuffer.Clear();

        for (int i = 0; i < _DSPChainParams.Length; i++)
        {
            dspBuffer.Add(_DSPChainParams[i].GetDSPBufferElement());
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = _InRangeTemp ? Color.yellow : Color.blue;
        Gizmos.DrawSphere(transform.position, .1f);
    }
}