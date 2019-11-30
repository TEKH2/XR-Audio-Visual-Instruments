using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EXP.XR;

namespace EXP.XR
{
    // Can highlight and interact with XRUI_InteractableBase
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(Rigidbody))]
    public class XRUI_Pointer : InteractableBase
    {
        // The interactable that the pointer is currently hovering or interacting with
        public InteractableBase _ActiveInteractable;
        
        #region UNITY METHODS

        // Start is called before the first frame update
        void Start()
        {
            XRControllers.Instance._RightControllerFeatures._XRFloatDict[XRFloats.Trigger].OnValueOne.AddListener(InteractionTrigger);
            XRControllers.Instance._RightControllerFeatures._XRFloatDict[XRFloats.Trigger].OnValueZero.AddListener(EndInteraction);

            Rigidbody rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        void OnTriggerEnter(Collider other)
        {
            if (_State == InteractableState.Interacting)
                return;

            print(name + "  TRIGGER ENTER: " + other.name);

            InteractableBase interactable = other.GetComponent<InteractableBase>();

            if (interactable == null)
                return;

            // If there is an existing active interactable, set it to normal
            if (_ActiveInteractable != null)
            {
                _ActiveInteractable.EndHover();
            }

            _ActiveInteractable = interactable;
            _ActiveInteractable.StartHover();
            _State = InteractableState.Hover;
        }

        void OnTriggerStay(Collider other)
        {
            if (_State != InteractableState.Normal)
                return;

            print(name + "  TRIGGER STAY: " + other.name);

            InteractableBase interactable = other.GetComponent<InteractableBase>();

            // If there is an existing active interactable, set it to normal
            if (_ActiveInteractable != null)
            {
                _ActiveInteractable.EndHover();
            }

            _ActiveInteractable = interactable;
            _ActiveInteractable.StartHover();
            _State = InteractableState.Hover;
        }

        void OnTriggerExit(Collider other)
        {
            if (_State == InteractableState.Interacting)
                return;

            print(name + "  TRIGGER EXIT: " + other.name);

            InteractableBase interactable = other.GetComponent<InteractableBase>();

            if (_ActiveInteractable != null && _ActiveInteractable.gameObject == other.gameObject)
            {
                _ActiveInteractable.EndHover();
                _State = InteractableState.Normal;
            }
        }

        #endregion

        void InteractionTrigger()
        {
            Interact();
        }

        public override bool Interact()
        {
            // If hovering then execture the interaction
            if (_State == InteractableState.Hover && _ActiveInteractable != null)
            {
               
                // If the interaction works then set state to interacting
                if (_ActiveInteractable.Interact())
                {                   
                    SetInteractingState();
                    return true;
                }
            }

            return false;
        }

        public override void EndInteraction()
        {
            print("Pointer end interaction");
            if (_State == InteractableState.Interacting)
            {
                _ActiveInteractable.EndInteraction();
                SetNormalState();
            }
        }
    }
}
