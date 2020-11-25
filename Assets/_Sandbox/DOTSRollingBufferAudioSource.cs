using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

public class DOTSRollingBufferAudioSource : MonoBehaviour
{
    EntityManager _EntityManager;
    Entity _RollingAudioBufferEntity;

    float[] _RollingBuffer;

    int _CurrentDSPSample;


    void Start()
    {
        //----  Create and entity and add a rolling audio buffer
        _EntityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        _RollingAudioBufferEntity = _EntityManager.CreateEntity();
        _EntityManager.SetName(_RollingAudioBufferEntity, "Rolling audio buffer");
        _EntityManager.AddComponentData(_RollingAudioBufferEntity, new RingBufferFiller { _StartIndex = 0, _SampleCount = 400 });
        _EntityManager.AddBuffer<AudioRingBufferElement>(_RollingAudioBufferEntity);

        //--  Initialize buffers
        _RollingBuffer = new float[44100];
        DynamicBuffer<AudioRingBufferElement> buffer = _EntityManager.GetBuffer<AudioRingBufferElement>(_RollingAudioBufferEntity);        
        for (int i = 0; i < 44100; i++)
        {
            _RollingBuffer[i] = 0;
            buffer.Add(new AudioRingBufferElement { Value = 0 });
        }
    }
    
    void Update()
    {
        //----  Copy buffer from entity
        NativeArray<float> samples = _EntityManager.GetBuffer<AudioRingBufferElement>(_RollingAudioBufferEntity).Reinterpret<float>().ToNativeArray(Allocator.Temp);
        GrainSynth.NativeToManagedCopyMemory(_RollingBuffer, samples);       
    }

    void OnAudioFilterRead(float[] data, int channels)
    {
        //----    TEST ROLLING BUFFER WORKING CORRECTLY
        //int currentDSPSample = GrainSynth.Instance._CurrentDSPSample;
        int rollingIndex;
        // For length of audio buffer, populate with grain samples, maintaining index over successive buffers
        for (int dataIndex = 0; dataIndex < data.Length; dataIndex += channels)
        {
            rollingIndex = (_CurrentDSPSample + dataIndex) % _RollingBuffer.Length;
            data[dataIndex] = _RollingBuffer[rollingIndex];
            data[dataIndex+1] = _RollingBuffer[rollingIndex];
        }

        _CurrentDSPSample += data.Length;
    }
}
