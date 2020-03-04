using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Basic class to record and playback data values
/// </summary>
public class DataRecordPlayback<T>
{
    List<float> _RecordedTimes = new List<float>();
    List<T> _RecordedValues = new List<T>();
    float _RecordingLength = 0;

    int _PrevPlaybackIndex = 0;
    float _PrevSeekTime = 0;
    float _StartTime;

    // Resets all lists
    public void StartRecording(T value)
    {
        Debug.Log("Starting recording");

        _PrevPlaybackIndex = 0;
        _PrevSeekTime = 0;
        _RecordingLength = 0;
        _StartTime = Time.time;

        _RecordedTimes.Clear();
        _RecordedValues.Clear();
        RecordValue(value);
    }

    // Record a value
    public void RecordValue(T value)
    {
        _RecordedTimes.Add(Time.time - _StartTime);
        _RecordedValues.Add(value);
        _RecordingLength = Time.time - _StartTime;
    }

    public T SeekValueAtNoramlizedTime(float normalizedTime)
    {
        return SeekValueAtTime(normalizedTime * _RecordingLength);
    }

    // Gets the closest value to the seek time, does not interpolate data
    public T SeekValueAtTime(float seekTime)
    {
        if (seekTime > _RecordingLength)
            seekTime %= _RecordingLength;

        int startIndex = 0;
        int index = 0;

        if (seekTime > _PrevSeekTime)
            startIndex = _PrevPlaybackIndex;

        for (int i = startIndex; i < _RecordedTimes.Count-1; i++)
        {
            // if seek time is larger than the recorded and less than next recorded time at this index
            if(seekTime > _RecordedTimes[i] && seekTime < _RecordedTimes[i+1]) 
            {
                // Get closest index
                if (Mathf.Abs(seekTime - _RecordedTimes[i]) < Mathf.Abs(seekTime - _RecordedTimes[i + 1]))
                    index = i;
                else
                    index = i + 1;
            }
        }

        _PrevSeekTime = seekTime;
        _PrevPlaybackIndex = index;

        return _RecordedValues[index];
    }
}
