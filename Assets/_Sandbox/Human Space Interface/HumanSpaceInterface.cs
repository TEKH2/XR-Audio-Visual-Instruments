using System.Collections;
using System.Collections.Generic;
using Unity.Entities.UniversalDelegates;
using UnityEngine;

[System.Serializable]
public class RangedFloat
{
    float _Value = 0;
    public float Value
    {
        get { return _Value; }
        set
        {
            if(_AutoRangeInput)
            {
                if (value < _InputMin)
                    _InputMin = value;
                else if(value > _InputMax)
                    _InputMax = value;
            }

            _Value = value;
        }
    }
    public float _InputMin = 0;
    public float _InputMax = 1;
   
    public float NormOutput { get { return Mathf.InverseLerp(_InputMin, _InputMax, _Value); } }

    public float _OutputMin = 0;
    public float _OutputMax = 1;
    public float RangedOutput { get { return Mathf.Lerp(_OutputMin, _OutputMax, NormOutput); } }

    public bool _AutoRangeInput = false;

}

public class HumanSpaceInterface : MonoBehaviour
{
    public Transform _LeftHand;
    public Transform _RightHand;
    public Transform _CenterTransform;


    // Seperation between hand positions
    public RangedFloat _Seperation;
    // X rotation difference between hands
    public RangedFloat _Twist;
    // Rotation of hand to hand vector about the forward axis
    public RangedFloat _Roll;
    // Y rotation of central point from forward
    public RangedFloat _Pan;

    public Vector3 _RotOffset;

    Vector3 _HandToHandVector;

    Vector3 _CenterFlatX;

    Vector3 _CenterOnGround;

    public bool _Debug = false;

    Vector3 _PrevCenterPos;

    // Start is called before the first frame update
    void Start()
    {
        // Vector from left hand ot right hand
        Vector3 leftToRightVector = _RightHand.position - _LeftHand.position;
        _CenterTransform.position = (_LeftHand.position + _RightHand.position) / 2f;
        Quaternion centerRot = Quaternion.LookRotation(leftToRightVector) * Quaternion.Euler(_RotOffset);
        _CenterTransform.rotation = centerRot;

        _PrevCenterPos = _CenterTransform.position;
    }

    Vector3 leftForwardOnPlane;
    Vector3 rightForwardOnPlane;
    // Update is called once per frame
    void Update()
    {
        // Vector from left hand ot right hand
        _HandToHandVector = _RightHand.position - _LeftHand.position;

        // Position and rotate the central point
        _CenterTransform.position = (_LeftHand.position + _RightHand.position) / 2f;

        //Quaternion centerRot = TransformExtensions.SmoothRotation(_CenterTransform.position, _PrevCenterPos, _CenterTransform.rotation);
        Quaternion centerRot = Quaternion.LookRotation(_HandToHandVector) * Quaternion.Euler(_RotOffset);
        _CenterTransform.rotation = centerRot;

        // -----------------  SEPERATION - Distance between hand positions
        _Seperation.Value = Vector3.Distance(_LeftHand.position, _RightHand.position);

        // -----------------  TWIST - Project each hands forward vector onto the plane perpendicular to the central transform and dot the vectors
        leftForwardOnPlane = _CenterTransform.InverseTransformPoint(_LeftHand.position + _LeftHand.forward);
        leftForwardOnPlane.x = 0;
        rightForwardOnPlane = _CenterTransform.InverseTransformPoint(_RightHand.position + _RightHand.forward);
        rightForwardOnPlane.x = 0;
        _Twist.Value = Vector3.Dot(leftForwardOnPlane, rightForwardOnPlane);
        //print(Vector3.Dot(leftForwardOnPlane, rightForwardOnPlane));

        // -----------------  ROLL - Dot between centreal transform right vector and vector between hands

        if(_CenterTransform.position.z < 0)
        {

        }
        else
        {

        }

        _CenterFlatX = Vector3.Cross((transform.position - _CenterTransform.position).normalized, Vector3.up);
        float signedAngleRoll = VectorExtensions.SignedAngleBetweenVectorsXY(_HandToHandVector.normalized, _CenterFlatX);
        signedAngleRoll = Mathf.Clamp(signedAngleRoll ,- 90,90);
        _Roll.Value = signedAngleRoll;


        //if(signedAngleRoll < -90 || signedAngleRoll > 90)
        //{
        //    _CenterFlatX = Vector3.Cross((_CenterTransform.position - transform.position).normalized, Vector3.up);
        //    signedAngleRoll = VectorExtensions.SignedAngleBetweenVectorsXY(_HandToHandVector.normalized, _CenterFlatX);
        //    _Roll.Value = signedAngleRoll;
        //}

        // -----------------  PAN
        _CenterOnGround = new Vector3(_CenterTransform.position.x, 0, _CenterTransform.position.z);
        float signedAnglePan = VectorExtensions.SignedAngleBetweenVectorsXZ(transform.forward, (_CenterOnGround - transform.position).normalized);
        _Pan.Value = signedAnglePan;


        if (_Debug)
            Debug.Log("Seperation: " + _Seperation.RangedOutput + "  Twist: " + _Twist.RangedOutput + "  Roll: " + _Roll.RangedOutput + "  Pan: " + _Pan.RangedOutput);


        _PrevCenterPos = _CenterTransform.position;
    }

    

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.gray;
        Gizmos.DrawLine(_LeftHand.position, _LeftHand.position + _LeftHand.forward);
        Gizmos.DrawLine(_RightHand.position, _RightHand.position + _RightHand.forward);
        Gizmos.DrawLine(_RightHand.position, _LeftHand.position);

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(_CenterTransform.position, _CenterTransform.position + _CenterTransform.forward * .5f);

        Gizmos.color = Color.green;
        Gizmos.DrawLine(_CenterTransform.position, _CenterTransform.position + _CenterTransform.up * .5f);



        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(_CenterTransform.TransformPoint(leftForwardOnPlane), .1f);
        Gizmos.DrawSphere(_CenterTransform.TransformPoint(rightForwardOnPlane), .1f);

        Gizmos.color = Color.yellow;
        Vector3 offsetCenter = _CenterTransform.position + (_CenterTransform.forward * .1f);
        Gizmos.DrawLine(offsetCenter, offsetCenter + _CenterFlatX);
        Gizmos.DrawLine(offsetCenter, offsetCenter + _HandToHandVector.normalized);

        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + _CenterOnGround.normalized);
        Gizmos.DrawLine(transform.position, transform.position + transform.forward);

        //Gizmos.DrawLine(_LeftHand.position, _LeftHand.position + leftForwardOnPlane);
        //Gizmos.DrawLine(_RightHand.position, _RightHand.position + rightForwardOnPlane);
    }
}
