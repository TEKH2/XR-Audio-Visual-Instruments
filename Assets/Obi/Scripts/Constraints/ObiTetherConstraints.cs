using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Obi{
	
	/**
 	* Holds information about tether constraints for an actor.
 	*/
	[DisallowMultipleComponent]
	public class ObiTetherConstraints : ObiBatchedConstraints
	{
		
		[Range(0.1f,2)]
		[Tooltip("Scale of tether constraints. Values > 1 will expand initial tether length, values < 1 will make it shrink.")]
		public float tetherScale = 1;				/**< Stiffness of structural spring constraints.*/
		
		[Range(0,1)]
		[Tooltip("Tether resistance to stretching. Lower values will enforce tethers with more strenght.")]
		public float stiffness = 1;		   /**< Resistance of structural spring constraints to stretch..*/
		
		
		[SerializeField][HideInInspector] private List<ObiTetherConstraintBatch> batches = new List<ObiTetherConstraintBatch>();

		public override Oni.ConstraintType GetConstraintType(){
			return Oni.ConstraintType.Tether;
		}
	
		public override IEnumerable<ObiConstraintBatch> GetBatches(){
			return batches.Cast<ObiConstraintBatch>();
		}

		public ObiTetherConstraintBatch GetFirstBatch(){
			return batches.Count > 0 ? batches[0] : null;
		}
	
		public override void Clear(){
			RemoveFromSolver(null); 
			batches.Clear();
		}
	
		public void AddBatch(ObiTetherConstraintBatch batch){
			if (batch != null && batch.GetConstraintType() == GetConstraintType())
				batches.Add(batch);
		}
	
		public void RemoveBatch(ObiTetherConstraintBatch batch){
			batches.Remove(batch);
		}

		public void OnDrawGizmosSelected(){

			if (!visualize || !isActiveAndEnabled) return;

			Gizmos.color = Color.yellow;

			foreach (ObiTetherConstraintBatch batch in batches){
				foreach(int i in batch.ActiveConstraints){
					Gizmos.DrawLine(actor.GetParticlePosition(batch.tetherIndices[i*2]),
									actor.GetParticlePosition(batch.tetherIndices[i*2+1]));
				}
			}

		}

	}
}
