using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Leap.Unity.Interaction;

public class SliderManager : MonoBehaviour
{
    //private Slider[] _Sliders;
    private List<Slider> _Sliders = new List<Slider>();
    private List<CollisionEvent> _CollisionEvent = new List<CollisionEvent>();
    private HandForce[] _HandForces;

    public string _Name = "default";

    public OSC _OscManager;
    public MeshManager _MeshManager;
    public MaterialManager _MaterialManager;
    public GameObject _LeapRig;

    public enum ContainerShapes { Sphere = 0, Cube = 1, Tether = 2, Open = 3 };
    public enum SliderShapes {Sphere = 0, Cube = 1};
    
    public ContainerShapes _ContainerShape = ContainerShapes.Sphere;
    public SliderShapes _SliderShape = SliderShapes.Sphere;

    public float _ContainerSize = 1;
    public float _SliderSize = 0.02f;

    public PhysicMaterial _SliderPhysics;
    public PhysicMaterial _WallPhysics;

    public bool _DirectControl = true;
    public float _NumberOfSliders = 5;
    private float _PreviousNumberOfSliders = 0;

    public float _Smoothing = 100.0f;
    public bool _UseGravity = true;
    public float _Mass = 1;
    public float _Drag = 0.5f;
    public float _AngularDrag = 0.2f;
    public float _Springiness = 50.0f;
    public float _Damping = 10.0f;
    public float _MaxSpeed = 4;
    public float _MaxRotation = 5.0f;

    public bool _ShowAxis = true;
    public float _AxisThickness = 0.01f;

    public GameObject _Container;

    private GameObject[] _BoundaryPlane;
    private Vector3[] _BoundaryPlanePositions;
    private Vector3[] _BoundaryPlaneScales;

    public GameObject _InvertedSphere;
    private GameObject _SphereCollider;
    public GameObject _DrawParent;
    public GameObject _SliderParent;
    public GameObject _CubeFrame;
    private GameObject _Frame;
    //public Mesh _ColliderSphereHalf;

    private static float _BoundaryThick = 0.4f;

    private Vector3 _ContainerPreviousPos;
    private Vector3 _ContainerMovement;

    private float _KeyboardForce = 0.2f;

    private float _ContainerTetherSize;
    private float _PreviousContainerSize = 0.0f;

    void Start()
    {
        _HandForces = new HandForce[2];
        BuildParents();
        BuildContainer(null);
    }

    private void FixedUpdate()
    {
        Rigidbody rb = _Container.GetComponent<Rigidbody>();
        if (Input.GetKey(KeyCode.W))
            rb.velocity += transform.forward * _KeyboardForce;
        if (Input.GetKey(KeyCode.S))
            rb.velocity += transform.forward * -_KeyboardForce;
        if (Input.GetKey(KeyCode.A))
            rb.velocity += transform.right * -_KeyboardForce;
        if (Input.GetKey(KeyCode.D))
            rb.velocity += transform.right * _KeyboardForce;

        if (_Sliders != null)
            foreach (Slider slider in _Sliders)
                if (slider != null)
                    slider.FixedUpdate();

        // Check if hand force buttons are pressed
        CheckHandForceButtons();

        _ContainerMovement = _Container.transform.position - _ContainerPreviousPos;
        _ContainerPreviousPos = _Container.transform.position;
    }

    void Update()
    {
        OVRInput.Update();
        //OVRInput.FixedUpdate();

        // Check controller buttons and container size to rebuild interface
        CheckToRebuildInterface();

        CheckIncreaseDecreaseOfSliders();
        // Spawn sliders if they don't exist
        SpawnSliders();

        // Call slider update method if they exist
        if (_Sliders != null)
            foreach (Slider slider in _Sliders)
                if (slider != null)
                    slider.Update();

        // Output all the slider OSC messages to Max
        OutputOscToMax(_Sliders);
    }

    void CheckIncreaseDecreaseOfSliders()
    {
        Vector2 thumbL = OVRInput.Get(OVRInput.RawAxis2D.LThumbstick);
        if (thumbL[1] > 0.5f)
            _NumberOfSliders += (thumbL[1] - 0.5f) / 10;
        else if (thumbL[1] < -0.5f)
            _NumberOfSliders += (thumbL[1] + 0.5f) / 10;


        _NumberOfSliders = Mathf.Clamp(_NumberOfSliders, 1, 64);
    }

    void CheckToRebuildInterface()
    {
        if (Input.GetKeyDown("space") ||
            OVRInput.GetDown(OVRInput.Button.Two) ||
            OVRInput.GetDown(OVRInput.Button.Four))
        {
            BuildParents();
            BuildContainer(_Container.transform);
        }

        if (_PreviousContainerSize != _ContainerSize)
        {
            _PreviousContainerSize = _ContainerSize;

            BuildParents();
            BuildContainer(_Container.transform);
        }
    }

