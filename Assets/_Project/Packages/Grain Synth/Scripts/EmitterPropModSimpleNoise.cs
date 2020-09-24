using EXPToolkit;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EmitterPropModSimpleNoise : MonoBehaviour
{
    public int _EmitterCount = 1;
    public float _LatencyInMS = 20;
    public GrainEmissionProps _EmissionProps;

    [Space]
    [Header("Automation")]

    public float _AutomationSpeed = 1;

    public bool _AutomatePlayhead = false;
    public Vector2 _AutomatePlayheadRange = new Vector2(0, 0);

    public bool _AutomateCadence = false;
    public Vector2 _AutomateCadenceRange = new Vector2(0, 0);

    public bool _AutomateDuration = false;
    public Vector2 _AutomateDurationRange = new Vector2(0, 0);

    public bool _AutomatePitch = false;
    public Vector2 _AutomatePitchRange = new Vector2(0, 0);

    [Space]
    [Space]
    [Space]

    public GrainEmitterAuthoring _EmitterPrefabDOTs;
    public GrainSynth _DOTSSystem;
    public Spawner _Spawner;

    GrainEmitterAuthoring[] _DOTSEmitters;

  

    // Start is called before the first frame update
    void Awake()
    {
        _EmitterPrefabDOTs._EmissionProps = _EmissionProps;

        _DOTSSystem._GrainQueueInMS = _LatencyInMS;

        _Spawner.m_ObjectsToSpawn = new GameObject[] { _EmitterPrefabDOTs.gameObject };        

        _Spawner.m_NumberToPool = _EmitterCount;
    }

    private void Update()
    {
        float automation = Mathf.PerlinNoise(Time.time * _AutomationSpeed, Time.time * _AutomationSpeed * .5f);

        if (_AutomatePlayhead)
            _EmissionProps.Position = Mathf.Lerp(_AutomatePlayheadRange.x, _AutomatePlayheadRange.y, automation);

        if (_AutomateCadence)
            _EmissionProps._Cadence = (int)Mathf.Lerp(_AutomateCadenceRange.x, _AutomateCadenceRange.y, automation);

        if (_AutomateDuration)
            _EmissionProps.Duration = Mathf.Lerp(_AutomateDurationRange.x, _AutomateDurationRange.y, automation);

        if (_AutomatePitch)
            _EmissionProps._Transpose = Mathf.Lerp(_AutomatePitchRange.x, _AutomatePitchRange.y, automation);
               
        if (_DOTSEmitters == null || _DOTSEmitters.Length == 0)
            _DOTSEmitters = FindObjectsOfType<GrainEmitterAuthoring>();

        for (int i = 0; i < _DOTSEmitters.Length; i++)
        {
            _DOTSEmitters[i]._EmissionProps = _EmissionProps;
        }    
    }
}
