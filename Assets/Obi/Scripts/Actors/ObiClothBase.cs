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
 * For convenience, solver gravity is expressed and applied in world space. 
 * Which means that no matter how you rotate a ObiCloth object, gravity will always pull particles down.
 * (as long as gravity in your solver is meant to pulls things down, heh).
 * 
 */
[ExecuteInEditMode]
[RequireComponent(typeof (Renderer))]
[RequireComponent(typeof (ObiDistanceConstraints))]
[RequireComponent(typeof (ObiBendingConstraints))]
[RequireComponent(typeof (ObiAerodynamicConstraints))]
[RequireComponent(typeof (ObiPinConstraints))]
public abstract class ObiClothBase : ObiActor
{
	public const float DEFAULT_PARTICLE_MASS = 0.1f;

	public event System.EventHandler OnDeformableMeshSetup;
	public event System.EventHandler OnDeformableMeshTeardown;
	public event System.EventHandler OnFrameBegin;			/**< This event should get triggered right before the actor starts simulating the current frame.*/
	public event System.EventHandler OnFrameEnd;			/**< This event should get triggered right after the actor has finished simulating the current frame.*/

	[SerializeProperty("SharedTopology")]
	[SerializeField] protected ObiMeshTopology sharedTopology;	/**< Reference mesh topology used to create a particle based physical representation of this actor.*/

	[SerializeProperty("NormalsUpdate")]
	[SerializeField] protected Oni.NormalsUpdate normalsUpdate = Oni.NormalsUpdate.Recalculate;

	[SerializeProperty("UpdateTangents")]
	[SerializeField] protected bool updateTangents = false;

	[HideInInspector] public ObiMeshTopology topology;							/**< Unique instance of the topology. Can be different from the shared topology due to tearing and other runtime topological changes.*/		
	[HideInInspector] public Mesh sharedMesh;				/**< Original unmodified mesh.*/
	[HideInInspector] public Mesh clothMesh;				/**< Unique instance of the shared mesh, gets modified by the simulation*/

	protected MeshFilter meshFilter;
	protected MeshRenderer meshRenderer;
 
	protected float[] transformData = new float[16];
	protected int[] meshTriangles;	
	protected Vector3[] meshVertices;
	protected Vector3[] meshNormals;
	protected Vector4[] meshTangents;
    protected Quaternion[] orientation;							/**< Per particle current orientation.*/

	protected IntPtr deformableMesh;												/**< pointer to Obi deformable mesh*/
	protected GCHandle particleIndicesHandle;
	protected GCHandle meshTrianglesHandle;
	protected GCHandle meshVerticesHandle;
	protected GCHandle meshNormalsHandle;
	protected GCHandle meshTangentsHandle;
	
	[HideInInspector] public float[] areaContribution;							/**< How much mesh surface area each particle represents.*/    

	public ObiDistanceConstraints DistanceConstraints{
		get{return GetConstraints(Oni.ConstraintType.Distance) as ObiDistanceConstraints;}
	}
	public ObiBendingConstraints BendingConstraints{
		get{return GetConstraints(Oni.ConstraintType.Bending) as ObiBendingConstraints;}
	}
	public ObiAerodynamicConstraints AerodynamicConstraints{
		get{return GetConstraints(Oni.ConstraintType.Aerodynamics) as ObiAerodynamicConstraints;}
	}
	public ObiPinConstraints PinConstraints{
		get{return GetConstraints(Oni.ConstraintType.Pin) as ObiPinConstraints;}
	}

	public ObiMeshTopology SharedTopology{
		get{return sharedTopology;}
		set{
			if (sharedTopology != value){
				sharedTopology = value;
				initialized = false;
			}
		}
	}

	public Oni.NormalsUpdate NormalsUpdate{
		get{return normalsUpdate;}
		set{
			normalsUpdate = value;
			Oni.SetDeformableMeshTBNUpdate(deformableMesh,normalsUpdate,updateTangents);
		}
	} 

	public bool UpdateTangents{
		get{return updateTangents;}
		set{
			updateTangents = value;
			Oni.SetDeformableMeshTBNUpdate(deformableMesh,normalsUpdate,updateTangents);
		}
	} 

	public IntPtr DeformableMesh{
		get{return deformableMesh;}
	}

	public Vector3[] MeshVertices{
		get{return meshVertices;}
	}
	public Vector3[] MeshNormals{
		get{return meshNormals;}
	}
	public Vector4[] MeshTangents{
		get{return meshTangents;}
	}

	public override bool UsesCustomExternalForces{ 
		get{return true;}
	}