    void BuildParents()
    {
        if (_DrawParent != null)
            Destroy(_DrawParent);
        _DrawParent = new GameObject();
        _DrawParent.transform.parent = this.transform;
        _DrawParent.name = "Draw Parent";

        foreach (Slider slider in _Sliders)
            if (slider != null)
                slider.Destroy();
        _Sliders = new List<Slider>();

        if (_SliderParent != null)
            Destroy(_SliderParent);
        _SliderParent = new GameObject();
        _SliderParent.transform.parent = this.transform;
        _SliderParent.transform.localPosition = Vector3.zero;
        _SliderParent.name = "Slider Parent";
    }

    public Vector3 GetContainerMovement()
    {
        return _ContainerMovement;
    }

    void BuildContainer(Transform previousTransform)
    {
        if (_Container != null)
            Destroy(_Container);

        if (_ContainerShape == ContainerShapes.Sphere)
            _Container = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        else if (_ContainerShape == ContainerShapes.Cube)
            _Container = GameObject.CreatePrimitive(PrimitiveType.Cube);
        else
            _Container = GameObject.CreatePrimitive(PrimitiveType.Sphere);

        _Container.transform.parent = transform;

        if (_ContainerShape == ContainerShapes.Tether)
        {
            _Container.name = "Anchor";
            _Container.GetComponent<Renderer>().material = _MaterialManager._Container;
            _ContainerTetherSize = _SliderSize * 2;
            _Container.transform.localScale = new Vector3(_ContainerTetherSize, _ContainerTetherSize, _ContainerTetherSize);
        }
        else
        {
            _Container.name = "Container";
            _Container.GetComponent<Renderer>().material = _MaterialManager._Container;
            _Container.transform.localScale = new Vector3(_ContainerSize, _ContainerSize, _ContainerSize);
            //Destroy(_Container.GetComponent<Collider>());
        }

        _Container.GetComponent<Collider>().isTrigger = true;
        Rigidbody rb = _Container.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.drag = 1000;
        rb.mass = 1000000;

        if (_ContainerShape == ContainerShapes.Tether)
        {
            rb.freezeRotation = true;
        }


        if (previousTransform == null)
            _Container.transform.localPosition = new Vector3(0, 0, 0);
        else
            _Container.transform.localPosition = previousTransform.localPosition;

        _Container.AddComponent<InteractionBehaviour>();
        _Container.GetComponent<InteractionBehaviour>().ignoreContact = true;

        if (_ContainerShape == ContainerShapes.Cube)
            CreateCubeContainerCollider();
        else if (_ContainerShape == ContainerShapes.Sphere)
            CreateSphereContainerCollider();
    }

    private void SpawnSliders()
    {
        if (_Sliders.Count < _NumberOfSliders)
            _Sliders.Add(new Slider(this, _Sliders.Count));

        if (_Sliders.Count > _NumberOfSliders)
            for (int i = _Sliders.Count - 1; i >= _NumberOfSliders; i--)
            {
                _Sliders[i].Destroy();
                _Sliders.RemoveAt(i);
            }
    }

    private void CreateCubeContainerCollider()
    {
        // Initialise boundary array, create objects and set transforms
        PrepBoundaryCoords();
        _BoundaryPlane = new GameObject[6];
        for (int i = 0; i < 6; i++)
        {
            _BoundaryPlane[i] = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _BoundaryPlane[i].transform.parent = _Container.transform;
            _BoundaryPlane[i].transform.localPosition = _BoundaryPlanePositions[i];
            _BoundaryPlane[i].transform.localScale = _BoundaryPlaneScales[(int)Mathf.Floor(i / 2)];
            _BoundaryPlane[i].GetComponent<BoxCollider>().material = _WallPhysics;
            _BoundaryPlane[i].AddComponent<Rigidbody>();
            _BoundaryPlane[i].GetComponent<Rigidbody>().useGravity = false;
            _BoundaryPlane[i].GetComponent<Rigidbody>().isKinematic = true;
            _BoundaryPlane[i].GetComponent<Rigidbody>().collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            _BoundaryPlane[i].name = "Boundary Wall " + i;

            MeshRenderer tempMeshRenderer = _BoundaryPlane[i].GetComponent<MeshRenderer>();
            Destroy(tempMeshRenderer);
        }

        _Frame = new GameObject();
        _Frame.transform.parent = _Container.transform;
        _Frame.transform.localPosition = Vector3.zero;
        _Frame.transform.localScale = new Vector3(_ContainerSize, _ContainerSize, _ContainerSize); ;
        _Frame.AddComponent<MeshRenderer>();
        _Frame.GetComponent<MeshRenderer>().material = _MaterialManager._Axis;
        _Frame.AddComponent<MeshFilter>();
        _Frame.GetComponent<MeshFilter>().mesh = _CubeFrame.GetComponent<MeshFilter>().mesh;

    }

