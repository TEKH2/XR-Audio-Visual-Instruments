using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class ExampleXRBaseInteractable : XRBaseInteractable
{
    protected override void OnSelectEntered(XRBaseInteractor interactor)
    {
        if (!interactor)
            return;
        base.OnSelectEntered(interactor);

        transform.position += Random.insideUnitSphere;
    }
}
