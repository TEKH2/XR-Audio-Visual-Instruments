using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Profiling;

namespace EXP.Painter
{
    /// <summary>
    /// A collection of matracies that are added to a canvas.
    /// Each stroke has a stroke renderer
    /// </summary>
    [System.Serializable]
    public class BrushStroke : MonoBehaviour
    {
        // Is the stroke currently recording
        public bool m_RecordingInput = false;

        #region Raw and spaced matracie lists
        // List of stroke nodes to replace the matracies
        public List<StrokeNode> _RawStrokeNodes = new List<StrokeNode>();
        public List<StrokeNode> RawStrokeNodes { get { return _RawStrokeNodes; } }
        public int RawNodeCount { get { return _RawStrokeNodes.Count; } }
        public List<StrokeNode> _SpacedStrokeNodes = new List<StrokeNode>();
        public List<StrokeNode> SpacedStrokeNodes { get { return _SpacedStrokeNodes; } }
        #endregion      

        #region Spacing and size
        // Spacing between matracies       
        public float m_Scale = 1;      
        #endregion
       
        // The renderer
        public StrokeRenderer m_StrokeRenderer;

        float m_TotalLength;
        public float TotalLength { get { return m_TotalLength; } }

        public bool redraw = false;

      
        float _StrokeStartTime;
        float _StrokeEndTime;
        public float _TotalTime;

        public bool _DEBUG_DrawRawdNodes = true;
        public bool _DEBUG_DrawSpacedNodes = false;

        #region Intialization and constructors
        public static BrushStroke GetNewStroke(StrokeRenderer sRend, int layer, float scale, Color col)
        {
            BrushStroke s = new GameObject("Stroke").AddComponent<BrushStroke>();
            s.Init(sRend, layer, scale, col);

            return s;
        }
        
        void Init(StrokeRenderer sRend, int layer, float scale, Color col)
        {
            SetStrokeRenderer(sRend, layer, col);
            m_Scale = scale;
        }
        
        public BrushStroke GetMirrorStroke(bool x, bool y, bool z)
        {
            BrushStroke s = GetNewStroke(Instantiate(m_StrokeRenderer), 0, m_Scale, m_StrokeRenderer.m_Color);

            s._RawStrokeNodes = new List<StrokeNode>();
            // Copy all raw nodes
            for (int i = 0; i < _RawStrokeNodes.Count; i++)
            {
                Vector3 pos = RawStrokeNodes[i].OriginalPos;
                Quaternion rot = RawStrokeNodes[i].OriginalRot;
                Vector3 scale = RawStrokeNodes[i].OriginalScale;

                Vector3 vel = RawStrokeNodes[i]._Velocity;

                if (x)
                {
                    Vector3 fwd = rot * Vector3.back;
                    fwd = Vector3.Reflect(fwd, Vector3.right);

                    Vector3 up = rot * Vector3.up;
                    up = Vector3.Reflect(up, Vector3.right);

                    rot = Quaternion.LookRotation(fwd, up);

                    pos.x = -pos.x;

                    scale.x = -scale.x;

                    vel = new Vector3(-vel.x, vel.y, vel.z);
                }

                s._RawStrokeNodes.Add(new StrokeNode(pos, rot, scale, vel, RawStrokeNodes[i]._Time, RawStrokeNodes[i]._NormAngleChange));
            }

            s.CalculateLength();
            s.UpdateSpacedStrokeNodes(true);
            s.m_StrokeRenderer.DrawStroke(true);

            return s;
        }
        #endregion

        // Sets the render being used by this stroke
        public void SetStrokeRenderer(StrokeRenderer strokeRend, int layer, Color col)
        {           
            if (m_StrokeRenderer != null) Destroy(m_StrokeRenderer.gameObject);

            m_StrokeRenderer = strokeRend;
        
            m_StrokeRenderer.Initialize(this, layer, col);

            UpdateSpacedStrokeNodes(true);
        }

        public bool m_DebugForceRedraw = false;
        void Update()
        {                      
            if (!m_RecordingInput)
            {
                if (m_DebugForceRedraw)
                    redraw = m_DebugForceRedraw;

                if (redraw)
                {
                    m_StrokeRenderer.DrawStroke(true); 
                    redraw = false;
                }
            }
        }
              
