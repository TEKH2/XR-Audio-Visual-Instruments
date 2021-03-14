using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Obi{

/**
 * Holds information about chain constraints for an actor.
 */
[DisallowMultipleComponent]
public class ObiChainConstraints : ObiBatchedConstraints
{	
	[Range(0,1)]
	[Tooltip("Low tightness values allow the chain to contract.")]
	public float tightness = 1;		   /**< Inverse of the percentage of contraction allowed for chain segments.*/


	[SerializeField][HideInInspector] private List<ObiChainConstraintBatch> batches = new List<ObiChainConstraintBatch>();

	public override Oni.ConstraintType GetConstraintType(){
		return Oni.ConstraintType.Chain;
	}

	public override IEnumerable<ObiConstraintBatch> GetBatches(){
		return batches.Cast<ObiConstraintBatch>();
	}

	public ObiChainConstraintBatch GetFirstBatch(){
		return batches.Count > 0 ? batches[0] : null;
	}

	public override void Clear(){
		RemoveFromSolver(null); 
		batches.Clear();
	}

	public void AddBatch(ObiChainConstraintBatch batch){
		if (batch != null && batch.GetConstraintType() == GetConstraintType())
			batches.Add(batch);
	}

	public void RemoveBatch(ObiChainConstraintBatch batch){
		batches.Remove(batch);
	}

	public void OnDrawGizmosSelected(){

		if (!visualize || !isActiveAndEnabled) return;

		Gizmos.color = Color.cyan;

		foreach (ObiChainConstraintBatch batch in batches){
			foreach(int i in batch.ActiveConstraints){
				
				int first = batch.firstParticle[i];
				int count = batch.numParticles[i];
			
				for (int j = 0; j < count-1; ++j){

					Gizmos.DrawLine(actor.GetParticlePosition(batch.particleIndices[first + j]),
									actor.GetParticlePosition(batch.particleIndices[first + j + 1]));

				}
			}
		}

	}
}
}
