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
        dspParams._SampleTail = _NumSegments * 3;
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
        int writeSegment = 0;   // Segment currently being written to
        int sampleIndexInSegment = 0;     // Number of samples since last capture
        int crossingCount = 0;  // Number of rising zero-crossings since last capture
        float prevInput = 0;    // Used to create smooth overlaps
        float energySum = 0;    // Used to accumulate segment energy total
        int totalLength = 0;    // Total length of all segments

        const int _MinLength = 100;
        const int _MaxSegmentLength = 2000;

        int segmentDataStartIndex = sampleBuffer.Length - dspParams._SampleTail - 1;

        int numSegments = (int)dspParams._Value8;

        // NOTE: The DSP buffer size is extended by samples to include segment length, offset and RMS data values.

        //Debug.Log("DSP Size: " + dspBuffer.Length);
        //Debug.Log("Sample Size: " + sampleBuffer.Length); // - 1 + 2 * numSegments + numSegments - 1);
        //Debug.Log("Segment Data Size: " + numSegments * 3);


        float inputCurrent = 0;
        float inputPrevious = 0;
        float unbiasedCurrent = 0;
        float unbiasedPrevious = 0;
        int offsetCurrent = 0;
        int offsetPrevious = 0;
        int lengthCurrent = 0;
        int lengthPrevious = 0;
        int previousLength = 0;

        float unbiasedInput = 0;
        float previousUnbiasedSample = 0;
        

        int[] lengths = new int[dspParams._SampleTail];

        //-- RECORDING SECTION
        for (int i = 0; i < segmentDataStartIndex; i++)
        {
            // unbiased_input = dcblock(in1); dcblock : A one-pole high-pass filter to remove DC components. Equivalent to the GenExpr: History x1, y1; y = in1 - x1 + y1*0.9997; x1 = in1; y1 = y; out1 = y;
            //


            inputCurrent = sampleBuffer[i].Value;
            unbiasedCurrent = inputCurrent - inputPrevious + unbiasedPrevious * 0.999997f;

            unbiasedPrevious = unbiasedCurrent;
            inputPrevious = inputCurrent;

            dspBuffer[i] = new DSPSampleBufferElement { Value = unbiasedCurrent };

            //unbiasedInput = DSP_Utils_DOTS.UnbiasedInput(sampleBuffer[i].Value, sampleBuffer[Mathf.Clamp(i-1, 0, segmentDataStartIndex)].Value, prevInput);
            //dspBuffer[i] = new DSPSampleBufferElement { Value = unbiasedInput };

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
                        //float offset = prevInput / (prevInput - unbiasedInput);
                        //previousOffset = GetSegmentData(1, writeSegment);

                        offsetPrevious = offsetCurrent;
                        offsetCurrent = i;

                        offset = i;

                        //int length = sampleIndexInSegment + offset - previousOffset - 1;
                        int segmentLength = sampleIndexInSegment;

                        //if (segmentLength + offset >= dspBuffer.Length - dspParams._SampleTail)
                        //    segmentLength = (int)Mathf.Clamp(segmentLength, 0, dspBuffer.Length - dspParams._SampleTail - offset);

                        totalLength = totalLength - previousLength + segmentLength;
                        SetSegmentData(0, writeSegment, segmentLength);
                        previousLength = segmentLength;

                        int currentSegmentLength = GetSegmentData(0, writeSegment);
                        lengths[writeSegment] = segmentLength;
                        //float rms = Mathf.Sqrt(energySum / Mathf.Floor(length));
                        //SetSegmentData(2, writeSegment, rms);

                        crossingCount = 0;
                        energySum = 0;

                        writeSegment++;
                        writeSegment = writeSegment % numSegments;

                        SetSegmentData(1, writeSegment, offset);
                        sampleIndexInSegment = 1;
                    }
                }
            }

            sampleIndexInSegment++;
        }

        float rate = dspParams._Value7;

        float playIndex = 0;
        int playSegment = 0;    // Segment currently being played
        int playOffset = 0;     // Offset of playing segment
        int playLength = GetSegmentData(0, playSegment);
        

        //Debug.Log(playLength);

        //-- PLAYBACK SECTIOn
        for (int i = 0; i < dspBuffer.Length - dspParams._SampleTail - 2; i++)
        {
            // Normal pitch mode
            if (dspParams._Value5 == 0)
            {

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


            //Debug.Log("Rate: " + rate);

            //Debug.Log("Play Index: " + playIndex);
            //Debug.Log("Offset: " + GetSegmentData(1, (int)playSegment));


            // Generate play index
            playIndex += rate;

            int actualPlayIndex = (int)playIndex % playLength;
            //while (actualPlayIndex >= playLength)
            //    actualPlayIndex -= playLength;
            int sampleToPlay = (int)(playOffset + actualPlayIndex); // + GetSegmentData(1, playSegment));

             if (sampleToPlay > dspBuffer.Length - dspParams._SampleTail - 1)
                Debug.Log("TOO LONG!");

            float outputSample = dspBuffer[sampleToPlay].Value;

            //Debug.Log(outputSample);


            //-- Populate output sample with interpolated DSP sample
            
            //sampleBuffer[i] = new GrainSampleBufferElement { Value = outputSample };
            //sampleBuffer[i] = new GrainSampleBufferElement { Value = DSP_Utils_DOTS.LinearInterpolate(dspBuffer, playOffset + actualPlayIndex + GetSegmentData(1, (int)playSegment)) };



            float phase = i / (dspBuffer.Length - dspParams._SampleTail);

            //-- Switch to a new playback segment?
            if (playIndex >= playLength * Mathf.Floor(dspParams._Value3))
            {
                playIndex = actualPlayIndex;

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

                while (playSegment > numSegments)
                    playSegment += playSegment;
                while (playSegment < 0)
                    playSegment += playSegment;
            }

            playLength = GetSegmentData(0, playSegment);
            playOffset = GetSegmentData(1, playSegment);
            //playRMS = GetSegmentData(2, playSegment);


        }


        // Segment types are: 0 = Length, 1 = Offset, 2 = RMS
        void SetSegmentData(int type, int segment, int input)
        {
            int index = dspBuffer.Length - dspParams._SampleTail - 1;
            dspBuffer[dspBuffer.Length - dspParams._SampleTail - 1 + type * numSegments + segment] = new DSPSampleBufferElement { Value = input};
        }

        int GetSegmentData(int type, int segment)
        {
            return (int)dspBuffer[dspBuffer.Length - dspParams._SampleTail - 1 + type * numSegments + segment].Value;
        }
    }
}