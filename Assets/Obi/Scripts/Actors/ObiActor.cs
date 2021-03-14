using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;


namespace Obi{

/**
 * Represents a group of related particles. ObiActor does not make
 * any assumptions about the relationship between these particles, except that they get allocated 
 * and released together.
 */
[ExecuteInEditMode]
[DisallowMultipleComponent]
public abstract class ObiActor : MonoBehaviour, IObiSolverClient
{
	public class ObiActorSolverArgs : System.EventArgs{

        private ObiSolver solver;
		public ObiSolver Solver{
            get{return solver;}
        }

		public ObiActorSolverArgs(ObiSolver solver){
			this.solver = solver;
		}
	}

	public event System.EventHandler<ObiActorSolverArgs> OnAddedToSolver;
	public event System.EventHandler<ObiActorSolverArgs> OnRemovedFromSolver;
	public event System.EventHandler OnInitialized;

	[SerializeProperty("Solver")]
	[SerializeField] protected ObiSolver solver;

	[SerializeProperty("CollisionMaterial")]
	[SerializeField] protected ObiCollisionMaterial collisionMaterial;

	[SerializeProperty("SelfCollisions")]
	[SerializeField] protected bool selfCollisions = false;

	[HideInInspector][NonSerialized] public int[] particleIndices;					/**< indices of allocated particles in the solver.*/

	protected ObiBatchedConstraints[] constraints;	/**< list of constraint components used by this actor.*/
	
	[HideInInspector] public bool[] active;					/**< Particle activation status.*/
	[HideInInspector] public Vector3[] positions;			/**< Particle positions.*/
	[HideInInspector] public Vector4[] restPositions;		/**< Particle rest positions, used to filter collisions.*/	
		
	[HideInInspector] public Quaternion[] orientations;		/**< Particle orientations.*/
	[HideInInspector] public Quaternion[] restOrientations;	/**< Particle rest orientations.*/

	[HideInInspector] public Vector3[] velocities;			/**< Particle velocities.*/
	[HideInInspector] public Vector3[] angularVelocities;	/**< Particle angular velocities.*/

	[HideInInspector] public float[] invMasses;				/**< Particle inverse masses*/
	[HideInInspector] public float[] invRotationalMasses;

	[HideInInspector] public int[] phases;					/**< Particle phases.*/
	[HideInInspector] public Vector3[] principalRadii;		/**< Particle ellipsoid principal radii. These are the ellipsoid radius in each axis.*/
	[HideInInspector] public Color[] colors = null;				/**< Particle colors (not used by all actors, can be null)*/

	[HideInInspector] public int[] deformableTriangles = new int[0];	/**< Indices of deformable triangles (3 per triangle)*/
	[NonSerialized] protected int trianglesOffset = 0;					/**< Offset of deformable trtiangles in curent solver*/

	[SerializeField][HideInInspector] protected Matrix4x4 initialScaleMatrix = Matrix4x4.identity; /**< World scale of the actor, at the moment of initialization.*/

	private bool inSolver = false;
	protected bool initializing = false;	
	
	[HideInInspector][SerializeField] protected bool initialized = false;

	public ObiSolver Solver{
		get{return solver;}
		set{
			if (solver != value){
				RemoveFromSolver(null);
				solver = value;
			}
		}
	}

	public ObiCollisionMaterial CollisionMaterial{
		get{return collisionMaterial;}
		set{
			if (collisionMaterial != value){
				collisionMaterial = value;
				PushDataToSolver(ParticleData.COLLISION_MATERIAL);
			}
		}
	}
	
	public bool Initializing{
		get{return initializing;}
	}
	
	public bool Initialized{
		get{return initialized;}
	}

	public bool InSolver{
		get{return inSolver;}
	}

	public bool SelfCollisions{
		get{return selfCollisions;}
		set{
			if (value != selfCollisions){
				selfCollisions = value;
				UpdateParticlePhases();
			}
		}
	}

	public bool UsesOrientedParticles{
		get{ return invRotationalMasses != null && invRotationalMasses.Length > 0 &&
					orientations != null && orientations.Length > 0 &&
					restOrientations != null && restOrientations.Length > 0;}
	}

	public Matrix4x4 InitialScaleMatrix{
		get{
			return initialScaleMatrix;
		}
	}

	public virtual Matrix4x4 ActorLocalToWorldMatrix{
		get{
			return transform.localToWorldMatrix;
		}
	}

