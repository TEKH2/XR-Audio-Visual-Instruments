using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EmitterPropBase
{
    public InteractionBase _InteractionInput;

    [Range(0f, 1.0f)]
    [SerializeField]
    public float _Noise = 0f;

    public float GetInteractionValue()
    {
        float interaction = 0;

        if (_InteractionInput != null)
            interaction = _InteractionInput.GetValue();

        return interaction;
    }

    public void CheckInteractionInput()
    {
        if (_InteractionInput == null)
            _InteractionInput = new BlankInteraction();
    }
}

public class GrainPropBase : EmitterPropBase
{
    public bool _PerlinNoise = false;
}

public class BurstPropBase : EmitterPropBase
{
    public bool _LockNoise = false;
}

[System.Serializable]
public class EmitterPropDensity : GrainPropBase
{
    [Range(0.1f, 10f)]
    [SerializeField]
    public float _Idle = 2f;
    [Range(-9.9f, 9.9f)]
    [SerializeField]
    public float _InteractionAmount = 0f;
    [Range(0.5f, 5.0f)]
    [SerializeField]
    public float _InteractionShape = 1f;
    [HideInInspector]
    public float _Min = 0.1f;
    [HideInInspector]
    public float _Max = 10f;
}

[System.Serializable]
public class EmitterPropDuration : GrainPropBase
{
    [Range(2f, 500f)]
    [SerializeField]
    public float _Idle = 50f;
    [Range(-502f, 502f)]
    [SerializeField]
    public float _InteractionAmount = 0f;
    [Range(0.5f, 5.0f)]
    [SerializeField]
    public float _InteractionShape = 1f;
    [HideInInspector]
    public float _Min = 2f;
    [HideInInspector]
    public float _Max = 500f;
}

[System.Serializable]
public class EmitterPropPlayhead : GrainPropBase
{
    [Range(0f, 1f)]
    [SerializeField]
    public float _Idle = 0f;
    [Range(-1f, 1f)]
    [SerializeField]
    public float _InteractionAmount = 0f;
    [Range(0.5f, 5.0f)]
    [SerializeField]
    public float _InteractionShape = 1f;
    [HideInInspector]
    public float _Min = 0f;
    [HideInInspector]
    public float _Max = 1f;
}

[System.Serializable]
public class EmitterPropTranspose : GrainPropBase
{
    [Range(-3f, 3f)]
    [SerializeField]
    public float _Idle = 1;
    [Range(-6f, 6f)]
    [SerializeField]
    public float _InteractionAmount = 0f;
    [Range(0.5f, 5.0f)]
    [SerializeField]
    public float _InteractionShape = 1f;
    [HideInInspector]
    public float _Min = -3f;
    [HideInInspector]
    public float _Max = 3f;
}

[System.Serializable]
public class EmitterPropVolume : GrainPropBase
{
    [Range(0f, 2f)]
    [SerializeField]
    public float _Idle = 1f;
    [Range(-2f, 2f)]
    [SerializeField]
    public float _InteractionAmount = 0f;
    [Range(0.5f, 5.0f)]
    [SerializeField]
    public float _InteractionShape = 1f;
    [HideInInspector]
    public float _Min = 0f;
    [HideInInspector]
    public float _Max = 2f;
}



[System.Serializable]
public class BurstPropDensity : BurstPropBase
{
    [Range(0.1f, 10f)]
    [SerializeField]
    public float _Start = 2f;
    [Range(0.1f, 10f)]
    [SerializeField]
    public float _End = 2f;
    [Range(-9.9f, 9.9f)]
    [SerializeField]
    public float _InteractionAmount = 0f;
    [Range(0.5f, 5.0f)]
    [SerializeField]
    public float _InteractionShape = 1f;
    [HideInInspector]
    public float _Min = 0.1f;
    [HideInInspector]
    public float _Max = 10f;
}

[System.Serializable]
public class BurstPropDuration : BurstPropBase
{
    [Range(10f, 1000f)]
    [SerializeField]
    public float _Default = 100f;
    [Range(-990f, 990f)]
    [SerializeField]
    public float _InteractionAmount = 0f;
    [Range(0.5f, 5.0f)]
    [SerializeField]
    public float _InteractionShape = 1f;
    [HideInInspector]
    public float _Min = 10f;
    [HideInInspector]
    public float _Max = 1000f;
}

[System.Serializable]
public class BurstPropGrainDuration : BurstPropBase
{
    [Range(5f, 500f)]
    [SerializeField]
    public float _Start = 20f;
    [Range(5f, 500f)]
    [SerializeField]
    public float _End = 20f;
    [Range(-495f, 495f)]
    [SerializeField]
    public float _InteractionAmount = 0f;
    [Range(0.5f, 5.0f)]
    [SerializeField]
    public float _InteractionShape = 1f;
    [HideInInspector]
    public float _Min = 5f;
    [HideInInspector]
    public float _Max = 500f;
}

[System.Serializable]
public class BurstPropPlayhead : BurstPropBase
{
    [Range(0f, 1f)]
    [SerializeField]
    public float _Start = 0f;
    [SerializeField]
    public bool _LockStartValue = true;
    [Range(0f, 1f)]
    [SerializeField]
    public float _End = 1f;
    [Range(-1f, 1f)]
    [SerializeField]
    public float _InteractionAmount = 0f;
    [Range(0.5f, 5.0f)]
    [SerializeField]
    public float _InteractionShape = 1f;
    [HideInInspector]
    public float _Min = 0f;
    [HideInInspector]
    public float _Max = 1f;
}

[System.Serializable]
public class BurstPropTranspose : BurstPropBase
{
    [Range(-3f, 3f)]
    [SerializeField]
    public float _Start = 0f;
    [Range(-3f, 3f)]
    [SerializeField]
    public float _End = 0f;
    [SerializeField]
    public bool _LockEndValue = true;
    [Range(-3f, 3f)]
    [SerializeField]
    public float _InteractionAmount = 0f;
    [Range(0.5f, 5.0f)]
    [SerializeField]
    public float _InteractionShape = 1f;
    [HideInInspector]
    public float _Min = -3f;
    [HideInInspector]
    public float _Max = 3f;
}

[System.Serializable]
public class BurstPropVolume : BurstPropBase
{
    [Range(0f, 1f)]
    [SerializeField]
    public float _Start = 0f;
    [Range(0f, 1f)]
    [SerializeField]
    public float _End = 0f;
    [SerializeField]
    public bool _LockEndValue = true;
    [Range(-2f, 2f)]
    [SerializeField]
    public float _InteractionAmount = 1f;
    [Range(0.5f, 5.0f)]
    [SerializeField]
    public float _InteractionShape = 1f;
    [HideInInspector]
    public float _Min = 0f;
    [HideInInspector]
    public float _Max = 2f;
}
