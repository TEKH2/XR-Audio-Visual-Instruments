using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class EventTypes
{
    public class FloatEvent : UnityEvent<float> { }
    public class IntEvent : UnityEvent<int> { }
    public class BoolEvent : UnityEvent<float> { }
    public class Vector3Event : UnityEvent<Vector3> { }
    public class QuaternionEvent : UnityEvent<Quaternion> { }
    public class ColorEvent : UnityEvent<Color> { }
}
