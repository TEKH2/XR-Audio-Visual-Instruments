using UnityEngine;
using System.Collections;
using System;

namespace Obi
{
	public class ObiEmitterMaterialGranular : ObiEmitterMaterial
	{
	
		public float randomness = 0;
	
		public void OnValidate(){
	
			resolution = Mathf.Max(0.001f,resolution);
			restDensity =  Mathf.Max(0.001f,restDensity);
			randomness =  Mathf.Max(0,randomness);
		}
	
	}
}

