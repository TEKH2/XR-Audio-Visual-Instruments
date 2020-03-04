using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EXP
{
    // Follows on update or reparents to a transform at start
    public class Transform_Follow : MonoBehaviour
    {
        #region VARIABLES

        [Header("Transform Follow Base")]
        public Transform _TransformToFollow;
        public float _FollowSmoothing = 8;
        public bool _ParentAtStart = false;

        public bool _FollowPosition = false;
        public bool _FollowRotation = false;

        #endregion
        
        #region UNITY METHODS

        protected virtual void Start()
        {
            if (_ParentAtStart)
            {
                Debug.LogWarning(name + " PARENTING TOO: " + _TransformToFollow.name);
                transform.SetParent(_TransformToFollow);
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
                Destroy(this);
            }
        }

        void Update()
        {
            if (_FollowPosition)
            {
                if (_FollowSmoothing > 0)
                    transform.position = Vector3.Lerp(transform.position, _TransformToFollow.position, Time.deltaTime * _FollowSmoothing);
                else
                    transform.position = _TransformToFollow.position;
            }

            if(_FollowRotation)
            {
                if (_FollowSmoothing > 0)
                    transform.rotation = Quaternion.Slerp(transform.rotation, _TransformToFollow.rotation, Time.deltaTime * _FollowSmoothing);
                else
                    transform.rotation = _TransformToFollow.rotation;
            }
        }

        #endregion
    }
}
