using System;
using UnityEngine;
using UnityEditor;
using System.IO;

namespace Obi{

	public class ObiEditorUtils
	{
		public static float GaussianBrushProfile(float distance, float ro){
			// maxradius = 15:
			return Mathf.Exp(-(distance*distance*225)/(2*ro*ro));
		}

		/**
	 	* This makes it easy to create, name and place unique new ScriptableObject asset files.
		*/
		public static void CreateAsset<T> () where T : ScriptableObject
		{
			T asset = ScriptableObject.CreateInstance<T> ();
			
			string path = AssetDatabase.GetAssetPath (Selection.activeObject);
			if (path == "") 
			{
				path = "Assets";
			} 
			else if (Path.GetExtension (path) != "") 
			{
				path = path.Replace (Path.GetFileName (AssetDatabase.GetAssetPath (Selection.activeObject)), "");
			}
			
			string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath (path + "/New " + typeof(T).ToString() + ".asset");
			
			AssetDatabase.CreateAsset (asset, assetPathAndName);
			
			AssetDatabase.SaveAssets ();
			EditorUtility.FocusProjectWindow ();
			Selection.activeObject = asset;
		}

		public static void SaveMesh (Mesh mesh, string title, string name, bool makeNewInstance = true, bool optimizeMesh = true) {

			string path = EditorUtility.SaveFilePanel(title, "Assets/", name, "asset");
			if (string.IsNullOrEmpty(path)) return;
	        
			path = FileUtil.GetProjectRelativePath(path);

			Mesh meshToSave = (makeNewInstance) ? GameObject.Instantiate(mesh) as Mesh : mesh;

			if (optimizeMesh)
			     MeshUtility.Optimize(meshToSave);
	        
			AssetDatabase.CreateAsset(meshToSave, path);
			AssetDatabase.SaveAssets();
		}
	}
}


