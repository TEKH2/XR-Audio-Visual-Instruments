/**
\mainpage Obi documentation
 
Introduction:
------------- 

Obi is a position-based dynamics framework for unity. It enables the simulation of cloth, ropes and fluid in realtime, complete with two-way
rigidbody interaction.
 
Features:
-------------------

- Particles can be pinned both in local space and to rigidbodies (kinematic or not).
- Realistic wind forces.
- Rigidbodies react to particle dynamics, and particles reach to each other and to rigidbodies too.
- Easy prefab instantiation, particle-based actors can be translated, scaled and rotated.
- Simulation can be warm-started in the editor, then all simulation state gets serialized with the object. This means
  your prefabs can be stored at any point in the simulation, and they will resume it when instantiated.
- Custom editor tools.

*/

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Runtime.InteropServices;
using System.Linq;

namespace Obi
{

/**
 * An ObiSolver component simulates particles and their interactions using the Oni unified physics library.
 * Several kinds of constraint types and their parameters are exposed, and several Obi components can
 * be used to feed particles and constraints to the solver.
 */
[ExecuteInEditMode]
[AddComponentMenu("Physics/Obi/Obi Solver")]
[DisallowMultipleComponent]
public sealed class ObiSolver : MonoBehaviour
{

	public enum UpdateMode{
		FixedUpdate,
		AfterFixedUpdate,
		LateUpdate
	}

	public class ObiCollisionEventArgs : System.EventArgs{
		public ObiList<Oni.Contact> contacts = new ObiList<Oni.Contact>();	/**< collision contacts.*/
	}

	public class ParticleInActor{
		public ObiActor actor;
		public int indexInActor;

		public ParticleInActor(ObiActor actor, int indexInActor){
			this.actor = actor;
			this.indexInActor = indexInActor;
		}
	}

	public static event System.EventHandler OnUpdateColliders;
	public static event System.EventHandler OnAfterUpdateColliders;
	public static event System.EventHandler OnUpdateRigidbodies;

	public event System.EventHandler OnFrameBegin;
	public event System.EventHandler OnStepBegin;
	public event System.EventHandler OnFixedParticlesUpdated;
	public event System.EventHandler OnStepEnd;
	public event System.EventHandler OnBeforePositionInterpolation;
	public event System.EventHandler OnBeforeActorsFrameEnd;
	public event System.EventHandler OnFrameEnd;
	public event System.EventHandler<ObiCollisionEventArgs> OnCollision;
	public event System.EventHandler<ObiCollisionEventArgs> OnParticleCollision;
	public event System.EventHandler OnUpdateParameters;
	
	public int maxParticles = 5000;

	[HideInInspector] [NonSerialized] public bool simulate = true;

	public uint substeps = 1;

	[Tooltip("If enabled, will force the solver to keep simulating even when not visible from any camera.")]
	public bool simulateWhenInvisible = true; 			/**< Whether to keep simulating the cloth when its not visible by any camera.*/

	[Tooltip("If enabled, the solver object transform will be used as the frame of reference for all actors using this solver, instead of the world's frame.")]
	public bool simulateInLocalSpace = false;

	[Indent()]
	[VisibleIf("simulateInLocalSpace")]
	public float worldLinearVelocityScale = 0;			/**< how much does world-space linear velocity affect the actor. This only applies when the solver has "simulateInLocalSpace" enabled.*/

	[Indent()]
	[VisibleIf("simulateInLocalSpace")]
	public float worldAngularVelocityScale = 0;			/**< how much does world-space angular velocity affect the actor. This only applies when the solver has "simulateInLocalSpace" enabled.*/

	[Indent()]
	[VisibleIf("simulateInLocalSpace")]
	public float worldLinearInertiaScale = 0;			/**< how much does world-space linear inertia affect the actor. This only applies when the solver has "simulateInLocalSpace" enabled.*/

	[Indent()]
	[VisibleIf("simulateInLocalSpace")]
	public float worldAngularInertiaScale = 0;			/**< how much does world-space angular inertia affect the actor. This only applies when the solver has "simulateInLocalSpace" enabled.*/


	[Tooltip("Determines when will the solver update particles.")]
	[SerializeProperty("UpdateOrder")]
	[SerializeField] private UpdateMode updateMode = UpdateMode.FixedUpdate;

	[ChildrenOnly]
	public Oni.SolverParameters parameters = new Oni.SolverParameters(Oni.SolverParameters.Interpolation.None,
	                                                                  new Vector4(0,-9.81f,0,0));

	[HideInInspector] [NonSerialized] public List<ObiActor> actors = new List<ObiActor>();

