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

public class BurstEmitterAuthoring : BaseEmitterClass
{
    public BurstEmissionProps _EmissionProps;
    public float _TimeExisted = 0;

    public override void Initialise()
    {
        _EmitterType = EmitterType.Burst;
    }

    public override void SetupTempEmitter(Collision collision, GrainSpeakerAuthoring speaker)
    {
        _ColldingObject = collision.collider.gameObject;
        _EmitterSetup = EmitterSetup.Temp;
        _EmissionProps._Playing = true;
        _Colliding = true;
        _CollisionTriggered = true;
        _PairedSpeaker = speaker;
        _StaticallyPaired = true;

        _TimeExisted = 0;
        gameObject.transform.localPosition = Vector3.zero;

        _EmissionProps._Playhead._InteractionInput.UpdateTempEmitterInteractionSource(this.transform.parent.gameObject, collision);
        _EmissionProps._BurstDuration._InteractionInput.UpdateTempEmitterInteractionSource(this.transform.parent.gameObject, collision);
        _EmissionProps._Density._InteractionInput.UpdateTempEmitterInteractionSource(this.transform.parent.gameObject, collision);
        _EmissionProps._GrainDuration._InteractionInput.UpdateTempEmitterInteractionSource(this.transform.parent.gameObject, collision);
        _EmissionProps._Transpose._InteractionInput.UpdateTempEmitterInteractionSource(this.transform.parent.gameObject, collision);
        _EmissionProps._Volume._InteractionInput.UpdateTempEmitterInteractionSource(this.transform.parent.gameObject, collision);
    }

    public override void NewCollision(Collision collision)
    {
        _CollisionTriggered = true;
        _EmissionProps._Playing = true;

        if (!_MultiplyVolumeByColliderRigidity)
            _VolumeMultiply = 1;
        else if (collision.collider.GetComponent<SurfaceParameters>() != null)
            _VolumeMultiply = collision.collider.GetComponent<SurfaceParameters>()._Rigidity;
    }

    public override void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
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
        dstManager.AddComponentData(_EmitterEntity, new BurstEmitterComponent
        {
            _Playing = false,
            _AttachedToSpeaker = _StaticallyPaired,
            _StaticallyPaired = _StaticallyPaired,
            _PingPong = _PingPongAtEndOfClip,

            _BurstDuration = new ModulateParameterComponent
            {
                _StartValue = _EmissionProps._BurstDuration._Default * samplesPerMS,
                _InteractionAmount = _EmissionProps._BurstDuration._InteractionAmount * samplesPerMS,
                _Shape = _EmissionProps._BurstDuration._InteractionShape,
                _Noise = _EmissionProps._BurstDuration._Noise,
                _LockNoise = _EmissionProps._BurstDuration._LockNoise,
                _Min = _EmissionProps._BurstDuration._Min * samplesPerMS,
                _Max = _EmissionProps._BurstDuration._Max * samplesPerMS,
                _LockStartValue = false,
                _LockEndValue = false
            },
            _Playhead = new ModulateParameterComponent
            {
                _StartValue = _EmissionProps._Playhead._Start,
                _EndValue = _EmissionProps._Playhead._End,                
                _InteractionAmount = _EmissionProps._Playhead._InteractionAmount,
                _Shape = _EmissionProps._Playhead._InteractionShape,
                _Noise = _EmissionProps._Playhead._Noise,
                _LockNoise = _EmissionProps._Playhead._LockNoise,
                _Min = _EmissionProps._Playhead._Min,
                _Max = _EmissionProps._Playhead._Max,
                _LockStartValue = _EmissionProps._Playhead._LockStartValue,
                _LockEndValue = false
            },
            _Density = new ModulateParameterComponent
            {
                _StartValue = _EmissionProps._Density._Start,
                _EndValue = _EmissionProps._Density._End,
                _InteractionAmount = _EmissionProps._Density._InteractionAmount,
                _Shape = _EmissionProps._Density._InteractionShape,
                _Noise = _EmissionProps._Density._Noise,
                _LockNoise = _EmissionProps._Density._LockNoise,
                _Min = _EmissionProps._Density._Min,
                _Max = _EmissionProps._Density._Max,
                _LockStartValue = false,
                _LockEndValue = false
            },
            _GrainDuration = new ModulateParameterComponent
            {
                _StartValue = _EmissionProps._GrainDuration._Start * samplesPerMS,
                _EndValue = _EmissionProps._GrainDuration._End * samplesPerMS,                
                _InteractionAmount = _EmissionProps._GrainDuration._InteractionAmount * samplesPerMS,
                _Shape = _EmissionProps._GrainDuration._InteractionShape,
                _Noise = _EmissionProps._GrainDuration._Noise,
                _LockNoise = _EmissionProps._GrainDuration._LockNoise,
                _Min = _EmissionProps._GrainDuration._Min * samplesPerMS,
                _Max = _EmissionProps._GrainDuration._Max * samplesPerMS,
                _LockStartValue = false,
                _LockEndValue = false
            },
            _Transpose = new ModulateParameterComponent
            {
                _StartValue = _EmissionProps._Transpose._Start,
                _EndValue = _EmissionProps._Transpose._End,
                _InteractionAmount = _EmissionProps._Transpose._InteractionAmount,
                _Shape = _EmissionProps._Transpose._InteractionShape,
                _Noise = _EmissionProps._Transpose._Noise,
                _LockNoise = _EmissionProps._Transpose._LockNoise,
                _Min = _EmissionProps._Transpose._Min,
                _Max = _EmissionProps._Transpose._Max,
                _LockStartValue = false,
                _LockEndValue = _EmissionProps._Transpose._LockEndValue
            },
            _Volume = new ModulateParameterComponent
            {
                _StartValue = _EmissionProps._Volume._Start,
                _EndValue = _EmissionProps._Volume._End,
                _Shape = _EmissionProps._Volume._InteractionShape,
                _InteractionAmount = _EmissionProps._Volume._InteractionAmount,
                _Noise = _EmissionProps._Volume._Noise,
                _LockNoise = _EmissionProps._Volume._LockNoise,
                _Min = _EmissionProps._Volume._Min,
                _Max = _EmissionProps._Volume._Max,
                _LockStartValue = false,
                _LockEndValue = _EmissionProps._Volume._LockEndValue
            },


            _DistanceAmplitude = 1,
            _AudioClipIndex = _EmissionProps._ClipIndex,
            _SpeakerIndex = attachedSpeakerIndex,
            _EmitterIndex = index,
            _SampleRate = AudioSettings.outputSampleRate
        });