        // Updates the spaced matracies list based on the spacing
        public void UpdateSpacedStrokeNodes(bool forceRebuild)
        {
            // If the length of the stroke is 0, retrun
            if (TotalLength == 0)
                return;

            // Set scaled spacing depending on dynamic or static placement
            float scaledSpacing = Mathf.Max(.001f, m_StrokeRenderer.m_Spacing);
            if (m_StrokeRenderer.m_DynamicSpacing) scaledSpacing = m_StrokeRenderer.AdjustedSpacing;

            // Total number of matriceis that are needed to populate the length of the stroke
            int newCount = (int)(TotalLength / scaledSpacing) + 1;

            // If force rebuild then clear the matracie list, forcing them all to be regenerated
            if (forceRebuild)
                _SpacedStrokeNodes.Clear();

            // Generate new matracies
            for (int i = _SpacedStrokeNodes.Count; i < newCount; i++)
            {
                float length = i * scaledSpacing;

                // Clamp length
                length = Mathf.Clamp(length, 0, TotalLength);

                // Get new node at length
                StrokeNode newNode = GetNodeAtLength(length);

                // Set node jitter
                m_StrokeRenderer.SetJitter(newNode, i);

                // Add node to list
                _SpacedStrokeNodes.Add(newNode);
            }            
        }

        #region Serialization
        public void LoadFromData(StrokeData sData)
        {
            m_TotalLength = 0;
            Vector3 prevPos = Vector3.zero;
            float norm = 0;

            for (int i = 0; i < sData.m_RawStrokeNodeData.Length; i++)
            {
                norm = i / (sData.m_RawStrokeNodeData.Length - 1f);

                TransformData sNodeData = sData.m_RawStrokeNodeData[i];

                Vector3 pos = VectorExtensions.Parse(sNodeData.m_PosData);
                Vector3 rot = VectorExtensions.Parse(sNodeData.m_RotData);
                Vector3 scale = VectorExtensions.Parse(sNodeData.m_ScaleData);

                // TODO load in proper vel time and angle change
                // For each stroke node data add a new node
                StrokeNode newNode = new StrokeNode(pos, Quaternion.Euler(rot), scale, Vector3.right, norm, 0);
                _RawStrokeNodes.Add(newNode);

                // Accumulate length
                if (i > 0)
                    m_TotalLength += Vector3.Distance(pos, prevPos);

                prevPos = pos;
            }

            // Update spaced stroke nodes
            UpdateSpacedStrokeNodes(true);

            //Vector3 colVec = VectorExtensions.Parse(sData.m_Color);
            // m_StrokeRenderer.SetColour(m_StrokeRenderer.m_Color);            
        }

        public void LoadFromData(StrokeData sData, float duration)
        {
            StartCoroutine(LoadOverTime(sData, duration));
        }

        IEnumerator LoadOverTime(StrokeData sData, float duration)
        {
            m_TotalLength = 0;
            Vector3 prevPos = Vector3.zero;

            float timeBetweenNodes = duration / sData.m_RawStrokeNodeData.Length;
            float norm = 0;

            for (int i = 0; i < sData.m_RawStrokeNodeData.Length; i++)
            {
                norm = i / (sData.m_RawStrokeNodeData.Length - 1f);
                TransformData sNodeData = sData.m_RawStrokeNodeData[i];

                Vector3 pos = VectorExtensions.Parse(sNodeData.m_PosData);
                Vector3 rot = VectorExtensions.Parse(sNodeData.m_RotData);
                Vector3 scale = VectorExtensions.Parse(sNodeData.m_ScaleData);

                // For each stroke node data add a new node
                StrokeNode newNode = new StrokeNode(pos, Quaternion.Euler(rot), scale, Vector3.right, norm, 0);
                _RawStrokeNodes.Add(newNode);

                // Accumulate length
                if (i > 0)
                    m_TotalLength += Vector3.Distance(pos, prevPos);

                prevPos = pos;


                // Set colour
                Vector3 colVec = VectorExtensions.Parse(sData.m_Color);
                m_StrokeRenderer.m_Color = new Color(colVec.x, colVec.y, colVec.z);

                // Force render
                m_StrokeRenderer.DrawStroke(true);
                m_StrokeRenderer.SetColour(m_StrokeRenderer.m_Color);

                yield return new WaitForSeconds(timeBetweenNodes);
            }
        }
        #endregion

        #region Get pos, rot, scale
        public Vector3 GetPositionAt(int index)
        {
            return _RawStrokeNodes[index].OriginalPos;
        }        
               
