using UnityEditor;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Obi{
	
	/**
	 * Custom inspector for ObiClothProxy component. 
	 */

	[CustomEditor(typeof(ObiClothProxy)), CanEditMultipleObjects] 
	public class ObiClothProxyEditor : Editor
	{
	
		ObiClothProxy proxy;
		
		public void OnEnable(){
			proxy = (ObiClothProxy)target;
		}
		
		public override void OnInspectorGUI() {
			
			serializedObject.UpdateIfRequiredOrScript();

			proxy.Proxy = EditorGUILayout.ObjectField("Particle Proxy",proxy.Proxy, typeof(ObiClothBase), true) as ObiClothBase;
			
			Editor.DrawPropertiesExcluding(serializedObject,"m_Script");
			
			// Apply changes to the serializedProperty
			if (GUI.changed){
				
				serializedObject.ApplyModifiedProperties();
				
			}
			
		}
		
	}

}