	public virtual Matrix4x4 ActorWorldToLocalMatrix{
		get{
			return transform.worldToLocalMatrix;
		}
	}

	public Matrix4x4 ActorLocalToSolverMatrix{
		get{
			if (Solver != null && Solver.simulateInLocalSpace)
				return Solver.transform.worldToLocalMatrix * ActorLocalToWorldMatrix * initialScaleMatrix.inverse;
			else 
				return ActorLocalToWorldMatrix * initialScaleMatrix.inverse;
		}
	}

	/**
	 * If true, it means external forces aren't applied to the particles directly. For instance,
	 * cloth uses aerodynamic constraints to do so, and fluid uses drag.
	 */
	public virtual bool UsesCustomExternalForces{ 
		get{return false;}
	}

	/**
	 * Since Awake is not guaranteed to be called before OnEnable, we must add the mesh to the solver here.
	 */
	public virtual void Start(){
		LazyBuildConstraintComponentCache();
		if (Application.isPlaying)
			AddToSolver(null);
	}

	public virtual void OnDestroy(){
		RemoveFromSolver(null);
	}

	public virtual void DestroyRequiredComponents(){
		#if UNITY_EDITOR
			if (constraints != null)
				foreach (ObiBatchedConstraints c in constraints)
					GameObject.DestroyImmediate(c);
		#endif
	}

	/**
	 * Flags all particles allocated by this actor as active or inactive depending on the "active array".
	 * The solver will then only simulate the active ones.
	 */
	public virtual void OnEnable(){

		if (InSolver)
		{

			LazyBuildConstraintComponentCache();
			foreach (ObiBatchedConstraints c in constraints)
				if (c != null && isActiveAndEnabled)
					c.OnEnable();
	
			// update active status of all particles in the actor:
			solver.UpdateActiveParticles();
	
			// maybe this actor makes the solver visible to a camera now:
			solver.UpdateVisibility();
		}
	}

	/**
	 * Flags all particles allocated by this actor as inactive, so the solver will not include them 
	 * in the simulation. To "teleport" the actor to a new position, disable it and then pull positions
	 * and velocities from the solver. Move it to the new position, and enable it.
	 */
	public virtual void OnDisable(){

		if (InSolver)
		{

			// flag all the actor's particles as disabled:
			solver.UpdateActiveParticles();
	
			// pull current position / velocity data from solver:
			PullDataFromSolver(ParticleData.POSITIONS | ParticleData.VELOCITIES);
	
			// disable constraints:
			LazyBuildConstraintComponentCache();
			foreach (ObiBatchedConstraints c in constraints)
				if (c != null)
					c.OnDisable();
	
			// maybe this actor makes the solver invisible to all cameras now:
			solver.UpdateVisibility();
		}
	}

	/**
	 * Builds the constraint component list cache if needed.
	 */
	private void LazyBuildConstraintComponentCache(){

		if (this != null && (constraints == null || constraints.Length == 0)){

			ObiBatchedConstraints[] constraintComponents = GetComponents<ObiBatchedConstraints>();
			constraints = new ObiBatchedConstraints[Oni.ConstraintTypeCount];
	
			foreach(ObiBatchedConstraints c in constraintComponents){
				constraints[(int)c.GetConstraintType()] = c;
				c.GrabActor();
			}
		}

	}

	/**
	 * Returns constraints component of a given type. Will cache components if needed (usually upon first call, or if the cache is empty).
	 */
	protected ObiBatchedConstraints GetConstraints(Oni.ConstraintType constraintType){

		LazyBuildConstraintComponentCache();

		int index = (int)constraintType;

		if (index >= 0 && index < constraints.Length)
			return constraints[index];
		return null;

	}

	/**
	 * Generates the particle based physical representation of the actor. This is the initialization method for the actor
	 */
	public IEnumerator GeneratePhysicRepresentationForMesh(){

		IEnumerator g = Initialize();

		while (g.MoveNext()) 
			yield return g.Current;

		if (OnInitialized != null)
			OnInitialized(this,EventArgs.Empty);
	}

	protected abstract IEnumerator Initialize();

	/**
	 * Resets the actor to its original state.
	 */
	public virtual void ResetActor(){
	}

	/**
	 * Updates particle phases in the solver.
	 */
	public virtual void UpdateParticlePhases(){

		if (!InSolver) return;

		for(int i = 0; i < phases.Length; i++){
			phases[i] = Oni.MakePhase(Oni.GetGroupFromPhase(phases[i]),selfCollisions?Oni.ParticlePhase.SelfCollide:0);
		}

		PushDataToSolver(ParticleData.PHASES);
	}

