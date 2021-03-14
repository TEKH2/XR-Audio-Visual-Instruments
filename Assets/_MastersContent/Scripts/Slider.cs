using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity.Interaction;
using static SliderManager;

[System.Serializable]
public class Slider
{
    private SliderManager _SliderManager;
    private GameObject _SliderObject;
    private GameObject _TetherDraw;
    public int _Index;
    public float _Speed;
    
    public Vector3 _SliderPosInContainer;

    private Transform _PreviousTransform;
    private Rigidbody _PreviousRB;
    private Collision newCollision;
    private Collision _StayCollision;

    private float _CollisionValue;
    private int _CollisionIndex;

    private float _PreviousAngularSpeed = 0;

    public void Update()
    {
        if (_SliderManager._ContainerShape == SliderManager.ContainerShapes.Tether)
            TetherDraw(_SliderManager);
    }

    public void FixedUpdate()
    {
        CalculatePosRelativeToContainer(_SliderManager);
        LimitVelocity(_SliderManager);
        LimitDistance(_SliderManager);

        _PreviousTransform = _SliderObject.transform;
        _PreviousRB = _SliderObject.GetComponent<Rigidbody>();
    }



    // Constructor to hold parsed SliderManager settings, create slider object and fully initialise
    public Slider(SliderManager settings, int index)
    {
        _SliderManager = settings;
        _SliderObject = new GameObject();
        _Index = index;

        _SliderObject.tag = "Slider";

        InitaliseName(_SliderManager);
        InitaliseTransform(_SliderManager);
        InitialiseMesh(_SliderManager);
        InitalisePhysics(_SliderManager);
        InitialiseBehaviour(_SliderManager);

        UpdateBehaviour(_SliderManager);

        //_Utils = new Utilities();
    }
    
    // Reset name
    public void InitaliseName(SliderManager settings)
    {
        _SliderObject.name = settings._Name + _Index;
    }

    // Reset transforms
    public void InitaliseTransform(SliderManager settings)
    {
        _SliderObject.transform.parent = settings._SliderParent.transform;
        _SliderObject.transform.localPosition = settings._Container.transform.localPosition;
    }

    // Clean up and apply mesh components
    public void InitialiseMesh(SliderManager settings)
    {
        if (_SliderObject.GetComponent<MeshRenderer>() != null)
        {
            MeshRenderer renderer = _SliderObject.GetComponent<MeshRenderer>();
            SliderManager.Destroy(renderer);
        }
        
        if (_SliderObject.GetComponent<MeshFilter>() != null)
        {
            MeshFilter filter = _SliderObject.GetComponent<MeshFilter>();
            SliderManager.Destroy(filter);
        }

        _SliderObject.AddComponent<MeshRenderer>();
        _SliderObject.GetComponent<Renderer>().material = settings._MaterialManager._Slider;

        
        Mesh mesh;
        if (settings._SliderShape == SliderManager.SliderShapes.Sphere)
            mesh = settings._MeshManager._Sphere;
        else if (settings._SliderShape == SliderManager.SliderShapes.Cube)
            mesh = settings._MeshManager._Cube;
        else
            mesh = settings._MeshManager._Sphere;

        _SliderObject.AddComponent<MeshFilter>();
        _SliderObject.GetComponent<MeshFilter>().mesh = mesh;

        if (_TetherDraw != null)
            GameObject.Destroy(_TetherDraw);

        if (settings._ContainerShape == SliderManager.ContainerShapes.Tether)
        {
            _TetherDraw = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Component tempComp = _TetherDraw.GetComponent<Collider>();
            GameObject.Destroy(tempComp);
            _TetherDraw.transform.parent = settings._DrawParent.transform;
        }
    }

