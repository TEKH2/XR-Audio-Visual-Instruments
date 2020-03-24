using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EXP.Painter
{
    /// <summary>
    /// Base class for rendering a stroke. Stokes could be rendered as meshes, particles or line renderers
    /// </summary>
    public class StrokeRenderer : MonoBehaviour
    {
        #region VARIABLES

        protected BrushStroke _Stroke;

        public Material m_Mat;

        protected Renderer m_Renderer;
        public virtual Renderer Renderer { get { return null; } }
        public virtual Material Material { get { return null; } }
        
        public Sprite m_ButtonSprite;
        
        public Color m_Color = Color.blue;


        [Header("Range and Spacing")]
        // The space between nodes. This is scaled by the size so it acts as a scaler. i.e. .5 will overlap half way, 1 will touch at the edges
        public float m_Spacing = .2f;
        public float AdjustedSpacing {get { return m_Spacing * AdjustedScale; } }

        // The size range that the normalized input value is reranged too
        public Vector2 m_ScaleRange = new Vector2(0, 1);

        public float AdjustedScale { get { return _Stroke.m_Scale.ScaleFrom01(m_ScaleRange.x, m_ScaleRange.y); } }

         
        [Header("Jitter")]
        #region Jitter values      
        public Vector3 m_PositionJitter = Vector3.zero;
        public Vector3 m_RotationJitter = Vector3.zero;
        public Vector3 m_ScaleJitter = Vector3.zero;
        #endregion

        bool m_Rendering = true;

        public bool m_DynamicSpacing = false;

        // The index of the matrix that renderer is up to
        protected int _PopulatedMatrixIndex = 0;

        public bool m_Debug = false;

        #endregion

        public virtual void Initialize(BrushStroke s, int layer, Color col)
        {
            _Stroke = s;
            transform.SetParent(s.transform);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            gameObject.layer = layer;

            print("Setting stroke renderer col to: " + col);
            SetColour(col);
        }

        // Use this to render mesh, particles, what not
        public virtual void DrawStroke(bool forceRedraw)
        {
        }

        public virtual void SetMaterial(Material mat)
        {
            m_Mat = mat;
            Renderer.material.SetCol(m_Color);
        }

        public virtual void SetRenderState( bool active )
        {
            m_Rendering = active;
        }

        public virtual void SetColour( Color col )
        {
            m_Color = col;

            Renderer.material.SetColor("_BaseColor",col);
            //Renderer.material.SetCol(col);
        }
        
        // Gets position with offset and jitter        
        public void SetJitter(StrokeNode node, int randomSeed)
        {
            // Set random seed for consistency
            Random.InitState(randomSeed);

            // If there is a position jitter then calc
            if (m_PositionJitter != Vector3.zero)
            {
                // Set nodes jitter position
                node.JitterPos = (Random.insideUnitSphere * .5f).ScaleReturn(m_PositionJitter);
                node.JitterPos *= AdjustedScale;
            }

            // If there is a rotation jitter then calc
            if (m_RotationJitter != Quaternion.identity.eulerAngles)
            {
                node.JitterRot = Quaternion.Euler((Random.insideUnitSphere * .5f * AdjustedScale).ScaleReturn(m_RotationJitter));
            }

            // If there is a scale jitter then calc
            if (m_ScaleJitter != Vector3.zero)
            {
                node.JitterScale = Random.insideUnitSphere.ScaleReturn(m_ScaleJitter);
                node.JitterScale *= AdjustedScale;
            }
        }

    }
}
