using Unity.Entities;
using UnityEngine;

// A wonderfully glitchy DSP effect, ported from Graham Wakefield's 2012 example in Max MSP's Gen DSP effects
public class DSP_Chopper : DSPBase
{
    const int _NumSegments = 32;

    [Range(0f, 1f)]
    [SerializeField]
    public float _Mix = 1;

    [Range(1f, 16f)]
    [SerializeField]
    public float _Crossings = 2;

    [Range(32, 2048)]
    [SerializeField]
    public int _MaxSegmentLength = 2048;

    [Range(32, 512)]
    [SerializeField]
    public int _MinSegmentLength = 128;

    [Range(1f, 16f)]
    [SerializeField]
    public float _Repeats = 1;

    public PlayMode _PlayMode;
    public PitchedMode _PitchedMode;

    [Range(1f, 2000f)]
    [SerializeField]
    public float _Frequency = 220;

    [Range(0f, 16f)]
    [SerializeField]
    public float _Rate = 1;


    public enum PlayMode
    {
        Forward,
        Reverse,
        Walk,
        Random
    }

    public enum PitchedMode
    {
        Normal,
        Ascending,
        Decending,
        Pitched
    }


    int _SampleRate;

    public void Start()
    {
        _SampleRate = AudioSettings.outputSampleRate;
    }

    public override DSPParametersElement GetDSPBufferElement()
    {
        DSPParametersElement dspParams = new DSPParametersElement();
        dspParams._DSPType = DSPTypes.Chopper;
        dspParams._SampleRate = _SampleRate;
        dspParams._SampleTail = _NumSegments * 2;
        dspParams._Mix = _Mix;
        dspParams._Value0 = _Crossings;
        dspParams._Value1 = _MaxSegmentLength;
        dspParams._Value2 = _MinSegmentLength;
        dspParams._Value3 = _Repeats;
        dspParams._Value4 = (int)_PlayMode;
        dspParams._Value5 = (int)_PitchedMode;
        dspParams._Value6 = _Frequency;
        dspParams._Value7 = _Rate;
        dspParams._Value8 = _NumSegments;


        return dspParams;
    }

