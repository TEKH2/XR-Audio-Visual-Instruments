using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine.Events;

namespace EXPToolkit
{
    public class MasterTimer : MonoBehaviour
    {
        public static MasterTimer Instance;

        #region EVENTS

        public EventTypes.FloatEvent OnSetBPMEvent = new EventTypes.FloatEvent();
        public EventTypes.IntEvent OnBeatEvent = new EventTypes.IntEvent();
        public UnityEvent OnFirstBeatEvent = new UnityEvent();

        #endregion
        
        #region TEMPO

        // Different tempo scales
        public enum Tempo
        {
            Phrase4,// *0.0625
            Phrase,// *0.25
            Bar,//1
            Beat, //4
            Eighth,//8
            Sixteenth,//16
        }

        public static string[] _TempoStrings = new string[]
        {
            "Phrase4",
            "Phrase",
            "Bar",
            "Beat",
            "Eighth",
            "Sixteenth",
        };

        // Dictionary of tempo timers that give various tempos using the BPM as the base
        public Dictionary<Tempo, TempoTimer> TempoTimers { get; private set; } = new Dictionary<Tempo, TempoTimer>();

        #endregion

        // Frequency == Beats per second
        public float Frequency { get { return BPM / 60; } }

        // The amount of time between beats
        public float TimeBetweenBeats { get { return 1f / Frequency; } }

        // ROund the BPM to nearest Int?
        bool _RoundBPM = true;
        
        // The amount by which delta time is scaled
        public float _OverrideScaler = 1;

        // The normalized position in the cycle that the maser timer is at
        public float _NormPosInCycle = 0;

        // The master timers continuous value. Aggregate of all delta times
        public float _ContinuousValue = 0;

        float _TimerOffset = 0;

        public bool m_OutputOSC = false;
        float previousMasterScaler = 1;

        public AnimationCurve m_BeatCurve;
        
        #region BPM TAPPER VARS

        float _BPM = 0;
        public float BPM
        {
            get { return _BPM; }
            set
            {
                if (_RoundBPM)
                    _BPM = Mathf.Round(value);
                else
                    _BPM = value;

                _BPM = Mathf.Clamp(_BPM, 0, _MaxBPM);
                OnSetBPMEvent.Invoke(_BPM);

                if (_BPM == 0)
                {
                    _AverageTimeBetweenBeatsCalculation = 0;
                    _Timer = 0;
                    _NextBeatTime = 0;
                    _CurrentBeatIndex = 0;
                    _CurrentBarCount = 0;
                    _BPMTapTimes.Clear();
                }
            }
        }

        // Maximum BPM to stop accidentally smashing bpm super high
        int _MaxBPM = 200;
        // Tracks the tap times
        public List<float> _BPMTapTimes;
        // Tracks time ins first beat
        float _Timer = 0;
        // Average time between the recorded beat taps 
        public float _AverageTimeBetweenBeatsCalculation;
        // The time the next beat will occur
        float _NextBeatTime;
        // Index of current beat, assuming a 4/4 time sig
        int _CurrentBeatIndex = 0;
        // Index of current bar, assuming a 4/4 time sig
        int _CurrentBarCount = 0;
        // Weather or not to send beat event. Used to stop sending beats during break downs
        public bool _SendBeatEvent = true;
        // normalized value for showing beat as a gizmo 
        float _DEBUG_BeatNorm = 0;

        #endregion

        void Awake()
        {
            // Set singleton instance
            Instance = this;
        }

        // Use this for initialization
        void Start()
        {
            // Create a dictionary with all the rhythm timers
            for (int i = 0; i < Enum.GetNames(typeof(Tempo)).Length; i++)            
                TempoTimers.Add((Tempo)i, new TempoTimer((Tempo)i));
        }

        // Update is called once per frame
        void Update()
        {
            #region KB INPUT

            // Inputs to control the direction of the timer
            if (Input.GetKey(KeyCode.LeftArrow))
            {
                _OverrideScaler = -1;
            }
            else if (Input.GetKey(KeyCode.RightArrow))
            {
                _OverrideScaler = 1;
            }

            if (Input.GetKeyDown(KeyCode.B))
            {
                if (Input.GetKey(KeyCode.LeftShift))
                    RestartTapperCalculation();
                else
                    AddBeat();
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                ResetBPM();
            }

            #endregion

            #region BPM TAPPER UPDATE

            if (_BPM > 0)
            {
                // If time has past of next beat
                if (_Timer > _NextBeatTime)
                {
                    // Set next beat time and increment beat index
                    _NextBeatTime += _AverageTimeBetweenBeatsCalculation;
                    _CurrentBeatIndex++;

                    // Check beat index and loop beat index around if larger than 4
                    if (_CurrentBeatIndex == 4)
                    {
                        _CurrentBeatIndex = 0;
                        _CurrentBarCount++;

                        if (_CurrentBarCount == 4)
                        {
                            _CurrentBarCount = 0;
                        }
                    }

                    // Set debug norm to 1
                    _DEBUG_BeatNorm = 1;

                    // Send on beat event if toggled
                    if (_SendBeatEvent)
                        OnBeatEvent.Invoke(_CurrentBeatIndex);
                }

                _Timer += Time.deltaTime;
            }

            _DEBUG_BeatNorm = Mathf.Lerp(_DEBUG_BeatNorm, 0, Time.deltaTime * 16);

            #endregion

            #region MASTER TIMER UPDATE

            // Get scaledTime since last frame
            float scaledDelta = Time.deltaTime;

            // Adjust scaledTime by frequency and the override scaler in case you want to slow or reverse it
            scaledDelta *= Frequency * _OverrideScaler;

            // Update position in cycle
            _NormPosInCycle += scaledDelta;

            // Wrap it to 0-1 		
            _NormPosInCycle %= 1;

            // Update continuous value
            _ContinuousValue += scaledDelta;          

            // Update all Rhythm timers
            foreach (KeyValuePair<Tempo, TempoTimer> timer in TempoTimers)
                timer.Value.Update(_ContinuousValue);

            #endregion
        }

