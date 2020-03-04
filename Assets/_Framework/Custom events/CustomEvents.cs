using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace EXP
{
    [System.Serializable]
    public class BoolEvent : UnityEvent<bool> { }

    [System.Serializable]
    public class FloatEvent : UnityEvent<float> { }

    [System.Serializable]
    public class Vector2Event : UnityEvent<Vector2> { }

    [System.Serializable]
    public class Vector3Event : UnityEvent<Vector3> { }

    [System.Serializable]
    public class QuaternionEvent : UnityEvent<Quaternion> { }

    [System.Serializable]
    public class ColourEvent : UnityEvent<Color> { }
}