	private int allocatedParticleCount = 0;
	[HideInInspector] [NonSerialized] public ParticleInActor[] particleToActor;
	[HideInInspector] [NonSerialized] public int[] materialIndices;
	[HideInInspector] [NonSerialized] public int[] fluidMaterialIndices;

	private int[] activeParticles;

	// positions
	[HideInInspector] [NonSerialized] public AlignedVector4Array positions;
	[HideInInspector] [NonSerialized] public AlignedVector4Array restPositions;
	[HideInInspector] [NonSerialized] public AlignedVector4Array prevPositions;
	[HideInInspector] [NonSerialized] public AlignedVector4Array startPositions;
	[HideInInspector] [NonSerialized] public AlignedVector4Array renderablePositions;	 

	// orientations
	[HideInInspector] [NonSerialized] public AlignedQuaternionArray orientations;
	[HideInInspector] [NonSerialized] public AlignedQuaternionArray restOrientations;
	[HideInInspector] [NonSerialized] public AlignedQuaternionArray prevOrientations;
	[HideInInspector] [NonSerialized] public AlignedQuaternionArray startOrientations;
	[HideInInspector] [NonSerialized] public AlignedQuaternionArray renderableOrientations; /**< renderable particle orientations.*/

	// velocities
	[HideInInspector] [NonSerialized] public AlignedVector4Array velocities;
	[HideInInspector] [NonSerialized] public AlignedVector4Array angularVelocities;

	// masses/inertia tensors
	[HideInInspector] [NonSerialized] public AlignedFloatArray invMasses;
	[HideInInspector] [NonSerialized] public AlignedFloatArray invRotationalMasses;
	[HideInInspector] [NonSerialized] public AlignedVector4Array invInertiaTensors;

	// external forces
	[HideInInspector] [NonSerialized] public AlignedVector4Array externalForces;
	[HideInInspector] [NonSerialized] public AlignedVector4Array externalTorques;
	[HideInInspector] [NonSerialized] public AlignedVector4Array wind;

	// deltas
	[HideInInspector] [NonSerialized] public AlignedVector4Array positionDeltas;
	[HideInInspector] [NonSerialized] public AlignedQuaternionArray orientationDeltas;
	[HideInInspector] [NonSerialized] public AlignedIntArray positionConstraintCounts;
	[HideInInspector] [NonSerialized] public AlignedIntArray orientationConstraintCounts;

	// particle phase / shape
	[HideInInspector] [NonSerialized] public AlignedIntArray phases;
	[HideInInspector] [NonSerialized] public AlignedVector4Array anisotropies;
	[HideInInspector] [NonSerialized] public AlignedVector4Array principalRadii;
	[HideInInspector] [NonSerialized] public AlignedVector4Array normals;

	// fluids
	[HideInInspector] [NonSerialized] public AlignedVector4Array vorticities;
	[HideInInspector] [NonSerialized] public AlignedVector4Array fluidData;
	[HideInInspector] [NonSerialized] public AlignedVector4Array userData;
	[HideInInspector] [NonSerialized] public AlignedFloatArray smoothingRadii;
	[HideInInspector] [NonSerialized] public AlignedFloatArray buoyancies;
	[HideInInspector] [NonSerialized] public AlignedFloatArray restDensities;
	[HideInInspector] [NonSerialized] public AlignedFloatArray viscosities;
	[HideInInspector] [NonSerialized] public AlignedFloatArray surfaceTension;
	[HideInInspector] [NonSerialized] public AlignedFloatArray vortConfinement;
	[HideInInspector] [NonSerialized] public AlignedFloatArray atmosphericDrag;
	[HideInInspector] [NonSerialized] public AlignedFloatArray atmosphericPressure;
	[HideInInspector] [NonSerialized] public AlignedFloatArray diffusion;
	