	/**
	 * Adds this actor to a solver. No simulation will take place for this actor
 	 * unless it has been added to a solver. Returns true if the actor was succesfully added,
 	 * false if it was already added or couldn't add it for any other reason.
	 */
	public virtual bool AddToSolver(object info){

		if (solver != null && Initialized && !InSolver){
			
			// Allocate particles in the solver:
			if (!solver.AddActor(this,positions.Length)){
				Debug.LogWarning("Obi: Solver could not allocate enough particles for this actor. Please increase max particles.");
				return false;
			}

			inSolver = true;

			// Update particle phases before sending data to the solver, as layers/flags settings might have changed.
			UpdateParticlePhases();
			
			// find our offset in the deformable triangles array.
			trianglesOffset = Oni.GetDeformableTriangleCount(solver.OniSolver);

			// Send deformable triangle indices to the solver:
			UpdateDeformableTriangles();

			// Send our particle data to the solver:
			PushDataToSolver(ParticleData.ALL);

			// Add constraints to solver:
			LazyBuildConstraintComponentCache();
			foreach (ObiBatchedConstraints c in constraints)
				if (c != null)
					c.AddToSolver(null);

			if (OnAddedToSolver != null)
				OnAddedToSolver(this,new ObiActorSolverArgs(solver));

			return true;
		}
		
		return false;
	}

	public void UpdateDeformableTriangles(){

		// Send deformable triangle indices to the solver:
		int[] solverTriangles = new int[deformableTriangles.Length];
		for (int i = 0; i < deformableTriangles.Length; ++i)
		{
			solverTriangles[i] = particleIndices[deformableTriangles[i]];
		}
		Oni.SetDeformableTriangles(solver.OniSolver,solverTriangles,solverTriangles.Length/3,trianglesOffset);
	}
	
	/**
	 * Removes this actor from its current solver, if any.
	 */
	public virtual bool RemoveFromSolver(object info){

		if (solver != null && InSolver){

			// remove constraints from solver:
			LazyBuildConstraintComponentCache();
			foreach (ObiBatchedConstraints c in constraints)
				if (c != null)
					c.RemoveFromSolver(null);

			// remove rest positions:
			for (int i = 0; i < particleIndices.Length; ++i){
				solver.restPositions[particleIndices[i]] = Vector4.zero;
			}
	
			int index = solver.RemoveActor(this);
			particleIndices = null;

			// update other actor's triangle offset:
			for (int i = index; i < solver.actors.Count; i++){
				solver.actors[i].trianglesOffset -= deformableTriangles.Length/3;
			}	
			// remove triangles:
			Oni.RemoveDeformableTriangles(solver.OniSolver,deformableTriangles.Length/3,trianglesOffset);

			inSolver = false;

			if (OnRemovedFromSolver != null)
				OnRemovedFromSolver(this,new ObiActorSolverArgs(solver));

			return true;
		}
		
		return false;
		
	}

