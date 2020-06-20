using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Granulator_UI : MonoBehaviour
{
    public Slider _Cadence;
    public Slider _Durtion;
    public Slider _Pos;

    public Granulator _Granulator;

    // Start is called before the first frame update
    void Start()
    {
        _Cadence.onValueChanged.AddListener((float f) => _Granulator._TimeBetweenGrains = (int)f);
        _Durtion.onValueChanged.AddListener((float f) => _Granulator._EmitGrainProps.Duration = (int)f);
        _Pos.onValueChanged.AddListener((float f) => _Granulator._EmitGrainProps.Position = f);
    }
}
