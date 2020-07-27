using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrainEmitter : MonoBehaviour
{
    public GrainEmissionProps _GrainEmissionProps;
    public FilterCoefficients _FilterCoefficients;
    int _LastGrainSampleIndex = 0;
    public GrainAudioSource _AudioSource;

    bool _Initialized = false;

    public void Init(int currentDSPIndex, GrainAudioSource audioSource)
    {
        _LastGrainSampleIndex = currentDSPIndex;
        _AudioSource = audioSource;
    }

    public void ManualUpdate(GranulatorManager manager, int maxDSPIndex, int sampleRate)
    {
        if (!_Initialized)
            OnEnable();

        // Calculate random sample rate
        int currentCadence = (int)(sampleRate * _GrainEmissionProps.Cadence * .001f);
        // Find sample that next grain is emitted at
        int sampleIndexNextGrainStart = _LastGrainSampleIndex + currentCadence;

        // Clamp audio clip selection to available clips
        _GrainEmissionProps._ClipIndex = Mathf.Clamp(_GrainEmissionProps._ClipIndex, 0, manager._AudioClipLibrary._Clips.Length - 1);

        while (sampleIndexNextGrainStart <= maxDSPIndex)
        {
            GrainData tempGrainData = new GrainData
            (
                transform.position,
                Vector3.zero, 0,
                _GrainEmissionProps._ClipIndex,
                _GrainEmissionProps.Duration,
                _GrainEmissionProps.Position,
                _GrainEmissionProps.Pitch,
                _GrainEmissionProps.Volume,
                _FilterCoefficients,
                sampleIndexNextGrainStart
            );

            // EMit grain from manager
            manager.EmitGrain(tempGrainData, _AudioSource);

            // Set last grain index
            _LastGrainSampleIndex = sampleIndexNextGrainStart;

            currentCadence = (int)(sampleRate * _GrainEmissionProps.Cadence * .001f);
            sampleIndexNextGrainStart = sampleIndexNextGrainStart + currentCadence;
        }
    }

    private void OnEnable()
    {
        if (GranulatorManager.Instance != null)
        {
            GranulatorManager.Instance.AddGrainEmitter(this);
            _Initialized = true;
        }      
    }

    private void OnDisable()
    {
        GranulatorManager.Instance.RemoveGrainEmitter(this);
    }

    private void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            Gizmos.DrawLine(transform.position, _AudioSource.transform.position);
        }
    }
}
