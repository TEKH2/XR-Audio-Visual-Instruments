using UnityEngine;
using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;

namespace Obi{

[ExecuteInEditMode]
[AddComponentMenu("Physics/Obi/Obi Particle Renderer")]
[RequireComponent(typeof(ObiActor))]
public class ObiParticleRenderer : MonoBehaviour
{
	public bool render = true;
	public Shader shader;
	public Color particleColor = Color.white; 
	public float radiusScale = 1;

	private ObiActor actor;
	private List<Mesh> meshes = new List<Mesh>();
	private Material material;

	// Geometry buffers:
	private List<Vector3> vertices = new List<Vector3>(4000);
	private List<Vector3> normals = new List<Vector3>(4000);
	private List<Color> colors = new List<Color>(4000);
	private List<int> triangles = new List<int>(6000);

	private List<Vector4> anisotropy1 = new List<Vector4>(4000);
	private List<Vector4> anisotropy2 = new List<Vector4>(4000);
	private List<Vector4> anisotropy3 = new List<Vector4>(4000);

	int particlesPerDrawcall = 0;
	int drawcallCount;

	private Vector3 particleOffset0 = new Vector3(1,1,0);
	private Vector3 particleOffset1 = new Vector3(-1,1,0);
	private Vector3 particleOffset2 = new Vector3(-1,-1,0);
	private Vector3 particleOffset3 = new Vector3(1,-1,0);

	public IEnumerable<Mesh> ParticleMeshes{
		get { return meshes; }
	}

	public Material ParticleMaterial{
		get { return material; }
	}

	public void Awake(){
		actor = GetComponent<ObiActor>();
	}

	public void OnEnable(){
		Camera.onPreCull += ScenePreCull;
	}

	public void OnDisable(){
		Camera.onPreCull -= ScenePreCull;

		ClearMeshes();
		GameObject.DestroyImmediate(material);
	}

	void CreateMaterialIfNeeded(){

		if (shader != null){

			if (!shader.isSupported)
				Debug.LogWarning("Particle rendering shader not suported.");
			
			if (material == null || material.shader != shader){
				GameObject.DestroyImmediate(material);
				material= new Material (shader);
	        	material.hideFlags = HideFlags.HideAndDontSave;
			}
		}
	}	
		
	void ScenePreCull(Camera cam) 
	{
		if (!isActiveAndEnabled || !actor.isActiveAndEnabled || !actor.Initialized){
			ClearMeshes();
			return;
		}

		CreateMaterialIfNeeded();

		ObiSolver solver = actor.Solver;

		// figure out the size of our drawcall arrays:
		particlesPerDrawcall = Constants.maxVertsPerMesh/4;
		drawcallCount = actor.positions.Length / particlesPerDrawcall + 1;
		particlesPerDrawcall = Mathf.Min(particlesPerDrawcall,actor.positions.Length);

		// If the amount of meshes we need to draw the particles has changed:
		if (drawcallCount != meshes.Count){

			// Re-generate meshes:
			ClearMeshes();
			for (int i = 0; i < drawcallCount; i++){
				Mesh mesh = new Mesh();
				mesh.name = "Particle imposters";
				mesh.hideFlags = HideFlags.HideAndDontSave;
				mesh.MarkDynamic();
				meshes.Add(mesh);
			}

		}

		Vector3 position;
		Vector4 basis1;
		Vector4 basis2;
		Vector4 basis3;
		Color color;

		//Convert particle data to mesh geometry:
		for (int i = 0; i < drawcallCount; i++){

			// Clear all arrays
			vertices.Clear();
			normals.Clear();
			colors.Clear();
			triangles.Clear();
			anisotropy1.Clear();
			anisotropy2.Clear();	
			anisotropy3.Clear();
			
			int index = 0;
			int limit = Mathf.Min((i+1) * particlesPerDrawcall, actor.active.Length);
			
			for(int j = i * particlesPerDrawcall; j < limit; ++j)
			{
				if (actor.active[j]){

					position = actor.GetParticlePosition(j);
					actor.GetParticleAnisotropy(j,out basis1,out basis2,out basis3); 
					color = (actor.colors != null && j < actor.colors.Length) ? actor.colors[j] : Color.white;

					vertices.Add(position);
					vertices.Add(position);
					vertices.Add(position);
					vertices.Add(position);
			
					normals.Add(particleOffset0);
					normals.Add(particleOffset1);
					normals.Add(particleOffset2);
					normals.Add(particleOffset3);
					
					colors.Add(color);
					colors.Add(color);
					colors.Add(color);
			        colors.Add(color);
			
					anisotropy1.Add(basis1);
					anisotropy1.Add(basis1);
					anisotropy1.Add(basis1);
					anisotropy1.Add(basis1);
			
					anisotropy2.Add(basis2);
					anisotropy2.Add(basis2);
					anisotropy2.Add(basis2);
					anisotropy2.Add(basis2);
			
					anisotropy3.Add(basis3);
					anisotropy3.Add(basis3);
					anisotropy3.Add(basis3);
					anisotropy3.Add(basis3);
			
					triangles.Add(index+2);
					triangles.Add(index+1);
					triangles.Add(index);
					triangles.Add(index+3);
			        triangles.Add(index+2);
			        triangles.Add(index);

					index += 4;
				}
			}

			Apply(meshes[i]);
		}
	
		DrawParticles();
	}

	private void DrawParticles(){

		if (material != null){

			material.SetFloat("_RadiusScale",radiusScale);
			material.SetColor("_Color",particleColor);

			// Send the meshes to be drawn:
			if (render){
				foreach(Mesh mesh in meshes)
					Graphics.DrawMesh(mesh, Matrix4x4.identity, material, gameObject.layer);
			}
		}

	}

	private void Apply(Mesh mesh){
		mesh.Clear();
		mesh.SetVertices(vertices);
		mesh.SetNormals(normals);
		mesh.SetColors(colors);
		mesh.SetUVs(0,anisotropy1);
		mesh.SetUVs(1,anisotropy2);
		mesh.SetUVs(2,anisotropy3);
		mesh.SetTriangles(triangles,0,true);
	}

	private void ClearMeshes(){
		foreach(Mesh mesh in meshes)
			GameObject.DestroyImmediate(mesh);
		meshes.Clear();
	}

}
}