	// constraint parameters:
	[Header("Constraints")]
	public Oni.ConstraintParameters distanceConstraintParameters = new Oni.ConstraintParameters(true,Oni.ConstraintParameters.EvaluationOrder.Sequential,3);
	public Oni.ConstraintParameters bendingConstraintParameters = new Oni.ConstraintParameters(true,Oni.ConstraintParameters.EvaluationOrder.Parallel,3);
	public Oni.ConstraintParameters particleCollisionConstraintParameters = new Oni.ConstraintParameters(true,Oni.ConstraintParameters.EvaluationOrder.Sequential,3);
	public Oni.ConstraintParameters particleFrictionConstraintParameters = new Oni.ConstraintParameters(true,Oni.ConstraintParameters.EvaluationOrder.Parallel,3);
	public Oni.ConstraintParameters collisionConstraintParameters = new Oni.ConstraintParameters(true,Oni.ConstraintParameters.EvaluationOrder.Sequential,3);
	public Oni.ConstraintParameters frictionConstraintParameters = new Oni.ConstraintParameters(true,Oni.ConstraintParameters.EvaluationOrder.Parallel,3);
	public Oni.ConstraintParameters skinConstraintParameters = new Oni.ConstraintParameters(true,Oni.ConstraintParameters.EvaluationOrder.Sequential,3);
	public Oni.ConstraintParameters volumeConstraintParameters = new Oni.ConstraintParameters(true,Oni.ConstraintParameters.EvaluationOrder.Parallel,3);
	public Oni.ConstraintParameters shapeMatchingConstraintParameters = new Oni.ConstraintParameters(true,Oni.ConstraintParameters.EvaluationOrder.Parallel,3);
	public Oni.ConstraintParameters tetherConstraintParameters = new Oni.ConstraintParameters(true,Oni.ConstraintParameters.EvaluationOrder.Parallel,3);
	public Oni.ConstraintParameters pinConstraintParameters = new Oni.ConstraintParameters(true,Oni.ConstraintParameters.EvaluationOrder.Parallel,3);
	public Oni.ConstraintParameters stitchConstraintParameters = new Oni.ConstraintParameters(true,Oni.ConstraintParameters.EvaluationOrder.Parallel,2);
	public Oni.ConstraintParameters densityConstraintParameters = new Oni.ConstraintParameters(true,Oni.ConstraintParameters.EvaluationOrder.Parallel,2);
	public Oni.ConstraintParameters stretchShearConstraintParameters = new Oni.ConstraintParameters(true,Oni.ConstraintParameters.EvaluationOrder.Sequential,3);
	public Oni.ConstraintParameters bendTwistConstraintParameters = new Oni.ConstraintParameters(true,Oni.ConstraintParameters.EvaluationOrder.Sequential,3);
	public Oni.ConstraintParameters chainConstraintParameters = new Oni.ConstraintParameters(false,Oni.ConstraintParameters.EvaluationOrder.Sequential,3);

	private IntPtr oniSolver; 

	private ObiCollisionEventArgs collisionArgs = new ObiCollisionEventArgs();
	private ObiCollisionEventArgs particleCollisionArgs = new ObiCollisionEventArgs();

	private ObiEmitterMaterial defaultFluidMaterial;
	private UnityEngine.Bounds bounds = new UnityEngine.Bounds();
	private Plane[] planes = new Plane[6];
	private Camera[] sceneCameras = new Camera[1];
 
 	private bool initialized = false;
	private bool isVisible = true;
	private float lastStepDelta = 0;
	private float smoothDelta = 0.02f;

	private static bool frameBegan = false;

	public static ObiArbiter fixedUpdateArbiter = new ObiArbiter();
	public static ObiArbiter afterFixedUpdateArbiter = new ObiArbiter();
	public static ObiArbiter lateUpdateArbiter = new ObiArbiter();

	public IntPtr OniSolver
	{
		get{return oniSolver;}
	}

	public UnityEngine.Bounds Bounds
	{
		get{return bounds;}
	}

	public bool IsVisible
	{
		get{return isVisible;}
	}

	public UpdateMode UpdateOrder{
		set{
			if (updateMode != value){

				switch(updateMode){
					case UpdateMode.FixedUpdate: fixedUpdateArbiter.UnregisterSolver(this); break;	
					case UpdateMode.AfterFixedUpdate: afterFixedUpdateArbiter.UnregisterSolver(this); break;
					case UpdateMode.LateUpdate: lateUpdateArbiter.UnregisterSolver(this); break;
				}
				
				updateMode = value;

				switch(updateMode){
					case UpdateMode.FixedUpdate: fixedUpdateArbiter.RegisterSolver(this); break;	
					case UpdateMode.AfterFixedUpdate: afterFixedUpdateArbiter.RegisterSolver(this); break;
					case UpdateMode.LateUpdate: lateUpdateArbiter.RegisterSolver(this); break;
				}
			}
		}
		get{return updateMode;}
	}

	public int AllocParticleCount{
		get{return allocatedParticleCount;}
	}

	public bool IsUpdating{
		get{return (initialized && simulate && (simulateWhenInvisible || IsVisible));}
	}

	void OnValidate(){
		if (substeps < 1) substeps = 1;
	}

	void Awake(){

		if (Application.isPlaying) //only during game.
			Initialize();
	}

	void OnDestroy(){
		if (Application.isPlaying) //only during game.
			Teardown();
	}