	/**
	 * Sends local particle data to the solver.
	 */
	public virtual void PushDataToSolver(ParticleData data = ParticleData.NONE){

		if (!InSolver) return;

		Matrix4x4 l2sTransform = ActorLocalToSolverMatrix;
		Matrix4x4 l2wTransform = ActorLocalToWorldMatrix;

		Quaternion l2sRotation = l2sTransform.rotation;
		Quaternion l2wRotation = l2wTransform.rotation;

		for (int i = 0; i < particleIndices.Length; i++){
			int k = particleIndices[i];

			if ((data & ParticleData.POSITIONS) != 0 && positions != null && i < positions.Length){
				solver.startPositions[k] = solver.prevPositions[k] = solver.positions[k] = l2sTransform.MultiplyPoint3x4(positions[i]);
				solver.renderablePositions[k] = l2wTransform.MultiplyPoint3x4(positions[i]);
			}		

			if ((data & ParticleData.ORIENTATIONS) != 0 && orientations != null && i < orientations.Length){
				solver.startOrientations[k] = solver.prevOrientations[k] = solver.orientations[k] = l2sRotation * orientations[i];
				solver.renderableOrientations[k] = l2wRotation * orientations[i];
			}

			if ((data & ParticleData.VELOCITIES) != 0 && velocities != null && i < velocities.Length)
				solver.velocities[k] = l2sTransform.MultiplyVector(velocities[i]);

			if ((data & ParticleData.ANGULAR_VELOCITIES) != 0 && angularVelocities != null && i < angularVelocities.Length)
				solver.angularVelocities[k] = l2sTransform.MultiplyVector(angularVelocities[i]);

			if ((data & ParticleData.INV_MASSES) != 0 && invMasses != null && i < invMasses.Length)
				solver.invMasses[k] = invMasses[i];

			if ((data & ParticleData.INV_ROTATIONAL_MASSES) != 0 && invRotationalMasses != null && i < invRotationalMasses.Length)
				solver.invRotationalMasses[k] = invRotationalMasses[i];

			if ((data & ParticleData.PRINCIPAL_RADII) != 0 && principalRadii != null && i < principalRadii.Length)
				solver.principalRadii[k] = principalRadii[i];

			if ((data & ParticleData.PHASES) != 0 && phases != null && i < phases.Length)
				solver.phases[k] = phases[i]; 

			if ((data & ParticleData.REST_POSITIONS) != 0 && restPositions != null && i < restPositions.Length)
				solver.restPositions[k] = restPositions[i];

			if ((data & ParticleData.REST_ORIENTATIONS) != 0 && restOrientations != null && i < restOrientations.Length)
				solver.restOrientations[k] = restOrientations[i]; 

		}

		if ((data & ParticleData.COLLISION_MATERIAL) != 0){
			IntPtr[] materials = new IntPtr[particleIndices.Length];
			for (int i = 0; i < particleIndices.Length; i++) 
				materials[i] = collisionMaterial != null ? collisionMaterial.OniCollisionMaterial : IntPtr.Zero;
			Oni.SetCollisionMaterials(solver.OniSolver,materials,particleIndices,particleIndices.Length);
		}
        
        if ((data & ParticleData.ACTIVE_STATUS) != 0)
			solver.UpdateActiveParticles();

		// Recalculate inertia tensors if needed:
		if ((data & (ParticleData.PRINCIPAL_RADII | ParticleData.INV_ROTATIONAL_MASSES | ParticleData.INV_MASSES)) != 0)
			Oni.RecalculateInertiaTensors(solver.OniSolver);

	}

	/**
	 * Retrieves particle simulation data from the solver. Common uses are
	 * retrieving positions and velocities to set the initial status of the simulation,
 	 * or retrieving solver-generated data such as tensions, densities, etc.
	 */
	public virtual void PullDataFromSolver(ParticleData data = ParticleData.NONE){
		
		if (!InSolver) return;

		Matrix4x4 s2lTransform = ActorLocalToSolverMatrix.inverse;

		Quaternion s2lRotation = s2lTransform.rotation;

		for (int i = 0; i < particleIndices.Length; i++){

			int k = particleIndices[i];

			if ((data & ParticleData.POSITIONS) != 0 && positions != null && i < positions.Length)
				positions[i] = s2lTransform.MultiplyPoint3x4(solver.positions[k]);
			
			if ((data & ParticleData.ORIENTATIONS) != 0 && orientations != null && i < orientations.Length)
				orientations[i] = s2lRotation * solver.orientations[k];
			
			if ((data & ParticleData.VELOCITIES) != 0 && velocities != null && i < velocities.Length)
				velocities[i] = s2lTransform.MultiplyVector(solver.velocities[k]);
			
			if ((data & ParticleData.ANGULAR_VELOCITIES) != 0 && angularVelocities != null && i < angularVelocities.Length)
				angularVelocities[i] = s2lTransform.MultiplyVector(solver.angularVelocities[k]);
			
		}
		
	}

	/**
	 * Returns the position of a particle in world space. 
	 * Works both when the actor is managed by a solver and when it isn't. 
	 */
	public Vector3 GetParticlePosition(int index){
		if (InSolver){
			return solver.renderablePositions[particleIndices[index]];
		}else if (positions != null && index < positions.Length){
			return (ActorLocalToWorldMatrix * initialScaleMatrix.inverse).MultiplyPoint3x4(positions[index]);
		}
		return Vector3.zero;
	}

	/**
	 * Returns the orientation of a particle in world space. 
	 * Works both when the actor is managed by a solver and when it isn't. 
	 */
	public Quaternion GetParticleOrientation(int index){
		if (InSolver){
			return solver.renderableOrientations[particleIndices[index]];
		}else if (orientations != null && index < orientations.Length){
			return (ActorLocalToWorldMatrix * initialScaleMatrix.inverse).rotation * orientations[index];
		}
		return Quaternion.identity;
	}