        #region TEMPO TIMER GETTERS

        public float GetPositionInCycle(Tempo tempo)
        {
            return TempoTimers[tempo].NormalizedTimer;
        }

        public float GetCurveAdjustedPositionInCycle(Tempo tempo)
        {
            return m_BeatCurve.Evaluate(GetPositionInCycle(tempo));
        }

        #endregion

        #region BPM TAPPER
              
        void RestartTapperCalculation()
        {
            print("Restarting BPM calculation");

            _Timer = 0;
            _NextBeatTime = 0;
            _CurrentBeatIndex = 0;
            _CurrentBarCount = 0;
            _BPMTapTimes.Clear();
            _ContinuousValue = 0;
            _AverageTimeBetweenBeatsCalculation = 0;

            AddBeat();
            OnFirstBeatEvent.Invoke();
        }

        public void AddBeat()
        {
            // Reset BPM if there are bpm taps but has been longer than 2 seconds since last tap
            if (_BPMTapTimes.Count > 0)
            {
                if (Time.time - _BPMTapTimes[_BPMTapTimes.Count - 1] > 2)
                {
                    //BPM = 0;
                    RestartTapperCalculation();
                    return;
                }
            }

            // Add tap time to list
            _BPMTapTimes.Add(Time.time);

            // If more than 2 taps calc BPM
            if (_BPMTapTimes.Count > 2)
                CalcBPM();

            // Set debug norm to 1
            _DEBUG_BeatNorm = 1;

            print("Beat added @ time: " + Time.time + " beat index: " + _BPMTapTimes.Count);

            // Fire beat event
            OnBeatEvent.Invoke(0);
        }

        private void ResetBPM()
        {
            BPM = 0;
        }

        public void CalcBPM()
        {
            // Find time between first and last beat
            float timeBetweenFirstAndLastBeat = _BPMTapTimes[_BPMTapTimes.Count - 1] - _BPMTapTimes[0];
            // Divide that by beat count
            _AverageTimeBetweenBeatsCalculation = timeBetweenFirstAndLastBeat / (_BPMTapTimes.Count - 1);
            // Get the BPM
            BPM = 60 / _AverageTimeBetweenBeatsCalculation;
            // Set next beat time
            _NextBeatTime = _Timer + _AverageTimeBetweenBeatsCalculation;

            print("Calculating BPM: " + BPM + " / " + _AverageTimeBetweenBeatsCalculation);
            print(_BPMTapTimes.Count + "    " + timeBetweenFirstAndLastBeat);
        }

        public void ToggleSendOnBeat()
        {
            _SendBeatEvent = !_SendBeatEvent;
        }

        #endregion

        void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;

            // Draw tempo timers
            int i = 0;
            float scale = .3f;
            foreach (KeyValuePair<Tempo, TempoTimer> timer in TempoTimers)
            {
                Gizmos.color = timer.Key == Tempo.Beat ? Color.red : Color.gray;
                Gizmos.DrawSphere(transform.position + (Vector3.right * i * scale * 2), scale * (1 - timer.Value.NormalizedTimer));
                i++;
            }
        }
    }
    
    public class TempoTimer
    {
        public delegate void RhythmTrigger(MasterTimer.Tempo rhythmType, int index);
        public static event RhythmTrigger onRhythmTrigger;

        // Tempo of the timer
        public MasterTimer.Tempo Tempo { get; private set; }
       
        // Frequency is determined by the tempo
        public float Freq { get; private set; }

        // Master continuous timer / Freq
        public float NormalizedTimer { get; private set; }

        // INdex of the beat that it's sending
        int _BeatIndex;

        float _TimerOffset = 0;

        // Constructor that sets the Freq based on the tempo
        public TempoTimer(MasterTimer.Tempo tempoType)
        {
            Tempo = tempoType;
            switch(tempoType)
            {
                case MasterTimer.Tempo.Phrase4:
                    Freq = 64;
                    break;
                case MasterTimer.Tempo.Phrase:
                    Freq = 16;
                    break;
                case MasterTimer.Tempo.Bar:
                    Freq = 4;
                    break;
                case MasterTimer.Tempo.Beat:
                    Freq = 1;
                    break;
                case MasterTimer.Tempo.Eighth:
                    Freq = .25f;
                    break;
                case MasterTimer.Tempo.Sixteenth:
                    Freq = .0125f;
                    break;
            }
        }

        public void Update(float masterTimer)
        {
            float prevTime = NormalizedTimer;
            NormalizedTimer = (masterTimer % Freq) / Freq;

            //ModulatedTimer = masterTimer % Freq;

            // If the timer has looped back around past 0 then fire off beat event
            if (NormalizedTimer < prevTime)
            {                
                _BeatIndex = (int)(masterTimer / Freq);
                _BeatIndex = _BeatIndex % 4;

                onRhythmTrigger?.Invoke(Tempo, _BeatIndex);
            }
        }
    }
}

