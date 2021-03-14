using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using System.IO;
using System.Collections;
using System.Collections.Generic;

namespace Obi{

	[CustomEditor(typeof(ObiTriangleSkinMap))] 
	public class ObiTriangleSkinMapEditor : Editor
	{

		[MenuItem("Assets/Create/Obi/Obi Triangle Skin Map")]
		public static void CreateObiTriangleSkinMap ()
		{
			ObiEditorUtils.CreateAsset<ObiTriangleSkinMap> ();
		}
		
		public enum PaintBrushType{
			Add,
			Remove
		}

		ObiTriangleSkinMap skinMap;

		SceneSetup[] oldSetup;
		Object oldSelection;

		GameObject sourceObject;
		GameObject targetObject;

		Material paintMaterial;
		Material standardMaterial;

		// skin channel painting stuff:
		static bool editMode = false;
		static bool paintMode = false;
		static PaintBrushType brushType = PaintBrushType.Add;
		static int selectedSkinChannel = 0;
		static int targetSkinChannel = 0;
		static float brushRadius = 50;
		static string[] availableChannels;

		public void OnEnable(){
			skinMap = (ObiTriangleSkinMap) target;

			availableChannels = new string[32];
			for (int i = 0; i < 32; ++i)
				availableChannels[i] = i.ToString();
		}

		public void OnDisable(){
			ExitSkinEditMode();
		}

		public void GetMaterials(){
			if (paintMaterial == null)
				paintMaterial = Resources.Load<Material>("PropertyGradientMaterial");
			if (standardMaterial == null)
				standardMaterial = new Material(Shader.Find("Standard"));
		}
		
		public override void OnInspectorGUI() {

			GetMaterials();

			if (!editMode){
				UpdateNormalMode();
			}else{
				UpdateEditMode();
			}
			
			if (GUI.changed)
				serializedObject.ApplyModifiedProperties();
			
		}

		void EnterSkinEditMode()
		{
			if (!editMode)
			{
				SceneView.onSceneGUIDelegate += this.OnSceneGUI;

				oldSelection = Selection.activeObject;
				if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
				{
					ActiveEditorTracker.sharedTracker.isLocked = true;
	
					oldSetup = EditorSceneManager.GetSceneManagerSetup();
		     		EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects);
		
					if (skinMap.SourceTopology != null && skinMap.SourceTopology.input != null){
						sourceObject = new GameObject("Source mesh",typeof(MeshRenderer),typeof(MeshFilter));
						sourceObject.GetComponent<MeshRenderer>().material = standardMaterial;
						sourceObject.GetComponent<MeshFilter>().sharedMesh = GameObject.Instantiate(skinMap.SourceTopology.input);
						skinMap.sourceSkinTransform.Apply(sourceObject.transform);
						Selection.activeObject = sourceObject;
					}
	
					if (skinMap.TargetMesh != null){
						targetObject = new GameObject("Target mesh",typeof(MeshRenderer),typeof(MeshFilter));
						targetObject.GetComponent<MeshRenderer>().material = standardMaterial;
						targetObject.GetComponent<MeshFilter>().sharedMesh = GameObject.Instantiate(skinMap.TargetMesh);
						skinMap.targetSkinTransform.Apply(targetObject.transform);
						Selection.activeObject = targetObject;
					}
	
					SceneView.FrameLastActiveSceneView();

					editMode = true;
				}
			}
		}

