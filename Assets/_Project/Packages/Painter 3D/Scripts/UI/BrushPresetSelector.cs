using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EXP.Painter
{
    /// <summary>
    /// Draws the currently brush
    /// </summary>
    public class BrushPresetSelector : MonoBehaviour
    {
        public static BrushPresetSelector Instance;

        PainterResourceManager m_ResourceManager;
        BrushStroke m_ExampleStroke;

        int m_NumberOfNodes = 20;
        float m_Length = .2f;
        float strokeSize = .05f;

        public int m_PresetSelected = 0;

        private void Awake()
        {
            Instance = this;
        }

        // Use this for initialization
        void Start()
        {
            

            // Get reference to resource manager
            m_ResourceManager = PainterResourceManager.Instance;

            // Instantiate new example stroke
            m_ExampleStroke = BrushStroke.GetNewStroke(PainterResourceManager.Instance.GetActiveStrokeRenderer(), PainterManager.Instance.m_PaintingLayer, strokeSize, Color.gray);
            m_ExampleStroke.transform.SetParent(transform);
            m_ExampleStroke.transform.localPosition = Vector3.zero;

            //m_ExampleStroke.Init(Painter3DResourceManager.Instance.GetActiveStrokeRenderer(), Painter3DManager.Instance.m_PaintingLayer, strokeSize);

            Transform tempBrushTip = new GameObject("Temp brushtip").transform;
            tempBrushTip.SetParent(transform);

            tempBrushTip.localRotation = Quaternion.Euler(0, 90, 0);
            tempBrushTip.localScale = Vector3.one * strokeSize;

            tempBrushTip.localPosition = -(Vector3.right * m_Length * .5f);

            // Begin stroke
            m_ExampleStroke.BeginStroke(tempBrushTip);

            tempBrushTip.localPosition =  (Vector3.right * m_Length * .5f);
            m_ExampleStroke.EndStroke(tempBrushTip);
        }

        // Update is called once per frame
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                Prev();
            }

            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                Next();
            }
        }

        void SetExampleStrokeRenderer( StrokeRenderer strokeRenderer )
        {
            //print("Setting sample stroke too: " + strokeRenderer);
            m_ExampleStroke.SetStrokeRenderer(strokeRenderer, 0, Color.gray);
            m_ExampleStroke.m_StrokeRenderer.DrawStroke(true);
        }

        public void Next()
        {            
            SetExampleStrokeRenderer(m_ResourceManager.GetNextStrokeRenderer());
        }

        public void Prev()
        {
            SetExampleStrokeRenderer(m_ResourceManager.GetPrevStrokeRenderer());
        }
    }
}
