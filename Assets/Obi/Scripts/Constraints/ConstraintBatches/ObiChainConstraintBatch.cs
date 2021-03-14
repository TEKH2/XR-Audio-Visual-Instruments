using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Obi{

/**
 * Holds information about distance constraints for an actor.
 */
[Serializable]
public class ObiChainConstraintBatch : ObiConstraintBatch
{

	[HideInInspector] public List<int> particleIndices = new List<int>();			/**< Triangle indices.*/
	[HideInInspector] public List<int> firstParticle = new List<int>();				/**< index of first triangle for each constraint.*/
	[HideInInspector] public List<int> numParticles = new List<int>();				/**< num of triangles for each constraint.*/

	[HideInInspector] public List<Vector2> lengths = new List<Vector2>();			/**< min/max lenghts for each constraint.*/


	int[] solverIndices = new int[0];

	public ObiChainConstraintBatch(bool cooked, bool sharesParticles) : base(cooked,sharesParticles){
	}

	public ObiChainConstraintBatch(bool cooked, bool sharesParticles, float minYoungModulus, float maxYoungModulus) : 
	base(cooked,sharesParticles,minYoungModulus,maxYoungModulus){
	}

	public override Oni.ConstraintType GetConstraintType(){
		return Oni.ConstraintType.Chain;
	}

	public override void Clear(){
		activeConstraints.Clear();
		particleIndices.Clear();
		firstParticle.Clear();
		numParticles.Clear();
		lengths.Clear();
		constraintCount = 0;	
	}

	public void AddConstraint(int[] indices, float restLength, float stretchStiffness, float compressionStiffness){

		activeConstraints.Add(constraintCount);
		firstParticle.Add((int)particleIndices.Count);
		numParticles.Add((int)indices.Length);
		particleIndices.AddRange(indices);
		lengths.Add(new Vector2(restLength,restLength));
		constraintCount++;
	}

	public void RemoveConstraint(int index){

		if (index < 0 || index >= ConstraintCount)
			return;

		activeConstraints.Remove(index);
		for(int i = 0; i < activeConstraints.Count; ++i)
		    if (activeConstraints[i] > index) activeConstraints[i]--;

		particleIndices.RemoveRange(firstParticle[index],numParticles[index]);
		lengths.RemoveAt(index);
		firstParticle.RemoveAt(index);
        numParticles.RemoveAt(index);
		constraintCount--;
	}
	
	public override List<int> GetConstraintsInvolvingParticle(int particleIndex){
	
		List<int> constraints = new List<int>(4);
		
		for (int i = 0; i < ConstraintCount; i++){
			if (particleIndices[i*2] == particleIndex || particleIndices[i*2+1] == particleIndex) 
				constraints.Add(i);
		}
		
		return constraints;
	}

	protected override void OnAddToSolver(ObiBatchedConstraints constraints){

		// Set solver constraint data:
		solverIndices = new int[particleIndices.Count];
		for (int i = 0; i < particleIndices.Count; i++)
		{
			solverIndices[i] = constraints.Actor.particleIndices[particleIndices[i]];
		}

	}

	protected override void OnRemoveFromSolver(ObiBatchedConstraints constraints){
	}

	public override void PushDataToSolver(ObiBatchedConstraints constraints){ 

		if (constraints == null || constraints.Actor == null || !constraints.Actor.InSolver)
			return;

		ObiChainConstraints dc = (ObiChainConstraints) constraints;

		Vector2[] scaledLengths = new Vector2[lengths.Count];

		for (int i = 0; i < lengths.Count; i++){
			scaledLengths[i] = new Vector2(lengths[i].y*dc.tightness,lengths[i].y);
		}

		Oni.SetChainConstraints(batch,solverIndices,scaledLengths,firstParticle.ToArray(),numParticles.ToArray(),ConstraintCount);
	}

	public override void PullDataFromSolver(ObiBatchedConstraints constraints){
	}	

}
}