	public void GetParticleAnisotropy(int index, out Vector4 b1, out Vector4 b2, out Vector4 b3){
		if (InSolver){

			b1 = solver.anisotropies[particleIndices[index]*3];
			b2 = solver.anisotropies[particleIndices[index]*3+1];
			b3 = solver.anisotropies[particleIndices[index]*3+2];

		}else if (orientations != null && index < orientations.Length){

			Quaternion orientation = Quaternion.Inverse((ActorLocalToWorldMatrix * initialScaleMatrix.inverse).rotation * orientations[index]);

			b1 = orientation * Vector3.right;
			b2 = orientation * Vector3.up;
			b3 = orientation * Vector3.forward;

			b1[3] = principalRadii[index][0];
			b2[3] = principalRadii[index][1];
			b3[3] = principalRadii[index][2];

		}else{
			b1 = new Vector4(1,0,0,principalRadii[index][0]);
			b2 = new Vector4(0,1,0,principalRadii[index][0]);
			b3 = new Vector4(0,0,1,principalRadii[index][0]);
		}
	}

	/**
	 * Sets the inverse mass of each particle so that the total actor mass matches the one passed by parameter.
     */
	public void SetMass(float mass){

		float invMass = 1.0f/Mathf.Max(mass/invMasses.Length,0.00001f);

		for (int i = 0; i < invMasses.Length; ++i){
			invMasses[i] = invMass;
			invRotationalMasses[i] = invMass;
		}

		PushDataToSolver(ParticleData.INV_MASSES | ParticleData.INV_ROTATIONAL_MASSES);
	}

	/**
	 * Returns the actor's mass (sum of all particle masses), and the position of its center of mass expressed in solver space. 
	 * Fixed particles are considered to have a mass of 10000 kg.
	 */
	public float GetMass(out Vector3 com){

		float actorMass = 0;
		const float maxMass = 10000;

		if (InSolver){

			Vector4 com4 = Vector4.zero;

			for (int i = 0; i < particleIndices.Length; ++i){

				float mass = maxMass;
				if (solver.invMasses[particleIndices[i]] > 1.0f/maxMass)
                	mass = 1.0f/solver.invMasses[particleIndices[i]];

				actorMass += mass;
				com4 += solver.positions[particleIndices[i]] * mass;

			}

			com = com4;
			if (actorMass > float.Epsilon)
				com /= actorMass;

		}else{

			com = Vector3.zero;

			for (int i = 0; i < invMasses.Length; ++i){

				float mass = maxMass;
				if (invMasses[i] > 1.0f/maxMass)
                	mass = 1.0f/invMasses[i];

				actorMass += mass;
				com += positions[i] * mass;
			}

			if (actorMass > float.Epsilon)
				com /= actorMass;

		}


		return actorMass;
	}

	/**
	 * Adds a force to the actor. The force should be expressed in solver space.
     */
	public void AddForce(Vector3 force,ForceMode forceMode){

		Vector3 com;
		float mass = GetMass(out com);

		if (!float.IsInfinity(mass)){

			Vector4 bodyForce = force;
	
			switch(forceMode){
				case ForceMode.Force:{

					bodyForce /= mass;

					for (int i = 0; i < particleIndices.Length; ++i)
						solver.externalForces[particleIndices[i]] += bodyForce / solver.invMasses[particleIndices[i]];

				}break;
				case ForceMode.Acceleration:{

					for (int i = 0; i < particleIndices.Length; ++i)
						solver.externalForces[particleIndices[i]] += bodyForce / solver.invMasses[particleIndices[i]];

				}break;
				case ForceMode.Impulse:{

					bodyForce /= mass;

					for (int i = 0; i < particleIndices.Length; ++i)
						solver.externalForces[particleIndices[i]] += bodyForce / solver.invMasses[particleIndices[i]] / Time.fixedDeltaTime;

				}break;
				case ForceMode.VelocityChange:{

					for (int i = 0; i < particleIndices.Length; ++i)
						solver.externalForces[particleIndices[i]] += bodyForce / solver.invMasses[particleIndices[i]] / Time.fixedDeltaTime;

				}break;
			}
		}
	}

