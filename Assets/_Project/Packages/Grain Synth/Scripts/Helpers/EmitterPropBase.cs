using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EmitterPropBase
{
    public InteractionParameter _InteractionInput;

    public float GetInteractionValue()
    {
        float interaction = 0;

        if (_InteractionInput != null)
            interaction = _InteractionInput.GetValue();

        return interaction;
    }
}
