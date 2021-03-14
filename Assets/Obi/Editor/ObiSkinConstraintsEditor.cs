using UnityEditor;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Obi{
	
	/**
	 * Custom inspector for ObiSkinConstraints component. 
	 */
	
	[CustomEditor(typeof(ObiSkinConstraints)), CanEditMultipleObjects] 
	public class ObiSkinConstraintsEditor : Editor
	{
		
		ObiSkinConstraints constraints;
		
		public void OnEnable(){
			constraints = (ObiSkinConstraints)target;
		}
		
		public override void OnInspectorGUI() {
			
			serializedObject.UpdateIfRequiredOrScript();
			
			Editor.DrawPropertiesExcluding(serializedObject,"m_Script");
			
			// Apply changes to the serializedProperty
			if (GUI.changed){
				
				serializedObject.ApplyModifiedProperties();
				
				constraints.PushDataToSolver();
				
			}
			
		}

		public void DrawSkinRadius(){
			DrawSkinProperty(ObiClothEditor.ClothParticleProperty.SkinRadius);
		}

		public void DrawSkinBackstop(){
			DrawSkinProperty(ObiClothEditor.ClothParticleProperty.SkinBackstop);
		}

		public void DrawSkinBackstopRadius(){
			DrawSkinProperty(ObiClothEditor.ClothParticleProperty.SkinBackstopRadius);
		}

		private void DrawSkinProperty(int property){

			Material mat = Resources.Load<Material>("EditorLines");
		
			if (mat.SetPass(0) && constraints.GetFirstBatch() != null) {

				GL.PushMatrix();
				GL.Begin(GL.LINES);

				ObiSkinConstraintBatch batch = constraints.GetFirstBatch();

				Matrix4x4 invInitialScale = constraints.Actor.InitialScaleMatrix.inverse;

				Matrix4x4 s2wTransform = Matrix4x4.identity;
				if (constraints.InSolver)
					s2wTransform = constraints.Actor.Solver.transform.localToWorldMatrix;

				// get up to date constraint data:
				batch.PullDataFromSolver(constraints);
			
				foreach (int i in batch.ActiveConstraints){

					int particleIndex = batch.skinIndices[i];
					
					if (particleIndex >= 0 && particleIndex < ObiParticleActorEditor.selectionStatus.Length && 
						ObiParticleActorEditor.selectionStatus[particleIndex] &&
						ObiParticleActorEditor.IsParticleVisible(particleIndex)){

						float radius = batch.skinRadiiBackstop[i*3];
						float collisionRadius = batch.skinRadiiBackstop[i*3+1];
						float backstop = batch.skinRadiiBackstop[i*3+2];
						Vector3 point = batch.GetSkinPosition(i);
						Vector3 normal = batch.GetSkinNormal(i);

						if (!constraints.InSolver){

							point = (constraints.Actor.ActorLocalToWorldMatrix * invInitialScale).MultiplyPoint3x4(point);
							normal = (constraints.Actor.ActorLocalToWorldMatrix * invInitialScale).MultiplyVector(normal);

						}else if (constraints.Actor.Solver.simulateInLocalSpace){

							point = s2wTransform.MultiplyPoint3x4(point);
							normal = s2wTransform.MultiplyVector(normal);
	
						}

						switch(property){
				
							case ObiClothEditor.ClothParticleProperty.SkinRadius:
								GL.Color(Color.blue);
								GL.Vertex(point);
								GL.Color(Color.blue);
	        					GL.Vertex(point + normal * radius);
							break;

							case ObiClothEditor.ClothParticleProperty.SkinBackstop:
								GL.Color(Color.yellow);
								GL.Vertex(point);
								GL.Color(Color.yellow);
	        					GL.Vertex(point - normal * backstop);
							break;

							case ObiClothEditor.ClothParticleProperty.SkinBackstopRadius:
								GL.Color(Color.red);
								GL.Vertex(point - normal * backstop);
								GL.Color(Color.red);
        						GL.Vertex(point - normal*(collisionRadius + backstop));
							break;
						}
						
					}
				}

				GL.End();
				GL.PopMatrix();

			}
			
		}
		
	}
}

