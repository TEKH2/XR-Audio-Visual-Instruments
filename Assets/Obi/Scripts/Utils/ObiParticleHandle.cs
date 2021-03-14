using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Obi{

/**
 * Small helper class that allows particles to be (individually or in group) parented to a GameObject.
 */ 
[ExecuteInEditMode]
public class ObiParticleHandle : MonoBehaviour {

	public bool fixOrientations = true;

	[SerializeField][HideInInspector] private ObiActor actor;
	[SerializeField][HideInInspector] private List<int> handledParticleIndices = new List<int>();
	[SerializeField][HideInInspector] private List<Vector3> handledParticlePositions = new List<Vector3>();
	[SerializeField][HideInInspector] private List<Quaternion> handledParticleOrientations = new List<Quaternion>();
	[SerializeField][HideInInspector] private List<float> handledParticleInvMasses = new List<float>();
	[SerializeField][HideInInspector] private List<float> handledParticleInvRotMasses = new List<float>();

	public int ParticleCount{
		get{return handledParticleIndices.Count;}
	}

	public ObiActor Actor{
		set{
			if (actor != value)
			{
				if (actor != null && actor.Solver != null)
				{
					actor.Solver.OnFrameBegin -= Actor_solver_OnFrameBegin;
					actor.OnInitialized -= Actor_OnInitialized;
				}
				actor = value;
				if (actor != null && actor.Solver != null)
				{
					actor.Solver.OnFrameBegin += Actor_solver_OnFrameBegin;
					actor.OnInitialized += Actor_OnInitialized; 
				}
			}
		}
		get{ return actor;}
	}

	void OnEnable(){
		if (actor != null && actor.Solver != null)
		{
			actor.Solver.OnFrameBegin += Actor_solver_OnFrameBegin;
			actor.OnInitialized += Actor_OnInitialized; 
		}
	}

	void OnDisable(){
		if (actor != null && actor.Solver != null)
		{
			actor.Solver.OnFrameBegin -= Actor_solver_OnFrameBegin;
			actor.OnInitialized -= Actor_OnInitialized; 
			ResetInvMasses();
		}
	}

	public void Clear(){
		ResetInvMasses();
		handledParticleIndices.Clear();
		handledParticlePositions.Clear();
		handledParticleOrientations.Clear();
		handledParticleInvMasses.Clear();
		handledParticleInvRotMasses.Clear();
	}

	public void AddParticle(int index, Vector3 position, Quaternion orientation, float invMass, float invRotMass){

		handledParticleIndices.Add(index);

		handledParticlePositions.Add(transform.InverseTransformPoint(position));
		handledParticleInvMasses.Add(invMass);

		if (actor.UsesOrientedParticles){
			Quaternion matrixRotation = Quaternion.LookRotation(
							     		transform.worldToLocalMatrix.GetColumn(2),
							      		transform.worldToLocalMatrix.GetColumn(1));

			handledParticleOrientations.Add(matrixRotation * orientation);
			handledParticleInvRotMasses.Add(invRotMass);
		}
	}

	public void RemoveParticle(int index){

		int i = handledParticleIndices.IndexOf(index);

		if (i > -1){

			if (actor.InSolver){
				actor.Solver.invMasses[actor.particleIndices[index]] = actor.invMasses[index] = handledParticleInvMasses[i];
				if (actor.UsesOrientedParticles)
					actor.Solver.invRotationalMasses[actor.particleIndices[index]] = actor.invRotationalMasses[index] = handledParticleInvRotMasses[i];
			}
	
			handledParticleIndices.RemoveAt(i);
			handledParticlePositions.RemoveAt(i);
			handledParticleOrientations.RemoveAt(i);
			handledParticleInvMasses.RemoveAt(i);
			handledParticleInvRotMasses.RemoveAt(i);

		}
	}

	private void ResetInvMasses(){

		// Reset original mass of all handled particles:
		if (actor.InSolver)
		{
			for (int i = 0; i < handledParticleIndices.Count; ++i)
			{
				actor.Solver.invMasses[actor.particleIndices[handledParticleIndices[i]]] = actor.invMasses[handledParticleIndices[i]] = handledParticleInvMasses[i];
			}
			if (actor.UsesOrientedParticles){
				for (int i = 0; i < handledParticleIndices.Count; ++i)
				{
					actor.Solver.invRotationalMasses[actor.particleIndices[handledParticleIndices[i]]] = actor.invRotationalMasses[handledParticleIndices[i]] = handledParticleInvRotMasses[i];
				}
			}
		}
	}
	
	private void Actor_OnInitialized (object sender, System.EventArgs e){
		// When the actor has been reinitialized, clear the handle as actor particle count might have changed.
		Clear();
	}

	private void Actor_solver_OnFrameBegin (object sender, System.EventArgs e)
	{
		if (actor.InSolver){

			Matrix4x4 l2sTransform;
			if (actor.Solver.simulateInLocalSpace)
				l2sTransform = actor.Solver.transform.worldToLocalMatrix * transform.localToWorldMatrix;
			else 
				l2sTransform = transform.localToWorldMatrix;

			for (int i = 0; i < handledParticleIndices.Count; ++i){

				int solverParticleIndex = actor.particleIndices[handledParticleIndices[i]];

				// handled particles should always stay fixed:
				actor.Solver.velocities[solverParticleIndex] = Vector3.zero;
				actor.Solver.invMasses [solverParticleIndex] = 0;

				// set particle position:
				actor.Solver.positions[solverParticleIndex] = l2sTransform.MultiplyPoint3x4(handledParticlePositions[i]);
				
			}

			if (fixOrientations && actor.UsesOrientedParticles){

				Quaternion matrixRotation = Quaternion.LookRotation(
							     			l2sTransform.GetColumn(2),
							      			l2sTransform.GetColumn(1));

				for (int i = 0; i < handledParticleIndices.Count; ++i){
	
					int solverParticleIndex = actor.particleIndices[handledParticleIndices[i]];

					// handled particles should always stay fixed:
					actor.Solver.angularVelocities[solverParticleIndex] = Vector3.zero;
					actor.Solver.invRotationalMasses[solverParticleIndex] = 0;

					// set particle orientation:
					actor.Solver.orientations[solverParticleIndex] = matrixRotation * handledParticleOrientations[i];
				}
			}

		}
	}

}
}
