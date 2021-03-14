using UnityEngine;
using System;
using System.Collections;

namespace Obi{

	/**
	 * Small helper class that lets you specify Obi-only properties for rigidbodies.
	 */

	[ExecuteInEditMode]
	public abstract class ObiRigidbodyBase : MonoBehaviour
	{
		public bool kinematicForParticles = false;

		protected IntPtr oniRigidbody = IntPtr.Zero;

		protected Oni.Rigidbody adaptor = new Oni.Rigidbody();
		protected Oni.RigidbodyVelocities oniVelocities = new Oni.RigidbodyVelocities();

		protected Vector3 velocity, angularVelocity;

		public IntPtr OniRigidbody {
			get{return oniRigidbody;}
		}

		public virtual void Awake(){
			oniRigidbody = Oni.CreateRigidbody();
			UpdateIfNeeded(this,EventArgs.Empty);
			ObiSolver.OnUpdateColliders += UpdateIfNeeded;
			ObiSolver.OnUpdateRigidbodies += UpdateVelocities;
		}

		public void OnDestroy(){
			ObiSolver.OnUpdateColliders -= UpdateIfNeeded;
			ObiSolver.OnUpdateRigidbodies -= UpdateVelocities;
			Oni.DestroyRigidbody(oniRigidbody);
			oniRigidbody = IntPtr.Zero;
		}

		public abstract void UpdateIfNeeded(object sender, EventArgs e);

		/**
		 * Reads velocities back from the solver.
		 */
		public abstract void UpdateVelocities(object sender, EventArgs e);

	}
}

