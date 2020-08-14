using EXPToolkit;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DOTSComparisonTest : MonoBehaviour
{
    public enum Test
    {
        DOTS,
        Normal,
    }

    public Test _TestType = Test.DOTS;
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

    public EmitterDOTSAuthoring _EmitterPrefabDOTs;
    public GrainEmitter _EmitterPrefab;
    public GranulatorDOTS _DOTSSystem;
    public Spawner _Spawner;
    public GrainManager _GrainManager;

    EmitterDOTSAuthoring[] _DOTSEmitters;
    GrainEmitter[] _GrainEmitters;

  

    // Start is called before the first frame update
    void Awake()
    {
        _EmitterPrefab._EmissionProps = _EmissionProps;
        _EmitterPrefabDOTs._EmissionProps = _EmissionProps;

        _GrainManager._EmissionLatencyMS = _LatencyInMS;
        _DOTSSystem._LatencyInMS = _LatencyInMS;

        if (_TestType == Test.Normal)
        {
            _Spawner.m_ObjectsToSpawn = new GameObject[] { _EmitterPrefab.gameObject };
        }
        else
        {
            _Spawner.m_ObjectsToSpawn = new GameObject[] { _EmitterPrefabDOTs.gameObject };
        }

        _Spawner.m_NumberToPool = _EmitterCount;

        _DOTSSystem.gameObject.SetActive(_TestType == Test.DOTS);
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
            _EmissionProps.Pitch = Mathf.Lerp(_AutomatePitchRange.x, _AutomatePitchRange.y, automation);

        _EmissionProps._FilterCoefficients = DSP_Effects.CreateCoefficents(_EmissionProps._DSP_Properties);

        if (_TestType == Test.DOTS)
        {
            if (_DOTSEmitters == null || _DOTSEmitters.Length == 0)
                _DOTSEmitters = FindObjectsOfType<EmitterDOTSAuthoring>();

            for (int i = 0; i < _DOTSEmitters.Length; i++)
            {
                _DOTSEmitters[i]._EmissionProps = _EmissionProps;
            }
        }
        else
        {
            if (_GrainEmitters == null || _GrainEmitters.Length == 0)
                _GrainEmitters = FindObjectsOfType<GrainEmitter>();

            for (int i = 0; i < _GrainEmitters.Length; i++)
            {
                _GrainEmitters[i]._EmissionProps = _EmissionProps;
            }
        }
    }
}
