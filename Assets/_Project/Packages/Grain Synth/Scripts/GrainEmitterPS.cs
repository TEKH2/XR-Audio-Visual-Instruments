using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrainEmitterPS : MonoBehaviour
{
    ParticleSystem _PS;
    ParticleSystem.EmissionModule _Emission;
    ParticleSystem.MainModule _Main;

    public GrainEmitterAuthoring _GrainEmitter;

    // Start is called before the first frame update
    void Start()
    {
        _PS = GetComponent<ParticleSystem>();
        _Emission = _PS.emission;
        _Main = _PS.main;
    }

    // Update is called once per frame
    void Update()
    {
        if (_GrainEmitter._AttachedToSpeaker)
        {
            //_Main.startLifetime = _GrainEmitter._EmissionProps.Duration * .001f;
            //_Emission.rateOverTime = 1000f / _GrainEmitter._EmissionProps.Cadence;
        }
        else
        {
            _Emission.rateOverTime = 0;
        }
    }
}
