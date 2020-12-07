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
        int writeSegment = 1;   // Segment currently being written to
        int writeIndex = 0;     // Number of samples since last capture
        int crossingCount = 0;  // Number of rising zero-crossings since last capture
        float playSegment = 0;    // Segment currently being played
        float playIndex = 0;      // Sample index of playback
        float playLength = 0;     // Length of the playing segment
        float playOffset = 0;     // Offset of playing segment
        float playRMS = 0;      // Loudness of playing segment
        float prevInput = 0;    // Used to create smooth overlaps
        float energySum = 0;    // Used to accumulate segment energy total
        float totalLength = 0;    // Total length of all segments

        const int _MinLength = 100;
        const int _MaxLength = 2000;

        int numSegments = (int)dspParams._Value8;

        // NOTE: The DSP buffer size is extended by samples to include segment length, offset and RMS data values.

        //Debug.Log("DSP Size: " + dspBuffer.Length);
        //Debug.Log("Sample Size: " + sampleBuffer.Length); // - 1 + 2 * numSegments + numSegments - 1);
        //Debug.Log("Segment Data Size: " + numSegments * 3);
        

        float unbiasedInput = 0;
        float previousUnbiasedSample = 0;

        //-- RECORDING SECTION
        for (int i = 0; i < sampleBuffer.Length - dspParams._SampleTail; i++)
        {
            // unbiased_input = dcblock(in1); dcblock : A one-pole high-pass filter to remove DC components. Equivalent to the GenExpr: History x1, y1; y = in1 - x1 + y1*0.9997; x1 = in1; y1 = y; out1 = y;
            //


            unbiasedInput = DSP_Utils_DOTS.UnbiasedInput(sampleBuffer[i].Value, sampleBuffer[Mathf.Clamp(i-1, 0, sampleBuffer.Length)].Value, prevInput);

            energySum = energySum + unbiasedInput * unbiasedInput;

            writeIndex++;



            dspBuffer[i] = new DSPSampleBufferElement { Value = unbiasedInput };

            if (DSP_Utils_DOTS.IsCrossing(unbiasedInput, previousUnbiasedSample))
            {
                if (writeIndex > _MaxLength)
                {
                    crossingCount = 0;
                    writeIndex = 0;
                }
                else
                {
                    crossingCount++;

                    // If segment is complete
                    if (crossingCount > dspParams._Value0 && writeIndex >= _MinLength)
                    {
                        float offset = prevInput / (prevInput - unbiasedInput);
                        float previousOffset = GetSegmentData(1, writeSegment);
                        float length = writeIndex + offset - previousOffset - 1;
                        float previousLength = GetSegmentData(0, writeSegment);
                        totalLength = totalLength - previousLength + length;
                        SetSegmentData(0, writeSegment, length);
                        float rms = Mathf.Sqrt(energySum / Mathf.Floor(length));
                        SetSegmentData(2, writeSegment, rms);

                        crossingCount = 0;
                        energySum = 0;

                        writeSegment++;
                        writeSegment = writeSegment % numSegments;

                        SetSegmentData(1, writeSegment, offset);
                        writeIndex = 1;
                    }
                }
            }
            prevInput = unbiasedInput;   
        }

        float rate = dspParams._Value7;

        Debug.Log(rate);

        playLength = GetSegmentData(0, (int)Mathf.Round(playSegment));

        //-- PLAYBACK SECTIOn
        for (int i = 0; i < sampleBuffer.Length - dspParams._SampleTail; i++)
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
            playIndex = playIndex + rate;

            float actualPlayIndex = playIndex % playLength;
            //while (actualPlayIndex >= playLength)
            //    actualPlayIndex -= playLength;


            float outputSample = dspBuffer[(int)(playOffset + actualPlayIndex + GetSegmentData(1, (int)playSegment))].Value;

            //Debug.Log(outputSample);


            //-- Populate output sample with interpolated DSP sample
            
            //sampleBuffer[i] = new GrainSampleBufferElement { Value = outputSample };
            //sampleBuffer[i] = new GrainSampleBufferElement { Value = DSP_Utils_DOTS.LinearInterpolate(dspBuffer, playOffset + actualPlayIndex + GetSegmentData(1, (int)playSegment)) };



            float phase = i / sampleBuffer.Length;

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
                    float direction = Mathf.PerlinNoise(phase, phase * 0.5f) * 2 - 1;
                    playSegment += direction;
                }
                // Random
                else
                {
                    float direction = 1 + Mathf.Ceil((numSegments * Random.value + 1) / 2);
                    playSegment += direction;
                }

                while (playSegment > numSegments)
                    playSegment += playSegment;
                while (playSegment < 0)
                    playSegment += playSegment;
            }

            playLength = GetSegmentData(0, (int)Mathf.Round(playSegment));
            playOffset = GetSegmentData(1, (int)Mathf.Round(playSegment));
            playRMS = GetSegmentData(2, (int)Mathf.Round(playSegment));


        }


        // Segment types are: 0 = Length, 1 = Offset, 2 = RMS
        void SetSegmentData(int type, int segment, float input)
        {
            dspBuffer[sampleBuffer.Length - dspParams._SampleTail - 1 + type * numSegments + segment] = new DSPSampleBufferElement { Value = input};
        }

        float GetSegmentData(int type, int segment)
        {
            return dspBuffer[sampleBuffer.Length - dspParams._SampleTail - 1 + type * numSegments + segment].Value;
        }
    }
}