using UnityEngine;
using Klak.Math;

namespace Klak.Wiring
{
    [AddComponentMenu("Klak/Wiring/Filter/Float Normalizer")]
    public class FloatNormalizer : NodeBase
    {
        #region Editable properties

        [SerializeField]
        FloatInterpolator.Config _Interpolator = null;

        [SerializeField]
        public float _Min = 0;

        [SerializeField]
        public float _Max = 10;
        
        [SerializeField]
        public bool _AutoRange = false;
        bool _PrevAutoRange = false;

        #endregion

        #region Node I/O

        [Inlet]
        public float Input
        {
            set
            {
                if (!enabled) return;

                _InputValue = value;

                if(_AutoRange)
                {
                    _Min = Mathf.Min(_Min, _InputValue);
                    _Max = Mathf.Max(_Max, _InputValue);
                }

                if (_Interpolator.enabled)
                    _OutputValue.targetValue = EvalResponse();
                else
                    _OutputEvent.Invoke(EvalResponse());
            }
        }

        [SerializeField, Outlet]
        FloatEvent _OutputEvent = new FloatEvent();

        #endregion

        #region Private members

        float _InputValue;
        FloatInterpolator _OutputValue;

        float EvalResponse()
        {
            return Mathf.InverseLerp(_Min, _Max, _InputValue);
        }

        #endregion

        #region MonoBehaviour functions

        void Start()
        {
            _PrevAutoRange = _AutoRange;
            _OutputValue = new FloatInterpolator(EvalResponse(), _Interpolator);
        }

        void Update()
        {
            // TODO: Potentially halt output when auto ranging?
            if (_Interpolator.enabled)
                _OutputEvent.Invoke(_OutputValue.Step());

            // If just switched to auto ranging
            if(_AutoRange != _PrevAutoRange)
            {
                // if autorange on then set min and max
                if(_AutoRange)
                {
                    _Min = _InputValue;
                    _Max = _InputValue+.01f;
                }

                _PrevAutoRange = _AutoRange;
            }
        }

        #endregion
    }
}