	public virtual void Awake()
	{

		// Grab a copy of the serialized topology reference. This happens when duplicating a cloth.
		if (topology != null)
			topology = GameObject.Instantiate(topology);
		// Or a copy of the shared topology, if there is no serialized reference to a topology.
		else if (sharedTopology != null)
			topology = GameObject.Instantiate(sharedTopology);

		if (sharedMesh != null && !sharedMesh.isReadable){
			Debug.LogError(this.name + "'s cloth mesh is not readable. Please enable the read/write checkbox in the mesh importer inspector.");
		}
		
	}
		
	public override void OnDisable(){
		
		base.OnDisable();

		GameObject.DestroyImmediate(clothMesh);

		if (meshFilter != null)
			meshFilter.mesh = sharedMesh;

	}

	public override void OnSolverFrameBegin(bool fixedUpdate){

		base.OnSolverFrameBegin(fixedUpdate);

		if (OnFrameBegin != null)
			OnFrameBegin(this,EventArgs.Empty);
	}

	public override void OnSolverPreInterpolation(){

		base.OnSolverPreInterpolation();

		Matrix4x4 w2lTransform = ActorWorldToLocalMatrix;

		// if solver data is expressed in local space, convert 
		// from solver's local space to world, then from world to actor local space:
		//if (Solver.simulateInLocalSpace)
		//	w2lTransform *= Solver.transform.localToWorldMatrix;

		for (int i = 0; i < 16; ++i)
			transformData[i] = w2lTransform[i];

		Oni.SetDeformableMeshTransform(deformableMesh,transformData);

	}

	public override void OnSolverFrameEnd(float deltaTime){

		base.OnSolverFrameEnd(deltaTime);
            
		CommitResultsToMesh();

		if (OnFrameEnd != null)
			OnFrameEnd(this,null);

	}

	public override void OnSolverStepEnd(float deltaTime){	

		base.OnSolverStepEnd(deltaTime);

		// Break pin constraints when needed:
		if (isActiveAndEnabled){
			PinConstraints.BreakConstraints();
		}

	}
	
	public override void OnDestroy(){

		base.OnDestroy();

		// Destroy our copy of the topology:
		GameObject.DestroyImmediate(topology);

	}

	protected virtual void CallOnDeformableMeshSetup()
    {
        System.EventHandler handler = OnDeformableMeshSetup;
        if (handler != null)
        {
            handler(this, null);
        }
    }

	protected virtual void CallOnDeformableMeshTearDown()
    {
        System.EventHandler handler = OnDeformableMeshTeardown;
        if (handler != null)
        {
            handler(this, null);
        }
    }

	public virtual void GetMeshDataArrays(Mesh mesh){
		if (mesh != null)
		{
			Oni.UnpinMemory(meshTrianglesHandle);
			Oni.UnpinMemory(meshVerticesHandle);
			Oni.UnpinMemory(meshNormalsHandle);
			Oni.UnpinMemory(meshTangentsHandle);

			if (mesh.isReadable){

				meshTriangles = mesh.triangles;
				meshVertices = mesh.vertices;
				meshNormals = mesh.normals;
				meshTangents = mesh.tangents;
	
				meshTrianglesHandle = Oni.PinMemory(meshTriangles);
				meshVerticesHandle = Oni.PinMemory(meshVertices);
				
				if (meshNormals.Length == mesh.vertexCount){
					meshNormalsHandle = Oni.PinMemory(meshNormals);
				}

			    if (meshTangents.Length == mesh.vertexCount){
					meshTangentsHandle = Oni.PinMemory(meshTangents);
				}
	
				Oni.SetDeformableMeshData(deformableMesh,meshTrianglesHandle.AddrOfPinnedObject(),
														 meshVerticesHandle.AddrOfPinnedObject(),
														 meshNormalsHandle.IsAllocated ? meshNormalsHandle.AddrOfPinnedObject() : IntPtr.Zero,
														 meshTangentsHandle.IsAllocated ? meshTangentsHandle.AddrOfPinnedObject() : IntPtr.Zero,
														 IntPtr.Zero,IntPtr.Zero,IntPtr.Zero,IntPtr.Zero,IntPtr.Zero);
			}else{
				// TODO: just call SetDeformableMeshData with null for all parameters.
				Oni.DestroyDeformableMesh(solver.OniSolver,deformableMesh);
				deformableMesh = IntPtr.Zero;
			}
		}
	}

	protected void InitializeWithRegularMesh(){
		
		meshFilter = GetComponent<MeshFilter>();
		meshRenderer = GetComponent<MeshRenderer>();
		
		if (meshFilter == null || meshRenderer == null || !Initialized)
			return;

        sharedMesh = sharedTopology.InputMesh;
		
		// Make a deep copy of the original shared mesh.
		clothMesh = Mesh.Instantiate(sharedMesh) as Mesh;
		clothMesh.MarkDynamic(); 
		GetMeshDataArrays(clothMesh);
		
		// Use the freshly created mesh copy as the renderer mesh:
		meshFilter.mesh = clothMesh;
		
	}

