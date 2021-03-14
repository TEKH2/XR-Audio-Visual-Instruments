using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Obi
{

/**
 * Half-Edge data structure. Used to simplify and accelerate adjacency queries for
 * a triangular mesh. You can check out http://www.flipcode.com/archives/The_Half-Edge_Data_Structure.shtml
 * for more information on the half-edge mesh representation.
 *
 * This particular implementation does not use pointers, in order to benefit from Unity's serialization system.
 * Instead it uses arrays and indices, which makes some operations more cumbersome due to the need of updating
 * indices across the whole structure when removing faces, edges, or vertices.
 */

public class ObiMeshTopology : ScriptableObject
{
	
	public Mesh input = null;
    public Vector3 scale = Vector3.one;
	[HideInInspector] public bool initialized = false;

	//[NonSerialized] public EditorCoroutine generationRoutine = null;

	public class HEEdge{

		public int halfEdgeIndex;		  /**< Index to one of the half-edges in this edge. This is always the lower-index half-edge of the two.*/

		public HEEdge(int halfEdgeIndex){
			this.halfEdgeIndex = halfEdgeIndex;
		}

	}

	IntPtr halfEdgeMesh;										/** half-edge mesh representation used by Oni.*/
	
	[HideInInspector][SerializeField] protected int vertexCount = 0;  /**< actual welded vertex count, regardless of array capacity.*/

    [HideInInspector] public Oni.Face[] heFaces = null;				/**<faces list*/
    [HideInInspector] public Oni.HalfEdge[] heHalfEdges = null;		/**<half edges list*/
    [HideInInspector] public Oni.Vertex[] heVertices = null;		/**<vertices list*/
	[HideInInspector] public Quaternion[] heOrientations = null;	/**<per-vertex orientation list*/

	[HideInInspector] public Vector3[] normals = null;				/**< normal buffer of the input mesh.*/
	[HideInInspector] public Vector4[] tangents = null;				/**< tangent buffer of the input mesh.*/

	[HideInInspector] public int[] visualMap = null;			/**< maps visual (mesh) vertices to half-edge vertices.*/

	[HideInInspector][SerializeField] protected Oni.MeshInformation meshInfo;

	private GCHandle facesHandle;
	private GCHandle halfEdgesHandle;
	private GCHandle verticesHandle;
	private GCHandle orientationsHandle;
	private GCHandle visualMapHandle;
	private GCHandle normalsHandle;
	private GCHandle tangentsHandle;

	public bool Initialized{
		get{return initialized;}
	}

	public IntPtr HalfEdgeMesh{
		get {return halfEdgeMesh;}
	}

	public Mesh InputMesh{
		set{
			if (value != input){
				initialized = false;
				heFaces = new Oni.Face[0];
            	heVertices = new Oni.Vertex[0];
            	heHalfEdges = new Oni.HalfEdge[0];
				heOrientations = new Quaternion[0];
				visualMap = new int[0];
				input = value;
			}
		}
		get{return input;}
	}

	/**
	 * Returns volume for a closed mesh (readonly)
	 */
	public float MeshVolume{
		get{return meshInfo.volume;}
	}

	public float MeshArea{
		get{return meshInfo.area;}
	}

	public int VertexCount{
		get{return vertexCount;}
	}

	public int BorderEdgeCount{
		get{return meshInfo.borderEdgeCount;}
	}

	public bool IsClosed{
		get{return meshInfo.closed;}
	}

	public bool IsModified{
		get{return false;}
	} 

	public bool IsNonManifold{
		get{return meshInfo.nonManifold;}
	}

    public void OnEnable(){

		//TODO: when instantiating a mesh, do we really need to duplicate it in the solver also?
		// we could just keep a reference to the same one... checking if it's zero.
		//if (halfEdgeMesh == IntPtr.Zero)
		halfEdgeMesh = Oni.CreateHalfEdgeMesh();

        if (scale == Vector3.zero)
            scale = Vector3.one;

		// Set initial normals and tangents array:
		if (input != null){
			normals = input.normals;
			tangents = input.tangents;
		}

        // Check integrity after serialization, (re?) initialize if there's data missing.
		if (heFaces == null || heVertices == null || heHalfEdges == null ||
			heOrientations == null || visualMap == null){

			initialized = false;

            heFaces = new Oni.Face[0];
            heVertices = new Oni.Vertex[0];
            heHalfEdges = new Oni.HalfEdge[0];
			heOrientations = new Quaternion[0];
			visualMap = new int[0];

		}else{

			initialized = true;
	
			facesHandle = Oni.PinMemory(heFaces);
			verticesHandle = Oni.PinMemory(heVertices);
			halfEdgesHandle = Oni.PinMemory(heHalfEdges);
			orientationsHandle = Oni.PinMemory(heOrientations);
			normalsHandle = Oni.PinMemory(normals);
			tangentsHandle = Oni.PinMemory(tangents);
			visualMapHandle = Oni.PinMemory(visualMap);

			Oni.SetFaces(halfEdgeMesh,facesHandle.AddrOfPinnedObject(),heFaces.Length);
			Oni.SetVertices(halfEdgeMesh,verticesHandle.AddrOfPinnedObject(),heVertices.Length);
			Oni.SetHalfEdges(halfEdgeMesh,halfEdgesHandle.AddrOfPinnedObject(),heHalfEdges.Length);
			Oni.SetInverseOrientations(halfEdgeMesh,orientationsHandle.AddrOfPinnedObject());
			Oni.SetVisualMap(halfEdgeMesh,visualMapHandle.AddrOfPinnedObject());
			Oni.SetNormals(halfEdgeMesh,normalsHandle.AddrOfPinnedObject());
			Oni.SetTangents(halfEdgeMesh,tangentsHandle.AddrOfPinnedObject());

		}
    }

	public void CopyDataFrom(ObiMeshTopology source)
	{
		if (source != null && source.initialized && initialized){

			Oni.UnpinMemory(facesHandle);
			Oni.UnpinMemory(verticesHandle);
			Oni.UnpinMemory(halfEdgesHandle);
			Oni.UnpinMemory(orientationsHandle);
			Oni.UnpinMemory(normalsHandle);
			Oni.UnpinMemory(tangentsHandle);
			Oni.UnpinMemory(visualMapHandle);

			Array.Resize(ref heFaces, source.heFaces.Length);
			Array.Resize(ref heVertices, source.heVertices.Length);
			Array.Resize(ref heHalfEdges, source.heHalfEdges.Length);
			Array.Resize(ref heOrientations, source.heOrientations.Length);
			Array.Resize(ref normals, source.normals.Length);
			Array.Resize(ref tangents, source.tangents.Length);
			Array.Resize(ref visualMap, source.visualMap.Length);

			Array.Copy(source.heFaces,heFaces,source.heFaces.Length);
			Array.Copy(source.heVertices,heVertices,source.heVertices.Length);
			Array.Copy(source.heHalfEdges,heHalfEdges,source.heHalfEdges.Length);
			Array.Copy(source.heOrientations,heOrientations,source.heOrientations.Length);
			Array.Copy(source.normals,normals,source.normals.Length);
			Array.Copy(source.tangents,tangents,source.tangents.Length);
			Array.Copy(source.visualMap,visualMap,source.visualMap.Length);

			facesHandle = Oni.PinMemory(heFaces);
			verticesHandle = Oni.PinMemory(heVertices);
			halfEdgesHandle = Oni.PinMemory(heHalfEdges);
			orientationsHandle = Oni.PinMemory(heOrientations);
			normalsHandle = Oni.PinMemory(normals);
			tangentsHandle = Oni.PinMemory(tangents);
			visualMapHandle = Oni.PinMemory(visualMap);

			Oni.SetFaces(halfEdgeMesh,facesHandle.AddrOfPinnedObject(),heFaces.Length);
			Oni.SetVertices(halfEdgeMesh,verticesHandle.AddrOfPinnedObject(),heVertices.Length);
			Oni.SetHalfEdges(halfEdgeMesh,halfEdgesHandle.AddrOfPinnedObject(),heHalfEdges.Length);
			Oni.SetInverseOrientations(halfEdgeMesh,orientationsHandle.AddrOfPinnedObject());
			Oni.SetVisualMap(halfEdgeMesh,visualMapHandle.AddrOfPinnedObject());
			Oni.SetNormals(halfEdgeMesh,normalsHandle.AddrOfPinnedObject());
			Oni.SetTangents(halfEdgeMesh,tangentsHandle.AddrOfPinnedObject());

		}
	}	

	/**
     * Allocates extra memory space for new vertices. Used by tearable cloth.
	 */
	public void SetVertexCapacity(int maxVertices, int maxVisualVertices){

		Array.Resize(ref heVertices, maxVertices);
		Array.Resize(ref heOrientations, maxVertices);
		Array.Resize(ref visualMap,maxVisualVertices);
		Array.Resize(ref normals,maxVisualVertices);
		Array.Resize(ref tangents,maxVisualVertices);

		Oni.UnpinMemory(verticesHandle);
		Oni.UnpinMemory(orientationsHandle);
		Oni.UnpinMemory(visualMapHandle);
		Oni.UnpinMemory(normalsHandle);
		Oni.UnpinMemory(tangentsHandle);

		verticesHandle = Oni.PinMemory(heVertices);
		orientationsHandle = Oni.PinMemory(heOrientations);
		visualMapHandle = Oni.PinMemory(visualMap);
		normalsHandle = Oni.PinMemory(normals);
		tangentsHandle = Oni.PinMemory(tangents);

		Oni.SetVertices(halfEdgeMesh,verticesHandle.AddrOfPinnedObject(),vertexCount);
		Oni.SetInverseOrientations(halfEdgeMesh,orientationsHandle.AddrOfPinnedObject());
		Oni.SetVisualMap(halfEdgeMesh,visualMapHandle.AddrOfPinnedObject());
		Oni.SetNormals(halfEdgeMesh,normalsHandle.AddrOfPinnedObject());
		Oni.SetTangents(halfEdgeMesh,tangentsHandle.AddrOfPinnedObject());
	}

	public void OnDisable(){	
		Oni.DestroyHalfEdgeMesh(halfEdgeMesh);
	}

	public void UpdateVertexCount(){
		vertexCount = Oni.GetVertexCount(halfEdgeMesh);
	}

	public void Generate(){

		initialized = false;

		Oni.UnpinMemory(facesHandle);
		Oni.UnpinMemory(verticesHandle);
		Oni.UnpinMemory(halfEdgesHandle);
		Oni.UnpinMemory(orientationsHandle);
		Oni.UnpinMemory(visualMapHandle);

		// Calculate amount of memory that we need to allocate upfront:
		Oni.CalculatePrimitiveCounts(halfEdgeMesh,input.vertices,
								  				  input.triangles,
								  				  input.vertices.Length,
							      				  input.triangles.Length);

		vertexCount = Oni.GetVertexCount(halfEdgeMesh);

		// Allocate memory for half edge data:
		Array.Resize(ref heVertices, vertexCount);
		Array.Resize(ref heFaces, Oni.GetFaceCount(halfEdgeMesh));
		Array.Resize(ref heHalfEdges, Oni.GetHalfEdgeCount(halfEdgeMesh));
		Array.Resize(ref heOrientations, vertexCount);
		Array.Resize(ref visualMap, input.vertexCount);

		// Pin memory and hand it to Oni:
		facesHandle = Oni.PinMemory(heFaces);
		verticesHandle = Oni.PinMemory(heVertices);
		halfEdgesHandle = Oni.PinMemory(heHalfEdges);
		orientationsHandle = Oni.PinMemory(heOrientations);
		visualMapHandle = Oni.PinMemory(visualMap);

		Oni.SetFaces(halfEdgeMesh,facesHandle.AddrOfPinnedObject(),heFaces.Length);
		Oni.SetVertices(halfEdgeMesh,verticesHandle.AddrOfPinnedObject(),heVertices.Length);
		Oni.SetHalfEdges(halfEdgeMesh,halfEdgesHandle.AddrOfPinnedObject(),heHalfEdges.Length);
		Oni.SetInverseOrientations(halfEdgeMesh,orientationsHandle.AddrOfPinnedObject());
		Oni.SetVisualMap(halfEdgeMesh,visualMapHandle.AddrOfPinnedObject());

		// Generate half edge structure:
		Oni.Generate(halfEdgeMesh,input.vertices,
								  input.triangles,
								  input.vertices.Length,
							      input.triangles.Length,
								  ref scale);

		Oni.GetHalfEdgeMeshInfo(halfEdgeMesh,ref meshInfo);

		initialized = true;
	}

	public int GetHalfEdgeStartVertex(Oni.HalfEdge edge){

		// In a border edge, get the ending vertex of the pair edge:
		if (edge.face == -1)
			return  heHalfEdges[edge.pair].endVertex;

		// In case of an interior edge, find the vertex by going around the face:
		return heHalfEdges[heHalfEdges[edge.nextHalfEdge].nextHalfEdge].endVertex;
	}

	public float GetFaceArea(Oni.Face face){

		Oni.HalfEdge e1 = heHalfEdges[face.halfEdge];
		Oni.HalfEdge e2 = heHalfEdges[e1.nextHalfEdge];
		Oni.HalfEdge e3 = heHalfEdges[e2.nextHalfEdge];

		return Vector3.Cross(heVertices[e2.endVertex].position-heVertices[e1.endVertex].position,
					  		 heVertices[e3.endVertex].position-heVertices[e1.endVertex].position).magnitude / 2.0f;
	}

	public IEnumerable<Oni.Vertex> GetNeighbourVerticesEnumerator(Oni.Vertex vertex)
	{
		
		Oni.HalfEdge startEdge = heHalfEdges[vertex.halfEdge];
		Oni.HalfEdge edge = startEdge;
		
		do{
			yield return heVertices[edge.endVertex];
			edge = heHalfEdges[edge.pair];
			edge = heHalfEdges[edge.nextHalfEdge];
			
		} while (edge.index != startEdge.index);
		
	}

	public IEnumerable<Oni.HalfEdge> GetNeighbourEdgesEnumerator(Oni.Vertex vertex)
	{
		
		Oni.HalfEdge startEdge = heHalfEdges[vertex.halfEdge];
		Oni.HalfEdge edge = startEdge;
		
		do{
			edge = heHalfEdges[edge.pair];
			yield return edge;
			edge = heHalfEdges[edge.nextHalfEdge];
			yield return edge;
			
		} while (edge.index != startEdge.index);
		
	}

	public IEnumerable<Oni.Face> GetNeighbourFacesEnumerator(Oni.Vertex vertex)
	{

		Oni.HalfEdge startEdge = heHalfEdges[vertex.halfEdge];
		Oni.HalfEdge edge = startEdge;

		do{

			edge = heHalfEdges[edge.pair];
			if (edge.face > -1)
				yield return heFaces[edge.face];
			edge = heHalfEdges[edge.nextHalfEdge];

		} while (edge.index != startEdge.index);

	}

	public int[] GetFaceEdges(Oni.Face face){

		Oni.HalfEdge e1 = heHalfEdges[face.halfEdge];
		Oni.HalfEdge e2 = heHalfEdges[e1.nextHalfEdge];
		Oni.HalfEdge e3 = heHalfEdges[e2.nextHalfEdge];

		return new int[]{
			e1.index,e2.index,e3.index
		};
	}

	/**
	 * Calculates and returns a list of all edges (note: not half-edges, but regular edges) in the mesh.
	 * This is O(2N) in both time and space, with N = number of edges.
	 */
	public List<HEEdge> GetEdgeList(){

		List<HEEdge> edges = new List<HEEdge>();
		bool[] listed = new bool[heHalfEdges.Length];

		for (int i = 0; i < heHalfEdges.Length; i++)
		{
			if (!listed[heHalfEdges[i].pair])
			{
				edges.Add(new HEEdge(i));
				listed[heHalfEdges[i].pair] = true;
				listed[i] = true;
			}
		}

		return edges;
	}

	/**
	 * Returns true if the edge has been split in a vertex split operation. (as a result of tearing)
	 */
	public bool IsSplit(int halfEdgeIndex){

		Oni.HalfEdge edge = heHalfEdges[halfEdgeIndex];

		if (edge.pair < 0 || edge.face < 0) return false;

		Oni.HalfEdge pair = heHalfEdges[edge.pair];

		return edge.endVertex != heHalfEdges[heHalfEdges[pair.nextHalfEdge].nextHalfEdge].endVertex ||
			   pair.endVertex != heHalfEdges[heHalfEdges[edge.nextHalfEdge].nextHalfEdge].endVertex;
	 
	}

}
}


