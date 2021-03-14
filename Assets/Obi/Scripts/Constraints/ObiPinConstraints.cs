using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Obi{

/**
 * Holds information about pin constraints for an actor.
 */
[DisallowMultipleComponent]
public class ObiPinConstraints : ObiBatchedConstraints
{
	
	[Range(0,1)]
	[Tooltip("Pin resistance to stretching. Lower values will yield more elastic pin constraints.")]
	public float stiffness = 1;		   /**< Resistance of structural spring constraints to stretch..*/
	
	[SerializeField][HideInInspector] private List<ObiPinConstraintBatch> batches = new List<ObiPinConstraintBatch>();

	public override Oni.ConstraintType GetConstraintType(){
		return Oni.ConstraintType.Pin;
	}

	public override IEnumerable<ObiConstraintBatch> GetBatches(){
		return batches.Cast<ObiConstraintBatch>();
	}

	public ObiPinConstraintBatch GetFirstBatch(){
		return batches.Count > 0 ? batches[0] : null;
	}

	public override void Clear(){
		RemoveFromSolver(null); 
		batches.Clear();
	}

	public void AddBatch(ObiPinConstraintBatch batch){
		if (batch != null && batch.GetConstraintType() == GetConstraintType())
			batches.Add(batch);
	}

	public void RemoveBatch(ObiPinConstraintBatch batch){
		batches.Remove(batch);
	}

	public void BreakConstraints(){
		for (int i = 0; i < batches.Count; ++i){
			batches[i].BreakConstraints();
		}	
	}

	public void OnDrawGizmosSelected(){

		if (!visualize || !isActiveAndEnabled) return;

		Gizmos.color = Color.cyan;

		foreach (ObiPinConstraintBatch batch in batches){
			foreach(int i in batch.ActiveConstraints){

				if (batch.pinBodies[i] != null){
					Vector3 pinPosition = batch.pinBodies[i].transform.TransformPoint(batch.pinOffsets[i]);
	
					Gizmos.DrawLine(actor.GetParticlePosition(batch.pinIndices[i]),
									pinPosition);
				}
			}
		}

	}
}
}