	void OnEnable(){
		if (!Application.isPlaying) //only in editor.
			Initialize();

		StartCoroutine("RunLateFixedUpdate");

		switch(updateMode){
			case UpdateMode.FixedUpdate: fixedUpdateArbiter.RegisterSolver(this); break;	
			case UpdateMode.AfterFixedUpdate: afterFixedUpdateArbiter.RegisterSolver(this); break;
			case UpdateMode.LateUpdate: lateUpdateArbiter.RegisterSolver(this); break;
		}		
	}
	
	void OnDisable(){
		if (!Application.isPlaying) //only in editor.
			Teardown();
		StopCoroutine("RunLateFixedUpdate");
		switch(updateMode){
			case UpdateMode.FixedUpdate: fixedUpdateArbiter.UnregisterSolver(this); break;	
			case UpdateMode.AfterFixedUpdate: afterFixedUpdateArbiter.UnregisterSolver(this); break;
			case UpdateMode.LateUpdate: lateUpdateArbiter.UnregisterSolver(this); break;
		}
	}
	
	public void Initialize(){

		// Tear everything down first:
		Teardown();
			
		try{

			// Create a default material:
			defaultFluidMaterial = ScriptableObject.CreateInstance<ObiEmitterMaterialFluid>();
			defaultFluidMaterial.hideFlags = HideFlags.HideAndDontSave;
	
			// Create the Oni solver:
			oniSolver = Oni.CreateSolver(maxParticles);

			// Initialize moving transform:
			InitializeTransformFrame();
			
			actors = new List<ObiActor>();
			activeParticles = new int[maxParticles];
			particleToActor = new ParticleInActor[maxParticles];
			materialIndices = new int[maxParticles];
			fluidMaterialIndices = new int[maxParticles];

			positions = new AlignedVector4Array(maxParticles);
			Oni.SetParticlePositions(oniSolver, positions.GetIntPtr());

			startPositions = new AlignedVector4Array(maxParticles);
			Oni.SetParticleStartPositions(oniSolver, startPositions.GetIntPtr());

			prevPositions = new AlignedVector4Array(maxParticles);
			Oni.SetParticlePreviousPositions(oniSolver, prevPositions.GetIntPtr());

			restPositions = new AlignedVector4Array(maxParticles);
			Oni.SetRestPositions(oniSolver, restPositions.GetIntPtr());

			velocities = new AlignedVector4Array(maxParticles);
			Oni.SetParticleVelocities(oniSolver, velocities.GetIntPtr());

			orientations = new AlignedQuaternionArray(maxParticles,Quaternion.identity);
			Oni.SetParticleOrientations(oniSolver, orientations.GetIntPtr());

			startOrientations = new AlignedQuaternionArray(maxParticles,Quaternion.identity);
			Oni.SetParticleStartOrientations(oniSolver, startOrientations.GetIntPtr());

			prevOrientations = new AlignedQuaternionArray(maxParticles,Quaternion.identity);
			Oni.SetParticlePreviousOrientations(oniSolver, prevOrientations.GetIntPtr());

			restOrientations = new AlignedQuaternionArray(maxParticles,Quaternion.identity);
			Oni.SetRestOrientations(oniSolver, restOrientations.GetIntPtr());

			angularVelocities = new AlignedVector4Array(maxParticles);
			Oni.SetParticleAngularVelocities(oniSolver, angularVelocities.GetIntPtr());

			invMasses = new AlignedFloatArray(maxParticles);
			Oni.SetParticleInverseMasses(oniSolver, invMasses.GetIntPtr());

			invRotationalMasses = new AlignedFloatArray(maxParticles);
			Oni.SetParticleInverseRotationalMasses(oniSolver, invRotationalMasses.GetIntPtr());

			principalRadii = new AlignedVector4Array(maxParticles);
			Oni.SetParticlePrincipalRadii(oniSolver, principalRadii.GetIntPtr());

			phases = new AlignedIntArray(maxParticles);
			Oni.SetParticlePhases(oniSolver, phases.GetIntPtr());

			renderablePositions = new AlignedVector4Array(maxParticles);
			Oni.SetRenderableParticlePositions(oniSolver, renderablePositions.GetIntPtr());

			renderableOrientations = new AlignedQuaternionArray(maxParticles,Quaternion.identity);
			Oni.SetRenderableParticleOrientations(oniSolver, renderableOrientations.GetIntPtr());

			anisotropies = new AlignedVector4Array(maxParticles*3);
			Oni.SetParticleAnisotropies(oniSolver, anisotropies.GetIntPtr());

			smoothingRadii = new AlignedFloatArray(maxParticles);
			Oni.SetParticleSmoothingRadii(oniSolver, smoothingRadii.GetIntPtr());

			buoyancies = new AlignedFloatArray(maxParticles);
			Oni.SetParticleBuoyancy(oniSolver, buoyancies.GetIntPtr());

			restDensities = new AlignedFloatArray(maxParticles);
			Oni.SetParticleRestDensities(oniSolver, restDensities.GetIntPtr());

			viscosities = new AlignedFloatArray(maxParticles);
			Oni.SetParticleViscosities(oniSolver, viscosities.GetIntPtr());

			surfaceTension = new AlignedFloatArray(maxParticles);
			Oni.SetParticleSurfaceTension(oniSolver, surfaceTension.GetIntPtr());

			vortConfinement = new AlignedFloatArray(maxParticles);
			Oni.SetParticleVorticityConfinement(oniSolver, vortConfinement.GetIntPtr());

			atmosphericDrag = new AlignedFloatArray(maxParticles);
			atmosphericPressure = new AlignedFloatArray(maxParticles);
			Oni.SetParticleAtmosphericDragPressure(oniSolver, atmosphericDrag.GetIntPtr(), atmosphericPressure.GetIntPtr());

			diffusion = new AlignedFloatArray(maxParticles);
			Oni.SetParticleDiffusion(oniSolver, diffusion.GetIntPtr());

			vorticities = new AlignedVector4Array(maxParticles);
			Oni.SetParticleVorticities(oniSolver, vorticities.GetIntPtr());

			fluidData = new AlignedVector4Array(maxParticles);
			Oni.SetParticleFluidData(oniSolver, fluidData.GetIntPtr());

			userData = new AlignedVector4Array(maxParticles);
			Oni.SetParticleUserData(oniSolver, userData.GetIntPtr());

			externalForces = new AlignedVector4Array(maxParticles);
			Oni.SetParticleExternalForces(oniSolver, externalForces.GetIntPtr()); 

			externalTorques = new AlignedVector4Array(maxParticles);
			Oni.SetParticleExternalTorques(oniSolver, externalTorques.GetIntPtr());

			wind = new AlignedVector4Array(maxParticles);
			Oni.SetParticleWinds(oniSolver, wind.GetIntPtr());

			positionDeltas = new AlignedVector4Array(maxParticles);
			Oni.SetParticlePositionDeltas(oniSolver, positionDeltas.GetIntPtr());

			orientationDeltas = new AlignedQuaternionArray(maxParticles,new Quaternion(0,0,0,0));
			Oni.SetParticleOrientationDeltas(oniSolver, orientationDeltas.GetIntPtr());

			positionConstraintCounts = new AlignedIntArray(maxParticles);
			Oni.SetParticlePositionConstraintCounts(oniSolver, positionConstraintCounts.GetIntPtr());

			orientationConstraintCounts  = new AlignedIntArray(maxParticles);
			Oni.SetParticleOrientationConstraintCounts(oniSolver, orientationConstraintCounts.GetIntPtr());

			normals = new AlignedVector4Array(maxParticles);
			Oni.SetParticleNormals(oniSolver, normals.GetIntPtr());

			invInertiaTensors = new AlignedVector4Array(maxParticles);
			Oni.SetParticleInverseInertiaTensors(oniSolver, invInertiaTensors.GetIntPtr());
			
			// Initialize parameters:
			UpdateParameters();
			
		}catch (Exception exception){
			Debug.LogException(exception);
		}finally{
			initialized = true;
		};

	}

