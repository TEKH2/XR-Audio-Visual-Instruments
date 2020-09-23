using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Tekh2
{
    public class ScaledBehaviorCurve : MonoBehaviour
    {      
        [HideInInspector]
        public float _TimeNormAlongCurve;
        public float _InputWeightingBetweenValues = 1;
        public BehaviorModulator.EmitPropertyType _PropertyType = BehaviorModulator.EmitPropertyType.Volume;
        public ParticleSystem.MinMaxCurve _Curve;
        public Vector2 _OutputRange = new Vector2(0, 1);

        public float GetMinMaxValue()
        {
            float curveVal;
            switch (_Curve.mode)
            {
                case ParticleSystemCurveMode.Constant:
                    curveVal = _Curve.constant;
                    break;
                case ParticleSystemCurveMode.Curve:
                    curveVal = _Curve.curve.Evaluate(_TimeNormAlongCurve);
                    break;
                case ParticleSystemCurveMode.TwoConstants:
                    curveVal = Mathf.Lerp(_Curve.constantMin, _Curve.constantMax, _InputWeightingBetweenValues);
                    break;
                case ParticleSystemCurveMode.TwoCurves:
                    curveVal = Mathf.Lerp(_Curve.curveMin.Evaluate(_TimeNormAlongCurve), _Curve.curveMax.Evaluate(_TimeNormAlongCurve), _InputWeightingBetweenValues);
                    break;
                default:
                    curveVal = 0;
                    break;
            }

            return Mathf.Lerp(_OutputRange.x, _OutputRange.y, curveVal);
        }
    }
}