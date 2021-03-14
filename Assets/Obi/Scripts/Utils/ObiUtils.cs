using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Obi
{

public static class Constants{
	public const int maxVertsPerMesh = 65000;
	public const int maxInstancesPerBatch = 1023;
}

public static class ObiUtils
{

	public static void DrawArrowGizmo(float bodyLenght, float bodyWidth, float headLenght, float headWidth){

		float halfBodyLenght = bodyLenght*0.5f;
		float halfBodyWidth = bodyWidth*0.5f;

		// arrow body:
		Gizmos.DrawLine(new Vector3(halfBodyWidth,0,-halfBodyLenght),new Vector3(halfBodyWidth,0,halfBodyLenght));
		Gizmos.DrawLine(new Vector3(-halfBodyWidth,0,-halfBodyLenght),new Vector3(-halfBodyWidth,0,halfBodyLenght));
		Gizmos.DrawLine(new Vector3(-halfBodyWidth,0,-halfBodyLenght),new Vector3(halfBodyWidth,0,-halfBodyLenght));

		// arrow head:
		Gizmos.DrawLine(new Vector3(halfBodyWidth,0,halfBodyLenght),new Vector3(headWidth,0,halfBodyLenght));
		Gizmos.DrawLine(new Vector3(-halfBodyWidth,0,halfBodyLenght),new Vector3(-headWidth,0,halfBodyLenght));
		Gizmos.DrawLine(new Vector3(0,0,halfBodyLenght+headLenght),new Vector3(headWidth,0,halfBodyLenght));
		Gizmos.DrawLine(new Vector3(0,0,halfBodyLenght+headLenght),new Vector3(-headWidth,0,halfBodyLenght));
	}

	public static Bounds Transform(this Bounds b, Matrix4x4 m)
	{
	    var xa = m.GetColumn(0) * b.min.x;
	    var xb = m.GetColumn(0) * b.max.x;
	 
	    var ya = m.GetColumn(1) * b.min.y;
	    var yb = m.GetColumn(1) * b.max.y;
	 
	    var za = m.GetColumn(2) * b.min.z;
	    var zb = m.GetColumn(2) * b.max.z;
	 
		Bounds result = new Bounds();
		Vector3 pos = m.GetColumn(3);
		result.SetMinMax(Vector3.Min(xa, xb) + Vector3.Min(ya, yb) + Vector3.Min(za, zb) + pos,
						 Vector3.Max(xa, xb) + Vector3.Max(ya, yb) + Vector3.Max(za, zb) + pos);
					

		return result;
	}

	public static float Remap (this float value, float from1, float to1, float from2, float to2) {
		return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
	}

	/**
	 * Modulo operator that also follows intuition for negative arguments. That is , -1 mod 3 = 2, not -1.
	 */
	public static float Mod(float a,float b)
	{
		return a - b * Mathf.Floor(a / b);
	}

	public static Matrix4x4 Add(this Matrix4x4 a, Matrix4x4 other){
		for (int i = 0; i < 16; ++i)
			a[i] += other[i];
		return a;
	}

	public static Matrix4x4 ScalarMultiply(this Matrix4x4 a, float s){
		for (int i = 0; i < 16; ++i)
			a[i] *= s;
		return a;
	}

	public static Vector3 ProjectPointLine(Vector3 point, Vector3 lineStart, Vector3 lineEnd, out float mu, bool clampToSegment = true)
    {
         Vector3 ap = point - lineStart;
         Vector3 ab = lineEnd - lineStart;

		 mu = Vector3.Dot(ap, ab) / Vector3.Dot(ab,ab);

		 if (clampToSegment) 
			mu = Mathf.Clamp01(mu);

         return lineStart + ab * mu;
     }

	/**
	 * Calculates the area of a triangle.
	 */
	public static float TriangleArea(Vector3 p1, Vector3 p2, Vector3 p3){
		return Mathf.Sqrt(Vector3.Cross(p2-p1,p3-p1).sqrMagnitude) / 2f;
	}

	public static float EllipsoidVolume(Vector3 principalRadii){
		return 4.0f/3.0f * Mathf.PI * principalRadii.x * principalRadii.y * principalRadii.z;
	}

	public static Quaternion RestDarboux(Quaternion q1, Quaternion q2){
		Quaternion darboux = Quaternion.Inverse(q1) * q2;
		Vector4 omega_plus, omega_minus;
		omega_plus =  new Vector4(darboux.w,darboux.x,darboux.y,darboux.z) + new Vector4(1,0,0,0);
		omega_minus = new Vector4(darboux.w,darboux.x,darboux.y,darboux.z) - new Vector4(1,0,0,0);
		if (omega_minus.sqrMagnitude > omega_plus.sqrMagnitude){
			darboux = new Quaternion(darboux.x*-1,darboux.y*-1,darboux.z*-1,darboux.w*-1);
		}
		return darboux;
	}

	public static System.Collections.IEnumerable BilateralInterleaved(int count)
	{	
		for (int i = 0; i < count; ++i){			
			if (i % 2 != 0){
				yield return count - (count % 2) - i;
			}else yield return i;
		}
	}

}
}

