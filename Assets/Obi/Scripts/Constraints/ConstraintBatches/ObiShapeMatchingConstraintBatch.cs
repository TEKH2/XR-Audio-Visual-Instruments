using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Obi{

/**
 * Holds data about shape matching constraints for an actor.
 */
[Serializable]
public class ObiShapeMatchingConstraintBatch : ObiConstraintBatch
{

	[HideInInspector] public List<int> shapeIndices = new List<int>();				/**< particle indices.*/
	[HideInInspector] public List<int> firstIndex = new List<int>();				/**< index of first particle for each constraint.*/
	[HideInInspector] public List<int> numIndices = new List<int>();				/**< num of triangles for each constraint.*/
	[HideInInspector] public List<int> explicitGroup = new List<int>();				/**< whether the constraint is implicit (0) or explicit (>0).*/
	[HideInInspector] public List<Oni.ElastoplasticMaterial> shapeMaterialParameters = new List<Oni.ElastoplasticMaterial>();		/**< stiffness, plastic yield and plastic creep for each constraint.*/

	[HideInInspector][NonSerialized] public AlignedVector4Array restComs;
	[HideInInspector][NonSerialized] public AlignedVector4Array coms;
	[HideInInspector][NonSerialized] public AlignedQuaternionArray orientations;

	int[] solverIndices;

	public ObiShapeMatchingConstraintBatch(bool cooked, bool sharesParticles) : base(cooked,sharesParticles){
	}

	public override Oni.ConstraintType GetConstraintType(){
		return Oni.ConstraintType.ShapeMatching;
	}

	public override void Clear(){
		activeConstraints.Clear();
		shapeIndices.Clear();
		firstIndex.Clear();
		numIndices.Clear();
		explicitGroup.Clear();
		shapeMaterialParameters.Clear();
		constraintCount = 0;	
	}

	public int GetParticleIndex(int index){
		return shapeIndices[firstIndex[index]];
	}

	public void AddConstraint(int[] particleIndices,  float stiffness, float plasticYield, float plasticCreep, bool isExplicit){

		activeConstraints.Add(constraintCount);

		firstIndex.Add((int)shapeIndices.Count);
		numIndices.Add((int)particleIndices.Length);
		explicitGroup.Add(isExplicit?1:0);
		shapeIndices.AddRange(particleIndices);

		shapeMaterialParameters.Add(new Oni.ElastoplasticMaterial(stiffness,plasticYield,plasticCreep,1,1));

		constraintCount++;

	}

	public void RemoveConstraint(int index){

		if (index < 0 || index >= ConstraintCount)
			return;

		activeConstraints.Remove(index);
		for(int i = 0; i < activeConstraints.Count; ++i)
		    if (activeConstraints[i] > index) activeConstraints[i]--;

		shapeIndices.RemoveRange(firstIndex[index],numIndices[index]);
		firstIndex.RemoveAt(index);
	    numIndices.RemoveAt(index);
		explicitGroup.RemoveAt(index);
		shapeMaterialParameters.RemoveAt(index);
		constraintCount--;
	}

	public override List<int> GetConstraintsInvolvingParticle(int particleIndex){
	
		List<int> constraints = new List<int>(4);
		
		for (int i = 0; i < ConstraintCount; i++){
			for (int j = 0; j < numIndices[i]; j++){
				if (shapeIndices[firstIndex[i] + j] == particleIndex){ 
					constraints.Add(i);
				}
			}
		}
		
		return constraints;
	}

	protected override void OnAddToSolver(ObiBatchedConstraints constraints){

		// Set solver constraint data:
		solverIndices = new int[shapeIndices.Count];
		for (int i = 0; i < shapeIndices.Count; i++)
		{
			solverIndices[i] = constraints.Actor.particleIndices[shapeIndices[i]];
		}
	}

	protected override void OnRemoveFromSolver(ObiBatchedConstraints constraints){
	}

	public override void PushDataToSolver(ObiBatchedConstraints constraints){ 

		if (constraints == null || constraints.Actor == null || !constraints.Actor.InSolver)
			return;

		ObiShapeMatchingConstraints sc = (ObiShapeMatchingConstraints) constraints;

		restComs = new AlignedVector4Array(ConstraintCount);
		coms = new AlignedVector4Array(ConstraintCount);
			orientations = new AlignedQuaternionArray(ConstraintCount,constraints.Actor.ActorLocalToSolverMatrix.rotation);

		for (int i = 0; i < shapeMaterialParameters.Count; i++){
			shapeMaterialParameters[i] = new Oni.ElastoplasticMaterial(sc.stiffness,sc.plasticYield,sc.plasticCreep,sc.plasticRecovery,sc.maxDeform);
		}

		Oni.SetShapeMatchingConstraints(batch,
										solverIndices,
									    firstIndex.ToArray(),
								        numIndices.ToArray(),
										explicitGroup.ToArray(),
									    shapeMaterialParameters.ToArray(),
										restComs.GetIntPtr(),
										coms.GetIntPtr(),
										orientations.GetIntPtr(),
									    ConstraintCount);

		Oni.CalculateRestShapeMatching(constraints.Actor.Solver.OniSolver,batch); 
	}	 

	public override void PullDataFromSolver(ObiBatchedConstraints constraints){
	}	

}
}
