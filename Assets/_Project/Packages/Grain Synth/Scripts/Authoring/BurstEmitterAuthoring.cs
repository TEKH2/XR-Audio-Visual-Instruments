﻿using Unity.Entities;
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
    public BurstEmissionProps _BurstEmissionProps;

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

            _BurstDuration = new ModulateParameterComponent
            {
                _StartValue = _BurstEmissionProps._BurstDuration._Default * samplesPerMS,
                _InteractionAmount = _BurstEmissionProps._BurstDuration._InteractionAmount * samplesPerMS,
                _Shape = _BurstEmissionProps._BurstDuration._InteractionShape,
                _Noise = _BurstEmissionProps._BurstDuration._Noise,
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
                _Noise = _BurstEmissionProps._Playhead._Noise,
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
                _Noise = _BurstEmissionProps._Density._Noise,
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
                _Noise = _BurstEmissionProps._GrainDuration._Noise,
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
                _Noise = _BurstEmissionProps._Transpose._Noise,
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
                _Noise = _BurstEmissionProps._Volume._Noise,
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

#if UNITY_EDITOR
        dstManager.SetName(entity, "Emitter");
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

        float samplesPerMS = AudioSettings.outputSampleRate * 0.001f;

        if (_CollisionTriggered & _WithinEarshot)
        {
            BurstEmitterComponent burstData = _EntityManager.GetComponentData<BurstEmitterComponent>(_EmitterEntity);

            int attachedSpeakerIndex = _StaticallyPaired ? _PairedSpeaker._SpeakerIndex : burstData._SpeakerIndex;
            
            _DistanceVolume = AudioUtils.EmitterFromListenerVolumeAdjust(_HeadPosition.position, transform.position, _MaxAudibleDistance);
            
            float volumeDistanceAdjust = AudioUtils.EmitterFromSpeakerVolumeAdjust(_HeadPosition.position,
                    GrainSynth.Instance._GrainSpeakers[attachedSpeakerIndex].gameObject.transform.position,
                    transform.position) * _DistanceVolume;

            burstData._Playing = true;
            burstData._SpeakerIndex = attachedSpeakerIndex;
            burstData._AudioClipIndex = _BurstEmissionProps._ClipIndex;

            burstData._BurstDuration = new ModulateParameterComponent
            {
                _StartValue = _BurstEmissionProps._BurstDuration._Default * samplesPerMS,
                _InteractionAmount = _BurstEmissionProps._BurstDuration._InteractionAmount * samplesPerMS,
                _Shape = _BurstEmissionProps._BurstDuration._InteractionShape,
                _Noise = _BurstEmissionProps._BurstDuration._Noise,
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
                _Noise = _BurstEmissionProps._Playhead._Noise,
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
                _Noise = _BurstEmissionProps._Density._Noise,
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
                _Noise = _BurstEmissionProps._GrainDuration._Noise,
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
                _Noise = _BurstEmissionProps._Transpose._Noise,
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
                _Noise = _BurstEmissionProps._Volume._Noise,
                _Min = _BurstEmissionProps._Volume._Min,
                _Max = _BurstEmissionProps._Volume._Max,
                _LockStartValue = false,
                _LockEndValue = _BurstEmissionProps._Volume._LockEndValue,
                _InteractionInput = _BurstEmissionProps._Volume.GetInteractionValue()
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
        }
    }
}
