using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Obi{

	/**
	 * Implements common functionality for ObiCollider and ObiCollider2D.
	 */
	public abstract class ObiColliderBase : MonoBehaviour
	{
		public static Dictionary<int,Component> idToCollider = new Dictionary<int, Component>(); /**< holds pairs of <IntanceID,Collider>. 
																									  Contacts returned by Obi will provide the instance ID of the 
																									  collider involved in the collision, use it to index this dictionary
																									  and find the actual object.*/

		[SerializeProperty("CollisionMaterial")]
		[SerializeField] private ObiCollisionMaterial material;

		[SerializeProperty("Phase")]
		[SerializeField] private int phase = 0;

		[SerializeProperty("Thickness")]
		[SerializeField] private float thickness = 0;

		public ObiCollisionMaterial CollisionMaterial{
			set{
				material = value; 
				UpdateMaterial();
			}
			get{return material;}
		}

		public int Phase{
			set{
				if (phase != value){
					phase = value; 
					dirty = true;
				}
			}
			get{return phase;}
		}

		public float Thickness{
			set{
				if (thickness != value){
					thickness = value; 
					dirty = true;
				}
			}
			get{return thickness;}
		}

		public ObiShapeTracker Tracker{
			get{return tracker;}
		}

		public IntPtr OniCollider{
			get{return oniCollider;}
		}

		protected IntPtr oniCollider = IntPtr.Zero;
		protected ObiRigidbodyBase obiRigidbody;
		protected bool wasUnityColliderEnabled = true;
		protected bool dirty = false;

		protected ObiShapeTracker tracker;						 	     /**< tracker object used to determine when to update the collider's shape*/
		protected Oni.Collider adaptor = new Oni.Collider();			 /**< adaptor struct used to transfer collider data to the library.*/

		/**
		 * Creates an OniColliderTracker of the appropiate type.
   		 */
		protected abstract void CreateTracker();

		protected abstract Component GetUnityCollider(ref bool enabled);

		protected abstract void UpdateAdaptor();

		protected void CreateRigidbody(){

			obiRigidbody = null;

			// find the first rigidbody up our hierarchy:
			Rigidbody rb = GetComponentInParent<Rigidbody>();
			Rigidbody2D rb2D = GetComponentInParent<Rigidbody2D>();
				
			// if we have an rigidbody above us, see if it has a ObiRigidbody component and add one if it doesn't:
			if (rb != null){

				obiRigidbody = rb.GetComponent<ObiRigidbody>();

				if (obiRigidbody == null)
					obiRigidbody = rb.gameObject.AddComponent<ObiRigidbody>();

				Oni.SetColliderRigidbody(oniCollider,obiRigidbody.OniRigidbody);

			}else if (rb2D != null){

				obiRigidbody = rb2D.GetComponent<ObiRigidbody2D>();

				if (obiRigidbody == null)
					obiRigidbody = rb2D.gameObject.AddComponent<ObiRigidbody2D>();

				Oni.SetColliderRigidbody(oniCollider,obiRigidbody.OniRigidbody);

			}else{
				Oni.SetColliderRigidbody(oniCollider,IntPtr.Zero);
			}

		}

		private void UpdateMaterial(){
			if (material != null)
				Oni.SetColliderMaterial(oniCollider,material.OniCollisionMaterial);
			else
				Oni.SetColliderMaterial(oniCollider,IntPtr.Zero);
		}

		private void OnTransformParentChanged(){
			CreateRigidbody();
		}

		protected void AddCollider(){

			Component unityCollider = GetUnityCollider(ref wasUnityColliderEnabled);

			if (unityCollider != null){
	
				// register the collider:
				idToCollider[unityCollider.GetInstanceID()] = unityCollider;
	
				CreateTracker();
				oniCollider = Oni.CreateCollider();
	
				if (tracker != null)
					Oni.SetColliderShape(oniCollider,tracker.OniShape);
	
				// Send initial collider data:
				UpdateAdaptor();
	
				// Update collider material:
				UpdateMaterial();
	
				// Create rigidbody if necessary, and link ourselves to it:
				CreateRigidbody();
	
				// Subscribe collider callback:
				ObiSolver.OnUpdateColliders += UpdateIfNeeded;
				ObiSolver.OnAfterUpdateColliders += ResetTransformChanges;

			}
		}

		protected void RemoveCollider(){

			// Unregister collider:
			Component unityCollider = GetUnityCollider(ref wasUnityColliderEnabled);
			if (unityCollider != null){
				idToCollider.Remove(unityCollider.GetInstanceID());
			}

			// Unsubscribe collider callback:
			ObiSolver.OnUpdateColliders -= UpdateIfNeeded;
			ObiSolver.OnAfterUpdateColliders -= ResetTransformChanges;

			// Remove and destroy collider:
			Oni.RemoveCollider(oniCollider);
			Oni.DestroyCollider(oniCollider);
			oniCollider = IntPtr.Zero;

			// Destroy shape tracker:
			if (tracker != null){
				tracker.Destroy();
				tracker = null;
			}
		}

		/** 
		 * Check if the collider transform or its shape have changed any relevant property, and update their Oni counterparts.
		 */
		private void UpdateIfNeeded(object sender, EventArgs e){
	
			bool unityColliderEnabled = false;
			Component unityCollider = GetUnityCollider(ref unityColliderEnabled);

			if (unityCollider != null){

				// update the collider:
				if ((tracker != null && tracker.UpdateIfNeeded()) ||
					transform.hasChanged || 
				    dirty ||
				    unityColliderEnabled != wasUnityColliderEnabled){

					dirty = false;
					wasUnityColliderEnabled = unityColliderEnabled;
	
					// remove the collider from all solver's spatial partitioning grid:
					Oni.RemoveCollider(oniCollider);
					
					// update the collider:
					UpdateAdaptor();
					
					// re-add the collider to all solver's spatial partitioning grid:
					if (unityColliderEnabled){
						Oni.AddCollider(oniCollider);
					}
					
				}
			}
			// If the unity collider is null but its Oni counterpart isn't, the unity collider has been destroyed.
			else if (oniCollider != IntPtr.Zero){
				RemoveCollider();
			}

		}

		private void ResetTransformChanges(object sender, EventArgs e){
			transform.hasChanged = false;
		}

		private void OnDestroy(){

			// Always cleanup before leaving:
			RemoveCollider();

		}

		private void OnEnable(){

			// Add collider to current solvers:
			Oni.AddCollider(oniCollider);

		}

		private void OnDisable(){

			// Remove collider from current solvers:
			Oni.RemoveCollider(oniCollider);

		}

	}
}

