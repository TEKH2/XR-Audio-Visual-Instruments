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
public class ObiBendTwistConstraintBatch : ObiConstraintBatch
{

	public enum DistanceIndexType{
		First = 0,
		Second = 1
	}

	[HideInInspector] public List<int> springIndices = new List<int>();					/**< Distance constraint indices.*/
	[HideInInspector] public List<Quaternion> restDarbouxVectors = new List<Quaternion>();				/**< Rest distances.*/
	[HideInInspector] public List<Vector3> stiffnesses = new List<Vector3>();			/**< Stiffnesses of distance constraits.*/

	int[] solverIndices = new int[0];
	Quaternion[] solverDarboux = new Quaternion[0];
	Vector3[] solverStiffnesses = new Vector3[0];
	

	public ObiBendTwistConstraintBatch(bool cooked, bool sharesParticles) : base(cooked,sharesParticles){
	}

	public ObiBendTwistConstraintBatch(bool cooked, bool sharesParticles, float minYoungModulus, float maxYoungModulus) : 
	base(cooked,sharesParticles,minYoungModulus,maxYoungModulus){
	}

	public override Oni.ConstraintType GetConstraintType(){
		return Oni.ConstraintType.BendTwist;
	}

	public override void Clear(){
		activeConstraints.Clear();
		springIndices.Clear();
		restDarbouxVectors.Clear();
		stiffnesses.Clear();
		constraintCount = 0;	
	}

	public void AddConstraint(int index1, int index2, Quaternion restDarboux, Vector3 stiffness){
		activeConstraints.Add(constraintCount);
		springIndices.Add(index1);
		springIndices.Add(index2);
		restDarbouxVectors.Add(restDarboux);
        stiffnesses.Add(stiffness);
		constraintCount++;
	}

	public void InsertConstraint(int constraintIndex, int index1, int index2, Quaternion restDarboux, Vector3 stiffness){

		if (constraintIndex < 0 || constraintIndex > ConstraintCount)
			return;

		// update active indices:
		for (int i = 0; i < activeConstraints.Count; ++i){
			if (activeConstraints[i] >= constraintIndex)
				activeConstraints[i]++;
		}

		activeConstraints.Add(constraintIndex);

		springIndices.Insert(constraintIndex*2,index1);
		springIndices.Insert(constraintIndex*2+1,index2);
		restDarbouxVectors.Insert(constraintIndex,restDarboux);
        stiffnesses.Insert(constraintIndex,stiffness);
		constraintCount++;
	}

	public void SetParticleIndex(int constraintIndex, int particleIndex, DistanceIndexType type, bool wraparound){

		if (!wraparound){
			if (constraintIndex >= 0 && constraintIndex < ConstraintCount)
				springIndices[constraintIndex*2+(int)type] = particleIndex;
		}else
			springIndices[(int)ObiUtils.Mod(constraintIndex,ConstraintCount)*2+(int)type] = particleIndex;

	}

	public void RemoveConstraint(int index){

		if (index < 0 || index >= ConstraintCount)
			return;

		activeConstraints.Remove(index);
		for(int i = 0; i < activeConstraints.Count; ++i)
		    if (activeConstraints[i] > index) activeConstraints[i]--;

		springIndices.RemoveRange(index*2,2);
		restDarbouxVectors.RemoveAt(index);
        stiffnesses.RemoveAt(index);
		constraintCount--;
	}
	
	public override List<int> GetConstraintsInvolvingParticle(int particleIndex){
	
		List<int> constraints = new List<int>(10);
		
		for (int i = 0; i < ConstraintCount; i++){
			if (springIndices[i*2] == particleIndex || springIndices[i*2+1] == particleIndex) 
				constraints.Add(i);
		}
		
		return constraints;
	}

	protected override void OnAddToSolver(ObiBatchedConstraints constraints){

		// Set solver constraint data:
		solverIndices = new int[springIndices.Count];
		solverDarboux = new Quaternion[restDarbouxVectors.Count];
		solverStiffnesses = new Vector3[stiffnesses.Count];
		int j = 0;
		foreach (int i in ObiUtils.BilateralInterleaved(restDarbouxVectors.Count))
		{
			solverIndices[j*2] = constraints.Actor.particleIndices[springIndices[i*2]];
			solverIndices[j*2+1] = constraints.Actor.particleIndices[springIndices[i*2+1]];
			solverDarboux[j] = restDarbouxVectors[i];
			solverStiffnesses[j] = stiffnesses[i];
			++j;
		}

	}

	protected override void OnRemoveFromSolver(ObiBatchedConstraints constraints){
	}

	public override void PushDataToSolver(ObiBatchedConstraints constraints){ 

		if (constraints == null || constraints.Actor == null || !constraints.Actor.InSolver)
			return;

		ObiBendTwistConstraints dc = (ObiBendTwistConstraints) constraints;
		
		for (int i = 0; i < restDarbouxVectors.Count; i++){
			solverStiffnesses[i] = new Vector3(StiffnessToCompliance(dc.bendStiffness1),
										 	   StiffnessToCompliance(dc.bendStiffness2),
										 	   StiffnessToCompliance(dc.torsionStiffness));
		}

		Oni.SetBendTwistConstraints(batch,solverIndices,solverDarboux,solverStiffnesses,ConstraintCount);
	}

	public override void PullDataFromSolver(ObiBatchedConstraints constraints){
	}	

}
}