    public static void ProcessDSP(DSPParametersElement dspParams, DynamicBuffer<GrainSampleBufferElement> sampleBuffer, DynamicBuffer<DSPSampleBufferElement> dspBuffer)
    {
        int sampleIndexInSegment = 0;     // Number of samples since last capture
        int crossingCount = 0;  // Number of rising zero-crossings since last capture
        float energySum = 0;    // Used to accumulate segment energy total

        const int _MinLength = 100;
        const int _MaxSegmentLength = 2000;

        int segmentDataStartIndex = sampleBuffer.Length - dspParams._SampleTail - 1;

        int numSegments = (int)dspParams._Value8;

        // NOTE: The DSP buffer size is extended by samples to include segment length and offset.

        float inputCurrent = 0;
        float inputPrevious = 0;
        float unbiasedCurrent = 0;
        float unbiasedPrevious = 0;

        int segmentCurrent = 0;
       
        //-- RECORDING SECTION
        for (int i = 0; i < segmentDataStartIndex; i++)
        {
            inputCurrent = sampleBuffer[i].Value;
            unbiasedCurrent = inputCurrent - inputPrevious + unbiasedPrevious * 0.999997f;

            inputPrevious = inputCurrent;

            dspBuffer[i] = new DSPSampleBufferElement { Value = unbiasedCurrent };

            energySum = energySum + unbiasedCurrent * unbiasedCurrent;
            
            // Is sample rising and crossing zero?
            if (DSP_Utils_DOTS.IsCrossing(unbiasedCurrent, unbiasedPrevious))
            {
                if (sampleIndexInSegment > _MaxSegmentLength)
                {
                    crossingCount = 0;
                    sampleIndexInSegment = 0;
                }
                else
                {
                    crossingCount++;

                    // If segment is complete
                    if (crossingCount > dspParams._Value0 && sampleIndexInSegment >= _MinLength)
                    {
                        //-- Set length
                        SetSegmentData(0, segmentCurrent, sampleIndexInSegment);
                        //-- Set offset
                        SetSegmentData(1, segmentCurrent, i - sampleIndexInSegment);

                        //-- Reset countters, and increment segment
                        crossingCount = 0;
                        sampleIndexInSegment = 0;

                        segmentCurrent++;
                        segmentCurrent = (int)Mathf.Repeat(segmentCurrent, numSegments - 1);
                    }
                }
            }
            else
                sampleIndexInSegment++;

            unbiasedPrevious = unbiasedCurrent;
        }

        // Once the recording section is complete, update segment number to ACTUAL number of segements recorded
        numSegments = segmentCurrent;

        float rate = dspParams._Value7;

        float playIndex = 0;
        int playSegment = 0;
        int playOffset = 0;
        int playLength = GetSegmentData(0, playSegment);

        //-- PLAYBACK SECTION
        for (int i = 0; i < segmentDataStartIndex; i++)
        {
            // Normal pitch mode
            if (dspParams._Value5 == 0)
            {
                // Maintain default playback rate
            }
            // Ascending pitch mode
            else if (dspParams._Value5 == 1)
            {
                float d = playIndex / playLength;
                rate = rate * Mathf.Max(1, d);
            }
            // Decending pitch mode
            else if (dspParams._Value5 == 2)
            {
                float d = Mathf.Ceil(playIndex / playLength);
                rate = rate / Mathf.Max(1, d*d);
            }
            // Pitched pitch mode
            else
            {
                rate = dspParams._Value6 * playLength / (dspParams._SampleRate * dspParams._Value0);
            }

            // Increase playback index by playback rate
            playIndex += rate;

            // Keep segment playback index within size of current segment
            float sampleInSegment = Mathf.Repeat(playIndex, playLength - 1);

            // Ensure samples are only read from the grain sample area of the DSP buffer
            int sampleToPlay = (int)Mathf.Repeat(playOffset + sampleInSegment, segmentDataStartIndex - 1);

            // Populate output sample with interpolated DSP sample
            sampleBuffer[i] = new GrainSampleBufferElement { Value = Mathf.Lerp(sampleBuffer[i].Value, DSP_Utils_DOTS.LinearInterpolate(dspBuffer, sampleToPlay), dspParams._Mix) };

            // Prepare phase for noise mode
            float phase = i / (dspBuffer.Length - dspParams._SampleTail);

            //-- Switch to a new playback segment?
            if (playIndex >= playLength * Mathf.Floor(dspParams._Value3))
            {
                playIndex = sampleInSegment;

                // Forward
                if (dspParams._Value4 == 0)
                {
                    playSegment++;
                }
                // Reverse
                else if (dspParams._Value4 == 1)
                {
                    playSegment--;
                }
                // Walk
                else if (dspParams._Value4 == 2)
                {
                    int direction = (int)Mathf.PerlinNoise(phase, phase * 0.5f) * 2 - 1;
                    playSegment += direction;
                }
                // Random
                else
                {
                    int direction = 1 + (int)Mathf.Ceil((numSegments * Random.value + 1) / 2);
                    playSegment += direction;
                }

                // Ensure new playback segment is within stored segments
                playSegment = (int)Mathf.Repeat(playSegment, numSegments - 1);

                // Get new lenght and offset for next playback sample
                playLength = GetSegmentData(0, playSegment);
                playOffset = GetSegmentData(1, playSegment);


                //Debug.Log("NEW SEGMENT: " + playSegment + "    NEW OFFSET: " + playOffset + "    NEW LENGTH: " + playLength);
                //playRMS = GetSegmentData(2, playSegment);
            }
        }


        // Segment types are: 0 = Length, 1 = Offset
        void SetSegmentData(int type, int segment, int input)
        {
            dspBuffer[segmentDataStartIndex + type + segment * 2] = new DSPSampleBufferElement { Value = input};
        }

        int GetSegmentData(int type, int segment)
        {
            return (int)dspBuffer[segmentDataStartIndex + type + segment * 2].Value;
        }
    }
}