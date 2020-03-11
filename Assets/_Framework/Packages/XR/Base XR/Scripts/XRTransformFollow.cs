using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace EXP.XR
{
    public class XRTransformFollow : Transform_Follow
    {
        [Header("XR Follow")]
        public XRNode _XRNode = XRNode.RightHand;
       
        protected override void Start()
        {
           switch(_XRNode)
           {
                case XRNode.CenterEye:
                    _TransformToFollow = XRControllers.Instance._HMD;
                    break;
                case XRNode.LeftHand:
                    _TransformToFollow = XRControllers.Instance._LeftController;
                    break;
                case XRNode.RightHand:
                    _TransformToFollow = XRControllers.Instance._RightController;
                    break;
            }

            base.Start();
        }
    }
}
