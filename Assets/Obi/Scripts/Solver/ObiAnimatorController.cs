using UnityEngine;
using System;

namespace Obi
{
	[RequireComponent(typeof(Animator))]
	[DisallowMultipleComponent]
	public class ObiAnimatorController : MonoBehaviour
	{
		private Animator animator;
		private float lastUpdate;
		private float updateDelta;

		public event System.EventHandler OnAnimatorUpdated;

		public float UpdateDelta{
			get{return updateDelta;}
		}

		public void Awake(){
			animator = GetComponent<Animator>();
			lastUpdate = Time.time;
		}

		public void OnDisable(){
			ResumeAutonomousUpdate();
		}

		public void UpdateAnimation(bool fixedUpdate)
		{
			// UpdateAnimation might becalled during FixedUpdate(), but we still need to account for the full frame's worth of animation.
			// Since Time.deltaTime returns the fixed step during FixedUpdate(), we need to use our own delta.
			updateDelta = Time.time - lastUpdate;
			lastUpdate = Time.time;

			if (animator.updateMode == AnimatorUpdateMode.AnimatePhysics)
				updateDelta = Time.fixedDeltaTime;

			// Note: when using AnimatorUpdateMode.Normal, the update method of your character controller 
			// should be Update() instead of FixedUpdate() (ObiCharacterController.cs, in this case).

			if (animator != null && isActiveAndEnabled && (animator.updateMode != AnimatorUpdateMode.AnimatePhysics || fixedUpdate)){
				animator.enabled = false;
				animator.Update(updateDelta);

				if (OnAnimatorUpdated != null)
					OnAnimatorUpdated(this,EventArgs.Empty);
			}
		}

		public void ResumeAutonomousUpdate(){
			if (animator != null){
				animator.enabled = true;
			}
		}
	}
}

