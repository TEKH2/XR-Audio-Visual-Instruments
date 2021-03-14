using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class circleGenerator : MonoBehaviour
{
    private int _Slices = 360; //number of slices in the full circle

    public Mesh drawCircle(float radius, int degrees, float rotationOffset)
    {
        // Create mesh and instantiate arrays for vertices and triangles
        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[degrees * 3];
        int[] triangles = new int[degrees * 3];

        // Arrays to store cartesian and angle coordinates
        float angle;
        float[] x = new float[degrees + 1];
        float[] y = new float[degrees + 1];

        // Populate cartesian arrays with cartesian coordinates derived from angle
        // (+ 1 in loop is to ensure the circle draws correctly)
        for (int i = 0; i < degrees + 1; i++)
        {
            // Generate cartiesian and angle values for each point
            angle = i * 2f * Mathf.PI / _Slices;
            x[i] = (Mathf.Cos(angle + rotationOffset) * radius);
            y[i] = (Mathf.Sin(angle + rotationOffset) * radius);
        }

        // Iterate over each degree to draw the three vertices for each degree
        for (int i = 0; i < degrees; i++)
        {
            vertices[i * 3 + 0] = Vector3.zero;
            vertices[i * 3 + 1] = new Vector3(x[i], y[i], 0f);
            vertices[i * 3 + 2] = new Vector3(x[(i + 1) % (degrees + 1)], y[(i + 1) % (degrees + 1)], 0f);

            triangles[i * 3 + 0] = i * 3 + 0;
            triangles[i * 3 + 1] = i * 3 + 1;
            triangles[i * 3 + 2] = i * 3 + 2;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;

        return mesh;
    }
}
