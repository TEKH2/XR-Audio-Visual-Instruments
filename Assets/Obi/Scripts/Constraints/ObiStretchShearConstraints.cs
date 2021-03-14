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
public class ObiStretchShearConstraints : ObiBatchedConstraints
{
	[Tooltip("Scale of stretching constraints. Values > 1 will expand initial constraint size, values < 1 will make it shrink.")]
	public float stretchingScale = 1;		   /**< Stiffness of structural spring constraints.*/
	
	[Range(0,1)]
	[Tooltip("Resistance to stretching. Lower values will yield more elastic cloth.")]
	public float stretchStiffness = 1;		   /**< Resistance of structural spring constraints to stretch..*/

	[Range(0,1)]
	[Tooltip("Resistance to shearing across radial axis 1. Lower values will yield more elastic cloth.")]
	public float shearStiffness1 = 1;		   /**< Resistance of structural spring constraints to stretch..*/

	[Range(0,1)]
	[Tooltip("Resistance to shearing across radial axis 2. Lower values will yield more elastic cloth.")]
	public float shearStiffness2 = 1;		   /**< Resistance of structural spring constraints to stretch..*/

	[SerializeField][HideInInspector] private List<ObiStretchShearConstraintBatch> batches = new List<ObiStretchShearConstraintBatch>();

	public override Oni.ConstraintType GetConstraintType(){
		return Oni.ConstraintType.StretchShear;
	}

	public override IEnumerable<ObiConstraintBatch> GetBatches(){
		return batches.Cast<ObiConstraintBatch>();
	}

	public ObiStretchShearConstraintBatch GetFirstBatch(){
		return batches.Count > 0 ? batches[0] : null;
	}

	public override void Clear(){
		RemoveFromSolver(null); 
		batches.Clear();
	}

	public void AddBatch(ObiStretchShearConstraintBatch batch){
		if (batch != null && batch.GetConstraintType() == GetConstraintType())
			batches.Add(batch);
	}

	public void RemoveBatch(ObiStretchShearConstraintBatch batch){
		batches.Remove(batch);
	}

	public void OnDrawGizmosSelected(){

		if (!visualize || !isActiveAndEnabled) return;

		Gizmos.color = Color.green;

		foreach (ObiStretchShearConstraintBatch batch in batches){
			foreach(int i in batch.ActiveConstraints){

				int p1 = batch.springIndices[i*2];
				int p2 = batch.springIndices[i*2+1];

				Vector3 pos1 = actor.GetParticlePosition(p1);
				Vector3 pos2 = actor.GetParticlePosition(p2);
				Vector3 avgPos = (pos1 + pos2) * 0.5f;
				Quaternion orientation = actor.GetParticleOrientation(p1);

				Gizmos.DrawRay(avgPos,orientation * Vector3.forward*0.025f);
				Gizmos.DrawRay(avgPos,orientation * Vector3.right*0.025f);
				Gizmos.DrawRay(avgPos,orientation * Vector3.up*0.025f);
				
			}
		}
	}
}
}
