using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Obi
{
	/**
	 * Sample script that colors fluid particles based on their vorticity (2D only)
	 */
	[RequireComponent(typeof(ObiActor))]
	public class ColorFromVelocity : MonoBehaviour
	{
		ObiActor actor;
		public float sensibility = 0.2f;

		void Awake(){
			actor = GetComponent<ObiActor>();
			actor.OnAddedToSolver += Actor_OnAddedToSolver;
			actor.OnRemovedFromSolver += Actor_OnRemovedFromSolver;
		}

		void Actor_OnAddedToSolver (object sender, ObiActor.ObiActorSolverArgs e)
		{
			e.Solver.OnFrameEnd += E_Solver_OnFrameEnd;
		}

		void Actor_OnRemovedFromSolver (object sender, ObiActor.ObiActorSolverArgs e)
		{
			e.Solver.OnFrameEnd -= E_Solver_OnFrameEnd;
		}

		public void OnEnable(){}

		void E_Solver_OnFrameEnd (object sender, EventArgs e)
		{
			if (!isActiveAndEnabled || actor.colors == null)
				return;

			for (int i = 0; i < actor.colors.Length; ++i){

				int k = actor.particleIndices[i];

				Vector4 vel = actor.Solver.velocities[k];

				actor.colors[i] = new Color(Mathf.Clamp(vel.x / sensibility,-1,1) * 0.5f + 0.5f,
											Mathf.Clamp(vel.y / sensibility,-1,1) * 0.5f + 0.5f,
											Mathf.Clamp(vel.z / sensibility,-1,1) * 0.5f + 0.5f,1);

			}
		}
	
	}
}