    // Clean up and apply physics components
    public void InitalisePhysics(SliderManager settings)
    {
        if (_SliderObject.GetComponent<Rigidbody>() != null)
        {
            Rigidbody rigidbody = _SliderObject.GetComponent<Rigidbody>();
            SliderManager.Destroy(rigidbody);
        }
        _SliderObject.AddComponent<Rigidbody>();

        if (_SliderObject.GetComponent<Collider>() != null)
        {
            Collider collider = _SliderObject.GetComponent<Collider>();
            SliderManager.Destroy(collider);
        }

        if (_SliderManager._SliderShape == SliderManager.SliderShapes.Sphere)
            _SliderObject.AddComponent<SphereCollider>();
        else if (_SliderManager._SliderShape == SliderManager.SliderShapes.Cube)
            _SliderObject.AddComponent<BoxCollider>();
        else
            _SliderObject.AddComponent<BoxCollider>();

        _SliderObject.AddComponent<ChildCollision>();

        if (settings._ContainerShape == SliderManager.ContainerShapes.Sphere)
            _SliderObject.GetComponent<ChildCollision>().SetSphereCollider(settings.GetSphereCollider());

        _SliderObject.GetComponent<Collider>().material = settings._SliderPhysics;

        if (_SliderObject.GetComponent<ConfigurableJoint>() != null)
        {
            ConfigurableJoint joint = _SliderObject.GetComponent<ConfigurableJoint>();
            SliderManager.Destroy(joint);
        }

        if (_SliderManager._ContainerShape == SliderManager.ContainerShapes.Tether)
        {
            ConfigurableJoint joint = _SliderObject.AddComponent<ConfigurableJoint>();
            joint.connectedBody = settings._Container.GetComponent<Rigidbody>();
            joint.anchor = new Vector3(0, 0, 0);
            joint.axis = new Vector3(0, 0, 0);
            joint.connectedAnchor = new Vector3(0, 0, 0);
        }
    }

    // Clean up and apply interaction behaviour components
    public void InitialiseBehaviour(SliderManager settings)
    {
        if (settings._DirectControl)
        {
            if (_SliderObject.GetComponent<InteractionBehaviour>() != null)
            {
                InteractionBehaviour behaviour = _SliderObject.GetComponent<InteractionBehaviour>();
                SliderManager.Destroy(behaviour);
            }
            _SliderObject.AddComponent<InteractionBehaviour>();
            _SliderObject.GetComponent<InteractionBehaviour>().ignoreContact = true;

            if (_SliderObject.GetComponent<SimpleInteractionGlow>() != null)
            {
                SimpleInteractionGlow glow = _SliderObject.GetComponent<SimpleInteractionGlow>();
                SliderManager.Destroy(glow);
            }
            _SliderObject.AddComponent<SimpleInteractionGlow>();

            _SliderObject.GetComponent<Rigidbody>().maxAngularVelocity = settings._MaxRotation;
        }
    }

    // Apply behaviour parameters and apply physics material from manager
    public void UpdateBehaviour(SliderManager settings)
    {
        _SliderObject.transform.localScale = new Vector3(settings._SliderSize, settings._SliderSize, settings._SliderSize);

        Rigidbody rb = _SliderObject.GetComponent<Rigidbody>();
        rb.mass = settings._Mass;
        rb.drag = settings._Drag;
        rb.angularDrag = settings._AngularDrag;
        rb.maxAngularVelocity = settings._MaxRotation;
        rb.useGravity = settings._UseGravity;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        if (_SliderObject.GetComponent<ConfigurableJoint>() != null)
        {
            if (settings._ContainerShape == SliderManager.ContainerShapes.Tether)
            {
                ConfigurableJoint joint = _SliderObject.GetComponent<ConfigurableJoint>();

                SoftJointLimit newJoint = new SoftJointLimit();
                newJoint.limit = (settings._ContainerSize / 2);
                newJoint.bounciness = settings._Springiness;
                joint.linearLimit = newJoint;

                SoftJointLimitSpring springJoint = new SoftJointLimitSpring();
                springJoint.spring = settings._Springiness;
                springJoint.damper = settings._Damping;
                joint.linearLimitSpring = springJoint;

                joint.xMotion = ConfigurableJointMotion.Limited;
                joint.yMotion = ConfigurableJointMotion.Limited;
                joint.zMotion = ConfigurableJointMotion.Limited;
            }
        }
    }

    public void CalculatePosRelativeToContainer(SliderManager settings)
    {
        if (settings._ContainerShape == SliderManager.ContainerShapes.Tether)
            _SliderPosInContainer = settings._Container.transform.InverseTransformPoint(_SliderObject.transform.position) * settings.GetContainerTetherSize();
        else
            _SliderPosInContainer = settings._Container.transform.InverseTransformPoint(_SliderObject.transform.position);
    }