		void ExitSkinEditMode()
		{
			if (editMode){

				editMode = false;

				ActiveEditorTracker.sharedTracker.isLocked = false;

				if (SceneManager.GetActiveScene().path.Length <= 0)
        		{
					if (this.oldSetup != null && this.oldSetup.Length > 0)
					{
						EditorSceneManager.RestoreSceneManagerSetup(this.oldSetup);
						this.oldSetup = null;
					}
					else
					{
						EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects);
					}
				}
	
				Selection.activeObject = oldSelection;
				SceneView.onSceneGUIDelegate -= this.OnSceneGUI;
		
			}
		}

		void UpdateNormalMode(){

			EditorGUI.BeginChangeCheck();
			ObiMeshTopology sourceTopology = EditorGUILayout.ObjectField("Source topology",skinMap.SourceTopology, typeof(ObiMeshTopology), false) as ObiMeshTopology;
			if (EditorGUI.EndChangeCheck()){
				Undo.RecordObject(skinMap, "Set skin source");
				skinMap.SourceTopology = sourceTopology;
			}

			EditorGUI.BeginChangeCheck();
			Mesh target = EditorGUILayout.ObjectField("Target mesh",skinMap.TargetMesh, typeof(Mesh), false) as Mesh;
			if (EditorGUI.EndChangeCheck()){
				Undo.RecordObject(skinMap, "Set skin target");
				skinMap.TargetMesh = target;
			}

			// Print skin info:
			if (skinMap.bound){
				EditorGUILayout.HelpBox("Skin info generated." + skinMap.baryPositions.Length,MessageType.Info);
			}
			
			// Error / warning messages
			bool errors = false;
			if (skinMap.SourceTopology == null){
					EditorGUILayout.HelpBox("Please provide a source topology.",MessageType.Info);
					errors = true;
			}else{
				if (!skinMap.SourceTopology.Initialized || skinMap.SourceTopology.input == null){
					EditorGUILayout.HelpBox("The provided source topology has no input mesh, or hasn't been initialized.",MessageType.Error);
					errors = true;
				}
			}	
			if (skinMap.TargetMesh == null){
				EditorGUILayout.HelpBox("Please provide a target mesh.",MessageType.Info);
				errors = true;
			}
	
			// Edit mode buttons:
			GUI.enabled = !errors; 
			if (GUILayout.Button("Edit skin map")){
				EditorApplication.delayCall += EnterSkinEditMode;
			}
			GUI.enabled = true;
		}

		void UpdateEditMode(){

			paintMode = GUILayout.Toggle(paintMode,new GUIContent("Paint skin master/slave",Resources.Load<Texture2D>("PaintButton")),"LargeButton");

			// skin channel selector:
			if (paintMode){
				GUILayout.BeginHorizontal();

				if (GUILayout.Toggle(brushType == PaintBrushType.Add,new GUIContent("Add",Resources.Load<Texture2D>("AddIcon")),GUI.skin.FindStyle("ButtonLeft")))
					brushType = PaintBrushType.Add;

				if (GUILayout.Toggle(brushType == PaintBrushType.Remove,new GUIContent("Remove",Resources.Load<Texture2D>("RemoveIcon")),GUI.skin.FindStyle("ButtonMid")))
					brushType = PaintBrushType.Remove;

				if (GUILayout.Button(new GUIContent("Fill",Resources.Load<Texture2D>("FillButton")),GUI.skin.FindStyle("ButtonMid"))){
					if (sourceObject != null && Selection.activeGameObject == sourceObject){
						for (int i = 0; i < skinMap.masterFlags.Length; ++i)
							skinMap.masterFlags[i] |= (uint)(1 << selectedSkinChannel);
					}
					if (targetObject != null && Selection.activeGameObject == targetObject){
						for (int i = 0; i < skinMap.slaveFlags.Length; ++i)
							skinMap.slaveFlags[i] |= (uint)(1 << selectedSkinChannel);
					}
				}

				if (GUILayout.Button(new GUIContent("Clear",Resources.Load<Texture2D>("ClearButton2")),GUI.skin.FindStyle("ButtonRight"))){
					if (sourceObject != null && Selection.activeGameObject == sourceObject){
						for (int i = 0; i < skinMap.masterFlags.Length; ++i)
							skinMap.masterFlags[i] &= ~(uint)(1 << selectedSkinChannel);
					}
					if (targetObject != null && Selection.activeGameObject == targetObject){
						for (int i = 0; i < skinMap.slaveFlags.Length; ++i)
							skinMap.slaveFlags[i] &= ~(uint)(1 << selectedSkinChannel);
					}
				}

				GUILayout.EndHorizontal();

				brushRadius = EditorGUILayout.Slider("Brush radius",brushRadius,5,200);
				selectedSkinChannel = EditorGUILayout.Popup("Skin channel:",selectedSkinChannel,availableChannels);

				GUILayout.BeginHorizontal();
				if (GUILayout.Button("Copy channel to:")){
					CopySkinChannels(selectedSkinChannel,targetSkinChannel);
				}
				targetSkinChannel = EditorGUILayout.Popup(targetSkinChannel,availableChannels);
				GUILayout.EndHorizontal();
			
			}

			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Bind")){
				if (sourceObject != null && targetObject != null)
					skinMap.Bind(sourceObject.transform,targetObject.transform);
				EditorUtility.SetDirty(target);
			}

			if (GUILayout.Button("Done")){
				EditorApplication.delayCall += ExitSkinEditMode;
			}
			GUILayout.EndHorizontal();

			DrawTransformInspector("Source transform",sourceObject);
			DrawTransformInspector("Target transform",targetObject);

			// Change materials when painting weights:
			if (Event.current.type == EventType.Repaint){
				UpdateSourceObject();
				UpdateTargetObject();
			}

		}

		private void DrawTransformInspector(string text, GameObject obj){
			if (obj != null){
				EditorGUILayout.Separator();
				EditorGUILayout.BeginHorizontal();
					EditorGUILayout.LabelField(text,EditorStyles.boldLabel);
					if (GUILayout.Button("Reset",EditorStyles.miniButton)){
						obj.transform.position = Vector3.zero;
						obj.transform.rotation = Quaternion.identity;
						obj.transform.localScale = Vector3.one;
					}
				EditorGUILayout.EndHorizontal();
				obj.transform.position = EditorGUILayout.Vector3Field("Position",obj.transform.position);
				obj.transform.rotation = Quaternion.Euler(EditorGUILayout.Vector3Field("Rotation",obj.transform.rotation.eulerAngles));
				obj.transform.localScale = EditorGUILayout.Vector3Field("Scale",obj.transform.localScale);
			}
		}

		private void CopySkinChannels(int source, int dest){

			int shift = source - dest;
			uint destMask = (uint)(1 << dest);

			if (sourceObject != null && Selection.activeGameObject == sourceObject){
				for (int i = 0; i < skinMap.masterFlags.Length; ++i){
					// move bit from source to destination:
					uint copy = (shift > 0) ? (uint) (skinMap.masterFlags[i] >> shift) : (uint) (skinMap.masterFlags[i] << Mathf.Abs(shift));

					// clear destination bit and or with displaced source bit:
					skinMap.masterFlags[i] = (skinMap.masterFlags[i] & ~destMask) | (copy & destMask);
				}

			}

			if (targetObject != null && Selection.activeGameObject == targetObject){

				for (int i = 0; i < skinMap.slaveFlags.Length; ++i){
					// move bit from source to destination:
					uint copy = (shift > 0) ? (uint) (skinMap.slaveFlags[i] >> shift) : (uint) (skinMap.slaveFlags[i] << Mathf.Abs(shift));

					// clear destination bit and or with displaced source bit:
					skinMap.slaveFlags[i] = (skinMap.slaveFlags[i] & ~destMask) | (copy & destMask);
				}
			}
		}

		// OnSceneGUI doesnt seem to be called for ScriptableObjects, so we need to tap onto the (undocumented) SceneView class.
		public void OnSceneGUI(SceneView sceneView){

			if (!paintMode || sceneView.camera == null)
				return;

			Vector3[] wsPositions = null;
			bool[] facingCamera = null; 

			// get positions and camera facing flags for source object, if it is selected:
			if (sourceObject != null && Selection.activeGameObject == sourceObject){

				wsPositions = new Vector3[skinMap.masterFlags.Length];
				facingCamera = new bool[skinMap.masterFlags.Length];

				Vector3[] vertices = skinMap.SourceTopology.InputMesh.vertices;
				Vector3[] normals = skinMap.SourceTopology.InputMesh.normals;

				for (int i = 0; i < skinMap.masterFlags.Length; ++i){
					wsPositions[i] = sourceObject.transform.TransformPoint(vertices[i]);

					Vector3 meshNormal = normals[i];
					Vector3 camToParticle = sceneView.camera.transform.position - wsPositions[i];
					facingCamera[i] = (Vector3.Dot(sourceObject.transform.TransformVector(meshNormal),camToParticle) > 0);
				}

			}

			// get positions and camera facing flags for target object, if it is selected:
			if (targetObject != null && Selection.activeGameObject == targetObject){

				wsPositions = new Vector3[skinMap.slaveFlags.Length];
				facingCamera = new bool[skinMap.slaveFlags.Length];

				Vector3[] vertices = skinMap.TargetMesh.vertices;
				Vector3[] normals = skinMap.TargetMesh.normals;

				for (int i = 0; i < skinMap.slaveFlags.Length; ++i){
					wsPositions[i] = targetObject.transform.TransformPoint(vertices[i]);

					Vector3 camToParticle = sceneView.camera.transform.position - wsPositions[i];
					facingCamera[i] = (Vector3.Dot(sourceObject.transform.TransformVector(normals[i]),camToParticle) > 0);
				}
			}

			if (wsPositions == null)
				return;

			// Update paintbrush:
			ObiClothParticleHandles.ParticleBrush(wsPositions,ObiParticleActorEditor.ParticleCulling.Back,facingCamera,brushRadius,
												 	()=>{
														// As RecordObject diffs with the end of the current frame,
														// and this is a multi-frame operation, we need to use RegisterCompleteObjectUndo instead.
														Undo.RegisterCompleteObjectUndo(skinMap, "Paint skin channels");
												  	},
		                                          	PaintbrushStampCallback,
												  	()=>{
														EditorUtility.SetDirty(skinMap);
												  	},
		                                          	Resources.Load<Texture2D>("BrushHandle"));
		}

		private void PaintbrushStampCallback(List<ParticleStampInfo> stampInfo, bool modified){
			
			if (sourceObject != null && Selection.activeGameObject == sourceObject){
				foreach(ParticleStampInfo info in stampInfo){
					if (brushType == PaintBrushType.Remove)	
						skinMap.masterFlags[info.index] &= ~(uint)(1 << selectedSkinChannel);
					else
						skinMap.masterFlags[info.index] |= (uint)(1 << selectedSkinChannel);
				}
			}

			if (targetObject != null && Selection.activeGameObject == targetObject){
				foreach(ParticleStampInfo info in stampInfo){
					if (brushType == PaintBrushType.Remove)
						skinMap.slaveFlags[info.index] &= ~(uint)(1 << selectedSkinChannel);
					else
						skinMap.slaveFlags[info.index] |= (uint)(1 << selectedSkinChannel);
				}
			}
			
		}

		void UpdateSourceObject(){

			if (sourceObject == null)
				return;

			sourceObject.GetComponent<MeshRenderer>().material = standardMaterial;

			if (Selection.activeGameObject != sourceObject)
				return;

			Selection.objects = new Object[]{sourceObject};

			if (paintMode){

				sourceObject.GetComponent<MeshRenderer>().material = paintMaterial;
				if (targetObject!= null)
					targetObject.GetComponent<MeshRenderer>().material = standardMaterial;

				Mesh mesh = sourceObject.GetComponent<MeshFilter>().sharedMesh;
				Color[] colors = new Color[mesh.vertexCount];

				for(int i = 0; i < mesh.vertexCount; i++){
					if ((skinMap.masterFlags[i] & (1 << selectedSkinChannel) ) != 0)
						colors[i] = Color.white;
					else 
						colors[i] = Color.black;
				}

				mesh.colors = colors;
			}

		}

		void UpdateTargetObject(){

			if (targetObject == null)
				return;

			targetObject.GetComponent<MeshRenderer>().material = standardMaterial;

			if (Selection.activeGameObject != targetObject)
				return;

			Selection.objects = new Object[]{targetObject};

			if (paintMode){

				targetObject.GetComponent<MeshRenderer>().material = paintMaterial;
				if (sourceObject!= null)
					sourceObject.GetComponent<MeshRenderer>().material = standardMaterial;

				Mesh mesh = targetObject.GetComponent<MeshFilter>().sharedMesh;
				Color[] colors = new Color[mesh.vertexCount];

				for(int i = 0; i < mesh.vertexCount; i++){
					if ((skinMap.slaveFlags[i] & (1 << selectedSkinChannel) ) != 0)
						colors[i] = Color.white;
					else 
						colors[i] = Color.black;
				}

				mesh.colors = colors;

			}
				
		}
	
	}
}