        // Returns the position at the length along the stroke
        public StrokeNode GetNodeAtLength(float targetDist)
        {
            targetDist = Mathf.Clamp(targetDist, 0, TotalLength);

            float currentDist = 0;
            float lerp = 0;
            for (int i = 0; i < _RawStrokeNodes.Count - 1; i++)
            {
                // Get the distance between of the current and next stroke nodes
                Vector3 currentPos = _RawStrokeNodes[i].OriginalPos;
                Vector3 nextPos = _RawStrokeNodes[i + 1].OriginalPos;

                float interNodeDist = Vector3.Distance(currentPos, nextPos);

                // if the current + internode distance is larger than the target distance then return a new node at the right lerp
                if (currentDist + interNodeDist > targetDist)
                {
                    lerp = (targetDist - currentDist) / interNodeDist;
                    return LerpStrokeNodes(_RawStrokeNodes[i], _RawStrokeNodes[i + 1], lerp);
                }
                else
                {
                    currentDist += interNodeDist;
                }
            }

            return _RawStrokeNodes[0];
        }

        // Returns the position at the time along the stroke
        public StrokeNode GetNodeAtTime(float targetTime)
        {
            targetTime %= _TotalTime;
            float lerp = 0;

            if (_RawStrokeNodes.Count > 1)
            {
                for (int i = 1; i < _RawStrokeNodes.Count - 1; i++)
                {
                    // find first node with a time larger than target time
                    if (_RawStrokeNodes[i]._Time > targetTime)
                    {
                        StrokeNode prevNode = _RawStrokeNodes[i - 1];
                        float timeDiff = _RawStrokeNodes[i]._Time - prevNode._Time;
                        lerp = (targetTime - prevNode._Time) / timeDiff;

                        //print("Getting node: " + i + "    targetTime/Stroke time: " + targetTime +"/"+ _TotalTime + "    node: " + _RawStrokeNodes[i]._Time);
                        return LerpStrokeNodes(_RawStrokeNodes[i - 1], _RawStrokeNodes[i], lerp);
                    }
                }
            }

            return _RawStrokeNodes[0];
        }

        StrokeNode LerpStrokeNodes( StrokeNode node1, StrokeNode node2, float lerp)
        {
            Vector3 pos = Vector3.Lerp(node1.OriginalPos, node2.OriginalPos, lerp);
            Quaternion rot = Quaternion.Slerp(node1.OriginalRot, node2.OriginalRot, lerp);
            Vector3 scale = Vector3.Lerp(node1.OriginalScale, node2.OriginalScale, lerp);

            Vector3 vel = Vector3.Lerp(node1._Velocity, node2._Velocity, lerp);
            float time = Mathf.Lerp(node1._Time, node2._Time, lerp);
            float angle = Mathf.Lerp(node1._NormAngleChange, node2._NormAngleChange, lerp);

            return new StrokeNode(pos, rot, scale, vel, time, angle);
        }
        #endregion

        #region Begin, update and end stroke
        public void BeginStroke(Transform brushTip, Vector3 velocity)
        {
            m_RecordingInput = true;
            UpdateStroke(brushTip, velocity);

            // Set total length to 0
            m_TotalLength = 0;
            _StrokeStartTime = Time.time;
        }

        public void UpdateStroke(Transform brushTip, Vector3 brushVelocity)
        {
            // Create a new stoke node and add it to the list
            if (_RawStrokeNodes.Count > 1)
            {                
                _RawStrokeNodes.Add(new StrokeNode(brushTip, transform, Time.time - _StrokeStartTime, brushVelocity, _RawStrokeNodes[_RawStrokeNodes.Count-1]));
            }
            else
            {
                _RawStrokeNodes.Add(new StrokeNode(brushTip, transform, Time.time - _StrokeStartTime, brushVelocity));
            }

            // If there is more than 1 matrics then add to total length
            if (_RawStrokeNodes.Count > 1)
            {
                // Update total length by adding previous stroke node distance              
                m_TotalLength += Vector3.Distance(_RawStrokeNodes[_RawStrokeNodes.Count - 1].OriginalPos, _RawStrokeNodes[_RawStrokeNodes.Count - 2].OriginalPos);
            }    
            
            // if adding the second raw matrix, set the rotation of the first matrix to the same as the second          
            if( _RawStrokeNodes.Count == 2)
            {
                _RawStrokeNodes[0].SetOriginalRotation(_RawStrokeNodes[1].OriginalRot);
            }
            
            UpdateSpacedStrokeNodes(false);

            if (m_StrokeRenderer != null)
                m_StrokeRenderer.DrawStroke(false);            
        }

