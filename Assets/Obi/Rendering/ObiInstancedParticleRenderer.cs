using UnityEngine;
using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;

namespace Obi{

[ExecuteInEditMode]
[AddComponentMenu("Physics/Obi/Obi Instanced Particle Renderer")]
[RequireComponent(typeof(ObiActor))]
public class ObiInstancedParticleRenderer : MonoBehaviour
{
	public bool render = true;
	public Mesh mesh;
	public Material material;
	public Vector3 instanceScale = Vector3.one;

	private ObiActor actor;
	private List<Matrix4x4> matrices = new List<Matrix4x4>();
	private List<Vector4> colors = new List<Vector4>();
	private MaterialPropertyBlock mpb;

	int meshesPerBatch = 0;
	int batchCount;

	public void Awake(){
		actor = GetComponent<ObiActor>();
	}

	public void OnEnable(){
		Camera.onPreCull += ScenePreCull;
	}

	public void OnDisable(){
		Camera.onPreCull -= ScenePreCull;
	}
		
	void ScenePreCull(Camera cam) 
	{

		if (mesh == null || material == null || !render || !isActiveAndEnabled || !actor.isActiveAndEnabled || !actor.Initialized){
			return;
		}

		ObiSolver solver = actor.Solver;

		// figure out the size of our instance batches:
		meshesPerBatch = Constants.maxInstancesPerBatch;
		batchCount = actor.positions.Length / meshesPerBatch + 1;
		meshesPerBatch = Mathf.Min(meshesPerBatch,actor.positions.Length);

		Vector4 basis1;
		Vector4 basis2;
		Vector4 basis3;

		//Convert particle data to mesh instances:
		for (int i = 0; i < batchCount; i++){

			matrices.Clear();	
			colors.Clear();	
			mpb = new MaterialPropertyBlock();	
			int limit = Mathf.Min((i+1) * meshesPerBatch, actor.active.Length);
			
			for(int j = i * meshesPerBatch; j < limit; ++j)
			{
				if (actor.active[j]){

					actor.GetParticleAnisotropy(j,out basis1,out basis2,out basis3);
					matrices.Add(Matrix4x4.TRS(actor.GetParticlePosition(j),
								 			   actor.GetParticleOrientation(j),
											   Vector3.Scale(new Vector3(basis1[3],basis2[3],basis3[3]),instanceScale)));
					colors.Add((actor.colors != null && j < actor.colors.Length) ? actor.colors[j] : Color.white);
				}
			}

			if (colors.Count > 0)
				mpb.SetVectorArray("_Color",colors);

			// Send the meshes to be drawn:
			Graphics.DrawMeshInstanced(mesh, 0, material, matrices, mpb);
		}

	}

}
}