	/**
	 * Adds a torque to the actor. The torque should be expressed in solver space.
     */
	public void AddTorque(Vector3 force,ForceMode forceMode){

		Vector3 com;
		float mass = GetMass(out com);

		if (!float.IsInfinity(mass)){

			Vector3 bodyForce = force;
	
			switch(forceMode){
				case ForceMode.Force:{

					bodyForce /= mass;

					for (int i = 0; i < particleIndices.Length; ++i){

						Vector3 v = Vector3.Cross(bodyForce / solver.invMasses[particleIndices[i]], (Vector3)solver.positions[particleIndices[i]] - com);
						solver.externalForces[particleIndices[i]] += new Vector4(v.x,v.y,v.z,0);
					}

				}break;
				case ForceMode.Acceleration:{

					for (int i = 0; i < particleIndices.Length; ++i){

						Vector3 v = Vector3.Cross(bodyForce / solver.invMasses[particleIndices[i]], (Vector3)solver.positions[particleIndices[i]] - com);
						solver.externalForces[particleIndices[i]] += new Vector4(v.x,v.y,v.z,0);
					}

				}break;
				case ForceMode.Impulse:{

					bodyForce /= mass;

					for (int i = 0; i < particleIndices.Length; ++i){

						Vector3 v = Vector3.Cross(bodyForce / solver.invMasses[particleIndices[i]] / Time.fixedDeltaTime, (Vector3)solver.positions[particleIndices[i]] - com);
						solver.externalForces[particleIndices[i]] += new Vector4(v.x,v.y,v.z,0);
					}

				}break;
				case ForceMode.VelocityChange:{

					for (int i = 0; i < particleIndices.Length; ++i){

						Vector3 v = Vector3.Cross(bodyForce / solver.invMasses[particleIndices[i]] / Time.fixedDeltaTime ,(Vector3)solver.positions[particleIndices[i]] - com);
						solver.externalForces[particleIndices[i]] += new Vector4(v.x,v.y,v.z,0);
					}

				}break;
			}
		}
	}

	/**
	 * Sets the phase for all particles in the actor.
	 */
	public void SetPhase(int phase){

		if (phases != null){

			for (int i = 0; i < phases.Length; ++i)
				phases[i] = phase;
	
			UpdateParticlePhases();
		}
	}

	public virtual bool GenerateTethers(){
		return true;
	}

	public void ClearTethers(){
		if (constraints[(int)Oni.ConstraintType.Tether] != null){
			((ObiTetherConstraints)constraints[(int)Oni.ConstraintType.Tether]).Clear();
		}
	}

	/**
	 * Transforms the position of fixed particles from local space to simulation space and feeds them
	 * to the solver. This should be performed just before starting each frame's simulation.
	 */
	public void UpdateFixedParticles(){

		// check if any of the involved transforms has changed since last time:
		if (!transform.hasChanged && !Solver.transform.hasChanged)
			return;

		transform.hasChanged = false;
		Solver.transform.hasChanged = false;

		// apparently checking whether the actor is enabled or not doesn't take a despreciable amount of time.
		bool actorEnabled = this.enabled;
		int particleCount = particleIndices.Length;

		// build local to simulation space transform:
		Matrix4x4 l2sTransform = ActorLocalToSolverMatrix;

		// set particle position (both prev position and current one, as fixed particles inherit interpolation from the actor transform.)
		for(int i = 0; i < particleCount; i++){
			if (!actorEnabled || invMasses[i] == 0){
				solver.positions[particleIndices[i]] = l2sTransform.MultiplyPoint3x4(positions[i]);
			}
		}

		// set particle orientations:
		if (UsesOrientedParticles){

			Quaternion l2sRotation = l2sTransform.rotation;

			for(int i = 0; i < particleCount; i++){
				if (!actorEnabled || invRotationalMasses[i] == 0){
					solver.orientations[particleIndices[i]] = l2sRotation * restOrientations[i];
				}
			}
		}
	}

	public virtual void OnSolverPreInterpolation(){
	}

	public virtual void OnSolverStepBegin(){
		UpdateFixedParticles();
	}

	public virtual void OnSolverStepEnd(float deltaTime){
	}

	public virtual void OnSolverFrameBegin(bool fixedUpdate){
	}

	
	public virtual void OnSolverFrameEnd(float deltaTime){	
    }

	public virtual void OnSolverVisibilityChanged(bool visible){
	}

	public virtual bool ReadParticlePropertyFromTexture(Texture2D source,System.Action<int,Color> onReadProperty){return false;}
}
}

