using Ludiq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class GrainSynthVisualizer : MonoBehaviour
{
    ParticleSystem _PS;
    ParticleSystem.EmissionModule _Emission;
    ParticleSystem.MainModule _Main;

    public float _Scale = .1f;
    public float _Distance = 3;
    public float _Lifetime = 3;

    float _SampleRate;
    public int _HeightCount = 4;
    int _IncrementY = 0;

    public Transform _XAxisPivot;
    public Transform _XAxisPivot_Frametime;
    public Transform _YAxisPivot;

    public GrainSpeakerAuthoring _GrainSpeaker;


    GrainSynthVisualizerBlock[] _Blocks;
    int _PoolAmount = 200;
    int _BlockCounter = 0;
    public GrainSynthVisualizerBlock _BlockPrefab;

    public bool _EmitParticles = true;
    public bool _EmitBlocks = true;


    // Start is called before the first frame update
    void Start()
    {
        _PS = GetComponent<ParticleSystem>();
        _Emission = _PS.emission;
        _Main = _PS.main;
        _Main.startLifetime = _Lifetime;
        _Emission.rateOverTime = 0;

        _SampleRate = AudioSettings.outputSampleRate;
        _GrainSpeaker.OnGrainEmitted += EmitGrain;

        _Blocks = new GrainSynthVisualizerBlock[_PoolAmount];
        for (int i = 0; i < _Blocks.Length; i++)
        {
            GrainSynthVisualizerBlock newBlock = Instantiate(_BlockPrefab, transform);
            newBlock.gameObject.SetActive(false);
            _Blocks[i] = newBlock;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (_YAxisPivot != null)
            _YAxisPivot.SetScaleY(_Scale * _HeightCount);

        if (_XAxisPivot != null)
            _XAxisPivot.SetScaleX(_Distance);

        if (Application.isPlaying && _XAxisPivot_Frametime != null)
            _XAxisPivot_Frametime.SetScaleX(-GrainSynth.Instance._EmissionLatencyMS * .001f * (_Distance / _Lifetime));

        for (int i = 0; i < _Blocks.Length; i++)
        {
            if(_Blocks[i].gameObject.activeSelf)
            {
                int sampleDiff = _Blocks[i]._StartIndex - GrainSynth.Instance._CurrentDSPSample;
                Vector3 pos = transform.position + transform.right * (sampleDiff / _SampleRate) * (_Distance / _Lifetime);
                pos.y = _Blocks[i].transform.position.y;

                _Blocks[i].transform.position = pos;
            }
        }
    }

    Vector3 PosFromStartSampleIndex(int startSampleIndex)
    {
        int sampleDiff = startSampleIndex - GrainSynth.Instance._CurrentDSPSample;
        Vector3 pos = transform.position + transform.right * (sampleDiff / _SampleRate) * (_Distance / _Lifetime);

        pos.y += (_IncrementY % _HeightCount) * _Scale;
        pos.y += _Scale * .5f;
        _IncrementY++;

        return pos;
    }

    int prevEmitSample;
    int sampleGap;
    public void EmitGrain(GrainPlaybackData grainData, int currentDSPSample)
    {
        // Scale based on duration
        float durationInSeconds = grainData._PlaybackSampleCount / _SampleRate;
        durationInSeconds *= _Distance / _Lifetime;
        Vector3 size = new Vector3(durationInSeconds, _Scale, .001f);

        if (_EmitBlocks)
        {
            GrainSynthVisualizerBlock block = _Blocks[_BlockCounter];
            block.transform.position = PosFromStartSampleIndex(grainData._DSPStartIndex);
            block.transform.localScale = size;
            block._StartIndex = grainData._DSPStartIndex;
            block.gameObject.SetActive(true);
            _BlockCounter++;
            _BlockCounter %= _Blocks.Length;
        }


        // DEBUG
        //sampleGap = (int)(_SampleRate * .001f * 20);
        //int sampleTIming = grainData._DSPStartIndex - prevEmitSample;
        //if (sampleTIming != sampleGap)
        //    print(sampleTIming + "   " + sampleGap);

        prevEmitSample = grainData._DSPStartIndex;

        if (_EmitParticles)
        {
            ParticleSystem.EmitParams emit = new ParticleSystem.EmitParams();

            // Position based on start index       
            emit.position = PosFromStartSampleIndex(grainData._DSPStartIndex);


            emit.startSize3D = size;

            emit.startColor = Color.white;

            if (grainData._DSPStartIndex <= currentDSPSample)
                emit.startColor = Color.yellow;

            // Velocity based on lifetime/dist
            emit.velocity = -transform.right * (_Distance / _Lifetime);

            _PS.Emit(emit, 1);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawLine(transform.position, transform.position +  -transform.right * _Distance);
        Gizmos.DrawLine(transform.position, transform.position + transform.up * _Scale * _HeightCount);
    }
}
