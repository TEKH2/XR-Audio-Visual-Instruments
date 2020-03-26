using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

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

        public ParticleSystem _PS;
        ParticleSystem.MainModule _PSMain;

        public Color _LowAngle;
        public Color _HighAngle;
        public float _MaxSpeed = 2f;


        public FloatEvent _Out_Speed;
        public Vector3Event _Out_Direction;
        public FloatEvent _Out_AngleChange;


        // Start is called before the first frame update
        void Start()
        {
            FindObjectOfType<PainterCanvas>().OnStrokeAdded += OnStrokeAdded;
            _PSMain = _PS.main;
        }

        void OnStrokeAdded(BrushStroke brushStroke)
        {
            _ActiveStroke = brushStroke;
        }

        // Update is called once per frame
        void Update()
        {
            if(Input.GetKeyDown(KeyCode.P))
            {
                _State = State.Playing;
            }

            if(_State == State.Playing && _ActiveStroke != null)
            {
                _Timer += Time.deltaTime * _PlaybackSpeed;
             

                StrokeNode strokeNode = _ActiveStroke.GetNodeAtTime(_Timer);

                // update position
                _PlaybackHeadTransform.position = strokeNode.OriginalPos;

                _PSMain.startColor = Color.Lerp(_LowAngle, _HighAngle, strokeNode._Speed / _MaxSpeed);
                //print(strokeNode._NormAngleChange);

                //Output
                _Out_Speed?.Invoke(strokeNode._Speed);
                _Out_Direction?.Invoke(strokeNode._Direction);
                _Out_AngleChange?.Invoke(strokeNode._NormAngleChange);
            }
        }
    }
}
