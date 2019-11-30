using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace EXP
{
    public enum InteractableState
    {
        Normal,
        Hover,
        Interacting,
        Disabled,
    }        

    /// Base class for all interactables. 
    /// Handles basic state management, colour changing and scaling when in normal, hovered, interacting and disables states
    /// ** Collider should sit on a seperate parent object to the object that is scaled as to stop the trigger area being scaled with it
    [RequireComponent(typeof(Collider))]
    public class InteractableBase : MonoBehaviour
    {
        #region VARIABLES
        protected InteractableState _State = InteractableState.Normal;
        public InteractableState State { get { return _State; } }

        [Header("Base interactable vars")]
        public MeshRenderer _HoverHighlightMesh;

        // State colours
        public Color _NormalCol = Color.gray;
        public Color _HoverCol = Color.yellow;
        public Color _InteractingCol = Color.red;
        public Color _DisabledCol = Color.gray * .5f;

        // State scalars
        public Vector3 _HoverScaler = Vector3.one * .8f;
        protected Vector3 _OriginalScale;
        #endregion

        #region UNITY METHODS
        private void Awake()
        {
            // Set collider to true
            Collider collider = GetComponent<Collider>();
            collider.isTrigger = true;

            _OriginalScale = _HoverHighlightMesh.transform.localScale;
        }

        protected virtual void Update()
        {
            /*
            if (_State == InteractableState.Hover || _State == InteractableState.Interacting)
            {
                _HoverHighlightMesh.transform.localScale = Vector3.Lerp(_HoverHighlightMesh.transform.localScale, _OriginalScale.ScaleReturn(_HoverScaler), Time.deltaTime * 8);
            }
            else
            {
                _HoverHighlightMesh.transform.localScale = Vector3.Lerp(_HoverHighlightMesh.transform.localScale, _OriginalScale, Time.deltaTime * 8);
            }
            */
        }
        #endregion

        #region INTERACTIONS
        public virtual bool Interact()
        {
            if (_State == InteractableState.Hover)
            {
                print(name + " Starting interaction");
                SetInteractingState();
                return true;
            }

            return false;
        }

        public virtual void EndInteraction()
        {
            if (_State == InteractableState.Interacting)
            {
                print(name + " Ending interaction");
                SetNormalState();
            }
        }

        public void StartHover()
        {
            if (_State == InteractableState.Normal)
            {
                SetHoverState();
            }
        }

        public void EndHover()
        {
            if (_State == InteractableState.Hover)
            {
                SetNormalState();
            }
        }
        #endregion

        #region SET STATE FUNCTIONS
        protected virtual void SetNormalState()
        {
            _State = InteractableState.Normal;
            SetMeshColour(_NormalCol);
            _HoverHighlightMesh.transform.localScale = _OriginalScale;
        }

        protected virtual void SetHoverState()
        {
            _State = InteractableState.Hover;
            SetMeshColour(_HoverCol);
            _HoverHighlightMesh.transform.localScale = _OriginalScale.ScaleReturn(_HoverScaler);
        }

        protected virtual void SetInteractingState()
        {
            _State = InteractableState.Interacting;
            SetMeshColour(_InteractingCol);
            _HoverHighlightMesh.transform.localScale = _OriginalScale.ScaleReturn(_HoverScaler);
        }

        protected virtual void SetDisabledState()
        {
            _State = InteractableState.Disabled;
            SetMeshColour(_DisabledCol);
            _HoverHighlightMesh.transform.localScale = _OriginalScale;
        }
        #endregion

        protected void SetMeshColour(Color col)
        {
            _HoverHighlightMesh.material.SetColor("_BaseColor", col);
        }
    }
}
