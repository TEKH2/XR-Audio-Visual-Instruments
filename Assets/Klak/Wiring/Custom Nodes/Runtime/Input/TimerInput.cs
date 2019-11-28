using UnityEngine;
using Klak.Math;
using EXPToolkit;

namespace Klak.Wiring
{
    [AddComponentMenu("Klak/Wiring/Input/Timer")]
    public class TimerInput : NodeBase
    {
        MasterTimer _MasterTimer;

        #region Editable properties

        public MasterTimer.Tempo _Tempo = MasterTimer.Tempo.Beat;

        #endregion

        #region Node I/O

        [SerializeField, Outlet]
        FloatEvent _ContinuousValue = new FloatEvent();

        [SerializeField, Outlet]
        FloatEvent _TempoNormValue = new FloatEvent();

        [SerializeField, Outlet]
        VoidEvent _TempoBangEvent = new VoidEvent();

        #endregion

        #region MonoBehaviour functions

        void Start()
        {
            _MasterTimer = MasterTimer.Instance;
            TempoTimer.onRhythmTrigger += TempoTimer_onRhythmTrigger;
        }

        private void TempoTimer_onRhythmTrigger(MasterTimer.Tempo rhythmType, int index)
        {
            if (rhythmType == _Tempo)
                _TempoBangEvent.Invoke();
        }

        private void Update()
        {
            _ContinuousValue.Invoke(_MasterTimer._ContinuousValue);
            _TempoNormValue.Invoke(_MasterTimer.TempoTimers[_Tempo].NormalizedTimer);
        }

        #endregion
    }
}