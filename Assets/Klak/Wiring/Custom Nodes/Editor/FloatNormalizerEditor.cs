using UnityEngine;
using UnityEditor;

namespace Klak.Wiring
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(FloatNormalizer))]
    public class FloatFilterEditor : Editor
    {
        SerializedProperty _Min;
        SerializedProperty _Max;
        SerializedProperty _Interpolator;
        SerializedProperty _Clamp01;
        SerializedProperty _AutoRange;

        void OnEnable()
        {
            _Min = serializedObject.FindProperty("_Min");
            _Max = serializedObject.FindProperty("_Max");
            _Interpolator = serializedObject.FindProperty("_Interpolator");
            _AutoRange = serializedObject.FindProperty("_AutoRange");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_Min);
            EditorGUILayout.PropertyField(_Max);
            EditorGUILayout.PropertyField(_Interpolator);
            EditorGUILayout.PropertyField(_AutoRange);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
