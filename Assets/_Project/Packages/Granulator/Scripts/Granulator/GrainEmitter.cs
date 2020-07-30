using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Generates grains that are fed into a grain audio source to be played back
public class GrainEmitter : MonoBehaviour
{
    #region VARIABLES

    public GrainEmissionProps _GrainEmissionProps;
    public FilterCoefficients _FilterCoefficients;
    int _LastGrainSampleIndex = 0;
    int _RandomSampleOffset;
    public bool _RandomizedPlaybackPos = false;
    public bool _Active = false;
    #endregion

    private void Start()
    {
        GrainManager.Instance.AddNewEmitter(this);
    }

    public void Init(int currentDSPIndex)
    {
        // random offset so not all emitters play at the exact same time
        _RandomSampleOffset = Random.Range(0, 150);
        _LastGrainSampleIndex = currentDSPIndex;

        _Active = true;

        if (_RandomizedPlaybackPos)
            _GrainEmissionProps.Position = Random.Range(.1f, .9f);
    }

    public void ManualUpdate(GrainSpeaker output, int maxDSPIndex, int sampleRate)
    {
        if (!_Active)
            return;

        // Calculate random sample rate
        int currentCadence = (int)(sampleRate * _GrainEmissionProps.Cadence * .001f);
        // Find sample that next grain is emitted at
        int sampleIndexNextGrainStart = _LastGrainSampleIndex + currentCadence;

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
                sampleIndexNextGrainStart + _RandomSampleOffset
            );

            // EMit grain from manager
            output.EmitGrain(tempGrainData);

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
