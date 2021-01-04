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
    public int _ClipIndex = 0;
    public EmitterPropPlayhead _Playhead;
    public EmitterPropDensity _Density;
    public EmitterPropDuration _GrainDuration;
    public EmitterPropTranspose _Transpose;
    public EmitterPropVolume _Volume;
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

    public GrainEmissionProps _EmissionProps;
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

            _Playhead = new ModulateParameterComponent
            {
                _StartValue = _EmissionProps._Playhead._Idle,
                _InteractionAmount = _EmissionProps._Playhead._InteractionAmount,
                _Shape = _EmissionProps._Playhead._InteractionShape,
                _Random = _EmissionProps._Playhead._Random,
                _Min = _EmissionProps._Playhead._Min,
                _Max = _EmissionProps._Playhead._Max
            },
            _Density = new ModulateParameterComponent
            {
                _StartValue = _EmissionProps._Density._Idle,
                _InteractionAmount = _EmissionProps._Density._InteractionAmount,
                _Shape = _EmissionProps._Density._InteractionShape,
                _Random = _EmissionProps._Density._Random,
                _Min = _EmissionProps._Density._Min,
                _Max = _EmissionProps._Density._Max
            },
            _Duration = new ModulateParameterComponent
            {
                _StartValue = _EmissionProps._GrainDuration._Idle * samplesPerMS,
                _InteractionAmount = _EmissionProps._GrainDuration._InteractionAmount * samplesPerMS,
                _Shape = _EmissionProps._GrainDuration._InteractionShape,
                _Random = _EmissionProps._GrainDuration._Random,
                _Min = _EmissionProps._GrainDuration._Min * samplesPerMS,
                _Max = _EmissionProps._GrainDuration._Max * samplesPerMS
            },
            _Transpose = new ModulateParameterComponent
            {
                _StartValue = _EmissionProps._Transpose._Idle,
                _InteractionAmount = _EmissionProps._Transpose._InteractionAmount,
                _Shape = _EmissionProps._Transpose._InteractionShape,
                _Random = _EmissionProps._Transpose._Random,
                _Min = _EmissionProps._Transpose._Min,
                _Max = _EmissionProps._Transpose._Max
            },
            _Volume = new ModulateParameterComponent
            {
                _StartValue = _EmissionProps._Volume._Idle,
                _InteractionAmount = _EmissionProps._Volume._InteractionAmount,
                _Shape = _EmissionProps._Volume._InteractionShape,
                _Random = _EmissionProps._Volume._Random,
                _Min = _EmissionProps._Volume._Min,
                _Max = _EmissionProps._Volume._Max
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
        DynamicBuffer<DSPParametersElement> dspParams = dstManager.GetBuffer<DSPParametersElement>(_EmitterEntity);
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
        _EmissionProps._Playhead._InteractionInput.CollisionData(collision);
        _EmissionProps._Density._InteractionInput.CollisionData(collision);
        _EmissionProps._GrainDuration._InteractionInput.CollisionData(collision);
        _EmissionProps._Transpose._InteractionInput.CollisionData(collision);
        _EmissionProps._Volume._InteractionInput.CollisionData(collision);
}

    void Update()
    {
        if (!_Initialized)
            return;

        float samplesPerMS = AudioSettings.outputSampleRate * 0.001f;

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

        #region UPDATE EMITTER COMPONENT
        emitterData._Playhead = new ModulateParameterComponent
        {
            _StartValue = _EmissionProps._Playhead._Idle,
            _InteractionAmount = _EmissionProps._Playhead._InteractionAmount,
            _Shape = _EmissionProps._Playhead._InteractionShape,
            _Random = _EmissionProps._Playhead._Random,
            _Min = _EmissionProps._Playhead._Min,
            _Max = _EmissionProps._Playhead._Max,
            _InteractionInput = _EmissionProps._Playhead.GetInteractionValue()
        };
        emitterData._Density = new ModulateParameterComponent
        {
            _StartValue = _EmissionProps._Density._Idle,
            _InteractionAmount = _EmissionProps._Density._InteractionAmount,
            _Shape = _EmissionProps._Density._InteractionShape,
            _Random = _EmissionProps._Density._Random,
            _Min = _EmissionProps._Density._Min,
            _Max = _EmissionProps._Density._Max,
            _InteractionInput = _EmissionProps._Density.GetInteractionValue()
        };
        emitterData._Duration = new ModulateParameterComponent
        {
            _StartValue = _EmissionProps._GrainDuration._Idle * samplesPerMS,
            _InteractionAmount = _EmissionProps._GrainDuration._InteractionAmount * samplesPerMS,
            _Shape = _EmissionProps._GrainDuration._InteractionShape,
            _Random = _EmissionProps._GrainDuration._Random,
            _Min = _EmissionProps._GrainDuration._Min * samplesPerMS,
            _Max = _EmissionProps._GrainDuration._Max * samplesPerMS,
            _InteractionInput = _EmissionProps._GrainDuration.GetInteractionValue()
        };
        emitterData._Transpose = new ModulateParameterComponent
        {
            _StartValue = _EmissionProps._Transpose._Idle,
            _InteractionAmount = _EmissionProps._Transpose._InteractionAmount,
            _Shape = _EmissionProps._Transpose._InteractionShape,
            _Random = _EmissionProps._Transpose._Random,
            _Min = _EmissionProps._Transpose._Min,
            _Max = _EmissionProps._Transpose._Max,
            _InteractionInput = _EmissionProps._Transpose.GetInteractionValue()
        };
        emitterData._Volume = new ModulateParameterComponent
        {
            _StartValue = _EmissionProps._Volume._Idle,
            _InteractionAmount = _EmissionProps._Volume._InteractionAmount,
            _Shape = _EmissionProps._Volume._InteractionShape,
            _Random = _EmissionProps._Volume._Random,
            _Min = _EmissionProps._Volume._Min,
            _Max = _EmissionProps._Volume._Max,
            _InteractionInput = _EmissionProps._Volume.GetInteractionValue()
        };

        emitterData._DistanceAmplitude = distanceAmplitude;
        _EntityManager.SetComponentData(_EmitterEntity, emitterData);
        #endregion

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
        //--- TODO not sure if clearing and adding again is the best way to do this
        DynamicBuffer<DSPParametersElement> dspBuffer = _EntityManager.GetBuffer<DSPParametersElement>(_EmitterEntity);
        
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