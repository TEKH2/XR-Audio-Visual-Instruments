using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Obi
{

/**
 * An ObiCloth component generates a particle-based physical representation of the object geometry
 * to be feeded to an ObiSolver component. To do that, it needs connectivity information about the mesh,
 * which is provided by an ObiMeshConnectivity asset.
 * 
 * You can use it to make flags, capes, jackets, pants, ropes, drapes, nets, or any kind of object that exhibits cloth-like behavior.
 * 
 * ObiCloth objects have their particle properties expressed in local space. That means that particle positions, velocities, etc
 * are all expressed and serialized using the object's transform as reference. Thanks to this it is very easy to instantiate cloth prefabs and move/rotate/scale
 * them around, while keeping things working as expected. 
 * 
 * For convenience, solver gravity is expressed and applied in the simulation space used.
 */
[ExecuteInEditMode]
[AddComponentMenu("Physics/Obi/Obi Cloth")]
[RequireComponent(typeof (ObiSkinConstraints))]
[RequireComponent(typeof (ObiVolumeConstraints))]
[RequireComponent(typeof (ObiTetherConstraints))]
[DisallowMultipleComponent]
public class ObiCloth : ObiClothBase
{

	protected SkinnedMeshRenderer skinnedMeshRenderer;
	protected ObiAnimatorController animatorController;
	protected int rootBindPoseIndex = 0;

	protected Transform[] rendererBones;
	protected float[] boneData;
	protected float[] bindPoseData;

	public ObiSkinConstraints SkinConstraints{
		get{return GetConstraints(Oni.ConstraintType.Skin) as ObiSkinConstraints;}
	}
	public ObiVolumeConstraints VolumeConstraints{
		get{return GetConstraints(Oni.ConstraintType.Volume) as ObiVolumeConstraints;}
	}
	public ObiTetherConstraints TetherConstraints{
		get{return GetConstraints(Oni.ConstraintType.Tether) as ObiTetherConstraints;}
	}

	public bool IsSkinned{
		get{return skinnedMeshRenderer != null && skinnedMeshRenderer.rootBone != null;}
	}
	
	public override Matrix4x4 ActorLocalToWorldMatrix{
		get{
			if (!IsSkinned) 
				return transform.localToWorldMatrix;
			else{
				if (!InSolver) 
					// when we are in edit mode, we need to take the root bone bind pose into account, as skinning is not updated by Obi but by Unity:
					return skinnedMeshRenderer.rootBone.localToWorldMatrix * skinnedMeshRenderer.sharedMesh.bindposes[rootBindPoseIndex];
				else 
					return skinnedMeshRenderer.rootBone.localToWorldMatrix;
			}
		}
	}

	public override Matrix4x4 ActorWorldToLocalMatrix{
		get{
			if (!IsSkinned) 
				return transform.worldToLocalMatrix;
			else{ 
				return skinnedMeshRenderer.rootBone.worldToLocalMatrix;
			}
		}
	}

	public override void Awake(){

		base.Awake();
		skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();

		FindRootboneBindpose();
		SetupAnimatorController();
	}

	/**
	 * Finds the index of the rootbone bind pose in the sharedMesh's bindPose array. This is used
	 * to calculate actor world to local and local to world matrices when using skinned cloth.
     */
	private void FindRootboneBindpose(){

		if (IsSkinned){

			for(int i = 0; i < skinnedMeshRenderer.bones.Length; ++i){
				if (skinnedMeshRenderer.bones[i] == skinnedMeshRenderer.rootBone){
					rootBindPoseIndex = i;	
					return;
				}
			}
		}
	}

	/**
 	 * Find the first animator up the hierarchy of the cloth, and get its ObiAnimatorController component or add one if it is not present.
     */
	private void SetupAnimatorController(){

		if (IsSkinned){

			// find the first animator up our hierarchy:
			Animator animator = GetComponentInParent<Animator>();
				
			// if we have an animator above us, see if it has an animator controller component and add one if it doesn't:
			if (animator != null){

				animatorController = animator.GetComponent<ObiAnimatorController>();

				if (animatorController == null)
					animatorController = animator.gameObject.AddComponent<ObiAnimatorController>();
			}
		}		
	}

	public override void OnEnable(){

		base.OnEnable();

		// Initialize cloth:
		if (skinnedMeshRenderer == null)
			InitializeWithRegularMesh();
		else 
			InitializeWithSkinnedMesh();

	}
		
	public override void OnDisable(){
		
		base.OnDisable();

		if (skinnedMeshRenderer != null)
			skinnedMeshRenderer.sharedMesh = sharedMesh;

		if (animatorController != null)
			animatorController.ResumeAutonomousUpdate();

	}
	
	public override bool AddToSolver(object info){

		if (Initialized && base.AddToSolver(info)){
				
			particleIndicesHandle = Oni.PinMemory(particleIndices);

			Matrix4x4 w2lTransform = ActorWorldToLocalMatrix;

			// if solver data is expressed in local space, convert 
			// from solver's local space to world, then from world to actor local space:
			if (Solver.simulateInLocalSpace)
				w2lTransform *= Solver.transform.localToWorldMatrix;
	
			for (int i = 0; i < 16; ++i)
				transformData[i] = w2lTransform[i];

			IntPtr skinbatch = IntPtr.Zero;
			if (SkinConstraints.GetFirstBatch() != null)
				skinbatch = SkinConstraints.GetFirstBatch().OniBatch;

			deformableMesh = Oni.CreateDeformableMesh(Solver.OniSolver,
													  topology.HalfEdgeMesh,
													  skinbatch,
													  transformData,
													  particleIndicesHandle.AddrOfPinnedObject(),
												      sharedMesh.vertexCount,
													  sharedMesh.vertexCount);

			Oni.SetDeformableMeshTBNUpdate(deformableMesh,normalsUpdate,updateTangents);

			GetMeshDataArrays(clothMesh);

			UpdateBindPosesAndWeights();

			UpdateBoneTransforms();

			// Inits skeletal skinning, to ensure all particles are skinned during the first frame. This ensures
			// that the initial position of particles is the initial skinned position, instead of that dictated by the rootbone's local space.
			Oni.ForceDeformableMeshSkeletalSkinning(deformableMesh);

			CallOnDeformableMeshSetup();

			// remove bone weights so that the mesh is not affected by Unity's skinning:
			clothMesh.boneWeights = new BoneWeight[]{};

			return true;
		}
		return false;
    }

	public override bool RemoveFromSolver(object info){

		bool removed = false;

		try{

			// re-enable Unity skinning:
			if (clothMesh != null)
				clothMesh.boneWeights = sharedMesh.boneWeights;

			if (solver != null && InSolver){
				Oni.DestroyDeformableMesh(Solver.OniSolver,deformableMesh);
				deformableMesh = IntPtr.Zero;

				Oni.UnpinMemory(particleIndicesHandle);
				Oni.UnpinMemory(meshTrianglesHandle);
				Oni.UnpinMemory(meshVerticesHandle);
				Oni.UnpinMemory(meshNormalsHandle);
				Oni.UnpinMemory(meshTangentsHandle);
	
				CallOnDeformableMeshTearDown();
			}

		}catch(Exception e){
			Debug.LogException(e);
		}finally{
			removed = base.RemoveFromSolver(info);
		}
		return removed;
	}

	/**
	 * If a Skinned Mesh Renderer is present, grabs bind poses and skin weights and transfers it to the particle simulation.
	 * Does nothing if no Skinned Mesh Renderer can be found.
	 */
	public void UpdateBindPosesAndWeights(){

		if (skinnedMeshRenderer != null){

			Matrix4x4[] rendererBindPoses = sharedMesh.bindposes;
			BoneWeight[] rendererWeights = sharedMesh.boneWeights;
			rendererBones = skinnedMeshRenderer.bones;

			bindPoseData = new float[16*rendererBindPoses.Length];
				boneData = new float[16*rendererBones.Length];
			
			// get bind pose data:
			for (int p = 0; p < rendererBindPoses.Length; ++p){
				for (int i = 0; i < 16; ++i)
					bindPoseData[p*16+i] = rendererBindPoses[p][i];
			}

			// get weights data:
			Oni.BoneWeights[] weights = new Oni.BoneWeights[rendererWeights.Length];
			for (int i = 0; i < rendererWeights.Length; ++i)
				weights[i] = new Oni.BoneWeights(rendererWeights[i]);

			Oni.SetDeformableMeshAnimationData(deformableMesh,bindPoseData,weights,rendererBindPoses.Length);
		}
	}	

	/**
	 * If a Skinned Mesh Renderer is present, grabs all bone transform data and transfers it to the particle simulation.
	 * Does nothing if no Skinned Mesh Renderer can be found.
	 */
	public void UpdateBoneTransforms(){

		if (skinnedMeshRenderer != null){

			if (!Initialized || clothMesh == null || particleIndices == null) return;
			
			for (int p = 0; p < rendererBones.Length; ++p){
	
				Matrix4x4 bone;

				if (Solver.simulateInLocalSpace)
					bone = Solver.transform.worldToLocalMatrix * rendererBones[p].localToWorldMatrix;
				else 
					bone = rendererBones[p].localToWorldMatrix;

				for (int i = 0; i < 16; ++i)
					boneData[p*16+i] = bone[i];
			}

			Oni.SetDeformableMeshBoneTransforms(deformableMesh,boneData);
		}

	}

	protected void InitializeWithSkinnedMesh(){
	
        if (!Initialized)
           return;

        sharedMesh = sharedTopology.InputMesh;

		// Use the topology mesh as the shared mesh:
		skinnedMeshRenderer.sharedMesh = sharedMesh;
		
		// Make a deep copy of the original shared mesh.
		clothMesh = Mesh.Instantiate(sharedMesh) as Mesh;
		
		// remove bone weights so that the mesh is not affected by Unity's skinning:
		if (Application.isPlaying) 
			clothMesh.boneWeights = new BoneWeight[]{};

		clothMesh.MarkDynamic();
		GetMeshDataArrays(clothMesh);
		
		// Use the freshly created mesh copy as the renderer mesh:
		skinnedMeshRenderer.sharedMesh = clothMesh;

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

		active = new bool[topology.heVertices.Length];
		positions = new Vector3[topology.heVertices.Length];
		restPositions = new Vector4[topology.heVertices.Length];
		velocities = new Vector3[topology.heVertices.Length];
		invMasses  = new float[topology.heVertices.Length];
		principalRadii = new Vector3[topology.heVertices.Length];
		phases = new int[topology.heVertices.Length];
		areaContribution = new float[topology.heVertices.Length]; 
		deformableTriangles = new int[topology.heFaces.Length*3]; 

		initialScaleMatrix.SetTRS(Vector3.zero,Quaternion.identity,transform.lossyScale);

		// Create a particle for each vertex:
		for (int i = 0; i < topology.heVertices.Length; i++){
			
			Oni.Vertex vertex = topology.heVertices[i];

			// Get the particle's area contribution.
			areaContribution[i] = 0;
			foreach (Oni.Face face in topology.GetNeighbourFacesEnumerator(vertex)){
				areaContribution[i] += topology.GetFaceArea(face)/3;
            }
			
			// Get the shortest neighbour edge, particle radius will be half of its length.
			float minEdgeLength = Single.MaxValue;
			foreach (Oni.HalfEdge edge in topology.GetNeighbourEdgesEnumerator(vertex)){
	
				// vertices at each end of the edge:
				Vector3 v1 = initialScaleMatrix*topology.heVertices[topology.GetHalfEdgeStartVertex(edge)].position;
				Vector3 v2 = initialScaleMatrix*topology.heVertices[edge.endVertex].position;

				minEdgeLength = Mathf.Min(minEdgeLength,Vector3.Distance(v1,v2));
			}

			active[i] = true;
			invMasses[i] = (skinnedMeshRenderer == null && areaContribution[i] > 0) ? (1.0f / (DEFAULT_PARTICLE_MASS * areaContribution[i])) : 0;
			positions[i] = initialScaleMatrix * vertex.position ;
			restPositions[i] = positions[i];
			restPositions[i][3] = 1; // activate rest position.
			principalRadii[i] = Vector3.one * minEdgeLength * 0.5f;
			phases[i] = Oni.MakePhase(1,selfCollisions?Oni.ParticlePhase.SelfCollide:0);
			
			if (i % 500 == 0)
				yield return new CoroutineJob.ProgressInfo("ObiCloth: generating particles...",i/(float)topology.heVertices.Length);
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
		ObiDistanceConstraintBatch distanceBatch = new ObiDistanceConstraintBatch(true,false);
		DistanceConstraints.AddBatch(distanceBatch);

		// Create distance springs: 
		for (int i = 0; i < edges.Count; i++){
		
			Oni.HalfEdge hedge = topology.heHalfEdges[edges[i].halfEdgeIndex];
			Oni.Vertex startVertex = topology.heVertices[topology.GetHalfEdgeStartVertex(hedge)];
			Oni.Vertex endVertex = topology.heVertices[hedge.endVertex];
			
			distanceBatch.AddConstraint(topology.GetHalfEdgeStartVertex(hedge),hedge.endVertex,Vector3.Distance(initialScaleMatrix*startVertex.position,initialScaleMatrix*endVertex.position),1,1);		

			if (i % 500 == 0)
				yield return new CoroutineJob.ProgressInfo("ObiCloth: generating structural constraints...",i/(float)topology.heHalfEdges.Length);
		}

		// Cook distance constraints, for better cache and SIMD use:
		distanceBatch.Cook();
		
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

		//Create skin constraints (if needed)
		if (skinnedMeshRenderer != null){

			SkinConstraints.Clear();
			ObiSkinConstraintBatch skinBatch = new ObiSkinConstraintBatch(true,false);
			SkinConstraints.AddBatch(skinBatch);

			for (int i = 0; i < topology.heVertices.Length; ++i){

				skinBatch.AddConstraint(i,initialScaleMatrix * topology.heVertices[i].position, Vector3.up,0.05f,0.1f,0,1);

				if (i % 500 == 0)
					yield return new CoroutineJob.ProgressInfo("ObiCloth: generating skin constraints...",i/(float)topology.heVertices.Length);
			}

			for (int i = 0; i < topology.normals.Length; ++i){
				skinBatch.skinNormals[topology.visualMap[i]] = topology.normals[i];
			}

			skinBatch.Cook();

		}

		//Create pressure constraints if the mesh is closed:
		VolumeConstraints.Clear();

		if (topology.IsClosed){

			ObiVolumeConstraintBatch volumeBatch = new ObiVolumeConstraintBatch(false,false);
			VolumeConstraints.AddBatch(volumeBatch);

			float avgInitialScale = (initialScaleMatrix.m00 + initialScaleMatrix.m11 + initialScaleMatrix.m22) * 0.33f;

			int[] triangleIndices = new int[topology.heFaces.Length * 3];
			for (int i = 0; i < topology.heFaces.Length; i++){
				Oni.Face face = topology.heFaces[i];
			
				Oni.HalfEdge e1 = topology.heHalfEdges[face.halfEdge];
				Oni.HalfEdge e2 = topology.heHalfEdges[e1.nextHalfEdge];
				Oni.HalfEdge e3 = topology.heHalfEdges[e2.nextHalfEdge];

				triangleIndices[i*3] = e1.endVertex;
				triangleIndices[i*3+1] = e2.endVertex;
				triangleIndices[i*3+2] = e3.endVertex;

				if (i % 500 == 0)
					yield return new CoroutineJob.ProgressInfo("ObiCloth: generating volume constraints...",i/(float)topology.heFaces.Length);
			}

			volumeBatch.AddConstraint(triangleIndices,topology.MeshVolume * avgInitialScale,1,1);
		}
		
		//Create bending constraints:
		BendingConstraints.Clear();
		ObiBendConstraintBatch bendBatch = new ObiBendConstraintBatch(true,false);
		BendingConstraints.AddBatch(bendBatch);

		Dictionary<int,int> cons = new Dictionary<int, int>();
		for (int i = 0; i < topology.heVertices.Length; i++){
	
			Oni.Vertex vertex = topology.heVertices[i];
	
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

		bendBatch.Cook();

		// Initialize tether constraints:
		TetherConstraints.Clear();

		// Initialize pin constraints:
		PinConstraints.Clear();
		ObiPinConstraintBatch pinBatch = new ObiPinConstraintBatch(false,false);
		PinConstraints.AddBatch(pinBatch);

		initializing = false;
		initialized = true;

		if (skinnedMeshRenderer == null)
			InitializeWithRegularMesh();
		else 
			InitializeWithSkinnedMesh();
	}

	/**
	 * Leave OnSolverStepBegin implementation empty, because cloth updates fixed particles in OnSolverFrameBegin.
	 */
	public override void OnSolverStepBegin(){
	}

	/**
	 * In the case of skinned cloth, we need to tell the animator controller to update the skeletal animation, 
	 * then grab the skinned vertex positions prior to starting the simulation steps for this frame.
	 */
	public override void OnSolverFrameBegin(bool fixedUpdate){

		if (skinnedMeshRenderer == null){

			// regular on frame begin: transform fixed particles.
			UpdateFixedParticles();

		}else{

			// manually update animator (before particle physics):
			if (animatorController != null && isActiveAndEnabled){
				animatorController.UpdateAnimation(fixedUpdate);
			}

			// grab skeleton bone transforms:
			UpdateBoneTransforms();
		}
	}

	public override void OnSolverVisibilityChanged(bool visible){

		// make sure the animator updates autonomoustly when the solver stops being visible:
		if (!visible && animatorController != null && isActiveAndEnabled){
			animatorController.ResumeAutonomousUpdate();
		}
	}


	private List<HashSet<int>> GenerateIslands(IEnumerable<int> particles, bool onlyFixed){

		List<HashSet<int>> islands = new List<HashSet<int>>();
			
		// Partition fixed particles into islands:
		foreach (int i in particles){
			
			Oni.Vertex vertex = topology.heVertices[i];

			if ((onlyFixed && invMasses[i] > 0) || !active[i]) continue;
			
			int assignedIsland = -1;

			// keep a list of islands to merge with ours:
			List<int> mergeableIslands = new List<int>();
				
			// See if any of our neighbors is part of an island:
			foreach (Oni.Vertex n in topology.GetNeighbourVerticesEnumerator(vertex)){

				if (!active[n.index]) continue;
	
				for(int k = 0; k < islands.Count; ++k){

					if (islands[k].Contains(n.index)){

						// if we are not in an island yet, pick this one:
						if (assignedIsland < 0){
							assignedIsland = k;
                            islands[k].Add(i);
						}
						// if we already are in an island, we will merge this newfound island with ours:
						else if (assignedIsland != k && !mergeableIslands.Contains(k)){
							mergeableIslands.Add(k);
						}
					}
                }
			}

			// merge islands with the assigned one:
			foreach(int merge in mergeableIslands){
				islands[assignedIsland].UnionWith(islands[merge]);
			}

			// remove merged islands:
			mergeableIslands.Sort();
			mergeableIslands.Reverse();
			foreach(int merge in mergeableIslands){
				islands.RemoveAt(merge);
			}
			
			// If no adjacent particle is in an island, create a new one:
			if (assignedIsland < 0){
				islands.Add(new HashSet<int>(){i});
			}
		}	

		return islands;
	}

	/**
	 * This function generates tethers for a given set of particles, all belonging a connected graph. 
	 * This is use ful when the cloth mesh is composed of several
	 * disjoint islands, and we dont want tethers in one island to anchor particles to fixed particles in a different island.
	 * 
	 * Inside each island, fixed particles are partitioned again into "islands", then generates up to maxTethers constraints for each 
	 * particle linking it to the closest point in each fixed island.
	 */
	private void GenerateTethersForIsland(HashSet<int> particles, int maxTethers){

		if (maxTethers > 0){
			ObiTetherConstraintBatch tetherBatch = new ObiTetherConstraintBatch(true,false);
			TetherConstraints.AddBatch(tetherBatch);
			
			List<HashSet<int>> fixedIslands = GenerateIslands(particles,true);
			
			// Generate tether constraints:
			foreach (int i in particles){
			
				if (invMasses[i] == 0 || !active[i]) continue;
				
				List<KeyValuePair<float,int>> tethers = new List<KeyValuePair<float,int>>(fixedIslands.Count*maxTethers);
				
				// Find the closest particle in each island, and add it to tethers.
				foreach(HashSet<int> island in fixedIslands){
					int closest = -1;
					float minDistance = Mathf.Infinity;
					foreach (int j in island){
						float distance = (topology.heVertices[i].position - topology.heVertices[j].position).sqrMagnitude;
						if (distance < minDistance){
							minDistance = distance;
							closest = j;
						}
					}
					if (closest >= 0)
						tethers.Add(new KeyValuePair<float,int>(minDistance, closest));
				}
				
				// Sort tether indices by distance:
				tethers.Sort(
				delegate(KeyValuePair<float,int> x, KeyValuePair<float,int> y)
				{
					return x.Key.CompareTo(y.Key);
				}
				);
				
				// Create constraints for "maxTethers" closest anchor particles:
				for (int k = 0; k < Mathf.Min(maxTethers,tethers.Count); ++k){
					tetherBatch.AddConstraint(i,tethers[k].Value,Mathf.Sqrt(tethers[k].Key),
												TetherConstraints.tetherScale,
												TetherConstraints.stiffness);
				}
			}
	        
			tetherBatch.Cook();
		}
	}	

	/**
	 * Automatically generates tether constraints for the cloth.
	 * Partitions fixed particles into "islands", then generates up to maxTethers constraints for each 
	 * particle, linking it to the closest point in each island.
	 */
	public override bool GenerateTethers(){
		
		if (!Initialized) return false;

		TetherConstraints.Clear();
		
		// generate disjoint islands:
		List<HashSet<int>> islands = GenerateIslands(System.Linq.Enumerable.Range(0, topology.heVertices.Length),false);

		// generate tethers for each one:
		foreach(HashSet<int> island in islands)
			GenerateTethersForIsland(island,4);
        
        return true;
        
	}
}
}