	private void Teardown(){
	
		if (!initialized) return;
		
		try{

			while (actors.Count > 0){
				actors[actors.Count-1].RemoveFromSolver(null);
			}
				
			Oni.DestroySolver(oniSolver);
			oniSolver = IntPtr.Zero;

			GameObject.DestroyImmediate(defaultFluidMaterial);
		
		}catch (Exception exception){
			Debug.LogException(exception);
		}finally{
			initialized = false;
		}
	}

	private void InitializeTransformFrame(){
		if (simulateInLocalSpace){
			Vector4 translation = transform.position;
			Vector4 scale = transform.lossyScale;
			Quaternion rotation = transform.rotation;
			Oni.InitializeFrame(this.oniSolver,ref translation,ref scale, ref rotation);
		}else{
			Vector4 translation = Vector4.zero;
			Vector4 scale = Vector4.one;
			Quaternion rotation = Quaternion.identity;
			Oni.InitializeFrame(this.oniSolver,ref translation,ref scale, ref rotation);
		}
	}

	private void UpdateTransformFrame(float dt){
		if (simulateInLocalSpace){
			Vector4 translation = transform.position;
			Vector4 scale = transform.lossyScale;
			Quaternion rotation = transform.rotation;
			Oni.UpdateFrame(this.oniSolver,ref translation,ref scale, ref rotation,dt);
			Oni.ApplyFrame(this.oniSolver,worldLinearVelocityScale,worldAngularVelocityScale,worldLinearInertiaScale,worldAngularInertiaScale,dt);
		}else{
			Vector4 translation = Vector4.zero;
			Vector4 scale = Vector4.one;
			Quaternion rotation = Quaternion.identity;
			Oni.InitializeFrame(this.oniSolver,ref translation,ref scale, ref rotation);
		}
	}

