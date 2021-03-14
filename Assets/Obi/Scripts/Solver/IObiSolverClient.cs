using UnityEngine;
using System.Collections;

namespace Obi{

	[System.Flags]
	public enum ParticleData{

		NONE = 0,
		ACTIVE_STATUS = 1 << 0,

		POSITIONS = 1 << 1,
		REST_POSITIONS = 1 << 2,

		ORIENTATIONS = 1 << 3,
		REST_ORIENTATIONS = 1 << 4,

		VELOCITIES = 1 << 5,
		ANGULAR_VELOCITIES = 1 << 6,
		VORTICITIES = 1 << 7,

		INV_MASSES = 1 << 8,
		INV_ROTATIONAL_MASSES = 1 << 9,

		PHASES = 1 << 10,
		PRINCIPAL_RADII = 1 << 11,
		COLLISION_MATERIAL = 1 << 12,

		ALL = ~0
	}

	/**
   	 * Interface for components that want to benefit from the simulation capabilities of an ObiSolver.
	 */
	public interface IObiSolverClient
	{
		bool AddToSolver(object info);
		bool RemoveFromSolver(object info);
		void PushDataToSolver(ParticleData data = ParticleData.NONE);
		void PullDataFromSolver(ParticleData data = ParticleData.NONE);
	}
}

