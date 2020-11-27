using Unity.Entities;

public class DSP_Flange : DSPBase
{
    public float _FlangeSoft;
    public float _FlangeWide;

    public override DSPParametersElement GetDSPBufferElement()
    {
        DSPParametersElement dspBuffer = new DSPParametersElement();
        dspBuffer._DSPType = DSPTypes.Flange;
        dspBuffer._Value0 = _FlangeSoft;
        dspBuffer._Value1 = _FlangeWide;

        return dspBuffer;
    }

    public static void ProcessDSP(DSPParametersElement dspParams, DynamicBuffer<GrainSampleBufferElement> sampleBuffer, DynamicBuffer<DSPSampleBufferElement> dspBuffer)
    {
        // Do DSP magic here
        for (int i = 0; i < sampleBuffer.Length; i++)
        {
            sampleBuffer[i] = new GrainSampleBufferElement { Value = sampleBuffer[i].Value * dspParams._Value0 };
            sampleBuffer[i] = new GrainSampleBufferElement { Value = sampleBuffer[i].Value + dspParams._Value1 };
        }
    }
}
