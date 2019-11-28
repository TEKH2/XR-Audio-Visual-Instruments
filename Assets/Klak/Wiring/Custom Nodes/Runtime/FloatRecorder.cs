using UnityEngine;
using Klak.Math;

namespace Klak.Wiring
{
    [AddComponentMenu("Klak/Wiring/Recorder/Float Recorder")]
    public class FloatRecorder: NodeBase
    {
        enum State
        {
            Recording,
            Playingback,
            Idle,
        }

        State _State = State.Idle;

        DataRecordPlayback<float> _DataRecorder;

        float _RecordingValue;
        [Inlet]
        public float RecordingValue
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
                    _FloatOutlet.Invoke(_DataRecorder.SeekValueAtNoramlizedTime(value));
            }
        }

        [Inlet]
        public float SeekTime
        {
            set
            {
                if (!enabled) return;

                if (_State == State.Playingback)                
                    _FloatOutlet.Invoke(_DataRecorder.SeekValueAtTime(value));                
            }
        }

        [Inlet]
        public void StartRecording()
        {           
            _DataRecorder.StartRecording(_RecordingValue);
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
        FloatEvent _FloatOutlet = new FloatEvent();

        #region MonoBehaviour functions

        // Start is called before the first frame update
        void Start()
        {
            _DataRecorder = new DataRecordPlayback<float>();
        }

        // Update is called once per frame
        void Update()
        {
            if (_State == State.Recording)
            {
                _DataRecorder.RecordValue(_RecordingValue);
                _FloatOutlet.Invoke(_RecordingValue);
            }
        }

        #endregion
    }
}
