using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EXP.Painter
{
    public class PlaybackStrokeData : MonoBehaviour
    {
        public enum State
        {
            Paused,
            Playing,
        }

        public float _Timer;
        public float _PlaybackSpeed = 1;
        public float PlaybackNorm
        {
            get
            {
                if (_ActiveStroke != null)                
                    return _Timer / _ActiveStroke._TotalTime;                
                else
                    return 0;
            }
        }

        public BrushStroke _ActiveStroke;

        public State _State = State.Paused;

        public Transform _PlaybackHeadTransform;


        // Start is called before the first frame update
        void Start()
        {
            FindObjectOfType<PainterCanvas>().OnStrokeAdded += OnStrokeAdded;
        }

        void OnStrokeAdded(BrushStroke brushStroke)
        {
            _ActiveStroke = brushStroke;
        }

        // Update is called once per frame
        void Update()
        {
            if(_State == State.Playing && _ActiveStroke != null)
            {
                _Timer += Time.deltaTime * _PlaybackSpeed;

                StrokeNode strokeNode = _ActiveStroke.GetNodeAtTime(_Timer);

                // update position
                _PlaybackHeadTransform.position = strokeNode.OriginalPos;
            }
        }
    }
}