        public void EndStroke(Transform brushTip, Vector3 velocity)
        {
            m_RecordingInput = false;
            UpdateStroke(brushTip, velocity);

            _TotalTime = Time.time - _StrokeStartTime;

            // Check to make sure stroke has more than 1 stroke node, otherwise delete
            if( _RawStrokeNodes.Count <= 1 )
            {
                print("Removing stroke with less than 2 nodes");
                PainterManager.Instance.UndoLastStroke();
            }
        }
        #endregion

        #region Debug
        public void OnDrawGizmos()
        {
            if (_DEBUG_DrawRawdNodes)
            {
                DrawDebug(_RawStrokeNodes);
            }

            if (_DEBUG_DrawSpacedNodes)
            {
                DrawDebug(_SpacedStrokeNodes);
            }
        }

        void DrawDebug(List<StrokeNode> strokeNodes)
        {
            for (int i = 0; i < _RawStrokeNodes.Count; i++)
            {
                Gizmos.color = Color.Lerp(Color.blue, Color.yellow, strokeNodes[i]._Speed / 2.5f);
                Gizmos.DrawSphere(strokeNodes[i].ModifiedPos, (strokeNodes[i].ModifiedScale.x * .25f));
                Gizmos.DrawWireSphere(strokeNodes[i].ModifiedPos, (strokeNodes[i].ModifiedScale.x * .25f) + (strokeNodes[i]._NormAngleChange * strokeNodes[i].ModifiedScale.x * 2));
                Gizmos.DrawLine(strokeNodes[i].ModifiedPos, strokeNodes[i].ModifiedPos + strokeNodes[i]._Velocity * .2f);
            }
        }
        #endregion

        void CalculateLength()
        {
            m_TotalLength = 0;
            for (int i = 1; i < RawNodeCount; i++)
            {
                m_TotalLength += Vector3.Distance(_RawStrokeNodes[i - 1].OriginalPos, _RawStrokeNodes[i].OriginalPos);
            }
        }

        // Testing -----------------------
      
    }
}

/* Mirroring code to refactor
 *   // Generate the maticies for all duplications and mirroring of a single node. 
        void GenerateMatrix(Stroke_Node node)
        {
            // Get the radial count
            Matrix4x4 matrix = new Matrix4x4();

            int matrixListIndex = 0;
            for (int j = 0; j < m_StrokeDetails.RadialCount; j++)
            {
                // Get the radial angle 
                float angle = (float)j / ((float)m_StrokeDetails.RadialCount);
                angle *= 360;

                // Get the position
                Vector3 pos = node.m_CurrentPos.RotatePointAroundPivot( m_MirrorPos.transform.position, new Vector3(0, 0, angle));


                float moddedScale =  Mathf.Lerp(m_StrokeDetails.ScaleRange.x, m_StrokeDetails.ScaleRange.y, node.m_CurrentScale.x); 

                // Set the matrix using the pos, node roation and node scale
                matrix = Matrix4x4.TRS(pos, node.m_CurrentRotQ, node.m_CurrentScale * moddedScale);

                // Set radial matrix
                m_StrokeMatricies[matrixListIndex].Add(matrix);

                matrixListIndex++;
            }

            if (m_StrokeDetails.MirrorX)
            {
                for (int i = 0; i < m_StrokeDetails.RadialCount; i++)
                {
                    Matrix4x4 nodeMatrix = m_StrokeMatricies[i][m_StrokeMatricies[i].Count-1];
                    Vector3 pos = nodeMatrix.GetPosition();
                    Vector3 rot = Matrix4x4Extensions.ExtractRotationFromMatrix(ref nodeMatrix).eulerAngles;
                    Vector3 mirrorPos = new Vector3(m_MirrorPos.transform.position.x + (m_MirrorPos.transform.position.x - pos.x), pos.y, pos.z);
                    Matrix4x4 m = Matrix4x4.TRS(mirrorPos, Quaternion.Euler(rot.x, -rot.y, -rot.z), nodeMatrix.GetScale());

                    m_StrokeMatricies[matrixListIndex].Add(m);
                    matrixListIndex++;
                }              
            }

            if (m_StrokeDetails.MirrorY)
            {

            }

            if (m_StrokeDetails.MirrorZ)
            {

            }
        }
        */
