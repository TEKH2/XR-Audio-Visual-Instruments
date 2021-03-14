using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using System;
using System.Collections;
using System.Collections.Generic;


namespace Obi{
	
	/**
 * Custom inspector for ObiCloth components.
 * Allows particle selection and constraint edition. 
 * 
 * Selection:
 * 
 * - To select a particle, left-click on it. 
 * - You can select multiple particles by holding shift while clicking.
 * - To deselect all particles, click anywhere on the object except a particle.
 * 
 * Constraints:
 * 
 * - To edit particle constraints, select the particles you wish to edit.
 * - Constraints affecting any of the selected particles will appear in the inspector.
 * - To add a new pin constraint to the selected particle(s), click on "Add Pin Constraint".
 * 
 */
	[CustomEditor(typeof(ObiTearableCloth)), CanEditMultipleObjects] 
	public class ObiTearableClothEditor : ObiParticleActorEditor
	{

		public class TearableClothParticleProperty : ParticleProperty
		{
		  public const int TearResistance = 3;

		  public TearableClothParticleProperty (int value) : base (value){}
		}

		[MenuItem("GameObject/3D Object/Obi/Obi Tearable Cloth",false,2)]
		static void CreateObiCloth()
		{
			GameObject c = new GameObject("Obi Tearable Cloth");
			Undo.RegisterCreatedObjectUndo(c,"Create Obi Tearable Cloth");
			c.AddComponent<MeshFilter>();
			c.AddComponent<MeshRenderer>();
			c.AddComponent<ObiTearableCloth>();
		}

		[MenuItem("GameObject/3D Object/Obi/Obi Tearable Cloth (with solver)",false,3)]
		static void CreateObiClothWithSolver()
		{
			GameObject c = new GameObject("Obi Tearable Cloth");
			Undo.RegisterCreatedObjectUndo(c,"Create Obi Tearable Cloth");
			c.AddComponent<MeshFilter>();
			c.AddComponent<MeshRenderer>();
			ObiTearableCloth cloth = c.AddComponent<ObiTearableCloth>();
			ObiSolver solver = c.AddComponent<ObiSolver>();
			cloth.Solver = solver;
		}
		
		ObiTearableCloth cloth;
		
		public override void OnEnable(){

			base.OnEnable();
			cloth = (ObiTearableCloth)target;

			particlePropertyNames.AddRange(new string[]{"Tear Resistance"});

		}
		
		public override void OnDisable(){
			base.OnDisable();
			EditorUtility.ClearProgressBar();
		}

		public override void UpdateParticleEditorInformation(){
			
			for(int i = 0; i < cloth.positions.Length; i++)
			{
				wsPositions[i] = cloth.GetParticlePosition(i);	
				wsOrientations[i] = cloth.GetParticleOrientation(i);		
			}

			if (cloth.clothMesh != null && Camera.current != null){
			
				for(int i = 0; i < cloth.clothMesh.vertexCount; i++){

					int particle = cloth.topology.visualMap[i];
					Vector3 camToParticle = Camera.current.transform.position - wsPositions[particle];

					sqrDistanceToCamera[particle] = camToParticle.sqrMagnitude;
					facingCamera[particle] = (Vector3.Dot(cloth.transform.TransformVector(cloth.MeshNormals[i]),camToParticle) > 0);
		
				}
			}
			
		}

		protected override void DrawActorInfo(){

			if (cloth.clothMesh == null)
				return;

			Material mat = Resources.Load<Material>("PropertyGradientMaterial");
			
			if (mat.SetPass(0)) {

				Mesh gradientMesh = GameObject.Instantiate(cloth.clothMesh);

				Color[] colors = new Color[gradientMesh.vertexCount];

				for(int i = 0; i < gradientMesh.vertexCount; i++){

					// get particle index for this vertex:
					int particle = cloth.topology.visualMap[i];

					// calculate vertex color:
					if (selectionMask && !selectionStatus[particle]){
						colors[i] = Color.black;
					}else{
						colors[i] = GetPropertyValueGradient(GetPropertyValue(currentProperty,particle));
					}

				}

				gradientMesh.colors = colors;

				Graphics.DrawMeshNow(gradientMesh,cloth.ActorLocalToWorldMatrix);
				
			}

			DrawParticleRadii();

			if (!paintBrush){
				DrawParticles();
			}
		
		}
		
