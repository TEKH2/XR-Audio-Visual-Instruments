using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HumanSpaceInterface_UI : MonoBehaviour
{
    public HumanSpaceInterface _Interface;

    public Slider _PanSlider;
    public Slider _SeperationSlider;
    public Slider _RollSlider;
    public Slider _TwistSlider;

    void Update()
    {
        _PanSlider.value = _Interface._Pan.NormOutput;
        _SeperationSlider.value = _Interface._Seperation.NormOutput;
        _RollSlider.value = _Interface._Roll.NormOutput;
        _TwistSlider.value = _Interface._Twist.NormOutput;
    }
}
