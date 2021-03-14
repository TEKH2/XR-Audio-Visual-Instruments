using System;
using System.Collections;
using System.Collections.Generic;

namespace Obi
{
	/**
	 * ObiArbiter is used to synchronize the update cycle of several solvers.
	 */
	public class ObiArbiter
	{
		private HashSet<ObiSolver> solvers = new HashSet<ObiSolver>(); 
		private int solverCounter = 0;

		public void RegisterSolver (ObiSolver solver)
		{
			if (solver != null)
				solvers.Add(solver);
		}

		public void UnregisterSolver (ObiSolver solver)
		{
			if (solver != null)
				solvers.Remove(solver);
		}

		/**
		 * When all solvers have called this, it
		 * waits until all solver update tasks have been finished.
		 */
		public bool WaitForAllSolvers()
		{
			// Increase solver counter:
			solverCounter++;
		
			// If all solvers have started simulating, we must wait for them all to finish:
			if (solverCounter >= solvers.Count){

				solverCounter = 0;

				Oni.WaitForAllTasks(); 

				// Notify solvers that they've all completed this simulation step:
				foreach(ObiSolver s in solvers){
					s.AllSolversStepEnd();
				}

				return true;

			}
			return false;
		}

		/**
		 * This method signals the start of a frame to all registered solvers. Any solver in the scene might be the first to
		 * hit the first step, so this way all solvers can trigger events correctly.
		 */
		public void BeginFrame(bool fixedUpdate){

			foreach(ObiSolver s in solvers){

				s.SignalBeginFrame();
	
				foreach(ObiActor actor in s.actors)
	        		actor.OnSolverFrameBegin(fixedUpdate);
			}		
		}		
	}
}