	/**
	 * Adds the actor to this solver. Will return whether the allocation was sucessful or not.
	 */
	public bool AddActor(ObiActor actor, int numParticles){

		if (particleToActor == null || actor == null)
			return false;

		int[] allocated = new int[numParticles];
		int allocatedCount = 0;

		for (int i = 0; i < maxParticles && allocatedCount < numParticles; i++){
			if (particleToActor[i] == null){
				allocated[allocatedCount] = i;
				allocatedCount++;
			}
		}

		// could not allocate enough particles.
		if (allocatedCount < numParticles){
			return false; 
		}

		allocatedParticleCount += numParticles;

		// store per-particle actor reference:
		for (int i = 0; i < numParticles; ++i)
			particleToActor[allocated[i]] = new ParticleInActor(actor,i);

		// set the actor particle indices.
		actor.particleIndices = allocated;

        // Add the actor to the actor list:
		actors.Add(actor);

		// Update active particles. Update materials, in case the actor has a new one.
		UpdateActiveParticles();  
       
		return true;

	}

	/**
	 * Removes an actor from this solver. Returns the index that was occupied by the actor in the actor list, or -1 if it was not managed by this solver.
	 */
	public int RemoveActor(ObiActor actor){
		
		if (particleToActor == null || actor == null)
			return -1;

		// Find actor index in our actors array:
		int index = actors.IndexOf(actor);

		// If we are in charce of this actor indeed, perform all steps necessary to release it.
		if (index > -1){

			allocatedParticleCount -= actor.particleIndices.Length;

			for (int i = 0; i < actor.particleIndices.Length; ++i)
				particleToActor[actor.particleIndices[i]] = null;
	
			actors.RemoveAt(index); 
	
			// Update active particles. Update materials, in case the actor had one.
			UpdateActiveParticles(); 

		}
		
		return index;
	}

	/**
	 * Updates solver parameters, sending them to the Oni library.
	 */
	public void UpdateParameters(){

		Oni.SetSolverParameters(oniSolver,ref parameters);

		Oni.SetConstraintGroupParameters(oniSolver,(int)Oni.ConstraintType.Distance,ref distanceConstraintParameters);
		
		Oni.SetConstraintGroupParameters(oniSolver,(int)Oni.ConstraintType.Bending,ref bendingConstraintParameters);
	
		Oni.SetConstraintGroupParameters(oniSolver,(int)Oni.ConstraintType.ParticleCollision,ref particleCollisionConstraintParameters);

		Oni.SetConstraintGroupParameters(oniSolver,(int)Oni.ConstraintType.ParticleFriction,ref particleFrictionConstraintParameters);

		Oni.SetConstraintGroupParameters(oniSolver,(int)Oni.ConstraintType.Collision,ref collisionConstraintParameters);

		Oni.SetConstraintGroupParameters(oniSolver,(int)Oni.ConstraintType.Friction,ref frictionConstraintParameters);

		Oni.SetConstraintGroupParameters(oniSolver,(int)Oni.ConstraintType.Density,ref densityConstraintParameters);
		
		Oni.SetConstraintGroupParameters(oniSolver,(int)Oni.ConstraintType.Skin,ref skinConstraintParameters);
		
		Oni.SetConstraintGroupParameters(oniSolver,(int)Oni.ConstraintType.Volume,ref volumeConstraintParameters);

		Oni.SetConstraintGroupParameters(oniSolver,(int)Oni.ConstraintType.ShapeMatching,ref shapeMatchingConstraintParameters);
		
		Oni.SetConstraintGroupParameters(oniSolver,(int)Oni.ConstraintType.Tether,ref tetherConstraintParameters);
	
		Oni.SetConstraintGroupParameters(oniSolver,(int)Oni.ConstraintType.Pin,ref pinConstraintParameters);

		Oni.SetConstraintGroupParameters(oniSolver,(int)Oni.ConstraintType.Stitch,ref stitchConstraintParameters);

		Oni.SetConstraintGroupParameters(oniSolver,(int)Oni.ConstraintType.StretchShear,ref stretchShearConstraintParameters);

		Oni.SetConstraintGroupParameters(oniSolver,(int)Oni.ConstraintType.BendTwist,ref bendTwistConstraintParameters);

		Oni.SetConstraintGroupParameters(oniSolver,(int)Oni.ConstraintType.Chain,ref chainConstraintParameters);

		if (OnUpdateParameters != null)
			OnUpdateParameters(this,EventArgs.Empty);

    }

