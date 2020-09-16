using Ludiq;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.PackageManager.UI;
using UnityEngine;


public class GrainSynthVisualizer : MonoBehaviour
{
    #region ------------------------------------- VARIABLES 
    float _SampleRate;
    public GrainEmitterAuthoring _Emitter;
    Vector3 _LookAtPos;

    // ----------------------------------- WAVEFORM/PLAYHEAD
    [Header("Waveform - Playhead")]
    public float _ArcTotalAngle = 90;    
    public float _ArcRadius = 4;
    public int _WaveformBlockCount = 200;
    public float _WaveformHeight = 1;
    public float _WaveformBlockHeight = .5f;
    public GameObject _WaveformBlockPrefab;
    GameObject[] _WaveformBlocks;
    public LineRenderer _WaveformBorderLine;
    public int _BorderVerts = 30;

    public Transform _WaveformPlayhead;

    // ----------------------------------- TIMELINE
    [Header("Timeline")]
    public float _TimelineScale = .1f;
    public float _TimelineDistance = 3;
    public float _TimelineDuration = 3;

   
    public int _TimelineHeightCount = 4;
    int _IncrementY = 0;

    public Transform _XAxisPivot;
    public Transform _XAxisPivot_Frametime;
    public Transform _YAxisPivot;

    public GrainSpeakerAuthoring _GrainSpeaker;

    public GrainSynthVisualizerBlock _TimelineBlockPrefab;
    GrainSynthVisualizerBlock[] _TimelineBlocks;
    int _TimelinePoolAmount = 200;
    int _BlockCounter = 0;
    #endregion

