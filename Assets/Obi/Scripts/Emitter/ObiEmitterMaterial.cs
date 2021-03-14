using UnityEngine;
using System;

namespace Obi{

/**
 * Base class for emitter materials, which hold information about the physical properties of the substance emitted by an emitter.
 */
public abstract class ObiEmitterMaterial : ScriptableObject
{

	public float resolution = 1;
	public float restDensity = 1000;		/**< rest density of the material.*/

	private System.EventHandler<EventArgs> onChangesMade;
	public event System.EventHandler<EventArgs> OnChangesMade {
	
	    add {
	        onChangesMade -= value;
	        onChangesMade += value;
	    }
	    remove {
	        onChangesMade -= value;
	    }
	}

	public void CommitChanges(){
		if (onChangesMade != null)
			onChangesMade(this,EventArgs.Empty);
	}

	/** 
     * Returns the diameter (2 * radius) of a single particle of this material.
     */
	public float GetParticleSize(Oni.SolverParameters.Mode mode){
		return 1f / (10 * Mathf.Pow(resolution,1/(mode == Oni.SolverParameters.Mode.Mode3D ? 3.0f : 2.0f)));
	}

	/** 
     * Returns the mass (in kilograms) of a single particle of this material.
     */
	public float GetParticleMass(Oni.SolverParameters.Mode mode){
		return restDensity * Mathf.Pow(GetParticleSize(mode),mode == Oni.SolverParameters.Mode.Mode3D ? 3 : 2);
	}
}
}