	/**
	 * Updates the active particles array.
	 */
	public void UpdateActiveParticles(){

		int numActive = 0;

		for (int i = 0; i < actors.Count; ++i){

			ObiActor currentActor = actors[i];

			if (currentActor.isActiveAndEnabled){
				for (int j = 0; j < currentActor.particleIndices.Length; ++j){
					if (currentActor.active[j]){
						activeParticles[numActive] = currentActor.particleIndices[j];
						numActive++;
					}
				}
			}
		}	

		Oni.SetActiveParticles(oniSolver,activeParticles,numActive);

	}

	public void AccumulateSimulationTime(float dt){
		Oni.AddSimulationTime(oniSolver,dt);
	}

	public void ResetSimulationTime(){
		Oni.ResetSimulationTime(oniSolver);
	}

	public void SimulateStep(float stepTime){

		if (IsUpdating){

			// store last step delta.
			lastStepDelta = stepTime;

			UpdateColliders();
	
			if (OnStepBegin != null)
				OnStepBegin(this,EventArgs.Empty);
	
			foreach(ObiActor actor in actors)
	            actor.OnSolverStepBegin();
	
			// Trigger event right after actors have fixed their particles in OnSolverStepBegin.
			if (OnFixedParticlesUpdated != null)
				OnFixedParticlesUpdated(this,EventArgs.Empty);
	
			// Update the solver (this is internally split in tasks so multiple solvers can be updated in parallel)
			Oni.UpdateSolver(oniSolver, substeps, stepTime/(float)substeps); 
	
			// Wait here for all other solvers to finish:
		 	WaitForAllSolvers();

		}
	} 

	public void SignalBeginFrame(){
		EventHandler temp = OnFrameBegin;
        if (temp != null)
        {
			temp (this,null);
        }
	}

	/**
	 * This is called to signal the beginning of a frame. If the frame begins with a FixedUpdate(), then the boolean parameter will be true. If
	 * the frame starts in Update() (no physics performed this frame) it will be passed false.
	 */
	void BeginFrame(bool fixedUpdate){

		if (!frameBegan){

			frameBegan = true;

			// Start profiling this frame:
			if (ObiProfiler.Instance != null)
				ObiProfiler.Instance.StartStep();
	
			fixedUpdateArbiter.BeginFrame(fixedUpdate);
			afterFixedUpdateArbiter.BeginFrame(fixedUpdate);
			lateUpdateArbiter.BeginFrame(fixedUpdate);
		}
	}

	public void EndFrame(float stepTime){

		if (OnBeforePositionInterpolation != null)
			OnBeforePositionInterpolation(this,EventArgs.Empty);

		foreach(ObiActor actor in actors)
            actor.OnSolverPreInterpolation();

		// Note: using interpolation together with animator-driven colliders will cause sinking, as particle positions go 1-frame behind.
		Oni.UpdateSkeletalAnimation(oniSolver);

		Oni.ApplyPositionInterpolation(oniSolver, substeps, stepTime/(float)substeps);

		if (OnBeforeActorsFrameEnd != null)
			OnBeforeActorsFrameEnd(this,EventArgs.Empty);

		UpdateVisibility();
		
		foreach(ObiActor actor in actors)
            actor.OnSolverFrameEnd(stepTime);

		if (OnFrameEnd != null)
			OnFrameEnd(this,EventArgs.Empty);

		// Reset frame started flag for the next frame:
		frameBegan = false;

	}

	private void TriggerCollisionEvents(){
	
		if (OnCollision != null){

			int numCollisions = Oni.GetConstraintCount(oniSolver,(int)Oni.ConstraintType.Collision);
			collisionArgs.contacts.SetCount(numCollisions);

			if (numCollisions > 0)
				Oni.GetCollisionContacts(oniSolver,collisionArgs.contacts.Data,numCollisions);

			OnCollision(this,collisionArgs);

		}

		if (OnParticleCollision != null){

			int numCollisions = Oni.GetConstraintCount(oniSolver,(int)Oni.ConstraintType.ParticleCollision);
			particleCollisionArgs.contacts.SetCount(numCollisions);

			if (numCollisions > 0)
				Oni.GetParticleCollisionContacts(oniSolver,particleCollisionArgs.contacts.Data,numCollisions);

			OnParticleCollision(this,particleCollisionArgs);

		}
	}

