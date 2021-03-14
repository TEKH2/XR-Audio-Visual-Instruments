using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Obi{

/**
 * Holds information about distance constraints for an actor.
 */
[DisallowMultipleComponent]
public class ObiBendTwistConstraints : ObiBatchedConstraints
{
	[Tooltip("Scale of stretching constraints. Values > 1 will expand initial constraint size, values < 1 will make it shrink.")]
	public float stretchingScale = 1;				/**< Stiffness of structural spring constraints.*/
	
	[Range(0,1)]
	[Tooltip("Resistance to stretching. Lower values will yield more elastic cloth.")]
	public float torsionStiffness = 1;		   /**< Resistance of structural spring constraints to stretch..*/

	[Range(0,1)]
	[Tooltip("Resistance to shearing across radial axis 1. Lower values will yield more elastic cloth.")]
	public float bendStiffness1 = 1;		   /**< Resistance of structural spring constraints to stretch..*/

	[Range(0,1)]
	[Tooltip("Resistance to shearing across radial axis 2. Lower values will yield more elastic cloth.")]
	public float bendStiffness2 = 1;		   /**< Resistance of structural spring constraints to stretch..*/


	[SerializeField][HideInInspector] private List<ObiBendTwistConstraintBatch> batches = new List<ObiBendTwistConstraintBatch>();

	public override Oni.ConstraintType GetConstraintType(){
		return Oni.ConstraintType.BendTwist;
	}

	public override IEnumerable<ObiConstraintBatch> GetBatches(){
		return batches.Cast<ObiConstraintBatch>();
	}

	public ObiBendTwistConstraintBatch GetFirstBatch(){
		return batches.Count > 0 ? batches[0] : null;
	}

	public override void Clear(){
		RemoveFromSolver(null); 
		batches.Clear();
	}

	public void AddBatch(ObiBendTwistConstraintBatch batch){
		if (batch != null && batch.GetConstraintType() == GetConstraintType())
			batches.Add(batch);
	}

	public void RemoveBatch(ObiBendTwistConstraintBatch batch){
		batches.Remove(batch);
	}

	public void OnDrawGizmosSelected(){

		if (!visualize || !isActiveAndEnabled) return;

		Gizmos.color = Color.magenta;

		foreach (ObiBendTwistConstraintBatch batch in batches){
			foreach(int i in batch.ActiveConstraints){

				int p1 = batch.springIndices[i*2];
				int p2 = batch.springIndices[i*2+1];

				Vector3 pos1 = actor.GetParticlePosition(p1);
				Vector3 pos2 = actor.GetParticlePosition(p2);
				Vector3 avgPos = (pos1 + pos2) * 0.5f;
				Quaternion orientation = actor.GetParticleOrientation(p1);

				float length = (pos2 - pos1).magnitude;
				Gizmos.matrix = Matrix4x4.TRS(avgPos,orientation, new Vector3(0.015f,0.015f,length * 0.75f));
				Gizmos.DrawWireCube(Vector3.zero,Vector3.one);
				
			}
		}

	}
}
}
