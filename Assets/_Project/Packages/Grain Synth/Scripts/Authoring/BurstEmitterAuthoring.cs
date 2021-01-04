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

            _BurstDuration = new ModulateParameterComponent
            {
                _StartValue = _BurstEmissionProps._BurstDuration._Default * samplesPerMS,
                _InteractionAmount = _BurstEmissionProps._BurstDuration._InteractionAmount * samplesPerMS,
                _Shape = _BurstEmissionProps._BurstDuration._InteractionShape,
                _Random = _BurstEmissionProps._BurstDuration._Random,
                _Min = _BurstEmissionProps._BurstDuration._Min * samplesPerMS,
                _Max = _BurstEmissionProps._BurstDuration._Max * samplesPerMS,
                _LockStartValue = false,
                _LockEndValue = false
            },
            _Playhead = new ModulateParameterComponent
            {
                _StartValue = _BurstEmissionProps._Playhead._Start,
                _EndValue = _BurstEmissionProps._Playhead._End,                
                _InteractionAmount = _BurstEmissionProps._Playhead._InteractionAmount,
                _Shape = _BurstEmissionProps._Playhead._InteractionShape,
                _Random = _BurstEmissionProps._Playhead._Random,
                _Min = _BurstEmissionProps._Playhead._Min,
                _Max = _BurstEmissionProps._Playhead._Max,
                _LockStartValue = _BurstEmissionProps._Playhead._LockStartValue,
                _LockEndValue = false
            },
            _Density = new ModulateParameterComponent
            {
                _StartValue = _BurstEmissionProps._Density._Start,
                _EndValue = _BurstEmissionProps._Density._End,
                _InteractionAmount = _BurstEmissionProps._Density._InteractionAmount,
                _Shape = _BurstEmissionProps._Density._InteractionShape,
                _Random = _BurstEmissionProps._Density._Random,
                _Min = _BurstEmissionProps._Density._Min,
                _Max = _BurstEmissionProps._Density._Max,
                _LockStartValue = false,
                _LockEndValue = false
            },
            _GrainDuration = new ModulateParameterComponent
            {
                _StartValue = _BurstEmissionProps._GrainDuration._Start * samplesPerMS,
                _EndValue = _BurstEmissionProps._GrainDuration._End * samplesPerMS,                
                _InteractionAmount = _BurstEmissionProps._GrainDuration._InteractionAmount * samplesPerMS,
                _Shape = _BurstEmissionProps._GrainDuration._InteractionShape,
                _Random = _BurstEmissionProps._GrainDuration._Random,
                _Min = _BurstEmissionProps._GrainDuration._Min * samplesPerMS,
                _Max = _BurstEmissionProps._GrainDuration._Max * samplesPerMS,
                _LockStartValue = false,
                _LockEndValue = false
            },
            _Transpose = new ModulateParameterComponent
            {
                _StartValue = _BurstEmissionProps._Transpose._Start,
                _EndValue = _BurstEmissionProps._Transpose._End,
                _InteractionAmount = _BurstEmissionProps._Transpose._InteractionAmount,
                _Shape = _BurstEmissionProps._Transpose._InteractionShape,
                _Random = _BurstEmissionProps._Transpose._Random,
                _Min = _BurstEmissionProps._Transpose._Min,
                _Max = _BurstEmissionProps._Transpose._Max,
                _LockStartValue = false,
                _LockEndValue = _BurstEmissionProps._Transpose._LockEndValue
            },
            _Volume = new ModulateParameterComponent
            {
                _StartValue = _BurstEmissionProps._Volume._Start,
                _EndValue = _BurstEmissionProps._Volume._End,
                _Shape = _BurstEmissionProps._Volume._InteractionShape,
                _InteractionAmount = _BurstEmissionProps._Volume._InteractionAmount,
                _Random = _BurstEmissionProps._Volume._Random,
                _Min = _BurstEmissionProps._Volume._Min,
                _Max = _BurstEmissionProps._Volume._Max,
                _LockStartValue = false,
                _LockEndValue = _BurstEmissionProps._Volume._LockEndValue
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
        _BurstEmissionProps._BurstDuration._InteractionInput.CollisionData(collision);
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

            burstData._BurstDuration = new ModulateParameterComponent
            {
                _StartValue = _BurstEmissionProps._BurstDuration._Default * samplesPerMS,
                _InteractionAmount = _BurstEmissionProps._BurstDuration._InteractionAmount * samplesPerMS,
                _Shape = _BurstEmissionProps._BurstDuration._InteractionShape,
                _Random = _BurstEmissionProps._BurstDuration._Random,
                _Min = _BurstEmissionProps._BurstDuration._Min * samplesPerMS,
                _Max = _BurstEmissionProps._BurstDuration._Max * samplesPerMS,
                _LockStartValue = false,
                _LockEndValue = false,
                _InteractionInput = _BurstEmissionProps._BurstDuration.GetInteractionValue()
            };
            burstData._Playhead = new ModulateParameterComponent
            {
                _StartValue = _BurstEmissionProps._Playhead._Start,
                _EndValue = _BurstEmissionProps._Playhead._End,
                _InteractionAmount = _BurstEmissionProps._Playhead._InteractionAmount,
                _Shape = _BurstEmissionProps._Playhead._InteractionShape,
                _Random = _BurstEmissionProps._Playhead._Random,
                _Min = _BurstEmissionProps._Playhead._Min,
                _Max = _BurstEmissionProps._Playhead._Max,
                _LockStartValue = _BurstEmissionProps._Playhead._LockStartValue,
                _LockEndValue = false,
                _InteractionInput = _BurstEmissionProps._Playhead.GetInteractionValue()
            };
            burstData._Density = new ModulateParameterComponent
            {
                _StartValue = _BurstEmissionProps._Density._Start,
                _EndValue = _BurstEmissionProps._Density._End,
                _InteractionAmount = _BurstEmissionProps._Density._InteractionAmount,
                _Shape = _BurstEmissionProps._Density._InteractionShape,
                _Random = _BurstEmissionProps._Density._Random,
                _Min = _BurstEmissionProps._Density._Min,
                _Max = _BurstEmissionProps._Density._Max,
                _LockStartValue = false,
                _LockEndValue = false,
                _InteractionInput = _BurstEmissionProps._Density.GetInteractionValue()
            };
            burstData._GrainDuration = new ModulateParameterComponent
            {
                _StartValue = _BurstEmissionProps._GrainDuration._Start * samplesPerMS,
                _EndValue = _BurstEmissionProps._GrainDuration._End * samplesPerMS,
                _InteractionAmount = _BurstEmissionProps._GrainDuration._InteractionAmount * samplesPerMS,
                _Shape = _BurstEmissionProps._GrainDuration._InteractionShape,
                _Random = _BurstEmissionProps._GrainDuration._Random,
                _Min = _BurstEmissionProps._GrainDuration._Min * samplesPerMS,
                _Max = _BurstEmissionProps._GrainDuration._Max * samplesPerMS,
                _LockStartValue = false,
                _LockEndValue = false,
                _InteractionInput = _BurstEmissionProps._GrainDuration.GetInteractionValue()
            };
            burstData._Transpose = new ModulateParameterComponent
            {
                _StartValue = _BurstEmissionProps._Transpose._Start,
                _EndValue = _BurstEmissionProps._Transpose._End,
                _InteractionAmount = _BurstEmissionProps._Transpose._InteractionAmount,
                _Shape = _BurstEmissionProps._Transpose._InteractionShape,
                _Random = _BurstEmissionProps._Transpose._Random,
                _Min = _BurstEmissionProps._Transpose._Min,
                _Max = _BurstEmissionProps._Transpose._Max,
                _LockStartValue = false,
                _LockEndValue = _BurstEmissionProps._Transpose._LockEndValue,
                _InteractionInput = _BurstEmissionProps._Transpose.GetInteractionValue()
            };
            burstData._Volume = new ModulateParameterComponent
            {
                _StartValue = _BurstEmissionProps._Volume._Start,
                _EndValue = _BurstEmissionProps._Volume._End,
                _InteractionAmount = _BurstEmissionProps._Volume._InteractionAmount,
                _Shape = _BurstEmissionProps._Volume._InteractionShape,
                _Random = _BurstEmissionProps._Volume._Random,
                _Min = _BurstEmissionProps._Volume._Min,
                _Max = _BurstEmissionProps._Volume._Max,
                _LockStartValue = false,
                _LockEndValue = _BurstEmissionProps._Volume._LockEndValue,
                _InteractionInput = _BurstEmissionProps._Volume.GetInteractionValue()
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