    #region ------------------------------------- UNITY METHODS
    // Start is called before the first frame update
    void Start()
    {     
        _SampleRate = AudioSettings.outputSampleRate;
        _GrainSpeaker.OnGrainEmitted += EmitGrain;

        // Waveform
        float[] clipData = new float[GrainSynth.Instance._AudioClips[0].samples];
        GrainSynth.Instance._AudioClips[0].GetData(clipData, 0);
        int samplesPerBlock = clipData.Length / _WaveformBlockCount;

        float maxSampleValue = 0;
        float minSampleValue = float.MaxValue;
        _WaveformBlocks = new GameObject[_WaveformBlockCount];
        float waveformBlockWidth = Vector3.Distance(GetPositionOnArc(0), GetPositionOnArc(1f/ (_WaveformBlockCount-1) ) );
        float[] averagedSamples = new float[_WaveformBlockCount];

        _LookAtPos = Vector3.up * _WaveformHeight;

        int index = 0;
        for (int i = 0; i < _WaveformBlockCount; i++)
        {
            float sumedSquaredSamples = 0;
            for (int s = 0; s < samplesPerBlock; s++)
            {
                sumedSquaredSamples += clipData[index] * clipData[index]; // sum squared samples
                index++;
            }

            sumedSquaredSamples /= samplesPerBlock;
            float rmsValue = Mathf.Sqrt(sumedSquaredSamples / samplesPerBlock); // rms = square root of average

            maxSampleValue = Mathf.Max(rmsValue, maxSampleValue);
            minSampleValue = Mathf.Min(rmsValue, minSampleValue);
            averagedSamples[i] = rmsValue;

            float norm = i / (_WaveformBlockCount - 1f);
            _WaveformBlocks[i] = Instantiate(_WaveformBlockPrefab, transform);
            _WaveformBlocks[i].transform.position = GetPositionOnArc(norm);
            _WaveformBlocks[i].transform.LookAt(_LookAtPos);
            _WaveformBlocks[i].transform.rotation *= Quaternion.Euler(0, 180, 0);
        }

        for (int i = 0; i < _WaveformBlockCount; i++)
        {
            float height = Mathf.InverseLerp(minSampleValue, maxSampleValue, averagedSamples[i]);
            _WaveformBlocks[i].transform.localScale = new Vector3(waveformBlockWidth * .7f, height * _WaveformBlockHeight * .85f, .01f);
        }

        // Setup waveform border
        _WaveformBorderLine.positionCount = _BorderVerts * 2;
        

        int borderIndex = 0;
        for (int i = 0; i < _BorderVerts; i++)
        {
            float norm = i / (_BorderVerts - 1f);
            norm = Mathf.Lerp(-.01f, 1.01f, norm);
            _WaveformBorderLine.SetPosition(borderIndex, GetPositionOnArc(norm, _WaveformBlockHeight * .5f));
            borderIndex++;
        }

        for (int i = 0; i < _BorderVerts; i++)
        {
            float norm = 1 - (i / (_BorderVerts - 1f));
            norm = Mathf.Lerp(-.01f, 1.01f, norm);
            _WaveformBorderLine.SetPosition(borderIndex, GetPositionOnArc(norm, _WaveformBlockHeight * -.5f));
            borderIndex++;
        }

        _WaveformPlayhead.localScale = new Vector3(.03f, _WaveformBlockHeight * .85f, 1);


        // Create grain blocks
        _TimelineBlocks = new GrainSynthVisualizerBlock[_TimelinePoolAmount];
        for (int i = 0; i < _TimelineBlocks.Length; i++)
        {
            GrainSynthVisualizerBlock newBlock = Instantiate(_TimelineBlockPrefab, transform);
            newBlock.gameObject.SetActive(false);
            _TimelineBlocks[i] = newBlock;
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Waveform
        _WaveformPlayhead.position = GetPositionOnArc(_Emitter._EmissionProps._Playhead, 0, -.03f);
        _WaveformPlayhead.LookAt(_LookAtPos);

        // Grain timeline
        if (_YAxisPivot != null)
            _YAxisPivot.SetScaleY(_TimelineScale * _TimelineHeightCount);

        if (_XAxisPivot != null)
            _XAxisPivot.SetScaleX(_TimelineDistance);

        if (Application.isPlaying && _XAxisPivot_Frametime != null)
            _XAxisPivot_Frametime.SetScaleX(-GrainSynth.Instance._LatencyInMS * .001f * (_TimelineDistance / _TimelineDuration));

        for (int i = 0; i < _TimelineBlocks.Length; i++)
        {
            if(_TimelineBlocks[i].gameObject.activeSelf)
            {
                int sampleDiff = _TimelineBlocks[i]._StartIndex - GrainSynth.Instance._CurrentDSPSample;
                Vector3 pos = transform.position + transform.right * (sampleDiff / _SampleRate) * (_TimelineDistance / _TimelineDuration);
                pos.y = _TimelineBlocks[i].transform.position.y;

                _TimelineBlocks[i].transform.position = pos;
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawLine(transform.position, transform.position + -transform.right * _TimelineDistance);
        Gizmos.DrawLine(transform.position, transform.position + transform.up * _TimelineScale * _TimelineHeightCount);

        Vector3 prevPos;
        for (int i = 1; i < 10; i++)
        {
            float norm0 = (i-1) / 9f;
            float norm1 = i / 9f;

            Gizmos.DrawLine(GetPositionOnArc(norm0, _WaveformBlockHeight *.5f), GetPositionOnArc(norm1, _WaveformBlockHeight * .5f));
        }

        for (int i = 1; i < 10; i++)
        {
            float norm0 = (i - 1) / 9f;
            float norm1 = i / 9f;

            Gizmos.DrawLine(GetPositionOnArc(norm0, _WaveformBlockHeight * -.5f), GetPositionOnArc(norm1, _WaveformBlockHeight * -.5f));
        }
    }
    #endregion

    Vector3 PosFromStartSampleIndex(int startSampleIndex)
    {
        int sampleDiff = startSampleIndex - GrainSynth.Instance._CurrentDSPSample;
        Vector3 pos = transform.position + transform.right * (sampleDiff / _SampleRate) * (_TimelineDistance / _TimelineDuration);

        pos.y += (_IncrementY % _TimelineHeightCount) * _TimelineScale;
        pos.y += _TimelineScale * .5f;
        _IncrementY++;

        return pos;
    }

    public Vector3 GetPositionOnArc(float norm, float yOffset = 0, float radiusOffset = 0)
    {
        float radians = (-_ArcTotalAngle * .5f * Mathf.Deg2Rad) + (norm * _ArcTotalAngle * Mathf.Deg2Rad);

        Vector3 pos = Vector3.zero;

        pos.x = Mathf.Sin(radians) * (_ArcRadius + radiusOffset);
        pos.z = Mathf.Cos(radians) * (_ArcRadius + radiusOffset);
        pos.y = _WaveformHeight + yOffset;

        return pos;
    }
   
    public void EmitGrain(GrainPlaybackData grainData, int currentDSPSample)
    {
        // Scale based on duration
        float durationInSeconds = grainData._PlaybackSampleCount / _SampleRate;
        durationInSeconds *= _TimelineDistance / _TimelineDuration;
        Vector3 size = new Vector3(durationInSeconds, _TimelineScale, .001f);
                
        GrainSynthVisualizerBlock block = _TimelineBlocks[_BlockCounter];
        block.transform.position = PosFromStartSampleIndex(grainData._DSPStartIndex);
        block.transform.localScale = size;
        block._StartIndex = grainData._DSPStartIndex;
        block.gameObject.SetActive(true);
        _BlockCounter++;
        _BlockCounter %= _TimelineBlocks.Length;        
    }
}
