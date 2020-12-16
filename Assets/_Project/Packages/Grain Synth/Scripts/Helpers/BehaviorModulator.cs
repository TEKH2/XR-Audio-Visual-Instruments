using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Tekh2
{
    /// <summary>
    ///  Usability in edotir
    ///  Multi curve editing
    ///  Modulations only return 0-1 get scaled after output
    /// </summary>
    public class BehaviorModulator : MonoBehaviour
    {
        #region ----------------- ENUMS
        public enum State
        {
            Playing,
            Stopped,
            Paused
        }

        public enum EmitPropertyType
        {
            Cadence,
            Playhead,
            Duration,
            Volume,
            Transpose
        }

        public enum PlaybackType
        {
            OneShot,
            Looping
        }
        #endregion

        public GrainEmitterAuthoring _Emitter;

        public PlaybackType _PlaybackType = PlaybackType.OneShot;

        public float _Duration = 3;
        float _Timer;
        float _TimerNorm;
        State _State = State.Stopped;

        public bool _PlayOnAwake = false;

        ScaledBehaviorCurve[] _PropertieCurves;

        private void Start()
        {
            _PropertieCurves = GetComponentsInChildren<ScaledBehaviorCurve>();

            if (_PlayOnAwake)
                SetState(State.Playing);
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.P))
            {
                if(_State == State.Playing)
                    SetState(State.Stopped);
                else
                    SetState(State.Playing);
            }

            if (_State == State.Playing)
            {
                // Update timer
                _Timer += Time.deltaTime;
                _TimerNorm = Mathf.Clamp01(_Timer / _Duration);

                UpdateValues();
                
                if (_Timer >= _Duration)
                {
                    if (_PlaybackType == PlaybackType.OneShot)
                    {
                        _State = State.Stopped;
                        _Timer = 1;
                    }
                    else if (_PlaybackType == PlaybackType.Looping)
                    {
                        _Timer -= _Duration;
                    }
                }
            }
        }

        void UpdateValues()
        {
            for (int i = 0; i < _PropertieCurves.Length; i++)
            {
                _PropertieCurves[i]._TimeNormAlongCurve = _TimerNorm;

                //switch (_PropertieCurves[i]._PropertyType)
                //{
                //    case EmitPropertyType.Cadence:
                //        _Emitter._EmissionProps.Cadence = _PropertieCurves[i].GetMinMaxValue();
                //        break;
                //    case EmitPropertyType.Duration:
                //        _Emitter._EmissionProps.Duration = _PropertieCurves[i].GetMinMaxValue();
                //        break;
                //    case EmitPropertyType.Playhead:
                //        _Emitter._EmissionProps._Playhead = _PropertieCurves[i].GetMinMaxValue();
                //        break;
                //    case EmitPropertyType.Transpose:
                //        _Emitter._EmissionProps._Transpose = _PropertieCurves[i].GetMinMaxValue();
                //        break;
                //    case EmitPropertyType.Volume:
                //        _Emitter._EmissionProps.Volume = _PropertieCurves[i].GetMinMaxValue();
                //        break;
                //}
            }
        }

        public void SetState(State state)
        {
            switch (state)
            {
                case State.Playing:
                    _Timer = 0;
                    _TimerNorm = 0;
                    break;
                case State.Paused:
                    break;
                case State.Stopped:
                    _Timer = 0;
                    _TimerNorm = 0;
                    break;
            }

            print(name + "  Set state: " + state);

            UpdateValues();
            _State = state;
        }
    }
}