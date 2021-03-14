using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Obi{

/**
 * This class allows a mesh (skinned or not) to follow the simulation performed by a different cloth object. The most
 * common use case is having a high-poly mesh mimic the movement of a low-poly cloth simulation. 
 */
[ExecuteInEditMode]
[DisallowMultipleComponent]
[AddComponentMenu("Physics/Obi/Obi Cloth Proxy")]
public class ObiClothProxy : MonoBehaviour {

	public ObiTriangleSkinMap skinMap = null;
	public ObiMeshTopology targetTopology = null;	/**< needed if the target object is a skinned mesh renderer.*/

	[SerializeField][HideInInspector] private ObiClothBase source;

	[HideInInspector] public Mesh skinnedMesh;
	[HideInInspector] public Mesh sharedMesh;				/**< Original unmodified mesh.*/

	protected int[] meshTriangles;	
	protected Vector3[] meshVertices;
	protected Vector3[] meshNormals;
	protected Vector4[] meshTangents;

	protected IntPtr deformableMesh;
	protected GCHandle meshTrianglesHandle;
	protected GCHandle meshVerticesHandle;
	protected GCHandle meshNormalsHandle;
	protected GCHandle meshTangentsHandle;
	
	private MeshRenderer meshRenderer;
	private MeshFilter meshFilter;
	private SkinnedMeshRenderer skinnedMeshRenderer;
	protected float[] transformData = new float[16];

	public bool SelfSkinning{
		get{return source != null && source.gameObject == gameObject;}
	}

	public ObiClothBase Proxy{
		set{

			if (source != null){
				source.OnDeformableMeshSetup -= Source_OnAddedToSolver;
				source.OnDeformableMeshTeardown -= Source_OnRemovedFromSolver;
			}

				source = value;

			if (source != null){
				source.OnDeformableMeshSetup += Source_OnAddedToSolver;
				source.OnDeformableMeshTeardown += Source_OnRemovedFromSolver;
			}

		}
		get{return source;}
	}

	/*public void OnDrawGizmos(){

		Gizmos.color = Color.blue;
		Matrix4x4 normalMatrix = transform.localToWorldMatrix.inverse.transpose;
		if (meshNormals != null)
		for (int i = 0; i < meshNormals.Length; ++i){
			Gizmos.DrawRay(transform.localToWorldMatrix.MultiplyPoint3x4(meshVertices[i]),normalMatrix.MultiplyVector(meshNormals[i]).normalized*0.02f);
		}

		Gizmos.color = Color.red;
		if (meshTangents != null)
		for (int i = 0; i < meshTangents.Length; ++i){
			Gizmos.DrawRay(transform.localToWorldMatrix.MultiplyPoint3x4(meshVertices[i]),normalMatrix.MultiplyVector(meshTangents[i]).normalized*0.02f);
		}

   	}*/

	void OnEnable(){

		skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();

		if (!SelfSkinning){
			if (skinnedMeshRenderer == null)
				InitializeWithRegularMesh();
			else{
				if (targetTopology != null){
					InitializeWithSkinnedMesh();
				}else{
					Debug.LogError("You need to provide a mesh topology in order to use a SkinnedMeshRenderer as a proxy target.");
				}
			}
		}

		if (source != null){
			source.OnDeformableMeshSetup += Source_OnAddedToSolver;
			source.OnDeformableMeshTeardown += Source_OnRemovedFromSolver;
		}
	}

	void OnDisable(){

		if (!SelfSkinning){
			if (meshFilter != null)
				meshFilter.mesh = sharedMesh;
			if (skinnedMeshRenderer != null)
				skinnedMeshRenderer.sharedMesh = sharedMesh;
	
			GameObject.DestroyImmediate(skinnedMesh);
		}

		if (source != null){
			source.OnDeformableMeshSetup -= Source_OnAddedToSolver;
			source.OnDeformableMeshTeardown -= Source_OnRemovedFromSolver;
		}
	}

	private void UpdateTransformData()
	{
		if (source == null) return;

		// For a regular mesh, mesh data is expressed in its transform's local space:
		Matrix4x4 w2lTransform = transform.worldToLocalMatrix;

		// In case of self-skinning, we express mesh data in the actor's local space:
		if (SelfSkinning)
			w2lTransform = source.ActorWorldToLocalMatrix;

		// In case the target is a skinned mesh, mesh data is expressed in the root bone's local space:
		else if (skinnedMeshRenderer != null && skinnedMeshRenderer.rootBone != null)
			w2lTransform = skinnedMeshRenderer.rootBone.worldToLocalMatrix;

		Matrix4x4 s2lTransform;
		if (source.Solver.simulateInLocalSpace)
			s2lTransform = w2lTransform * source.Solver.transform.localToWorldMatrix;
		else 
			s2lTransform = w2lTransform;
		
		for (int i = 0; i < 16; ++i)
			transformData[i] = s2lTransform[i];
	}

	void Source_OnAddedToSolver (object sender, EventArgs e)
	{
		UpdateTransformData();

		// In case source and target are the same object:
		if (SelfSkinning){

			deformableMesh = source.DeformableMesh;

		// If source and target are different objects, create a new deformable mesh for the target:
		}else{

			deformableMesh = Oni.CreateDeformableMesh(source.Solver.OniSolver,
													  (targetTopology != null) ? targetTopology.HalfEdgeMesh : IntPtr.Zero,
													  IntPtr.Zero,
													  transformData,
													  IntPtr.Zero,
												      sharedMesh.vertexCount,
													  sharedMesh.vertexCount);
	
			GetMeshDataArrays(skinnedMesh);

			SetSkinnedMeshAnimationInfo();
		}

		Oni.SetDeformableMeshSkinMap(deformableMesh,source.DeformableMesh,skinMap.TriangleSkinMap);

		source.Solver.OnFrameEnd += Source_Solver_OnFrameEnd;
		source.Solver.OnStepEnd += Source_Solver_OnStepEnd;
	}

	void Source_OnRemovedFromSolver (object sender, EventArgs e)
	{
		source.Solver.OnFrameEnd -= Source_Solver_OnFrameEnd;
		source.Solver.OnStepEnd -= Source_Solver_OnStepEnd;

		if (!SelfSkinning){
			Oni.DestroyDeformableMesh(source.Solver.OniSolver,deformableMesh);
			Oni.UnpinMemory(meshVerticesHandle);
			Oni.UnpinMemory(meshNormalsHandle);
			Oni.UnpinMemory(meshTangentsHandle);
		}
	}

	void Source_Solver_OnFrameEnd (object sender, EventArgs e)
	{
		if (!SelfSkinning && skinnedMesh != null && skinnedMesh.isReadable){
			skinnedMesh.vertices = meshVertices;
			skinnedMesh.normals = meshNormals;
			skinnedMesh.tangents = meshTangents;
			skinnedMesh.RecalculateBounds();
		}
	}

	void Source_Solver_OnStepEnd (object sender, EventArgs e)
	{
		UpdateTransformData();

		if (deformableMesh != IntPtr.Zero)
			Oni.SetDeformableMeshTransform(deformableMesh,transformData);

		GrabSkeletonBones();
	}

	public virtual void GetMeshDataArrays(Mesh mesh){

		if (mesh != null && mesh.isReadable)
		{
			Oni.UnpinMemory(meshTrianglesHandle);
			Oni.UnpinMemory(meshVerticesHandle);
			Oni.UnpinMemory(meshNormalsHandle);
			Oni.UnpinMemory(meshTangentsHandle);

			meshTriangles = mesh.triangles;
			meshVertices = mesh.vertices;
			meshNormals = mesh.normals;
			meshTangents = mesh.tangents;

			meshTrianglesHandle = Oni.PinMemory(meshTriangles);
			meshVerticesHandle = Oni.PinMemory(meshVertices);
			meshNormalsHandle = Oni.PinMemory(meshNormals);
			meshTangentsHandle = Oni.PinMemory(meshTangents);

			Oni.SetDeformableMeshData(deformableMesh,meshTrianglesHandle.AddrOfPinnedObject(),
													 meshVerticesHandle.AddrOfPinnedObject(),
													 meshNormalsHandle.AddrOfPinnedObject(),
													 meshTangentsHandle.AddrOfPinnedObject(),
													 IntPtr.Zero,IntPtr.Zero,IntPtr.Zero,IntPtr.Zero,IntPtr.Zero);
		}
	}

	private void InitializeWithRegularMesh(){
		
		meshFilter = GetComponent<MeshFilter>();
		meshRenderer = GetComponent<MeshRenderer>();
		
		if (meshFilter == null || meshRenderer == null)
			return;
		
		// Store the shared mesh if it hasn't been stored previously.
		if (sharedMesh != null)
			meshFilter.mesh = sharedMesh;
		else
			sharedMesh = meshFilter.sharedMesh;
		
		// Make a deep copy of the original shared mesh.
		skinnedMesh = Mesh.Instantiate(meshFilter.sharedMesh) as Mesh;
		skinnedMesh.MarkDynamic(); 
		
		// Use the freshly created mesh copy as the renderer mesh and the half-edge input mesh, if it has been already analyzed.
		meshFilter.mesh = skinnedMesh;

	}

	protected void InitializeWithSkinnedMesh(){
	
		sharedMesh = targetTopology.InputMesh;
		
		// Make a deep copy of the original shared mesh.
		skinnedMesh = Mesh.Instantiate(sharedMesh) as Mesh;
		
		// remove bone weights so that the mesh is not affected by Unity's skinning:
		skinnedMesh.boneWeights = new BoneWeight[]{};
		skinnedMesh.MarkDynamic();

		if (Application.isPlaying)
			skinnedMeshRenderer.sharedMesh = skinnedMesh;
	}

	protected void SetSkinnedMeshAnimationInfo(){

		if (!SelfSkinning && skinnedMeshRenderer != null){

			Matrix4x4[] rendererBindPoses = sharedMesh.bindposes;
			BoneWeight[] rendererWeights = sharedMesh.boneWeights;

			float[] bindPoses = new float[16*rendererBindPoses.Length];
			
			for (int p = 0; p < rendererBindPoses.Length; ++p){
				for (int i = 0; i < 16; ++i)
					bindPoses[p*16+i] = rendererBindPoses[p][i];
			}

			Oni.BoneWeights[] weights = new Oni.BoneWeights[rendererWeights.Length];
			for (int i = 0; i < rendererWeights.Length; ++i)
				weights[i] = new Oni.BoneWeights(rendererWeights[i]);

			Oni.SetDeformableMeshAnimationData(deformableMesh,bindPoses,weights,rendererBindPoses.Length);
		}
	}

	public void GrabSkeletonBones(){

		if (!SelfSkinning && skinnedMeshRenderer != null && source.InSolver){

			Transform[] rendererBones = skinnedMeshRenderer.bones;
			float[] bones = new float[16*rendererBones.Length];
			
			for (int p = 0; p < sharedMesh.bindposes.Length; ++p){
	
				Matrix4x4 bone;

				if (source.Solver.simulateInLocalSpace)
					bone = source.Solver.transform.worldToLocalMatrix * rendererBones[p].localToWorldMatrix;
				else 
					bone = rendererBones[p].localToWorldMatrix;

				for (int i = 0; i < 16; ++i)
					bones[p*16+i] = bone[i];
			}

			Oni.SetDeformableMeshBoneTransforms(deformableMesh,bones);
		}

	}

	public void Update(){

		UpdateTransformData();

		if (deformableMesh != IntPtr.Zero)
			Oni.SetDeformableMeshTransform(deformableMesh,transformData);
	}
	
}
}
