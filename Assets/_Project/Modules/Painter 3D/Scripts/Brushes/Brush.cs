using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.EventSystems;

namespace EXP.Painter
{
    /// Brushes draw strokes (a collection of 4x4 matracies) to a canvas    
    public class Brush : MonoBehaviour
    {
        #region VARIABLES

        public enum PaintMode
        {
            TwoDimensional,
            ThreeDimensional,
        }

        [SerializeField]
        private PaintMode m_PaintMode = PaintMode.ThreeDimensional;

        // Stroke complete event
        public delegate void OnStrokeComplete(BrushStroke stroke);
        public event OnStrokeComplete OnStrokeCompleteEvent;

        // Reference to the paint manager
        public PainterManager m_PaintManager;

        // The canvas that the brush is drawing too
        public PainterCanvas ActiveCanvas { get { return m_PaintManager.ActiveCanvas; } }

        #region Brush Tip
        // The transform the the brush guides
        private Transform _BrushTip;
        // The size of the brush tip
        float _BrushSize = .05f;
        public float BrushSize {
            get { return _BrushSize; }
            set
            {
                _BrushSize = Mathf.Clamp(value, .01f, .1f);

                if (_BrushTip != null)
                    _BrushTip.localScale = Vector3.one * _BrushSize;
            }
        }
        #endregion

        public bool m_InputOverUI = false;

        // The previous node position
        Vector3 m_PrevNodePos;

        // The currently being drawn
        BrushStroke m_ActiveStroke;
        public BrushStroke ActiveStroke { get { return m_ActiveStroke; } }

        // The amount of spacing between the nodes in cm
        public float m_MinNodeSpacing = .01f;

        // Brush Colour
        Color _Colour = Color.black;

        // Objects that show where the brush will draw, what size and colour
        public MeshRenderer[] m_BrushReticleObjects;

        [SerializeField]
        // Is the brush currently painting
        bool m_Painting = false;
        public bool Painting { get { return m_Painting; } }

        #region 2D painting variable
        // Offsets brush tip per stroke. Usefeul for painting on a 2d plane so that the strokes get layered and don't zfight
        bool m_UseOffsetPerStroke = false;
        public float m_DepthOffsetPerStroke = -.006f;
        #endregion

        #endregion

        #region UNITY METHODS

        // Use this for initialization
        void Awake()
        {
            m_PaintManager = FindObjectOfType<PainterManager>();           
        }

        void Start()
        {
            BrushSize = _BrushSize;
        }

        // Update is called once per frame
        void Update()
        {
            if (m_PaintManager.InputActive)
            {
               
            }
            else
            {
                if (m_Painting) EndStroke();
            }
        }

        private void OnDrawGizmos()
        {
            // m_BrushTip.transform
        }

        #endregion

        #region STROKE METHODS
        public BrushStroke BeginStroke(Transform brushTip)
        {
            _BrushTip = brushTip;

            // Check if mouse is over UI           
            if (m_InputOverUI) // EventSystem.current.IsPointerOverGameObject())
            {
                Debug.Log("Tried to begin stroke while over UI");
                return null;
            }

            Debug.Log("Starting stroke");

            // Set painting flag
            m_Painting = true;

            // create stroke
            //m_ActiveStroke = new GameObject("Stroke").AddComponent<Stroke>();
            //m_ActiveStroke.Init(Instantiate(Painter3DResourceManager.Instance.ActiveStrokeRenderer), m_PaintManager.m_PaintingLayer, m_BrushSize);

            //Test
            m_ActiveStroke = BrushStroke.GetNewStroke(Instantiate(PainterResourceManager.Instance.ActiveStrokeRenderer), m_PaintManager.m_PaintingLayer, _BrushSize, _Colour);
           
            // Add stroke to canvas
            m_PaintManager.ActiveCanvas.AddStroke(m_ActiveStroke);

            // Begin stroke
            m_ActiveStroke.BeginStroke(brushTip);

            // Set prev node pos
            m_PrevNodePos = brushTip.position;

            return m_ActiveStroke;
        }

        public void UpdateStroke()
        {
            Profiler.BeginSample("Update stroke");
            if (m_Painting)
            {
                if (Vector3.Distance(_BrushTip.position, m_PrevNodePos) > m_MinNodeSpacing)
                {
                    m_ActiveStroke.UpdateStroke(_BrushTip);
                    m_PrevNodePos = _BrushTip.position;
                }
            }
            Profiler.EndSample();

            //Debug.Log("Updating stroke");
        }

        public void EndStroke()
        {
            if (m_Painting)
            {
                m_ActiveStroke.EndStroke(_BrushTip);
                m_Painting = false;

                if (OnStrokeCompleteEvent != null) OnStrokeCompleteEvent(m_ActiveStroke);

                /*
                // Debug get mirror
                m_ActiveStroke = m_ActiveStroke.GetMirrorStroke(true, false, false);
                m_PaintManager.ActiveCanvas.AddStroke(m_ActiveStroke);
                if (OnStrokeCompleteEvent != null) OnStrokeCompleteEvent(m_ActiveStroke);
                */
            }

            Debug.Log("End stroke");
        }
        #endregion

        public void SetBrushTip(Transform tip)
        {
            _BrushTip = tip;
        }

        // Sets the colour of the brush
        public void SetCol(Color col)
        {
            _Colour = col;

            for (int i = 0; i < m_BrushReticleObjects.Length; i++)            
                m_BrushReticleObjects[i].material.SetCol(col);
        }                    
    }
}
