using Unity.Entities;

public class DSP_Bitcrush : DSPBase
{
    public float _BitAmount;
    public float _CrushAmount;

    public override DSPParametersElement GetDSPBufferElement()
    {
        DSPParametersElement dspBuffer = new DSPParametersElement();
        dspBuffer._DSPType = DSPTypes.Bitcrush;
        dspBuffer._Value0 = _BitAmount;
        dspBuffer._Value1 = _CrushAmount;

        return dspBuffer;
    }

    public static void ProcessDSP(DSPParametersElement dspParams, DynamicBuffer<GrainSampleBufferElement> sampleBuffer)
    {
        // Do DSP magic here
        for (int i = 0; i < sampleBuffer.Length; i++)
        {
            sampleBuffer[i] = new GrainSampleBufferElement { Value = sampleBuffer[i].Value * dspParams._Value0 };
        }
    }
}