		protected override void SetPropertyValue(ParticleProperty property,int index, float value){
			if (index >= 0 && index < cloth.invMasses.Length){
				switch(property){
				case TearableClothParticleProperty.Mass: 
						cloth.invMasses[index] = 1.0f / (Mathf.Max(value,0.00001f) * cloth.areaContribution[index]);
					break; 
				case TearableClothParticleProperty.Radius:
						cloth.principalRadii[index] = Vector3.one * value;
					break;
				case ParticleProperty.Layer:
						cloth.phases[index] = Oni.MakePhase((int)value,cloth.SelfCollisions?Oni.ParticlePhase.SelfCollide:0);
					break;
				case TearableClothParticleProperty.TearResistance:
					if (cloth is ObiTearableCloth)
						cloth.tearResistance[index] = value;
					break;
				}
			}
		}
		
		protected override float GetPropertyValue(ParticleProperty property, int index){
			if (index >= 0 && index < cloth.invMasses.Length){
				switch(property){
				case TearableClothParticleProperty.Mass:
					return 1.0f / (cloth.invMasses[index] * cloth.areaContribution[index]);
				case TearableClothParticleProperty.Radius:
					return cloth.principalRadii[index][0];
				case ParticleProperty.Layer:
					return Oni.GetGroupFromPhase(cloth.phases[index]);
				case TearableClothParticleProperty.TearResistance:
					return cloth.tearResistance[index];
				}
			}
			return 0;
		}

		public override void OnInspectorGUI() {
			
			serializedObject.UpdateIfRequiredOrScript();

			GUI.enabled = cloth.Initialized;
			EditorGUI.BeginChangeCheck();
			editMode = GUILayout.Toggle(editMode,new GUIContent("Edit particles",Resources.Load<Texture2D>("EditParticles")),"LargeButton");
			if (EditorGUI.EndChangeCheck()){
				SceneView.RepaintAll();
			}		
			GUI.enabled = true;	

			EditorGUILayout.LabelField("Status: "+ (cloth.Initialized ? "Initialized":"Not initialized"));

			GUI.enabled = (cloth.SharedTopology != null);
			if (GUILayout.Button("Initialize")){
				if (!cloth.Initialized){
					EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
					CoroutineJob job = new CoroutineJob();
					routine = job.Start(cloth.GeneratePhysicRepresentationForMesh());
					EditorCoroutine.ShowCoroutineProgressBar("Generating physical representation...",ref routine);
					EditorGUIUtility.ExitGUI();
				}else{
					if (EditorUtility.DisplayDialog("Actor initialization","Are you sure you want to re-initialize this actor?","Ok","Cancel")){
						EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
						CoroutineJob job = new CoroutineJob();
						routine = job.Start(cloth.GeneratePhysicRepresentationForMesh());
						EditorCoroutine.ShowCoroutineProgressBar("Generating physical representation...",ref routine);
						EditorGUIUtility.ExitGUI();
					}
				}
			}
			GUI.enabled = true;

			if (cloth.SharedTopology == null){
				EditorGUILayout.HelpBox("No ObiMeshTopology asset present.",MessageType.Info);
			}

			GUI.enabled = cloth.Initialized;
			if (GUILayout.Button("Set Rest State")){
				Undo.RecordObject(cloth, "Set rest state");
				cloth.PullDataFromSolver(ParticleData.POSITIONS | ParticleData.VELOCITIES);
			}
			GUI.enabled = true;

			Editor.DrawPropertiesExcluding(serializedObject,"m_Script");
	
			// Apply changes to the serializedProperty
			if (GUI.changed){
				serializedObject.ApplyModifiedProperties();
			}
			
		}
		
	}
}


