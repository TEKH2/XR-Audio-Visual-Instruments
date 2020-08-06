﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Generates grains that are fed into a grain speaker to be played back
public class GrainEmitter : MonoBehaviour
{
    #region VARIABLES
    public GrainEmissionProps _GrainEmissionProps;
    private FilterCoefficients _FilterCoefficients; //TODO reimpliment
    private BitcrushSignal _Bitcrush;

    
    int _LastGrainSampleIndex = 0;

    public bool _RandomizedPlaybackPos = false;
    public bool _Active = false;
    #endregion

    private void Start()
    {
        GrainManager.Instance.AddGrainEmitterToList(this);
    }

    public void Init(int currentDSPIndex)
    {
        _LastGrainSampleIndex = currentDSPIndex;

        _Active = true;

        if (_RandomizedPlaybackPos)
            _GrainEmissionProps.Position = Random.Range(.1f, .9f);
    }

    public void ManualUpdate(GrainSpeaker speaker, int maxDSPIndex, int sampleRate)
    {
        if (!_Active)
            return;

        // Calculate random sample rate
        int currentCadence = (int)(sampleRate * _GrainEmissionProps.Cadence * .001f);
        // Find sample that next grain is emitted at
        int sampleIndexNextGrainStart = _LastGrainSampleIndex + currentCadence;


        _FilterCoefficients = DSP_Effects.CreateCoefficents(_GrainEmissionProps._DSP_Properties);
        _Bitcrush = new BitcrushSignal();
        _Bitcrush.downsampleFactor = _GrainEmissionProps._DSP_Properties.BitcrushAmount;

        while (sampleIndexNextGrainStart <= maxDSPIndex)
        {
            GrainData tempGrainData = new GrainData
            (
                _GrainEmissionProps._ClipIndex,
                _GrainEmissionProps.Duration,
                _GrainEmissionProps.Position,
                _GrainEmissionProps.Pitch,
                _GrainEmissionProps.Volume,
                _FilterCoefficients,
                _Bitcrush,
                sampleIndexNextGrainStart
            );

            // Emit grain from manager
            speaker.AddGrainData(tempGrainData);

            // Set last grain index
            _LastGrainSampleIndex = sampleIndexNextGrainStart;

            currentCadence = (int)(sampleRate * _GrainEmissionProps.Cadence * .001f);
            sampleIndexNextGrainStart = sampleIndexNextGrainStart + currentCadence;
        }
    }

    public void Deactivate()
    {
        _Active = false;
    }

    private void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            if (_Active)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(transform.position, .2f);
            }
            else
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawSphere(transform.position, .2f);
            }
        }
    }
}
