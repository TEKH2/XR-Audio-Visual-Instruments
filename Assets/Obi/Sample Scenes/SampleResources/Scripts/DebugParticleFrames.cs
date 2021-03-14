using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Obi;

[RequireComponent(typeof(ObiActor))]
public class DebugParticleFrames : MonoBehaviour {

	ObiActor actor;
	public float size = 0.1f;
	
	public void Awake(){
		actor = GetComponent<ObiActor>();
	}
	
	// Update is called once per frame
	void OnDrawGizmos () {

		if (actor == null || !actor.InSolver || actor.orientations == null) return;
		
		for (int i = 0; i < actor.orientations.Length; ++i){

			int solverIndex = actor.particleIndices[i];

			Vector3 position = actor.Solver.positions[solverIndex];
			Quaternion orientation = actor.Solver.orientations[solverIndex];

			Gizmos.color = Color.red;
			Gizmos.DrawRay(position, orientation * Vector3.right * size);
			Gizmos.color = Color.green;
			Gizmos.DrawRay(position, orientation * Vector3.up * size);
			Gizmos.color = Color.blue;
			Gizmos.DrawRay(position, orientation * Vector3.forward * size);
 
		}
	
	}
}