    private void PrepBoundaryCoords()
    {
        //Vector3 interactionSize = transform.localScale / 2.0f;
        Vector3 interactionSize = new Vector3(1, 1, 1);
        // Create vector to set the boundary positions for each side
        Vector3 BoundaryVector = new Vector3(
            interactionSize.x + _BoundaryThick,
            interactionSize.y + _BoundaryThick,
            interactionSize.z + _BoundaryThick);

        // Populate the position array
        _BoundaryPlanePositions = new Vector3[6];
        _BoundaryPlanePositions[0] = new Vector3(0, -BoundaryVector.y / 2, 0);
        _BoundaryPlanePositions[1] = new Vector3(0, BoundaryVector.y / 2, 0);
        _BoundaryPlanePositions[2] = new Vector3(-BoundaryVector.x / 2, 0, 0);
        _BoundaryPlanePositions[3] = new Vector3(BoundaryVector.x / 2, 0, 0);
        _BoundaryPlanePositions[4] = new Vector3(0, 0, -BoundaryVector.z / 2);
        _BoundaryPlanePositions[5] = new Vector3(0, 0, BoundaryVector.z / 2);

        // Apply scaling to each boundary object
        _BoundaryPlaneScales = new Vector3[3];
        _BoundaryPlaneScales[0] = new Vector3
            (interactionSize.x * (1 + _BoundaryThick), _BoundaryThick, interactionSize.z * (1 + _BoundaryThick));
        _BoundaryPlaneScales[1] = new Vector3
            (_BoundaryThick, interactionSize.y * (1 + _BoundaryThick), interactionSize.z * (1 + _BoundaryThick));
        _BoundaryPlaneScales[2] = new Vector3
            (interactionSize.x * (1 + _BoundaryThick), interactionSize.y * (1 + _BoundaryThick), _BoundaryThick);
    }

    public void CreateSphereContainerCollider()
    {
        _SphereCollider = new GameObject();
        _SphereCollider.name = "Sphere Collider A";
        _SphereCollider.transform.parent = _Container.transform;
        _SphereCollider.transform.localPosition = new Vector3(0, 0, 0);
        _SphereCollider.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
        _SphereCollider.AddComponent<MeshCollider>();
        _SphereCollider.GetComponent<MeshCollider>().sharedMesh = _MeshManager._HalvedSphereA;
        _SphereCollider.AddComponent<Rigidbody>().collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        _SphereCollider.GetComponent<Rigidbody>().isKinematic = true;
        _SphereCollider.GetComponent<Rigidbody>().useGravity = false;
        _SphereCollider.AddComponent<IgnoreColliderForInteraction>();
        _SphereCollider.transform.localRotation = Quaternion.Euler(180, 0, 0);

        _SphereCollider = new GameObject();
        _SphereCollider.name = "Sphere Collider B";
        _SphereCollider.transform.parent = _Container.transform;
        _SphereCollider.transform.localPosition = new Vector3(0, 0, 0);
        _SphereCollider.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
        _SphereCollider.AddComponent<MeshCollider>();
        _SphereCollider.GetComponent<MeshCollider>().sharedMesh = _MeshManager._HalvedSphereA;
        _SphereCollider.AddComponent<Rigidbody>().collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        _SphereCollider.GetComponent<Rigidbody>().isKinematic = true;
        _SphereCollider.GetComponent<Rigidbody>().useGravity = false;
        _SphereCollider.AddComponent<IgnoreColliderForInteraction>();

        // WORK HERE: ADDING INVERTED MESH TO COLLIDER AND REMOVE JOINT!!!!
    }

    public GameObject GetSphereCollider()
    {
        return _SphereCollider;
    }

    public float GetContainerTetherSize()
    {
        return _ContainerTetherSize;
    }


    public void CheckHandForceButtons()
    {
        float triggerL = OVRInput.Get(OVRInput.RawAxis1D.LHandTrigger);
        float triggerR = OVRInput.Get(OVRInput.RawAxis1D.RHandTrigger);

        if (triggerR > 0f)
        {
            if (_HandForces[0] == null)
                _HandForces[0] = new HandForce(OVRInput.Controller.RTouch, this.transform, _LeapRig.transform);
            _HandForces[0].Update(_Sliders, triggerR);
        }

        if (triggerL > 0f)
        {
            if (_HandForces[1] == null)
                _HandForces[1] = new HandForce(OVRInput.Controller.LTouch, this.transform, _LeapRig.transform);
            _HandForces[1].Update(_Sliders, triggerL);
        }


        if (triggerR == 0f)
            if (_HandForces[0] != null)
            {
                _HandForces[0].Delete();
                _HandForces[0] = null;
            }

        if (triggerL == 0f)
            if (_HandForces[1] != null)
            {
                _HandForces[1].Delete();
                _HandForces[1] = null;
            }
    }