	private bool AreBoundsValid(Bounds bounds){
		return !(float.IsNaN(bounds.center.x) || float.IsInfinity(bounds.center.x) ||
			     float.IsNaN(bounds.center.y) || float.IsInfinity(bounds.center.y) ||
			     float.IsNaN(bounds.center.z) || float.IsInfinity(bounds.center.z));
	}

	public void UpdateColliders(){
		if (OnUpdateColliders != null)
			OnUpdateColliders(this,EventArgs.Empty);
		if (OnAfterUpdateColliders != null)
			OnAfterUpdateColliders(this,EventArgs.Empty);
	}

	public void UpdateRigidbodies(){
		if (OnUpdateRigidbodies != null)
			OnUpdateRigidbodies(this,EventArgs.Empty);
	}

	public void WaitForAllSolvers(){

		switch(updateMode){

			case UpdateMode.FixedUpdate: 
				if (fixedUpdateArbiter.WaitForAllSolvers()) 
					UpdateRigidbodies();
			break;	

			case UpdateMode.AfterFixedUpdate: 
				afterFixedUpdateArbiter.WaitForAllSolvers(); 
			break;

			case UpdateMode.LateUpdate: 
				lateUpdateArbiter.WaitForAllSolvers(); 
			break;

		}
	}

	/**
	 * Checks if any particle in the solver is visible from at least one camera. If so, sets isVisible to true, false otherwise.
	 */
	public void UpdateVisibility(){

		Vector3 min = Vector3.zero, max = Vector3.zero;
		Oni.GetBounds(oniSolver,ref min, ref max);
		bounds.SetMinMax(min,max);

		if (AreBoundsValid(bounds)){

			Array.Resize(ref sceneCameras,Camera.allCamerasCount);
			Camera.GetAllCameras(sceneCameras);

			foreach (Camera cam in sceneCameras){
	        	GeometryUtility.CalculateFrustumPlanes(cam,planes);
	       		if (GeometryUtility.TestPlanesAABB(planes, bounds)){
					if (!isVisible){
						isVisible = true;
						foreach(ObiActor actor in actors)
							actor.OnSolverVisibilityChanged(isVisible);
					}
					return;
				}
			}
		}

		if (isVisible){
			isVisible = false;
			foreach(ObiActor actor in actors)
				actor.OnSolverVisibilityChanged(isVisible);
		}
	}
    
    void Update(){

		if (Application.isPlaying){

			BeginFrame(false);

			// Update solver transform:
			UpdateTransformFrame(Time.deltaTime);
	
			if (IsUpdating && updateMode != UpdateMode.LateUpdate){
				AccumulateSimulationTime(Time.deltaTime);
			}
		}

	}

	IEnumerator RunLateFixedUpdate() {
         while (true) {

             yield return new WaitForFixedUpdate();

			 if (Application.isPlaying && updateMode == UpdateMode.AfterFixedUpdate)
             	SimulateStep(Time.fixedDeltaTime); 
         }
     }

    void FixedUpdate()
    {
		if (Application.isPlaying){

			BeginFrame(true);

			if (updateMode == UpdateMode.FixedUpdate)
				SimulateStep(Time.fixedDeltaTime);
			
		}
    }

	public void AllSolversStepEnd()
	{
		// Trigger solver events:
		TriggerCollisionEvents();
	
		foreach(ObiActor actor in actors)
       	 	actor.OnSolverStepEnd(lastStepDelta);

		if (OnStepEnd != null)
			OnStepEnd(this,EventArgs.Empty);

	}

	private void LateUpdate(){

		float lateUpdateDelta = 0;

		if (Application.isPlaying && updateMode == UpdateMode.LateUpdate){

			 if (Time.deltaTime > 0){

			 	// smooth out timestep and accumulate it:
			 	lateUpdateDelta = smoothDelta = Mathf.Lerp(Time.deltaTime,smoothDelta,0.95f);

			 	AccumulateSimulationTime(lateUpdateDelta);
             	SimulateStep(lateUpdateDelta);

			 }else{
				lateUpdateDelta = 0;
			 }

		}

		if (Application.isPlaying)
			EndFrame (updateMode == UpdateMode.LateUpdate ? lateUpdateDelta : Time.fixedDeltaTime);

	}

}

}
