using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Obi;

[RequireComponent(typeof(ObiCloth))]
public class RuntimeClothGenerator : MonoBehaviour {

	public ObiSolver solver;			/**< Solver used to manage the generated cloth. */
	public Mesh mesh;					/** Mesh used for the cloth.*/
	public ObiMeshTopology topology; 	/** Topology asset to store mesh information into. Can be empty, as any information it contains will be wiped and replaced at runtime.*/

	public event System.EventHandler Done;

	private ObiCloth cloth;

	public ObiCloth Cloth{
		get{return cloth;}
	}

	public void Awake(){
		cloth = GetComponent<ObiCloth>();
	}

	public IEnumerator Start()
    {
		// just some user input checks:
		if (mesh == null || topology == null){
			Debug.LogError("Either the mesh or the topology are null. You must provide a mesh and an empty topology asset in order to generate cloth.");
			yield break;
		}

		// generate a topology for the cloth mesh:
        topology.InputMesh = mesh;
        topology.Generate();

		// set the cloth topology and solver:
		cloth.Solver = solver;
		cloth.SharedTopology = topology;
	
		// generate particles (in a coroutine, as it can be quite slow) and add to the solver:
		yield return StartCoroutine(GenerateAndAddToSolver());

		if (Done != null)
			Done(this,EventArgs.Empty);
    }

    IEnumerator GenerateAndAddToSolver()
    {
		// enable the cloth:
        cloth.enabled = true;
			
		// generate the particle representation:        
        yield return StartCoroutine(cloth.GeneratePhysicRepresentationForMesh());

		// finally, add the cloth to the solver to start the simulation:
        cloth.AddToSolver(null);
    }
}
