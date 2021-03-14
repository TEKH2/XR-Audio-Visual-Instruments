using UnityEngine;
using System;
using System.Collections;

namespace Obi{

	/**
	 * Small helper class that lets you specify Obi-only properties for rigidbodies.
	 */

	[ExecuteInEditMode]
	[RequireComponent(typeof(Rigidbody2D))]
	public class ObiRigidbody2D : ObiRigidbodyBase
	{
		private Rigidbody2D unityRigidbody;

		public override void Awake(){
			unityRigidbody = GetComponent<Rigidbody2D>();
			base.Awake();
		}

		public override void UpdateIfNeeded(object sender, EventArgs e){

			velocity = new Vector3(unityRigidbody.velocity.x,unityRigidbody.velocity.y,0);
			angularVelocity = new Vector3(0,0,unityRigidbody.angularVelocity * Mathf.Deg2Rad);

			adaptor.Set(unityRigidbody,kinematicForParticles);
			Oni.UpdateRigidbody(oniRigidbody,ref adaptor);

		}

		/**
		 * Reads velocities back from the solver.
		 */
		public override void UpdateVelocities(object sender, EventArgs e){

			// kinematic rigidbodies are passed to Obi with zero velocity, so we must ignore the new velocities calculated by the solver:
			if (Application.isPlaying && (unityRigidbody.isKinematic || !kinematicForParticles)){

				Oni.GetRigidbodyVelocity(oniRigidbody,ref oniVelocities);
				Vector3 deltaVel = oniVelocities.linearVelocity - velocity;	
				unityRigidbody.velocity += new Vector2(deltaVel.x,deltaVel.y);
				unityRigidbody.angularVelocity += (oniVelocities.angularVelocity[2] - angularVelocity[2]) * Mathf.Rad2Deg;

			}

		}
	}
}

