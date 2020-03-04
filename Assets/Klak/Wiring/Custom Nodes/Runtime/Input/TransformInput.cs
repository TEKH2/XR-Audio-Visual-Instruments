using UnityEngine;
using Klak.Math;

namespace Klak.Wiring
{
    /// <summary>
    /// Tracks a transforms speed and velocity
    /// </summary>
    [AddComponentMenu("Klak/Wiring/Input/Transform Input")]
    public class TransformInput : NodeBase
    {
        #region Editable properties

        [SerializeField]
        public Transform _InputTransform;
        
        [SerializeField]
        FloatInterpolator.Config _SpeedInterpolator = null;
        FloatInterpolator _Speed;
        #endregion

        #region Node I/O

        [SerializeField, Outlet]
        FloatEvent _SpeedEvent = new FloatEvent();

        [SerializeField, Outlet]
        Vector3Event _VelocityEvent = new Vector3Event();

        #endregion
              
        #region Private vars

        Vector3 _PrevPos;
        Vector3 _Velocity;

        #endregion

        #region MonoBehaviour functions

        // Start is called before the first frame update
        void Start()
        {
            _Speed = new FloatInterpolator(0, _SpeedInterpolator);
        }

        // Update is called once per frame
        void Update()
        {
            _Velocity = (_InputTransform.position - _PrevPos) / Time.deltaTime;
            _Speed.targetValue = _Velocity.magnitude;
            _PrevPos = _InputTransform.position;
            
            _VelocityEvent.Invoke(_Velocity);
            _SpeedEvent.Invoke(_Speed.Step());
        }

        #endregion
    }
}
