using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EmissionPropertiesUI : MonoBehaviour
{
    GrainEmissionProps _EmissionProps;

    public Slider _Playhead;
    public Slider _PlayheadRand;

    public Slider _Cadence;
    public Slider _CadenceRand;

    public Slider _Duration;
    public Slider _DurationRand;

    public Slider _Volume;
    public Slider _VolumeRand;

    public Slider _Transpose;
    public Slider _TransposeRand;



    // Start is called before the first frame update
    void Start()
    {
        // Set ranges
        _Playhead.minValue = 0;
        _Playhead.maxValue = 1;
        _PlayheadRand.minValue = 0;
        _PlayheadRand.maxValue = 1f;

        _Cadence.minValue = 2f;
        _Cadence.maxValue = 1000f;
        _CadenceRand.minValue = 0f;
        _CadenceRand.maxValue = 1000f;

        _Duration.minValue = 2f;
        _Duration.maxValue = 1000f;
        _DurationRand.minValue = 0f;
        _DurationRand.maxValue = 500f;

        _Volume.minValue = 0f;
        _Volume.maxValue = 2f;
        _VolumeRand.minValue = 0f;
        _VolumeRand.maxValue = 1f;

        _Transpose.minValue = -3f;
        _Transpose.maxValue = 3f;
        _TransposeRand.minValue = 0f;
        _TransposeRand.maxValue = 1f;


        // Hook up events
        _Playhead.onValueChanged.AddListener((float f) => _EmissionProps._Playhead = f);
        _PlayheadRand.onValueChanged.AddListener((float f) => _EmissionProps._PlayheadRand = f);

        _Cadence.onValueChanged.AddListener((float f) => _EmissionProps._Cadence = (int)f);
        _CadenceRand.onValueChanged.AddListener((float f) => _EmissionProps._CadenceRandom = (int)f);

        _Duration.onValueChanged.AddListener((float f) => _EmissionProps._Duration = (int)f);
        _DurationRand.onValueChanged.AddListener((float f) => _EmissionProps._DurationRandom = (int)f);

        _Volume.onValueChanged.AddListener((float f) => _EmissionProps._Volume = f);
        _VolumeRand.onValueChanged.AddListener((float f) => _EmissionProps._VolumeRandom = f);

        _Transpose.onValueChanged.AddListener((float f) => _EmissionProps._Transpose = f);
        _TransposeRand.onValueChanged.AddListener((float f) => _EmissionProps._TransposeRandom = f);
    }

    void Update()
    {
        UpdateSliderValues();
    }

    public void AssignEmitterProps(GrainEmissionProps emitProps)
    {
        _EmissionProps = emitProps;
        UpdateSliderValues();
    }

    void UpdateSliderValues()
    {
        _Playhead.SetValueWithoutNotify(_EmissionProps._Playhead);
        _PlayheadRand.SetValueWithoutNotify(_EmissionProps._PlayheadRand);

        _Cadence.SetValueWithoutNotify(_EmissionProps._Cadence);
        _CadenceRand.SetValueWithoutNotify(_EmissionProps._CadenceRandom);

        _Duration.SetValueWithoutNotify(_EmissionProps._Duration);
        _DurationRand.SetValueWithoutNotify(_EmissionProps._DurationRandom);

        _Volume.SetValueWithoutNotify(_EmissionProps._Volume);
        _VolumeRand.SetValueWithoutNotify(_EmissionProps._VolumeRandom);

        _Transpose.SetValueWithoutNotify(_EmissionProps._Transpose);
        _TransposeRand.SetValueWithoutNotify(_EmissionProps._TransposeRandom);
    }


}
