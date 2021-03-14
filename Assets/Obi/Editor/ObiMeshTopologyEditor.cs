using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections;
using System.Collections.Generic;

namespace Obi{

	[CustomEditor(typeof(ObiMeshTopology))] 
	public class ObiMeshTopologyEditor : Editor
	{
		
		ObiMeshTopology halfEdge;
		PreviewHelpers previewHelper;
		Vector2 previewDir;
		Material previewMaterial;
		Material previewMaterialBorders;

		Mesh bordersMesh;
		bool drawBordersOnly = false;

		[MenuItem("Assets/Create/Obi/Obi Mesh Topology")]
		public static void CreateObiMesh ()
		{
			ObiEditorUtils.CreateAsset<ObiMeshTopology> ();
		}

		private void UpdatePreview(){

			CleanupPreview();

			drawBordersOnly = false;

			previewHelper = new PreviewHelpers();
			previewMaterial = Resources.Load<Material>("TopologyPreview");
			previewMaterialBorders = Resources.Load<Material>("TopologyPreviewBorder");

			bordersMesh = new Mesh();
			bordersMesh.hideFlags = HideFlags.HideAndDontSave;

			UpdateBordersMesh();

		}
		
		private void CleanupPreview(){
			GameObject.DestroyImmediate(bordersMesh);
		}
		
		public void OnEnable(){
			halfEdge = (ObiMeshTopology) target;
			UpdatePreview();
		}
		
		public void OnDisable(){
			EditorUtility.ClearProgressBar();
			previewHelper.Cleanup();
			CleanupPreview();
		}
		
		public override void OnInspectorGUI() {

			halfEdge.InputMesh = EditorGUILayout.ObjectField("Input mesh",halfEdge.InputMesh, typeof(Mesh), false) as Mesh;
			halfEdge.scale = EditorGUILayout.Vector3Field("Scale",halfEdge.scale);

			GUI.enabled = (halfEdge.InputMesh != null);
			if (GUILayout.Button("Generate")){
				// Start a coroutine job in the editor.
				//CoroutineJob job = new CoroutineJob();
				//halfEdge.generationRoutine = EditorCoroutine.StartCoroutine( job.Start( halfEdge.Generate()));
				halfEdge.Generate();
				EditorUtility.SetDirty(target);
				UpdatePreview();
			}
			GUI.enabled = true;
			
			// Show job progress:
			//EditorCoroutine.ShowCoroutineProgressBar("Analyzing mesh",halfEdge.generationRoutine);
			
			//If the generation routine has been completed, release it and update volumetric preview:
			/*if (halfEdge.generationRoutine != null && halfEdge.generationRoutine.IsDone){
				halfEdge.generationRoutine = null;
				EditorUtility.SetDirty(target);
				UpdatePreview();
			}*/
			
			// Print topology info:
			if (halfEdge.Initialized){
				EditorGUILayout.HelpBox("Vertices:"+ halfEdge.heVertices.Length + " "+
					                    "Edges:"+ halfEdge.heHalfEdges.Length/2 + " "+
										"Faces:"+halfEdge.heFaces.Length + "\n"+
										"Total Area:"+halfEdge.MeshArea + " " +
				                        "Total Volume:"+halfEdge.MeshVolume +"\n"+
				                        "Closed:"+halfEdge.IsClosed,MessageType.Info);
			}

			if (halfEdge.IsNonManifold){
				EditorGUILayout.HelpBox("Your mesh is non-manifold. Obi will still try to make things work, but consider fixing it.", MessageType.Warning);
			}
			
			GUI.enabled = (halfEdge.BorderEdgeCount > 0);
				drawBordersOnly = EditorGUILayout.Toggle("Draw borders only",drawBordersOnly);
			GUI.enabled = true;
			
			if (GUI.changed)
				serializedObject.ApplyModifiedProperties();
			
		}
		
		public override bool HasPreviewGUI(){
			return true;
		}
		
		public override bool RequiresConstantRepaint(){
			return false;	
		}
		
		public override void OnPreviewGUI(Rect region, GUIStyle background)
		{
			previewDir = PreviewHelpers.Drag2D(previewDir, region);
			
			if (Event.current.type != EventType.Repaint || halfEdge.InputMesh == null || !halfEdge.Initialized)
			{
				return;
			}
			
			Quaternion quaternion = Quaternion.Euler(this.previewDir.y, 0f, 0f) * Quaternion.Euler(0f, this.previewDir.x, 0f) * Quaternion.Euler(0, 120, -20f);
			
			previewHelper.BeginPreview(region, background);
			
			Bounds bounds = halfEdge.InputMesh.bounds;
			float magnitude = bounds.extents.magnitude;
			float num = 4f * magnitude;
			previewHelper.m_Camera.transform.position = -Vector3.forward * num;
			previewHelper.m_Camera.transform.rotation = Quaternion.identity;
			previewHelper.m_Camera.nearClipPlane = num - magnitude * 1.1f;
			previewHelper.m_Camera.farClipPlane = num + magnitude * 1.1f;
			
			// Compute matrix to rotate the mesh around the center of its bounds:
			Matrix4x4 matrix = Matrix4x4.TRS(Vector3.zero,quaternion,Vector3.one) * Matrix4x4.TRS(-bounds.center,Quaternion.identity,Vector3.one);
			
			if (!drawBordersOnly) Graphics.DrawMesh(halfEdge.InputMesh,matrix * Matrix4x4.Scale(halfEdge.scale), previewMaterial,1, previewHelper.m_Camera, 0);
			Graphics.DrawMesh(bordersMesh, matrix, previewMaterialBorders,1, previewHelper.m_Camera, 0);
			
			Texture texture = previewHelper.EndPreview();
			GUI.DrawTexture(region, texture, ScaleMode.StretchToFill, true);
			
		}

		private void UpdateBordersMesh(){
	
			if (bordersMesh == null || halfEdge.InputMesh == null || !halfEdge.Initialized) return;

			bordersMesh.Clear();

			Vector3[] vertices = new Vector3[halfEdge.BorderEdgeCount*2];
			int[] edgeIndices = new int[halfEdge.BorderEdgeCount*2];

			int borderIndex = 0;		
			for (int i = 0; i < halfEdge.heHalfEdges.Length; i++){
				if (halfEdge.heHalfEdges[i].face == -1){

					Oni.Vertex v1 = halfEdge.heVertices[halfEdge.heHalfEdges[i].endVertex];
					Oni.Vertex v2 = halfEdge.heVertices[halfEdge.GetHalfEdgeStartVertex(halfEdge.heHalfEdges[i])];

					vertices[borderIndex*2] = v1.position;
					vertices[borderIndex*2+1] = v2.position;

					edgeIndices[borderIndex*2] = borderIndex*2;
					edgeIndices[borderIndex*2+1] = borderIndex*2+1;

					borderIndex++;
				}
			}

			bordersMesh.vertices = vertices;
			bordersMesh.SetIndices(edgeIndices,UnityEngine.MeshTopology.Lines,0);

		}
	
	}
}

