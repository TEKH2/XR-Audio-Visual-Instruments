using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Obi
{
/**
 * ObiTearableCloth is a dynamically tearable version of regular cloth. It doesn't support every type of constraint used by regular cloth,
 * yields slightly slower performance, and cannot be used with skinned meshes.
 */
[ExecuteInEditMode]
[AddComponentMenu("Physics/Obi/Obi Tearable Cloth")]
public class ObiTearableCloth : ObiClothBase
{
	[Tooltip("Amount of memory preallocated to create extra particles and mesh data when tearing the cloth. 0 means no extra memory will be allocated, and the cloth will not be tearable. 1 means all cloth triangles will be fully tearable.")]
	[Range(0,1)]
	public float tearCapacity = 0.5f;
	
	[Tooltip("Maximum strain betweeen particles before the spring constraint holding them together would break.")]
	[Delayed]
	public float tearResistanceMultiplier = 1000;					/**< Factor that controls how much a structural cloth spring can stretch before breaking.*/

	[Tooltip("Maximum amount of edges torn per frame. Low values will result in cleaner cuts and improve performance, high values will result in frayed edges and lower performance.")]
	[Delayed]
	public int tearRate = 1;

	[Tooltip("Percentage of debilitation suffered by the cloth around existing tears. Higher values cause already teared regions to become less tear resistant.")]
	[Range(0,1)]
	public float tearDebilitation = 0.5f;
	
	[HideInInspector] public float[] tearResistance; 	/**< Per-particle tear resistances.*/

	[HideInInspector][SerializeField] private int maxVertexValency = 0; /**< maximum valence (amount of half-edges sharing  vertex) in the mesh.*/

	[HideInInspector][SerializeField] private int pooledVertices = 0;
	[HideInInspector][SerializeField] private int pooledParticles = 0;
	[HideInInspector][SerializeField] private int usedParticles = 0; 

	[HideInInspector][SerializeField] private int[] distanceConstraintMap;		/** constraintHalfEdgeMap[half-edge index] = distance constraint index, or -1 if there's no constraint. 
																					Each initial constraint is the lower-index of each pair of half-edges. When a half-edge is split during
																					tearing, one of the two half-edges gets its constraint updated and the other gets a new constraint.*/
	[HideInInspector][SerializeField] private int[] bendConstraintOffsets;	   		

	public class ObiConstraintTornEventArgs : System.EventArgs{
		public int constraintIndex;	/**< index of the constraint torn.*/
		public int particleIndex;	/**< index of the particle being torn*/
		public ObiConstraintTornEventArgs(int constraint, int particle){
			this.constraintIndex = constraint;
			this.particleIndex = particle;
		}
	}
	public event System.EventHandler<ObiConstraintTornEventArgs> OnConstraintTorn;	/**< Called when a constraint is torn.*/

	protected Color[] meshColors = null;
	protected Vector2[] meshUV1 = null;
	protected Vector2[] meshUV2 = null;
	protected Vector2[] meshUV3 = null;
	protected Vector2[] meshUV4 = null;

	protected GCHandle meshColorsHandle;
	protected GCHandle meshUV1Handle;
	protected GCHandle meshUV2Handle;
	protected GCHandle meshUV3Handle;
	protected GCHandle meshUV4Handle;

	public int UsedParticles{
		get{return usedParticles;}
	}

	struct TornEdge{
		public int index;
		public float force;
		public TornEdge(int index, float force){
			this.index = index;
			this.force = force;
		}
	}

	public override void OnSolverStepEnd(float deltaTime){	

		base.OnSolverStepEnd(deltaTime);

		if (isActiveAndEnabled)
			ApplyTearing();

	}

	public override void OnEnable(){

		base.OnEnable();

		// Initialize cloth:
		InitializeWithRegularMesh();

	}

	public override bool AddToSolver(object info){

		// Note: we use the new keyword to hide ObiClothBase's implementation with our own, and be able to call ObiActor's AddToSolver from here.

		if (Initialized && base.AddToSolver(info)){
				
			particleIndicesHandle = Oni.PinMemory(particleIndices); 

			for (int i = 0; i < 16; ++i)
				transformData[i] = transform.worldToLocalMatrix[i];

			deformableMesh = Oni.CreateDeformableMesh(Solver.OniSolver,
													  topology.HalfEdgeMesh,
													  IntPtr.Zero,
													  transformData,
													  particleIndicesHandle.AddrOfPinnedObject(),
												      sharedMesh.vertexCount + pooledVertices,
													  sharedMesh.vertexCount);

			// allocate extra memory for the topology:
			topology.SetVertexCapacity(usedParticles + pooledParticles,
								 	   sharedMesh.vertexCount + pooledVertices);

			Oni.SetDeformableMeshTBNUpdate(deformableMesh,normalsUpdate,updateTangents);

			GetMeshDataArrays(clothMesh);

			CallOnDeformableMeshSetup();

			return true;
		}
		return false;
    }

	public override bool RemoveFromSolver(object info){

		bool removed = false;

		try{

			if (solver != null && InSolver)
				Oni.DestroyDeformableMesh(Solver.OniSolver,deformableMesh);

			Oni.UnpinMemory(particleIndicesHandle);
			Oni.UnpinMemory(meshTrianglesHandle);
			Oni.UnpinMemory(meshVerticesHandle);
			Oni.UnpinMemory(meshNormalsHandle);
			Oni.UnpinMemory(meshTangentsHandle);
			Oni.UnpinMemory(meshColorsHandle);
			Oni.UnpinMemory(meshUV1Handle);
			Oni.UnpinMemory(meshUV2Handle);
			Oni.UnpinMemory(meshUV3Handle);
			Oni.UnpinMemory(meshUV4Handle);

			CallOnDeformableMeshTearDown();

		}catch(Exception e){
			Debug.LogException(e);
		}finally{
			removed = base.RemoveFromSolver(info);
		}
		return removed;
	}

	public override void GetMeshDataArrays(Mesh mesh){

		if (mesh != null)
		{
			Oni.UnpinMemory(meshTrianglesHandle);
			Oni.UnpinMemory(meshVerticesHandle);
			Oni.UnpinMemory(meshNormalsHandle);
			Oni.UnpinMemory(meshTangentsHandle);
			Oni.UnpinMemory(meshColorsHandle);
			Oni.UnpinMemory(meshUV1Handle);
			Oni.UnpinMemory(meshUV2Handle);
			Oni.UnpinMemory(meshUV3Handle);
			Oni.UnpinMemory(meshUV4Handle);

			if (mesh.isReadable){

				meshTriangles = mesh.triangles;
				meshVertices = mesh.vertices;
				meshNormals = mesh.normals;
				meshTangents = mesh.tangents;
				meshColors = mesh.colors;
				meshUV1 = mesh.uv;
				meshUV2 = mesh.uv2;
				meshUV3 = mesh.uv3;
				meshUV4 = mesh.uv4;
	
				int totalVertexCount = mesh.vertexCount + pooledVertices;
	
				Array.Resize(ref meshVertices,totalVertexCount);

				meshTrianglesHandle = Oni.PinMemory(meshTriangles);
				meshVerticesHandle = Oni.PinMemory(meshVertices);
				
				if (meshNormals.Length == mesh.vertexCount){
					Array.Resize(ref meshNormals,totalVertexCount);
					meshNormalsHandle = Oni.PinMemory(meshNormals);
				}
				if (meshTangents.Length == mesh.vertexCount){
					Array.Resize(ref meshTangents,totalVertexCount);
					meshTangentsHandle = Oni.PinMemory(meshTangents);
				}
				if (meshColors.Length == mesh.vertexCount){
					 Array.Resize(ref meshColors,totalVertexCount);
					 meshColorsHandle = Oni.PinMemory(meshColors);
				}
				if (meshUV1.Length == mesh.vertexCount){ 
					Array.Resize(ref meshUV1,totalVertexCount);
					meshUV1Handle = Oni.PinMemory(meshUV1);
				}	
				if (meshUV2.Length == mesh.vertexCount){
					Array.Resize(ref meshUV2,totalVertexCount);
					meshUV2Handle = Oni.PinMemory(meshUV2);
				}
				if (meshUV3.Length == mesh.vertexCount){
					Array.Resize(ref meshUV3,totalVertexCount);
					meshUV3Handle = Oni.PinMemory(meshUV3);
				}
				if (meshUV4.Length == mesh.vertexCount){
					Array.Resize(ref meshUV4,totalVertexCount);
					meshUV4Handle = Oni.PinMemory(meshUV4);
				}
	
				Oni.SetDeformableMeshData(deformableMesh,meshTrianglesHandle.AddrOfPinnedObject(),
														 meshVerticesHandle.AddrOfPinnedObject(),
														 meshNormalsHandle.IsAllocated ? meshNormalsHandle.AddrOfPinnedObject() : IntPtr.Zero,
														 meshTangentsHandle.IsAllocated ? meshTangentsHandle.AddrOfPinnedObject() : IntPtr.Zero,
														 meshColorsHandle.IsAllocated ? meshColorsHandle.AddrOfPinnedObject() : IntPtr.Zero,
														 meshUV1Handle.IsAllocated ? meshUV1Handle.AddrOfPinnedObject() : IntPtr.Zero,
														 meshUV2Handle.IsAllocated ? meshUV2Handle.AddrOfPinnedObject() : IntPtr.Zero,
														 meshUV3Handle.IsAllocated ? meshUV3Handle.AddrOfPinnedObject() : IntPtr.Zero,
														 meshUV4Handle.IsAllocated ? meshUV4Handle.AddrOfPinnedObject() : IntPtr.Zero);

			}
			else{
				deformableMesh = IntPtr.Zero;
			}
		}

	}

	/**
 	 * Applies changes in physics model to the cloth mesh.
 	 */
	public override void CommitResultsToMesh()
	{
		if (!Initialized || particleIndices == null || clothMesh == null) return;
	
		clothMesh.vertices = meshVertices;
		clothMesh.normals = meshNormals;
		clothMesh.tangents = meshTangents;
		clothMesh.triangles = meshTriangles;
		clothMesh.colors = meshColors;
		clothMesh.uv = meshUV1;
		clothMesh.uv2 = meshUV2;
		clothMesh.uv3 = meshUV3;
		clothMesh.uv4 = meshUV4;

		clothMesh.RecalculateBounds();
		
	}
 
	/**
	 * Generates the particle based physical representation of the cloth mesh. This is the initialization method for the cloth object
	 * and should not be called directly once the object has been created.
	 */
	protected override IEnumerator Initialize()
	{		
		initialized = false;
		initializing = false;
		
		if (sharedTopology == null){
			Debug.LogError("No ObiMeshTopology provided. Cannot initialize physical representation.");
			yield break;
		}else if (!sharedTopology.Initialized){
			Debug.LogError("The provided ObiMeshTopology contains no data. Cannot initialize physical representation.");
            yield break;
		}
		
		initializing = true;

		RemoveFromSolver(null);

		GameObject.DestroyImmediate(topology);
		topology = GameObject.Instantiate(sharedTopology);

		maxVertexValency = 0;

		pooledParticles = (int)((topology.heFaces.Length * 3 - topology.heVertices.Length) * tearCapacity);
		usedParticles = topology.heVertices.Length;

		int totalParticles = usedParticles + pooledParticles;

		active = new bool[totalParticles];
		positions = new Vector3[totalParticles];
		restPositions = new Vector4[totalParticles];
		velocities = new Vector3[totalParticles];
		invMasses  = new float[totalParticles];
		principalRadii = new Vector3[totalParticles];
		phases = new int[totalParticles];
		areaContribution = new float[totalParticles]; 
		tearResistance = new float[totalParticles];
		deformableTriangles = new int[topology.heFaces.Length*3]; 

		initialScaleMatrix.SetTRS(Vector3.zero,Quaternion.identity,transform.lossyScale);

		// Create a particle for each vertex, and gather per-vertex data (area, valency)
		for (int i = 0; i < topology.heVertices.Length; i++){
			
			Oni.Vertex vertex = topology.heVertices[i];

			// Get the particle's area contribution.
			areaContribution[i] = 0;
			foreach (Oni.Face face in topology.GetNeighbourFacesEnumerator(vertex)){
				areaContribution[i] += topology.GetFaceArea(face)/3;
            }

			// Calculate particle's valency:
			int valency = 0;
			foreach (Oni.HalfEdge edge in topology.GetNeighbourEdgesEnumerator(vertex)){
				valency++;
            }
			maxVertexValency = Mathf.Max(maxVertexValency,valency);
			
			// Get the shortest neighbour edge, particle radius will be half of its length.
			float minEdgeLength = Single.MaxValue;
			foreach (Oni.HalfEdge edge in topology.GetNeighbourEdgesEnumerator(vertex)){

				// vertices at each end of the edge:
				Vector3 v1 = initialScaleMatrix*topology.heVertices[topology.GetHalfEdgeStartVertex(edge)].position;
				Vector3 v2 = initialScaleMatrix*topology.heVertices[edge.endVertex].position;

				minEdgeLength = Mathf.Min(minEdgeLength,Vector3.Distance(v1,v2));

			}

			active[i] = true;
			tearResistance[i] = 1;
			invMasses[i] = (areaContribution[i] > 0) ? (1.0f / (DEFAULT_PARTICLE_MASS * areaContribution[i])) : 0;
			positions[i] = initialScaleMatrix * vertex.position;
			restPositions[i] = positions[i];
			restPositions[i][3] = 1; // activate rest position.
			principalRadii[i] = Vector3.one * minEdgeLength * 0.5f;
			phases[i] = Oni.MakePhase(1,selfCollisions?Oni.ParticlePhase.SelfCollide:0);
			
			if (i % 500 == 0)
				yield return new CoroutineJob.ProgressInfo("ObiCloth: generating particles...",i/(float)topology.heVertices.Length);
		}

		// Initialize basic data for pooled particles:
		for (int i = topology.heVertices.Length; i < pooledParticles; i++){

			active[i] = false;
			tearResistance[i] = 1;
			invMasses[i] = 1.0f/0.05f;
			principalRadii[i] = Vector3.one * 0.1f;
			phases[i] = Oni.MakePhase(1,selfCollisions?Oni.ParticlePhase.SelfCollide:0);

			if (i % 100 == 0)
				yield return new CoroutineJob.ProgressInfo("ObiRope: generating pooled particles...",i/(float)pooledParticles);

		}
		
		// Generate deformable triangles:
		for (int i = 0; i < topology.heFaces.Length; i++){

			Oni.Face face = topology.heFaces[i];
			
			Oni.HalfEdge e1 = topology.heHalfEdges[face.halfEdge];
			Oni.HalfEdge e2 = topology.heHalfEdges[e1.nextHalfEdge];
			Oni.HalfEdge e3 = topology.heHalfEdges[e2.nextHalfEdge];

			deformableTriangles[i*3] = e1.endVertex;
			deformableTriangles[i*3+1] = e2.endVertex;
			deformableTriangles[i*3+2] = e3.endVertex;

			if (i % 500 == 0)
				yield return new CoroutineJob.ProgressInfo("ObiCloth: generating deformable geometry...",i/(float)topology.heFaces.Length);
		}

		List<ObiMeshTopology.HEEdge> edges = topology.GetEdgeList();

		DistanceConstraints.Clear();
		ObiDistanceConstraintBatch distanceBatch = new ObiDistanceConstraintBatch(false,false);
		DistanceConstraints.AddBatch(distanceBatch);

		// Initialize constraint-halfedge map for cloth tearing purposes: TODO: reset on awake!!!
		distanceConstraintMap = new int[topology.heHalfEdges.Length];
		for (int i = 0; i < distanceConstraintMap.Length; i++) distanceConstraintMap[i] = -1;

		// Create distance springs: 
		for (int i = 0; i < edges.Count; i++){
		
			distanceConstraintMap[edges[i].halfEdgeIndex] = i;
			Oni.HalfEdge hedge = topology.heHalfEdges[edges[i].halfEdgeIndex];
			Oni.Vertex startVertex = topology.heVertices[topology.GetHalfEdgeStartVertex(hedge)];
			Oni.Vertex endVertex = topology.heVertices[hedge.endVertex];
			
			distanceBatch.AddConstraint(topology.GetHalfEdgeStartVertex(hedge),hedge.endVertex,Vector3.Distance(initialScaleMatrix*startVertex.position,initialScaleMatrix*endVertex.position),1,1);		

			if (i % 500 == 0)
				yield return new CoroutineJob.ProgressInfo("ObiCloth: generating structural constraints...",i/(float)topology.heHalfEdges.Length);
		}

		// Create aerodynamic constraints:
		AerodynamicConstraints.Clear();
		ObiAerodynamicConstraintBatch aeroBatch = new ObiAerodynamicConstraintBatch(false,false);
		AerodynamicConstraints.AddBatch(aeroBatch);

		for (int i = 0; i < topology.heVertices.Length; i++){

			aeroBatch.AddConstraint(i,
									areaContribution[i],
			                        AerodynamicConstraints.dragCoefficient,
			                        AerodynamicConstraints.liftCoefficient);

			if (i % 500 == 0)
				yield return new CoroutineJob.ProgressInfo("ObiCloth: generating aerodynamic constraints...",i/(float)topology.heFaces.Length);
		}
		
		BendingConstraints.Clear();
		ObiBendConstraintBatch bendBatch = new ObiBendConstraintBatch(false,false);
		BendingConstraints.AddBatch(bendBatch);

		bendConstraintOffsets = new int[topology.heVertices.Length+1];

		Dictionary<int,int> cons = new Dictionary<int, int>();
		for (int i = 0; i < topology.heVertices.Length; i++){
	
			Oni.Vertex vertex = topology.heVertices[i];

			bendConstraintOffsets[i] = bendBatch.ConstraintCount;
	
			foreach (Oni.Vertex n1 in topology.GetNeighbourVerticesEnumerator(vertex)){
	
				float cosBest = 0;
				Oni.Vertex vBest = n1;
	
				foreach (Oni.Vertex n2 in topology.GetNeighbourVerticesEnumerator(vertex)){
					float cos = Vector3.Dot((n1.position-vertex.position).normalized,
					                        (n2.position-vertex.position).normalized);
					if (cos < cosBest){
						cosBest = cos;
						vBest = n2;
					}
				}
				
				if (!cons.ContainsKey(vBest.index) || cons[vBest.index] != n1.index){
				
					cons[n1.index] = vBest.index;

					Vector3 n1Pos = initialScaleMatrix * n1.position;
					Vector3 bestPos = initialScaleMatrix * vBest.position;
					Vector3 vertexPos = initialScaleMatrix * vertex.position;
				
					float[] bendRestPositions = new float[]{n1Pos[0],n1Pos[1],n1Pos[2],
														    bestPos[0],bestPos[1],bestPos[2],
														    vertexPos[0],vertexPos[1],vertexPos[2]};

					float restBend = Oni.BendingConstraintRest(bendRestPositions);
					bendBatch.AddConstraint(n1.index,vBest.index,vertex.index,restBend,0,1);
				}
	
			}
	
			if (i % 500 == 0)
				yield return new CoroutineJob.ProgressInfo("ObiCloth: adding bend constraints...",i/(float)sharedTopology.heVertices.Length);
		}
		bendConstraintOffsets[topology.heVertices.Length] = bendBatch.ConstraintCount;

		//Initialize pin constraints:
		PinConstraints.Clear();
		ObiPinConstraintBatch pinBatch = new ObiPinConstraintBatch(false,false);
		PinConstraints.AddBatch(pinBatch);

		AddToSolver(null);

		initializing = false;
		initialized = true;

		InitializeWithRegularMesh();

		pooledVertices = (int)((topology.heFaces.Length * 3 - sharedMesh.vertexCount) * tearCapacity);

	}

	private void ApplyTearing(){

		ObiDistanceConstraintBatch distanceBatch = DistanceConstraints.GetFirstBatch();
		float[] forces = new float[distanceBatch.ConstraintCount];
		Oni.GetBatchConstraintForces(distanceBatch.OniBatch,forces,distanceBatch.ConstraintCount,0);	

		List<TornEdge> tornEdges = new List<TornEdge>();
		for (int i = 0; i < forces.Length; i++){

			float p1Resistance = tearResistance[distanceBatch.springIndices[i*2]];
			float p2Resistance = tearResistance[distanceBatch.springIndices[i*2+1]];

			// average particle resistances:
			float resistance = (p1Resistance + p2Resistance) * 0.5f * tearResistanceMultiplier;

			if (-forces[i] * 1000 > resistance){ // units are kilonewtons.
				tornEdges.Add(new TornEdge(i,forces[i]));
			}
		}

		if (tornEdges.Count > 0){

			// sort edges by tear force:
			tornEdges.Sort(delegate(TornEdge x, TornEdge y) {
				return x.force.CompareTo(y.force);
			});

			DistanceConstraints.RemoveFromSolver(null);
			for(int i = 0; i < Mathf.Min(tearRate,tornEdges.Count); i++)
				Tear(tornEdges[i].index);
			DistanceConstraints.AddToSolver(this);

			// update active bending constraints:
			BendingConstraints.SetActiveConstraints();

			// update solver deformable triangle indices:
			UpdateDeformableTriangles();

			// upload active particle list to solver:
			solver.UpdateActiveParticles();
		}
		
	}

	/**
	 * Tears a cloth distance constraint, affecting both the physical representation of the cloth and its mesh.
	 */
	public void Tear(int constraintIndex){

		if (topology == null) return;

		// don't allow splitting if there are no free particles left in the pool.
		if (usedParticles >= sharedMesh.vertexCount + pooledVertices) return;

		// get involved constraint batches: 
		ObiDistanceConstraintBatch distanceBatch = DistanceConstraints.GetFirstBatch();
		ObiBendConstraintBatch bendingBatch = BendingConstraints.GetFirstBatch();

		// get particle indices at both ends of the constraint:
		int splitIndex = distanceBatch.springIndices[constraintIndex*2];
		int intactIndex = distanceBatch.springIndices[constraintIndex*2+1];

		// we will split the particle with higher mass, so swap them if needed.
		if (invMasses[splitIndex] > invMasses[intactIndex]){
			int aux = splitIndex;
			splitIndex = intactIndex;
			intactIndex = aux;
		}

		// Calculate the splitting plane in local space:
		Vector3 v1 = transform.worldToLocalMatrix.MultiplyPoint3x4(solver.positions[particleIndices[splitIndex]]);
		Vector3 v2 = transform.worldToLocalMatrix.MultiplyPoint3x4(solver.positions[particleIndices[intactIndex]]);
		Vector3 normal = (v2-v1).normalized;

		int numUpdatedHalfEdges = maxVertexValency;
		int[] updatedHalfEdges = new int[numUpdatedHalfEdges];

		// Try to split the vertex at that particle. 
		// If we cannot not split the higher mass particle, try the other one. If that fails too, we cannot tear this edge.
		if (invMasses[splitIndex] == 0 ||
			!Oni.TearDeformableMeshAtVertex(deformableMesh,splitIndex,ref v1,ref normal,updatedHalfEdges,ref numUpdatedHalfEdges))
		{
			// Try to split the other particle:
			int aux = splitIndex;
			splitIndex = intactIndex;
            intactIndex = aux;

			v1 = transform.worldToLocalMatrix.MultiplyPoint3x4(solver.positions[particleIndices[splitIndex]]);
			v2 = transform.worldToLocalMatrix.MultiplyPoint3x4(solver.positions[particleIndices[intactIndex]]);
			normal = (v2-v1).normalized;
            
			if (invMasses[splitIndex] == 0 || 
				!Oni.TearDeformableMeshAtVertex(deformableMesh,splitIndex,ref v1,ref normal,updatedHalfEdges,ref numUpdatedHalfEdges))
				return;
		}

		if (OnConstraintTorn != null)
			OnConstraintTorn(this,new ObiConstraintTornEventArgs(constraintIndex, splitIndex));

		// identify weak points around the cut:
		int weakPt1 = -1;
		int weakPt2 = -1;
		float weakestValue = float.MaxValue;
		float secondWeakestValue = float.MaxValue;

		foreach (Oni.Vertex v in topology.GetNeighbourVerticesEnumerator(topology.heVertices[splitIndex])){

			Vector3 neighbour = transform.worldToLocalMatrix.MultiplyPoint3x4(solver.positions[particleIndices[v.index]]);
			float weakness = Mathf.Abs(Vector3.Dot(normal,(neighbour - v1).normalized));

			if (weakness < weakestValue){
				secondWeakestValue = weakestValue;
				weakestValue = weakness;
				weakPt2 = weakPt1;
				weakPt1 = v.index;
			}else if (weakness < secondWeakestValue){
				secondWeakestValue = weakness;
				weakPt2 = v.index;
			}
		}

		// reduce tear resistance at the weak spots of the cut, to encourage coherent tear formation.
		if (weakPt1 >= 0) tearResistance[weakPt1] *= 1-tearDebilitation;
		if (weakPt2 >= 0) tearResistance[weakPt2] *= 1-tearDebilitation;

		topology.UpdateVertexCount();

		// halve the mass and radius of the original particle:
		invMasses[splitIndex] *= 2;
		principalRadii[splitIndex] *= 0.5f;

		// copy the new particle data in the actor and solver arrays:
		positions[usedParticles] = positions[splitIndex];
		velocities[usedParticles] = velocities[splitIndex];
		active[usedParticles] = active[splitIndex];
		invMasses[usedParticles] = invMasses[splitIndex];
		principalRadii[usedParticles] = principalRadii[splitIndex];
		phases[usedParticles] = phases[splitIndex];
		areaContribution[usedParticles] = areaContribution[splitIndex];
		tearResistance[usedParticles] = tearResistance[splitIndex];
		restPositions[usedParticles] = positions[splitIndex];
		restPositions[usedParticles][3] = 1; // activate rest position.
		
		// update solver particle data:
		solver.velocities[particleIndices[usedParticles]] = solver.velocities[particleIndices[splitIndex]];
		solver.startPositions[particleIndices[usedParticles]] = solver.positions [particleIndices[usedParticles]] = solver.positions [particleIndices[splitIndex]];
		
		solver.invMasses [particleIndices[usedParticles]] = solver.invMasses [particleIndices[splitIndex]] = invMasses[splitIndex];
		solver.principalRadii[particleIndices[usedParticles]] = solver.principalRadii[particleIndices[splitIndex]] = principalRadii[splitIndex];
		solver.phases	 [particleIndices[usedParticles]] = solver.phases	[particleIndices[splitIndex]];

		usedParticles++;

		// update distance constraints:
		for (int i = 0; i < numUpdatedHalfEdges; ++i){

			int halfEdgeIndex = updatedHalfEdges[i];
			Oni.HalfEdge e = topology.heHalfEdges[halfEdgeIndex];

			// find start and end vertex indices for this edge:
			int startVertex = topology.GetHalfEdgeStartVertex(topology.heHalfEdges[halfEdgeIndex]);
			int endVertex = topology.heHalfEdges[halfEdgeIndex].endVertex;

			if (distanceConstraintMap[halfEdgeIndex] > -1){ // update existing edge

				distanceBatch.springIndices[distanceConstraintMap[halfEdgeIndex]*2] = startVertex;
				distanceBatch.springIndices[distanceConstraintMap[halfEdgeIndex]*2+1] = endVertex;

			}else if (topology.IsSplit(halfEdgeIndex)){ // new edge

				int pairConstraintIndex = distanceConstraintMap[topology.heHalfEdges[halfEdgeIndex].pair];
				
				// update constraint-edge map:
				distanceConstraintMap[halfEdgeIndex] = distanceBatch.restLengths.Count;
				
				// add the new constraint:
	            distanceBatch.AddConstraint(startVertex,
			                                endVertex,
			                                distanceBatch.restLengths[pairConstraintIndex],
			                                distanceBatch.stiffnesses[pairConstraintIndex].x,
			                                distanceBatch.stiffnesses[pairConstraintIndex].y);
			}
	
			// update deformable triangles:
			if (e.indexInFace > -1){
				deformableTriangles[e.face*3 + e.indexInFace] = e.endVertex;
			}
		}

		if (splitIndex < bendConstraintOffsets.Length-1){

			// deactivate bend constraints that contain the split vertex...

			// ...at the center:
			for (int i = bendConstraintOffsets[splitIndex]; i < bendConstraintOffsets[splitIndex+1]; ++i){
				bendingBatch.DeactivateConstraint(i);
			}
	
			// ...at one end:
			foreach(Oni.Vertex v in topology.GetNeighbourVerticesEnumerator(topology.heVertices[splitIndex])){
				if (v.index < bendConstraintOffsets.Length-1){
					for (int i = bendConstraintOffsets[v.index]; i < bendConstraintOffsets[v.index+1]; ++i){
						if (bendingBatch.bendingIndices[i*3] == splitIndex || bendingBatch.bendingIndices[i*3+1] == splitIndex){
							bendingBatch.DeactivateConstraint(i);
						}
					}
				}
			}

		}

	}
}
}