	/**
 	 * Applies changes in physics model to the cloth mesh.
 	 */
	public virtual void CommitResultsToMesh()
	{
		if (!Initialized || particleIndices == null || clothMesh == null) return;
	
		if (clothMesh.isReadable){
			clothMesh.vertices = meshVertices;
			clothMesh.normals = meshNormals;
			clothMesh.tangents = meshTangents;
			clothMesh.RecalculateBounds();
		}
	}

	/**
 	* Resets cloth mesh to its original state.
 	*/
	public override void ResetActor(){

		//reset topology:
		topology.CopyDataFrom(sharedTopology);
		
		//reset particle positions:
		foreach (Oni.Vertex vertex in sharedTopology.heVertices){
			positions[vertex.index] = restPositions[vertex.index];
            velocities[vertex.index] = Vector3.zero;
        }

		//reset mesh, if any:
		if (clothMesh != null){
			GetMeshDataArrays(sharedMesh);

			if (clothMesh.isReadable){
				clothMesh.vertices = meshVertices;
				clothMesh.normals = meshNormals;
				clothMesh.tangents = meshTangents;
				clothMesh.RecalculateBounds();
			}
		}

		PushDataToSolver(ParticleData.POSITIONS | ParticleData.VELOCITIES);
		            
	}

	/**
	 * Reads particle data from a 2D texture. Can be used to adjust per particle mass, skin radius, etc. using 
	 * a texture instead of painting it in the editor. 
	 *	
     * Will call onReadProperty once for each particle, passing the particle index and the bilinearly interpolated 
	 * color of the texture at its coordinate.
	 *
	 * Be aware that, if a particle corresponds to more than
	 * one physical vertex and has multiple uv coordinates, 
	 * onReadProperty will be called multiple times for that particle.
	 */
	public override bool ReadParticlePropertyFromTexture(Texture2D source,Action<int,Color> onReadProperty){
		
		if (source == null || clothMesh == null || topology == null || onReadProperty == null)
			return false;

		Vector2[] uvs = clothMesh.uv;

		// Iterate over all vertices in the mesh reading back colors from the texture:
		for (int i = 0; i < clothMesh.vertexCount; ++i){

			try{

				onReadProperty(topology.visualMap[i],source.GetPixelBilinear(uvs[i].x, uvs[i].y));

			}catch(UnityException e){	
				Debug.LogException(e);
				return false;
			}

		}
		
		return true;
	}

	/**
	 * Deactivates all fixed particles that are attached to fixed particles only, and all the constraints
	 * affecting them.
	 */
	public void Optimize(){

		// Iterate over all particles and get those fixed ones that are only linked to fixed particles.
		for (int i = 0; i < topology.heVertices.Length; ++i){

			Oni.Vertex vertex = topology.heVertices[i];
			if (invMasses[i] > 0) continue;

			active[i] = false;
			foreach (Oni.Vertex n in topology.GetNeighbourVerticesEnumerator(vertex)){
				
				// If at least one neighbour particle is not fixed, then the particle we are considering for optimization should not be removed.
				if (invMasses[n.index] > 0){
					active[i]  = true;
					break;
				}
				
			}
			
			// Deactivate all constraints involving this inactive particle:
			if (!active[i]){

				// for each constraint type:
				foreach (ObiBatchedConstraints constraint in constraints){

					// for each constraint batch (usually only one)
					if (constraint != null){
						foreach (ObiConstraintBatch batch in constraint.GetBatches()){
	
							// deactivate constraints that affect the particle:
							List<int> affectedConstraints = batch.GetConstraintsInvolvingParticle(i);
							foreach (int j in affectedConstraints) batch.DeactivateConstraint(j);
							batch.SetActiveConstraints();
						}
					}
				}

			}

		}	

		PushDataToSolver(ParticleData.ACTIVE_STATUS);
					
	}

	/**
	 * Undoes the optimization performed by Optimize(). This means that all particles and constraints in the
	 * cloth are activated again.
	 */
	public void Unoptimize(){
	
		// Activate all particles and constraints (particles first):
		
		for (int i = 0; i < topology.heVertices.Length; ++i)
		 	active[i] = true;

		PushDataToSolver(ParticleData.ACTIVE_STATUS);

		// for each constraint type:
		foreach (ObiBatchedConstraints constraint in constraints){

			// for each constraint batch (usually only one)
			if (constraint != null){
				foreach (ObiConstraintBatch batch in constraint.GetBatches()){
	
					// activate all constraints:
					for (int i = 0; i < batch.ConstraintCount; ++i) batch.ActivateConstraint(i);
					batch.SetActiveConstraints();
				}
			}
		}
		
	}


}
}

