using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Obi
{
	[RequireComponent(typeof(ObiActor))]
	public class ColorRandomizer : MonoBehaviour
	{
		ObiActor actor;
		public Gradient gradient = new Gradient();

		void Start(){
			actor = GetComponent<ObiActor>();
			if (actor.colors != null)
			for (int i = 0; i < actor.colors.Length; ++i){
				actor.colors[i] = gradient.Evaluate(UnityEngine.Random.value);
			}
		}
	
	}
}