    // Prevent slider from reaching the maximum velocity (set in the SliderManager)
    void LimitVelocity(SliderManager settings)
    {
        Vector3 velocity = _SliderObject.GetComponent<Rigidbody>().velocity;
        _SliderObject.GetComponent<Rigidbody>().velocity = Vector3.ClampMagnitude(velocity, settings._MaxSpeed);
        _Speed = _SliderObject.GetComponent<Rigidbody>().velocity.sqrMagnitude;
    }

    // Reset slider position inside centre of container if outside
    public void LimitDistance(SliderManager settings)
    {
        if (settings._ContainerShape == SliderManager.ContainerShapes.Sphere)
        {
            //float distance =  _SliderPosInContainer.sqrMagnitude;
            Vector3 differenceWorld = settings._Container.transform.position - _SliderObject.transform.position;
            float distance = differenceWorld.sqrMagnitude;
            

            if (distance > settings._ContainerSize / 7)
            {
                //_SliderObject.GetComponent<Rigidbody>().position = _SliderObject.transform.position + settings.GetContainerMovement();
                _SliderObject.GetComponent<Rigidbody>().position = settings._Container.transform.position - differenceWorld.normalized * settings._ContainerSize / 2.1f;
            }
            


            

            /*
            if (distance > settings._ContainerSize * 1.1)
            {
                _SliderObject.GetComponent<Rigidbody>().position = settings._Container.transform.position +
                    _SliderPosInContainer.normalized * settings._ContainerSize * 0.45f;
            }
            */
        }
        else if (settings._ContainerShape == SliderManager.ContainerShapes.Cube)
        {
            if (!settings._Container.GetComponent<Collider>().bounds.Contains(_SliderObject.transform.position))
            {
                Vector3 limit = new Vector3(settings._ContainerSize, settings._ContainerSize, settings._ContainerSize) * 0.45f;
                Vector3 limitedPosition = Utilities.LimitAxis(limit, _SliderPosInContainer);
                _SliderObject.GetComponent<Rigidbody>().position = settings._Container.transform.position + limitedPosition;
            }
        }
    }


    private void TetherDraw(SliderManager settings)
    {
        Vector3 between = (_SliderObject.transform.position - settings._Container.transform.position) / 2;
        float distance = between.magnitude;

        _TetherDraw.transform.localPosition = settings._Container.transform.position + between;
        _TetherDraw.transform.localScale = new Vector3(settings._AxisThickness / 10, distance, settings._AxisThickness / 10);

        Vector3 lookPos = settings._Container.transform.position - _SliderObject.transform.position;
        Quaternion rotation = Quaternion.LookRotation(lookPos);
        rotation *= Quaternion.Euler(90, 0, 0);
        _TetherDraw.transform.rotation = rotation;
    }

    public float GetAngularSpeed()
    {
        return _SliderObject.GetComponent<Rigidbody>().angularVelocity.magnitude;
    }

    public float GetAngularAcceleration()
    {
        float angularAcceleration = Mathf.Abs(_PreviousAngularSpeed - _SliderObject.GetComponent<Rigidbody>().angularVelocity.magnitude);
        _PreviousAngularSpeed = _SliderObject.GetComponent<Rigidbody>().angularVelocity.magnitude;
        return angularAcceleration;
    }


    public CollisionEvent GetCollisionEvent(int type)
    {
        CollisionEvent collisionEvent = null;

        if (type == 0)
            newCollision = _SliderObject.GetComponent<ChildCollision>().GetCollision();
        else
            newCollision = _SliderObject.GetComponent<ChildCollision>().GetCollisionStay();

        if (newCollision != null)
        {
            int index = 0;
            float amount = 0;

            if (newCollision.collider.CompareTag("Slider"))
                index = 0;
            else if (newCollision.collider.CompareTag("Obstacle"))
                index = 1;

            if (type == 0)
                amount = newCollision.relativeVelocity.magnitude;
            else
                amount = _Speed;

            if (amount < 0.0001)
                amount = 0;

            collisionEvent = new CollisionEvent(index, amount);

        }

        return collisionEvent;
    }


    public void ApplyForce(Transform target, float force)
    {
        _SliderObject.GetComponent<Rigidbody>().AddForce((target.position - _SliderObject.transform.position).normalized * force);
    }

    public Transform GetTransform()
    {
        return _SliderObject.transform;
    }

    public void Destroy()
    {
        SliderManager.Destroy(_SliderObject);
        SliderManager.Destroy(_TetherDraw);
    }
}
