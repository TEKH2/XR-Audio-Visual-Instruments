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
public class ObiStretchShearConstraintBatch : ObiConstraintBatch
{

	public enum DistanceIndexType{
		First = 0,
		Second = 1
	}

	[HideInInspector] public List<int> springIndices = new List<int>();					/**< Distance constraint indices.*/
	[HideInInspector] public List<float> restLengths = new List<float>();				/**< Rest distances.*/
	[HideInInspector] public List<Quaternion> restOrientations = new List<Quaternion>();			/**< Stiffnesses of distance constraits.*/
	[HideInInspector] public List<Vector3> stiffnesses = new List<Vector3>();			/**< Stiffnesses of distance constraits.*/

	int[] solverIndices = new int[0];
	float[] solverRestLengths = new float[0];
	Quaternion[] solverRestOrientations = new Quaternion[0];
	Vector3[] solverStiffnesses = new Vector3[0];

	public ObiStretchShearConstraintBatch(bool cooked, bool sharesParticles) : base(cooked,sharesParticles){
	}

	public ObiStretchShearConstraintBatch(bool cooked, bool sharesParticles, float minYoungModulus, float maxYoungModulus) : 
	base(cooked,sharesParticles,minYoungModulus,maxYoungModulus){
	}

	public override Oni.ConstraintType GetConstraintType(){
		return Oni.ConstraintType.StretchShear;
	}

	public override void Clear(){
		activeConstraints.Clear();
		springIndices.Clear();
		restLengths.Clear();
		restOrientations.Clear();
		stiffnesses.Clear();
		constraintCount = 0;	
	}

	public void AddConstraint(int index1, int index2, float restLength, Quaternion restOrientation, Vector3 stiffness){
		activeConstraints.Add(constraintCount);
		springIndices.Add(index1);
		springIndices.Add(index2);
		restLengths.Add(restLength);
		restOrientations.Add(restOrientation);
        stiffnesses.Add(stiffness);
		constraintCount++;
	}

	public void InsertConstraint(int constraintIndex, int index1, int index2, float restLength, Quaternion restOrientation, Vector3 stiffness){

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
		restLengths.Insert(constraintIndex,restLength);	
		restOrientations.Insert(constraintIndex,restOrientation);
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
		restLengths.RemoveAt(index);
		restOrientations.RemoveAt(index);
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
		solverRestLengths = new float[restLengths.Count];
		solverRestOrientations = new Quaternion[restLengths.Count];
		solverStiffnesses = new Vector3[stiffnesses.Count];
		int j = 0;
		foreach (int i in ObiUtils.BilateralInterleaved(restLengths.Count))
		{
			solverIndices[j*2] = constraints.Actor.particleIndices[springIndices[i*2]];
			solverIndices[j*2+1] = constraints.Actor.particleIndices[springIndices[i*2+1]];	
			solverRestLengths[j] = restLengths[i];
			solverRestOrientations[j] = restOrientations[i];
			solverStiffnesses[j] = stiffnesses[i];
			j++;
		}

	}

	protected override void OnRemoveFromSolver(ObiBatchedConstraints constraints){
	}

	public override void PushDataToSolver(ObiBatchedConstraints constraints){ 

		if (constraints == null || constraints.Actor == null || !constraints.Actor.InSolver)
			return;

		ObiStretchShearConstraints dc = (ObiStretchShearConstraints) constraints;

		int[] orientationIndices = new int[restLengths.Count];
		
		for (int i = 0; i < restLengths.Count; i++){
			solverStiffnesses[i] = new Vector3(StiffnessToCompliance(dc.shearStiffness1),
										 	   StiffnessToCompliance(dc.shearStiffness2),
										 	   StiffnessToCompliance(dc.stretchStiffness));
			orientationIndices[i] = solverIndices[i*2];
		}

		Oni.SetStretchShearConstraints(batch,solverIndices,orientationIndices,solverRestLengths,solverRestOrientations,solverStiffnesses,ConstraintCount);
	}

	public override void PullDataFromSolver(ObiBatchedConstraints constraints){
	}	

}
}
