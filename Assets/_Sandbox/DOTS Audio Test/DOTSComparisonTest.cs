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
    [Space]
    [Space]

    public EmitterDOTSAuthoring _EmitterPrefabDOTs;
    public GrainEmitter _EmitterPrefab;
    public GranulatorDOTS _DOTSSystem;
    public Spawner _Spawner;
    public GrainManager _GrainManager;


    // Start is called before the first frame update
    void Awake()
    {
        _EmitterPrefab._GrainEmissionProps = _EmissionProps;
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
}