        #if UNITY_EDITOR
                dstManager.SetName(entity, "Burst Emitter:   " + transform.parent.name + " " + gameObject.name);
        #endif

#endregion


        dstManager.AddBuffer<DSPParametersElement>(_EmitterEntity);
        DynamicBuffer<DSPParametersElement> dspParams = dstManager.GetBuffer<DSPParametersElement>(_EmitterEntity);
        for (int i = 0; i < _DSPChainParams.Length; i++)
        {
            dspParams.Add(_DSPChainParams[i].GetDSPBufferElement());
        }

        dstManager.AddComponentData(entity, new QuadEntityType { _Type = QuadEntityType.QuadEntityTypeEnum.Emitter });

        _Initialized = true;
    }

    void Update()
    {
        if (!_Initialized)
            return;

        if ((_EmitterSetup == EmitterSetup.Temp && !_Colliding) || _EmitterSetup == EmitterSetup.Dummy)
            _EmissionProps._Playing = false;

        _CurrentDistance = Mathf.Abs((_HeadPosition.position - transform.position).magnitude);

        if (_CurrentDistance < _MaxAudibleDistance)
        {
            _WithinEarshot = true;
            _EntityManager.AddComponent<WithinEarshot>(_EmitterEntity);
        }
        else
        {
            _WithinEarshot = false;
            _EntityManager.RemoveComponent<WithinEarshot>(_EmitterEntity);
        }

        if (_EmissionProps._Playing)
            _EntityManager.AddComponent<IsPlayingTag>(_EmitterEntity);
        else
            _EntityManager.RemoveComponent<IsPlayingTag>(_EmitterEntity);

        if (_CollisionTriggered & _WithinEarshot & _EmissionProps._Playing)
        {
            float samplesPerMS = AudioSettings.outputSampleRate * 0.001f;

            BurstEmitterComponent burstData = _EntityManager.GetComponentData<BurstEmitterComponent>(_EmitterEntity);

            int attachedSpeakerIndex = _StaticallyPaired ? _PairedSpeaker._SpeakerIndex : burstData._SpeakerIndex;

            _DistanceVolume = AudioUtils.EmitterFromListenerVolumeAdjust(_HeadPosition.position, transform.position, _MaxAudibleDistance);

            float volumeDistanceAdjust = AudioUtils.EmitterFromSpeakerVolumeAdjust(_HeadPosition.position,
                    GrainSynth.Instance._GrainSpeakers[attachedSpeakerIndex].gameObject.transform.position,
                    transform.position) * _DistanceVolume;

            burstData._Playing = true;
            burstData._SpeakerIndex = attachedSpeakerIndex;
            burstData._AudioClipIndex = _EmissionProps._ClipIndex;
            burstData._PingPong = _PingPongAtEndOfClip;

            burstData._BurstDuration = new ModulateParameterComponent
            {
                _StartValue = _EmissionProps._BurstDuration._Default * samplesPerMS,
                _InteractionAmount = _EmissionProps._BurstDuration._InteractionAmount * samplesPerMS,
                _Shape = _EmissionProps._BurstDuration._InteractionShape,
                _Noise = _EmissionProps._BurstDuration._Noise,
                _LockNoise = _EmissionProps._BurstDuration._LockNoise,
                _Min = _EmissionProps._BurstDuration._Min * samplesPerMS,
                _Max = _EmissionProps._BurstDuration._Max * samplesPerMS,
                _LockStartValue = false,
                _LockEndValue = false,
                _InteractionInput = _EmissionProps._BurstDuration.GetInteractionValue()
            };
            burstData._Playhead = new ModulateParameterComponent
            {
                _StartValue = _EmissionProps._Playhead._Start,
                _EndValue = _EmissionProps._Playhead._End,
                _InteractionAmount = _EmissionProps._Playhead._InteractionAmount,
                _Shape = _EmissionProps._Playhead._InteractionShape,
                _Noise = _EmissionProps._Playhead._Noise,
                _LockNoise = _EmissionProps._Playhead._LockNoise,
                _Min = _EmissionProps._Playhead._Min,
                _Max = _EmissionProps._Playhead._Max,
                _LockStartValue = _EmissionProps._Playhead._LockStartValue,
                _LockEndValue = false,
                _InteractionInput = _EmissionProps._Playhead.GetInteractionValue()
            };
            burstData._Density = new ModulateParameterComponent
            {
                _StartValue = _EmissionProps._Density._Start,
                _EndValue = _EmissionProps._Density._End,
                _InteractionAmount = _EmissionProps._Density._InteractionAmount,
                _Shape = _EmissionProps._Density._InteractionShape,
                _Noise = _EmissionProps._Density._Noise,
                _LockNoise = _EmissionProps._Density._LockNoise,
                _Min = _EmissionProps._Density._Min,
                _Max = _EmissionProps._Density._Max,
                _LockStartValue = false,
                _LockEndValue = false,
                _InteractionInput = _EmissionProps._Density.GetInteractionValue()
            };
            burstData._GrainDuration = new ModulateParameterComponent
            {
                _StartValue = _EmissionProps._GrainDuration._Start * samplesPerMS,
                _EndValue = _EmissionProps._GrainDuration._End * samplesPerMS,
                _InteractionAmount = _EmissionProps._GrainDuration._InteractionAmount * samplesPerMS,
                _Shape = _EmissionProps._GrainDuration._InteractionShape,
                _Noise = _EmissionProps._GrainDuration._Noise,
                _LockNoise = _EmissionProps._GrainDuration._LockNoise,
                _Min = _EmissionProps._GrainDuration._Min * samplesPerMS,
                _Max = _EmissionProps._GrainDuration._Max * samplesPerMS,
                _LockStartValue = false,
                _LockEndValue = false,
                _InteractionInput = _EmissionProps._GrainDuration.GetInteractionValue()
            };
            burstData._Transpose = new ModulateParameterComponent
            {
                _StartValue = _EmissionProps._Transpose._Start,
                _EndValue = _EmissionProps._Transpose._End,
                _InteractionAmount = _EmissionProps._Transpose._InteractionAmount,
                _Shape = _EmissionProps._Transpose._InteractionShape,
                _Noise = _EmissionProps._Transpose._Noise,
                _LockNoise = _EmissionProps._Transpose._LockNoise,
                _Min = _EmissionProps._Transpose._Min,
                _Max = _EmissionProps._Transpose._Max,
                _LockStartValue = false,
                _LockEndValue = _EmissionProps._Transpose._LockEndValue,
                _InteractionInput = _EmissionProps._Transpose.GetInteractionValue()
            };
            burstData._Volume = new ModulateParameterComponent
            {
                _StartValue = _EmissionProps._Volume._Start,
                _EndValue = _EmissionProps._Volume._End,
                _InteractionAmount = _EmissionProps._Volume._InteractionAmount,
                _Shape = _EmissionProps._Volume._InteractionShape,
                _Noise = _EmissionProps._Volume._Noise,
                _LockNoise = _EmissionProps._Volume._LockNoise,
                _Min = _EmissionProps._Volume._Min,
                _Max = _EmissionProps._Volume._Max,
                _LockStartValue = false,
                _LockEndValue = _EmissionProps._Volume._LockEndValue,
                _InteractionInput = _EmissionProps._Volume.GetInteractionValue() * _VolumeMultiply
            };


            burstData._DistanceAmplitude = volumeDistanceAdjust;

            _EntityManager.SetComponentData(_EmitterEntity, burstData);

            //---   DSP CHAIN        
            UpdateDSPBuffer();

            _InRangeTemp = burstData._InRange;

            _AttachedSpeakerIndex = burstData._SpeakerIndex;
            _AttachedToSpeaker = burstData._AttachedToSpeaker;

            Translation trans = _EntityManager.GetComponentData<Translation>(_EmitterEntity);
            _EntityManager.SetComponentData(_EmitterEntity, new Translation
            {
                Value = transform.position
            });

            _CollisionTriggered = false;
            _EmissionProps._Playing = false;
        }
        // Clear emitter props and colliding object when burst is complete if this is a remote burst emitter
        if (_EmitterSetup == EmitterSetup.Temp)
        {
            _TimeExisted += Time.deltaTime;

            if (_TimeExisted > 3)
                Destroy(gameObject);
        }
    }
}
