using UnityEngine;
using Klak.Math;

namespace Klak.Wiring
{
    [AddComponentMenu("Klak/Wiring/Recorder/Transform Recorder")]
    public class TransformRecorder : NodeBase
    {
        enum State
        {
            Recording,
            Playingback,
            Idle,
        }

        State _State = State.Idle;

        DataRecordPlayback<Matrix4x4> _DataRecorder;

        Transform _RecordingValue;
        [Inlet]
        public Transform RecordingValue
        {
            set
            {
                _RecordingValue = value;
            }
        }

        [Inlet]
        public float SeekTimeNormalized
        {
            set
            {
                if (_State == State.Playingback)
                    _MatrixOutlet.Invoke(_DataRecorder.SeekValueAtNoramlizedTime(value) );
            }
        }

        [Inlet]
        public float SeekTime
        {
            set
            {
                if (!enabled) return;

                if (_State == State.Playingback)                
                    _MatrixOutlet.Invoke(_DataRecorder.SeekValueAtTime(value));                
            }
        }

        [Inlet]
        public void StartRecording()
        {           
            _DataRecorder.StartRecording(_RecordingValue.ToMatrix4x4());
            _State = State.Recording;
        }

        [Inlet]
        public void StopRecording()
        {
            _State = State.Idle;
        }

        [Inlet]
        public void TogglePlayback()
        {
            if (_State == State.Playingback)
                _State = State.Idle;
            else
                _State = State.Playingback;
        }

        [SerializeField, Outlet]
        Matrix4x4Event _MatrixOutlet = new Matrix4x4Event();

        #region MonoBehaviour functions

        // Start is called before the first frame update
        void Start()
        {
            _DataRecorder = new DataRecordPlayback<Matrix4x4>();
        }

        // Update is called once per frame
        void Update()
        {
            if (_State == State.Recording)
            {
                Matrix4x4 m = _RecordingValue.ToMatrix4x4();
                _DataRecorder.RecordValue(m);
                _MatrixOutlet.Invoke(m);
            }
        }

        #endregion
    }
}
