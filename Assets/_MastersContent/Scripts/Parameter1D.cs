using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Parameter1D : ParameterController
{
    private float _Value;
    private float _ValuePrevious;

    private void Start()
    {
        // Get Interaction Size
        _InteractionSize = _InteractionBoundsObject.transform.localScale / 2.0f;
    }

    private void FixedUpdate()
    {
    }

    void Update()
    {
        // Limit the sliders position
        ApplyConstraints();
        // Output OSC
        Output();
        // Store current value for next update
        _ValuePrevious = _Value;
    }

    void ApplyConstraints()
    {
        Vector3 newLocalPos = _SliderObject.transform.localPosition;

        newLocalPos = new Vector3
            (newLocalPos.x, LimitAxis(_InteractionSize.y, newLocalPos.y), newLocalPos.z);

        _SliderObject.transform.localPosition = newLocalPos;

        _Value = Map(newLocalPos.y, -_InteractionSize.y, _InteractionSize.y, 0f, 1f, true);
    }

    void Output()
    {
        if (_Value != _ValuePrevious)
            SendOSC(_Name + "/x", _Value);
    }
}