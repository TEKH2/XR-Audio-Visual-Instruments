using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Generates grains that are fed into a grain audio source to be played back
public class GrainEmitter : MonoBehaviour
{
    public GrainEmissionProps _GrainEmissionProps;
    public FilterCoefficients _FilterCoefficients;
    int _LastGrainSampleIndex = 0;
    public GrainAudioOutput _AudioSource;

    int _RandomSampleOffset;

    public bool _RandomizedPlaybackPos = false;

    bool _Initialized = false;
    bool _Active = false;

    private void Awake()
    {
        if (GranulatorManager.Instance != null)
        {
            GranulatorManager.Instance.AssignEmitterToSource(this);
            _Initialized = true;
        }
    }

    public void Init(int currentDSPIndex, GrainAudioOutput audioSource)
    {
        // random offset so not all emitters play at the exact same time
        _RandomSampleOffset = Random.Range(0, 150);
        _LastGrainSampleIndex = currentDSPIndex;
        _AudioSource = audioSource;
        audioSource.AttachGrainEmitter(this);

        _Active = true;

        if (_RandomizedPlaybackPos)
            _GrainEmissionProps.Position = Random.Range(.1f, .9f);
    }

    public void ManualUpdate(GranulatorManager manager, int maxDSPIndex, int sampleRate)
    {
        if (!_Initialized)
            Awake();

        if (!_Active)
            return;

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
                _GrainEmissionProps._ClipIndex,
                _GrainEmissionProps.Duration,
                _GrainEmissionProps.Position,
                _GrainEmissionProps.Pitch,
                _GrainEmissionProps.Volume,
                _FilterCoefficients,
                sampleIndexNextGrainStart + _RandomSampleOffset
            );

            // EMit grain from manager
            manager.EmitGrain(tempGrainData, _AudioSource);

            // Set last grain index
            _LastGrainSampleIndex = sampleIndexNextGrainStart;

            currentCadence = (int)(sampleRate * _GrainEmissionProps.Cadence * .001f);
            sampleIndexNextGrainStart = sampleIndexNextGrainStart + currentCadence;
        }
    }

    public void Deactivate()
    {
        _Active = false;
        GranulatorManager.Instance.RemoveGrainEmitter(this);
    }

    private void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            if (_Active)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(transform.position, .2f);
                Gizmos.DrawLine(transform.position, _AudioSource.transform.position);
            }
            else
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawSphere(transform.position, .2f);
            }
        }
    }
}
