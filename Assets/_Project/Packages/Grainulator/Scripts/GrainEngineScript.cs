using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(Granulator))]
public class GrainEngineScript : MonoBehaviour
{
    [Range(0.0f, 1.0f)]
    public float RPM = 0;
    [Range(0.0f, 1.0f)]
    public float minGrainPos = 0;
    [Range(0.0f, 1.0f)]
    public float maxGrainPos = 1;

    public int minGrainLength = 10;
    public int maxGrainLength = 1000;

    public int minGrainDist = 50;
    public int maxGrainDist = 10;

    public float minGrainPitch = .5f;
    public float maxGrainPitch = 2f;

    [Range(0.0f, 1.0f)]
    public float minGrainVol = 1.0f;
    [Range(0.0f, 1.0f)]
    public float maxGrainVol = .8f;

    private Granulator granulator;

    //---------------------------------------------------------------------
    void Start() // change to awake?
    {
        granulator = GetComponent<Granulator>();
    }

    //---------------------------------------------------------------------
    void Update()
    {
        if (minGrainPitch < 0) minGrainPitch = 0;
        granulator._EmitGrainProps.Duration = (int)Mathf.Lerp(minGrainLength, maxGrainLength, RPM);
        granulator._EmitGrainProps.Position = Mathf.Lerp(minGrainPos, maxGrainPos, RPM);
        granulator._TimeBetweenGrains = (int)Mathf.Lerp(minGrainDist, maxGrainDist, RPM);
        granulator._EmitGrainProps.Pitch = Mathf.Lerp(minGrainPitch, maxGrainPitch, RPM);
        granulator._EmitGrainProps.Volume = Mathf.Lerp(minGrainVol,maxGrainVol, RPM);
    }
}
