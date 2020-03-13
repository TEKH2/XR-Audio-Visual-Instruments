using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

public class TestingDOTsEventsV1 : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<PipeMoveSystemV2>().OnPassedXEvent += TestingDOTsEvents_OnPipePassed;
    }

    private void TestingDOTsEvents_OnPipePassed(object sender, System.EventArgs e)
    {
        Debug.Log("Passed x0.");
    }
}
