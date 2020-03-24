using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EXP.Painter
{
    public class StrokeRenderer_Tube : StrokeRenderer
    {
        #region VARIABLES

        public override Renderer Renderer
        {
            get
            {
                if (m_Renderer == null)
                {
                    m_Renderer = gameObject.GetComponent<MeshRenderer>();

                    if (m_Renderer == null)
                    {
                        m_Renderer = gameObject.AddComponent<MeshRenderer>();
                    }
                }

                return m_Renderer;
            }
        }
        MeshFilter m_Filter;
        Mesh m_Mesh;

        #region Mesh variables
        // Mesh variables as lists and arrays
        List<Vector3> m_VertList = new List<Vector3>();
        List<int> m_IndiciesList = new List<int>();
        List<Vector3> m_NormalsList = new List<Vector3>();
        List<Vector2> m_UVList = new List<Vector2>();

        Vector3[] m_Verts;
        int[] m_Indicies;
        Vector3[] m_Normals;
        Vector2[] m_UVs;
        Color[] m_Colors;
        #endregion
        
        public AnimationCurve m_ProfileCurve;

        public Vector3 m_LineDirection = Vector3.right;
        float m_MasterScaler = 1;

        public int m_RadialFaces = 6;

        #endregion
        
        void Awake()
        {
            //if (m_Renderer == null)
            {
                m_Renderer = gameObject.GetComponent<MeshRenderer>();
                //m_Renderer = m_MeshRenderer;
                m_Filter = gameObject.GetComponent<MeshFilter>();

                if (m_Renderer == null)
                {
                    m_Renderer = gameObject.AddComponent<MeshRenderer>();
                    m_Filter = gameObject.AddComponent<MeshFilter>();
                }
            }
        }

        public bool _Play = false;
        float _PlayNorm = 0;
        float _PlayLength = .2f;
        private void Update()
        {
            
        }

        #region OVERRIDDEN BASE CLASS METHODS
        public override void DrawStroke(bool forceRedraw)
        {
			base.DrawStroke(forceRedraw);

            if (forceRedraw) _PopulatedMatrixIndex = 0;

            GenerateMeshFromStrokeNodes(_Stroke);
            
            SetColour(m_Color);
        }
        
        public override void SetRenderState(bool active)
        {
            base.SetRenderState(active);
            m_Renderer.enabled = active;
        }

        public override void SetColour(Color col)
        {
            base.SetColour(col);
            

            if (m_Colors != null)
            {
                for (int i = 0; i < m_Colors.Length; i++)
                {
                    m_Colors[i] = m_Color;
                }

                m_Mesh.colors = m_Colors;
            }
        }
        #endregion

        #region MESH GENERATION
        void GenerateMeshFromStrokeNodes(BrushStroke stroke, float startNorm = 0, float endNorm = 1)
        {
            // Clear mesh
            if (m_Mesh != null)
                m_Mesh.Clear();
            else
                m_Mesh = new Mesh();

            // Create new lists
            if (_PopulatedMatrixIndex == 0)
            {
                m_VertList = new List<Vector3>();
                m_IndiciesList = new List<int>();
                m_NormalsList = new List<Vector3>();
                m_UVList = new List<Vector2>();
            }


            List<StrokeNode> strokeNodes = _Stroke.SpacedStrokeNodes;
            int segmentCount = stroke.SpacedStrokeNodes.Count;
            int vertCount = 2 * strokeNodes.Count * m_RadialFaces;
            int vertIndex = 0;
            // int intIndex = 0;

            Vector3 scale;
            float profileCurveVal;
            Vector3 normal;
            Vector3 nodePos;

            // UnityEngine.Profiling.Profiler.BeginSample("draw ribbon");
            // Starting form the last populated matrix index, add new segment data
            for (int i = _PopulatedMatrixIndex; i < segmentCount; ++i)
            {
                float normVal = (float)i / (segmentCount - 1);    // 0.0 -> 1.0                
                normVal = Mathf.Clamp01(normVal);

                // calculate scale
                // Get the modified scale of the node
                scale = strokeNodes[i].ModifiedScale;
                // multiply it by the profile
                profileCurveVal = 1;
                if (segmentCount > 2)
                    scale *= m_ProfileCurve.Evaluate(normVal);

                // position of the node modified by any beahvior
                nodePos = strokeNodes[i].ModifiedPos;

                normal = Vector3.forward;
                float u = normVal;

                for (int j = 0; j < m_RadialFaces; j++)
                {
                    float norm = (float)j / m_RadialFaces;
                    // Calculate the angle of the corner in radians.
                    float cornerAngle = 2f * Mathf.PI / (float)m_RadialFaces * j;

                    // Get the X and Y coordinates of the corner point. // Mathf.Sin(cornerAngle)
                    Vector3 pos = new Vector3(Mathf.Cos(cornerAngle), Mathf.Sin(cornerAngle), 0) * scale.x * .5f; //TODO make if face along z
                    // rotate
                    pos = strokeNodes[i].OriginalRot * pos;
                    //translate
                    pos += nodePos;

                    // Add vert
                    m_VertList.Add(pos);

                    // Add normal
                    m_NormalsList.Add(normal);

                    // Add UV
                    m_UVList.Add(new Vector2(u, norm));
                }

                // Tri indecies
                // If one segment has been made
                if (i > 0)
                {
                    // for each face
                    int baseIndex = i * m_RadialFaces;
                    for (int j = 0; j < m_RadialFaces; j++)
                    {
                        int index0;
                        int index1;
                        int index2;
                        int index3;

                        // if its the last face
                        if (j == m_RadialFaces - 1)
                        {
                            index0 = baseIndex;
                            index1 = baseIndex - m_RadialFaces;
                            index2 = baseIndex - (2 * m_RadialFaces) + 1;
                            index3 = baseIndex - m_RadialFaces + 1;
                        }
                        else
                        {
                            index0 = baseIndex;
                            index1 = baseIndex - m_RadialFaces;
                            index2 = baseIndex - m_RadialFaces + 1;
                            index3 = baseIndex + 1;
                        }

                        m_IndiciesList.Add(index0);
                        m_IndiciesList.Add(index1);
                        m_IndiciesList.Add(index2);

                        m_IndiciesList.Add(index0);
                        m_IndiciesList.Add(index2);
                        m_IndiciesList.Add(index3);

                        baseIndex++;
                    }
                }

                vertIndex = m_VertList.Count;
            }
         
            m_Colors = new Color[m_VertList.Count];
            for (int i = 0; i < m_Colors.Length; i++)
            {
                m_Colors[i] = m_Color;
            }

            m_Verts = m_VertList.ToArray();
            m_UVs = m_UVList.ToArray();
            m_Indicies = m_IndiciesList.ToArray();

            SetMeshFromData();          
        }

        void SetMeshFromData()
        {
            m_Mesh.vertices = m_Verts;
            m_Mesh.uv = m_UVs;
            m_Mesh.triangles = m_Indicies;
            m_Mesh.colors = m_Colors;

            m_Mesh.RecalculateBounds();
            m_Mesh.RecalculateNormals();

            m_Filter.mesh = m_Mesh;
            Renderer.material = m_Mat;
        }

        static void CalculateNormalsManaged(Vector3[] verts, Vector3[] normals, int[] tris, Mesh mesh)
        {
            for (int i = 0; i < tris.Length; i += 3)
            {
                int tri0 = tris[i];
                int tri1 = tris[i + 1];
                int tri2 = tris[i + 2];
                Vector3 vert0 = verts[tri0];
                Vector3 vert1 = verts[tri1];
                Vector3 vert2 = verts[tri2];
                // Vector3 normal = Vector3.Cross(vert1 - vert0, vert2 - vert0);
                Vector3 normal = new Vector3()
                {
                    x = vert0.y * vert1.z - vert0.y * vert2.z - vert1.y * vert0.z + vert1.y * vert2.z + vert2.y * vert0.z - vert2.y * vert1.z,
                    y = -vert0.x * vert1.z + vert0.x * vert2.z + vert1.x * vert0.z - vert1.x * vert2.z - vert2.x * vert0.z + vert2.x * vert1.z,
                    z = vert0.x * vert1.y - vert0.x * vert2.y - vert1.x * vert0.y + vert1.x * vert2.y + vert2.x * vert0.y - vert2.x * vert1.y
                };
                normals[tri0] += normal;
                normals[tri1] += normal;
                normals[tri2] += normal;
            }

            for (int i = 0; i < normals.Length; i++)
            {
                // normals [i] = Vector3.Normalize (normals [i]);
                Vector3 norm = normals[i];
                float invlength = 1.0f / (float)System.Math.Sqrt(norm.x * norm.x + norm.y * norm.y + norm.z * norm.z);
                normals[i].x = norm.x * invlength;
                normals[i].y = norm.y * invlength;
                normals[i].z = norm.z * invlength;
            }

            mesh.normals = normals;
        }
        #endregion

    }
}