    public void OutputOscToMax(List<Slider> sliders)
    {
        // Create arrays for bulk value OSC output
        float[] xPos = new float[sliders.Count];
        float[] radius = new float[sliders.Count];
        float[] speed = new float[sliders.Count];
        float[] rotation = new float[sliders.Count];
        float[] rotationAccel = new float[sliders.Count];
        float[] elevation = new float[sliders.Count];
        float[] polar = new float[sliders.Count];
        SphericalCoordinates sphericalCoordinates;

        // Populate arrays with slider data
        foreach (Slider slider in sliders)
            if (slider != null)
            {
                xPos[slider._Index] = slider._SliderPosInContainer.x;
                radius[slider._Index] = slider._SliderPosInContainer.magnitude * 2.0f;
                speed[slider._Index] = slider._Speed;
                rotation[slider._Index] = slider.GetAngularSpeed() / 10.0f;
                rotationAccel[slider._Index] = slider.GetAngularAcceleration();

                if (_ContainerShape == ContainerShapes.Tether || _ContainerShape == ContainerShapes.Sphere)
                {
                    sphericalCoordinates = SphericalCoordinates.CartesianToSpherical(slider._SliderPosInContainer);
                    elevation[slider._Index] = sphericalCoordinates.elevation / 180;
                    polar[slider._Index] = Mathf.PingPong(sphericalCoordinates.polar / 180, 1.0f);
                    
                    //elevation[slider._Index] = sphericalCoordinates.elevation;
                    //polar[slider._Index] = sphericalCoordinates.polar;


                }
            }

        // Send arrays via OSC
        if (_NumberOfSliders != _PreviousNumberOfSliders)
        {
            SendOSC(_Name + "/numSliders", (int)_NumberOfSliders);
            _PreviousNumberOfSliders = _NumberOfSliders;
        }

        SendOSC(_Name + "/xPos", xPos);
        SendOSC(_Name + "/radius", radius);
        SendOSC(_Name + "/speed", speed);
        SendOSC(_Name + "/rotation", rotation);
        SendOSC(_Name + "/rotationAccel", rotationAccel);

        if (_ContainerShape == ContainerShapes.Tether || _ContainerShape == ContainerShapes.Sphere)
        {
            SendOSC(_Name + "/elevation", elevation);
            SendOSC(_Name + "/polar", polar);
        }

        // Collect and send collision data via OSC
        foreach (Slider slider in sliders)
            if (slider != null)
            {
                CollisionEvent tempCollision = slider.GetCollisionEvent(0);
                CollisionEvent tempStay = slider.GetCollisionEvent(1);

                if (tempCollision != null)
                {
                    int tag = tempCollision.GetCollisionObjectTag();
                    float force = tempCollision.GetForce();

                    if (force > 0)
                        SendOSC(_Name + "/collision", new float[] { tag, slider._Index, force });
                }

                if (tempStay != null)
                {
                    int tag = tempStay.GetCollisionObjectTag();
                    float force = tempStay.GetForce();

                    if (force > 0)
                        SendOSC(_Name + "/stay", new float[] { tag, slider._Index, force });
                }
            }

        foreach (Slider slider in sliders)
            if (slider != null)
            {
                float[] position = new float[] { slider._Index,
                                                    slider.GetTransform().position.x,
                                                    slider.GetTransform().position.y,
                                                    slider.GetTransform().position.z};
                SendOSC(_Name + "/pos", position);
            }
    }


    public class CollisionEvent
    {
        private int _CollisionObjectTag;
        private float _Amount;

        public CollisionEvent(int collisionObjectTag, float amount)
        {
            _CollisionObjectTag = collisionObjectTag;
            _Amount = amount;
        }

        public int GetCollisionObjectTag() { return _CollisionObjectTag; }
        public float GetForce() { return _Amount; }
    }


    public void SendOSC(string name, float[] output)
    {
        OscMessage message = new OscMessage();
        message.address = "/" + name;

        foreach (float value in output)
            message.values.Add(value);
        _OscManager.Send(message);
    }

    public void SendOSC(string name, float output)
    {
        OscMessage message = new OscMessage();
        message.address = "/" + name;
        message.values.Add(output);
        _OscManager.Send(message);
    }
}
