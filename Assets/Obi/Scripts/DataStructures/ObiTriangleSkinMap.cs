using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Obi
{

public class ObiTriangleSkinMap : ScriptableObject
{
	[Serializable]
	public struct SkinTransform{
		public Vector3 position;
	 	public Quaternion rotation;
    	public Vector3 scale;

		public SkinTransform(Vector3 position,Quaternion rotation,Vector3 scale){
			this.position = position;
			this.rotation = rotation;
			this.scale = scale;
		} 

		public SkinTransform(Transform transform){
			position = transform.position;
			rotation = transform.rotation;
			scale = transform.localScale;
		} 

		public void Apply(Transform transform){
			transform.position = position;
			transform.rotation = rotation;
			transform.localScale = scale;
		} 
	}

	[HideInInspector] public bool bound = false;

	IntPtr triangleSkinMap;												  /**< half-edge mesh representation used by Oni.*/

	[HideInInspector] public uint[] masterFlags = null;					  /**< master channel flags for each source topology vertex.*/
	[HideInInspector] public uint[] slaveFlags = null;					  /**< source channel flags for each target mesh vertex.*/	

	[HideInInspector] public int[] skinnedVertices = null;				  /**< indices of skinned vertices in the target mesh.*/
	[HideInInspector] public int[] skinnedTriangles = null;				  /**< indices of triangles in the source mesh. */
	[HideInInspector] public Vector3[] baryPositions = null;			  /**< for each skinned vertex, barycentric position with respect to its corresponding source triangle.*/
	[HideInInspector] public Vector3[] baryNormals = null;				  /**< for each skinned vertex, barycentric position of its normal with respect to its corresponding source triangle.*/
	[HideInInspector] public Vector3[] baryTangents = null;				  /**< for each skinned vertex, barycentric position of its tangent with respect to its corresponding source triangle.*/

	[SerializeField][HideInInspector] private ObiMeshTopology sourceTopology = null;
	[SerializeField][HideInInspector] private Mesh targetMesh = null;

	[HideInInspector] public SkinTransform sourceSkinTransform = new SkinTransform(Vector3.zero,Quaternion.identity,Vector3.one);
	[HideInInspector] public SkinTransform targetSkinTransform = new SkinTransform(Vector3.zero,Quaternion.identity,Vector3.one);

	public ObiMeshTopology SourceTopology{
		set{
			if (value != sourceTopology){
				sourceTopology = value;
				sourceSkinTransform = new SkinTransform(Vector3.zero,Quaternion.identity,Vector3.one);
				ValidateMasterFlags(true);
			}
		}
		get{return sourceTopology;}
	}

	public Mesh TargetMesh{
		set{
			if (value != targetMesh){
				targetMesh = value;
				targetSkinTransform = new SkinTransform(Vector3.zero,Quaternion.identity,Vector3.one);
				ValidateSlaveFlags(true);
			}
		}
		get{return targetMesh;}
	}

	public IntPtr TriangleSkinMap{
		get {return triangleSkinMap;}
	}

	private void ValidateMasterFlags(bool resetValues){
		if (sourceTopology != null)
		{
			if (masterFlags == null || masterFlags.Length != sourceTopology.InputMesh.vertexCount){

				Array.Resize(ref masterFlags,sourceTopology.InputMesh.vertexCount);
				
				if (resetValues){
					for (int i = 0; i< masterFlags.Length; ++i) 
						masterFlags[i] = 0x00000001;
				}
			}
		}else{
			masterFlags = null;
		}
	}

	private void ValidateSlaveFlags(bool resetValues){
		if (targetMesh != null)
		{
			if (slaveFlags == null || slaveFlags.Length != targetMesh.vertexCount){

				Array.Resize(ref slaveFlags,targetMesh.vertexCount);

				if (resetValues){
					for (int i = 0; i< slaveFlags.Length; ++i) 
						slaveFlags[i] = 0x00000001;
				}
			}		
		}else{
			slaveFlags = null;
		}
	}

	public void OnEnable(){

		ValidateMasterFlags(false);
		ValidateSlaveFlags(false);

		triangleSkinMap = Oni.CreateTriangleSkinMap();

        // Check integrity after serialization, (re?) initialize if there's data missing.
		if (skinnedVertices == null || skinnedTriangles == null || baryPositions == null ||
			baryNormals == null || baryTangents == null){

			bound = false;

            skinnedVertices = new int[0];
            skinnedTriangles = new int[0];
            baryPositions = new Vector3[0];
			baryNormals = new Vector3[0];
			baryTangents = new Vector3[0];

		}else{

			bound = true;

			Oni.SetSkinInfo(triangleSkinMap,
							skinnedVertices,
						    skinnedTriangles,
							baryPositions,
							baryNormals,
							baryTangents,
							skinnedVertices.Length);

		}
    }

	public void Bind(Transform sourceTransform, 
					 Transform targetTransform)
	{
		bound = false;

		if (sourceTopology == null || sourceTopology.InputMesh == null || 
			targetMesh == null || sourceTransform == null || targetTransform == null) 
			return;

		this.sourceSkinTransform = new SkinTransform(sourceTransform);
		this.targetSkinTransform = new SkinTransform(targetTransform);

		IntPtr solver = Oni.CreateSolver(0);

		// get source mesh:
		float[] transformData = new float[16];
		for (int i = 0; i < 16; ++i)
			transformData[i] = sourceTransform.worldToLocalMatrix[i];
		IntPtr sourceDefMesh = Oni.CreateDeformableMesh(solver,
													    sourceTopology.HalfEdgeMesh,
														IntPtr.Zero,
														transformData,
														IntPtr.Zero,
														sourceTopology.InputMesh.vertexCount,
													    sourceTopology.InputMesh.vertexCount);

		GCHandle sourceTrianglesHandle = Oni.PinMemory(sourceTopology.InputMesh.triangles);
		GCHandle sourceVerticesHandle = Oni.PinMemory(sourceTopology.InputMesh.vertices);
		GCHandle sourceNormalsHandle = Oni.PinMemory(sourceTopology.InputMesh.normals);
		GCHandle sourceTangentsHandle = Oni.PinMemory(sourceTopology.InputMesh.tangents);

		Oni.SetDeformableMeshData(sourceDefMesh,sourceTrianglesHandle.AddrOfPinnedObject(),
											 	sourceVerticesHandle.AddrOfPinnedObject(),
											 	sourceNormalsHandle.AddrOfPinnedObject(),
											 	sourceTangentsHandle.AddrOfPinnedObject(),
											 	IntPtr.Zero,IntPtr.Zero,IntPtr.Zero,IntPtr.Zero,IntPtr.Zero);

		// get target mesh transform data:
		for (int i = 0; i < 16; ++i)
			transformData[i] = targetTransform.worldToLocalMatrix[i];
		IntPtr targetDefMesh = Oni.CreateDeformableMesh(solver,
												     IntPtr.Zero,
													 IntPtr.Zero,
												     transformData,
													 IntPtr.Zero,
												     targetMesh.vertexCount,
												     targetMesh.vertexCount);

		GCHandle meshTrianglesHandle = Oni.PinMemory(targetMesh.triangles);
		GCHandle meshVerticesHandle = Oni.PinMemory(targetMesh.vertices);
		GCHandle meshNormalsHandle = Oni.PinMemory(targetMesh.normals);
		GCHandle meshTangentsHandle = Oni.PinMemory(targetMesh.tangents);

		Oni.SetDeformableMeshData(targetDefMesh,meshTrianglesHandle.AddrOfPinnedObject(),
											    meshVerticesHandle.AddrOfPinnedObject(),
											 	meshNormalsHandle.AddrOfPinnedObject(),
											 	meshTangentsHandle.AddrOfPinnedObject(),
											 	IntPtr.Zero,IntPtr.Zero,IntPtr.Zero,IntPtr.Zero,IntPtr.Zero);	

		// Perform the binding process:
		Oni.Bind(triangleSkinMap,sourceDefMesh,targetDefMesh,masterFlags,slaveFlags);

		// Cleanup data:
		Oni.UnpinMemory(sourceTrianglesHandle);
		Oni.UnpinMemory(sourceVerticesHandle);
		Oni.UnpinMemory(sourceNormalsHandle);
		Oni.UnpinMemory(sourceTangentsHandle);

		Oni.UnpinMemory(meshTrianglesHandle);
		Oni.UnpinMemory(meshVerticesHandle);
		Oni.UnpinMemory(meshNormalsHandle);
		Oni.UnpinMemory(meshTangentsHandle);

		Oni.DestroyDeformableMesh(solver,sourceDefMesh);
		Oni.DestroyDeformableMesh(solver,targetDefMesh);

		Oni.DestroySolver(solver);

		// Get skinned vertex count:
		int skinCount =	Oni.GetSkinnedVertexCount(triangleSkinMap);
	
		// Create arrays of the appropiate size to store skinning data:
		skinnedVertices = new int[skinCount];
        skinnedTriangles = new int[skinCount];
        baryPositions = new Vector3[skinCount];
		baryNormals = new Vector3[skinCount];
		baryTangents = new Vector3[skinCount];

		// Retrieve skinning data:
		Oni.GetSkinInfo(triangleSkinMap,
						skinnedVertices,
						skinnedTriangles,
						baryPositions,
						baryNormals,
						baryTangents);

		bound = true;

	}
	
}
}


