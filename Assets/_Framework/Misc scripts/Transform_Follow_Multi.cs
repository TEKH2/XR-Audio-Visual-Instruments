using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EXP
{
    public class Transform_Follow_Multi : Transform_Follow
    {
        public Transform _FollowTform0;
        public Transform _FollowTform1;

        void Awake()
        {
            if (_FollowTform0.gameObject.activeInHierarchy)
                _TransformToFollow = _FollowTform0;
            else
                _TransformToFollow = _FollowTform1;
        }
    }
}
