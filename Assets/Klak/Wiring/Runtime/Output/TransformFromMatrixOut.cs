//
// Klak - Utilities for creative coding with Unity
//
// Copyright (C) 2016 Keijiro Takahashi
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
using UnityEngine;

namespace Klak.Wiring
{
    [AddComponentMenu("Klak/Wiring/Output/Component/Transform Out")]
    public class TransformFromMatrixOut : NodeBase
    {
        #region Editable properties

        [SerializeField]
        Transform _targetTransform = null;

        [SerializeField]
        bool _addToOriginal = true;

        #endregion

        #region Node I/O

        [Inlet]
        public Matrix4x4 matrix 
        {
            set 
            {
                Vector3 pos = ExtractTranslationFromMatrix(value);
                Quaternion rot = ExtractRotationFromMatrix(value);
                Vector3 scale = ExtractScaleFromMatrix(value);


                if (!enabled || _targetTransform == null) return;
               
                _targetTransform.localPosition =
                    _addToOriginal ? _originalPosition + pos : pos;

                _targetTransform.localRotation =
                   _addToOriginal ? _originalRotation * rot : rot;

                _targetTransform.localScale =
                   _addToOriginal ? _originalScale + scale : scale;
            }
        }
        #endregion

        #region Private members

        Vector3 _originalPosition;
        Quaternion _originalRotation;
        Vector3 _originalScale;

        void OnEnable()
        {
            if (_targetTransform != null)
            {
                _originalPosition = _targetTransform.localPosition;
                _originalRotation = _targetTransform.localRotation;
                _originalScale = _targetTransform.localScale;
            }
        }

        void OnDisable()
        {
            if (_targetTransform != null)
            {
                _targetTransform.localPosition = _originalPosition;
                _targetTransform.localRotation = _originalRotation;
                _targetTransform.localScale = _originalScale;
            }
        }

        #endregion

        // Helpers
        public Vector3 ExtractTranslationFromMatrix(Matrix4x4 matrix)
        {
            Vector3 translate;
            translate.x = matrix.m03;
            translate.y = matrix.m13;
            translate.z = matrix.m23;
            return translate;
        }


        /// <summary>
        /// Extract rotation quaternion from transform matrix.
        /// </summary>
        /// <param name="matrix">Transform matrix. This parameter is passed by reference
        /// to improve performance; no changes will be made to it.</param>
        /// <returns>
        /// Quaternion representation of rotation transform.
        /// </returns>
        public Quaternion ExtractRotationFromMatrix(Matrix4x4 matrix)
        {
            Vector3 forward;
            forward.x = matrix.m02;
            forward.y = matrix.m12;
            forward.z = matrix.m22;

            Vector3 upwards;
            upwards.x = matrix.m01;
            upwards.y = matrix.m11;
            upwards.z = matrix.m21;

            //if (forward.x == 0)
            //    return Quaternion.identity;
            // else
            return Quaternion.LookRotation(forward, upwards);
        }

        /// <summary>
        /// Extract scale from transform matrix.
        /// </summary>
        /// <param name="matrix">Transform matrix. This parameter is passed by reference
        /// to improve performance; no changes will be made to it.</param>
        /// <returns>
        /// Scale vector.
        /// </returns>
        public Vector3 ExtractScaleFromMatrix(Matrix4x4 matrix)
        {
            Vector3 scale;
            scale.x = new Vector4(matrix.m00, matrix.m10, matrix.m20, matrix.m30).magnitude;
            scale.y = new Vector4(matrix.m01, matrix.m11, matrix.m21, matrix.m31).magnitude;
            scale.z = new Vector4(matrix.m02, matrix.m12, matrix.m22, matrix.m32).magnitude;
            return scale;
        }
    }
}
