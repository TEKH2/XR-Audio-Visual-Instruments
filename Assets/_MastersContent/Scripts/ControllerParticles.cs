using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ControllerParticles : MonoBehaviour
{
    public bool _ParticleSystemOn;
    public int _NumberOfParticles;
    public GameObject _SpawnArea;
    public GameObject _Playhead;
    public GameObject _ParticleSystemPrefab;
    public GameObject _ParticleSystemFlashPrefab;
    public GameObject _ForcePrefab;
    public GameObject _WallPrefab;
    public GameObject _WallVisualsPrefab;
    public GameObject _LeapRig;
    public OSC _OscManager;


    public bool _TriggerOnSpeed = false;

    private GameObject _ParticleObject;
    private ParticleSystem _ParticleSystem;
    private ParticleSystem.Particle[] _ParticleDump;

    private GameObject _ParticleFlashObject;
    private ParticleSystem _ParticleFlashSystem;
    private ParticleSystem.Particle[] _ParticleFlashDump;
    private List<float> _ParticleFlashPositions;

    private List<int> _ParticlesToTrigger;

    private GameObject[] _Playheads;

    private GameObject[] _Walls;
    private Vector3[] _WallPositions;
    private Vector3[] _WallScales;

    private GameObject[] _WallVisuals;
    private Vector3[] _WallVisualsPositions;
    private Vector3[] _WallVisualsScales;

    private ForceObject[] _ForceObjects;

    private Vector3 _InverseSpawnScale;

    private ParticleSystem.EmitParams _Emit;

    private static Vector3 _ParticlePositionOutputOffset = new Vector3(0.5f, 0.5f, 0.5f);
    

    private const int _OscMessageSize = 128;
    private const float _ParticleSpeedTriggerThreshold = 0.1f;



    [SerializeField]
    private Vector3 controllerAngularVel;

    [SerializeField]
    private Vector3 _ParticleAngularVelocityCluster;


    // Start is called before the first frame update
    void Start()
    {
        CreateWalls();
        FindPlayheadObjects();
        BuildForceObjects();
        Invoke("SetupParticleSystems", 1.0f);

        _ParticlesToTrigger = new List<int>();
        _ParticleFlashPositions = new List<float>();

        _OscManager.SetAddressHandler("/particle/trigger", ParticleTriggerReceived);
    }



    // Update is called once per frame
    void Update()
    {
        OVRInput.Update();

        if (_ParticleSystemOn && _ParticleSystem != null)
        {
            if (Input.GetKeyDown("space"))
            {
                _ParticleSystem.Clear();
                EmitParticles(_ParticleSystem, _SpawnArea, _NumberOfParticles);
            }

            if (OVRInput.GetDown(OVRInput.Button.Two) ||
                OVRInput.GetDown(OVRInput.Button.Four))
            {
                SendOSC("/sampleChange", 1);
            }

            //AddParticle();

            _InverseSpawnScale = GetInverseSpawnScale(_SpawnArea.transform);
            GetParticlePositions();
            LookForParticlesToTrigger();
            CalculateDeviation();

            //CalculateParticleRotation();
        }
    }

    private void FixedUpdate()
    {
        UpdateForceField();
    }


    public void FindPlayheadObjects()
    {
        _Playheads = GameObject.FindGameObjectsWithTag("Playhead");

        foreach (GameObject playhead in _Playheads)
        {
            Debug.Log("Found Playhead: " + playhead.name);
        }
    }

    public void BuildForceObjects()
    {
        _ForceObjects = new ForceObject[2];

        _ForceObjects[0] = new ForceObject(_ForcePrefab, "Left Force", this.transform, _LeapRig.transform);
        _ForceObjects[1] = new ForceObject(_ForcePrefab, "Right Force", this.transform, _LeapRig.transform);
    }

    public void UpdateForceField()
    {
        //controllerAngularVel = OVRInput.GetLocalControllerAngularVelocity(OVRInput.Controller.LTouch);
        //SendOSC("/handAngular", new float[] { controllerAngularVel.x, controllerAngularVel.y, controllerAngularVel.z});

        float[] grip = { OVRInput.Get(OVRInput.RawAxis1D.LHandTrigger), OVRInput.Get(OVRInput.RawAxis1D.RHandTrigger) };
        OVRInput.Controller[] controller = { OVRInput.Controller.LTouch, OVRInput.Controller.RTouch };
        OVRInput.RawAxis2D[] thumb = { OVRInput.RawAxis2D.LThumbstick, OVRInput.RawAxis2D.RThumbstick };

        for (int i = 0; i < _ForceObjects.Length; i++)
        {
            if (_ForceObjects[i] != null && grip[i] > 0)
            {
                _ForceObjects[i].setActive(true);
                _ForceObjects[i].Update(grip[i],
                                        OVRInput.GetLocalControllerPosition(controller[i]),
                                        OVRInput.GetLocalControllerRotation(controller[i]),
                                        OVRInput.GetLocalControllerAngularVelocity(controller[i]),
                                        OVRInput.Get(thumb[i]));
            }
            else
                _ForceObjects[i].setActive(false);
        }
    }


    private void CreateWalls()
    {
        _Walls = new GameObject[6];
        _WallPositions = new Vector3[6];
        _WallScales = new Vector3[3];
        
        _WallVisuals = new GameObject[6];
        _WallVisualsPositions = new Vector3[6];
        _WallVisualsScales = new Vector3[3];

        PrepWallCoordinates();

        for (int i = 0; i < 6; i++)
        {
            _Walls[i] = Instantiate(_WallPrefab, new Vector3(0, 0, 0), Quaternion.identity);
            _Walls[i].transform.parent = _SpawnArea.transform;
            _Walls[i].transform.localPosition = _WallPositions[i];
            _Walls[i].transform.localScale = _WallScales[i/2];
            _Walls[i].name = "Wall" + i;
        }

        for (int i = 0; i < 6; i++)
        {
            _Walls[i] = Instantiate(_WallVisualsPrefab, new Vector3(0, 0, 0), Quaternion.identity);
            _Walls[i].transform.parent = _SpawnArea.transform;
            _Walls[i].transform.localPosition = _WallVisualsPositions[i];
            _Walls[i].transform.localScale = _WallVisualsScales[i / 2];
            _Walls[i].name = "Wall Visuals" + i;
        }
    }

    private void PrepWallCoordinates()
    {
        Vector3 area = _SpawnArea.transform.localScale;

        //Vector3 wallLength = new Vector3(area.x / 2, area.y / 2, area.z / 2);
        //Vector3 wallLength = new Vector3(0.5f, 0.5f, 0.5f);

        Vector3 wallThick = new Vector3(1 / area.x * 0.5f, 1 / area.y * 0.5f, 1 / area.z * 0.5f);

        // Populate the position array
        _WallPositions[0] = new Vector3(0, -0.5f - wallThick.y / 1.9f, 0);
        _WallPositions[1] = new Vector3(0, 0.5f + wallThick.y / 2, 0);
        _WallPositions[2] = new Vector3(-0.5f - wallThick.x / 2, 0, 0);
        _WallPositions[3] = new Vector3(0.5f + wallThick.x / 2, 0, 0);
        _WallPositions[4] = new Vector3(0, 0, -0.5f - wallThick.z / 2);
        _WallPositions[5] = new Vector3(0, 0, 0.5f + wallThick.z / 2);

        // Apply scaling to each boundary object
        _WallScales[0] = new Vector3
            (1 + wallThick.x * 2, wallThick.y, 1 + wallThick.z * 2);
        _WallScales[1] = new Vector3
            (wallThick.x, 1 + wallThick.y * 2, 1 + wallThick.z * 2);
        _WallScales[2] = new Vector3
            (1 + wallThick.x * 2, 1 + wallThick.y * 2, wallThick.z);

        // Populate the position array
        _WallVisualsPositions[0] = new Vector3(0, -0.5f, 0);
        _WallVisualsPositions[1] = new Vector3(0, 0.5f, 0);
        _WallVisualsPositions[2] = new Vector3(-0.5f, 0, 0);
        _WallVisualsPositions[3] = new Vector3(0.5f, 0, 0);
        _WallVisualsPositions[4] = new Vector3(0, 0, -0.5f);
        _WallVisualsPositions[5] = new Vector3(0, 0, 0.5f);

        // Apply scaling to each boundary object
        _WallVisualsScales[0] = new Vector3(1, 0.001f, 1);
        _WallVisualsScales[1] = new Vector3(0.001f, 1, 1);
        _WallVisualsScales[2] = new Vector3(1, 1, 0.001f);
    }



    private void SetupParticleSystems()
    {
        if (_ParticleObject != null)
            Destroy(_ParticleObject);

        _ParticleObject = Instantiate(_ParticleSystemPrefab, new Vector3(0, 0, 0), Quaternion.identity);
        _ParticleObject.transform.parent = _SpawnArea.transform;
        _ParticleObject.transform.localPosition = Vector3.zero;
        _ParticleObject.transform.localScale = new Vector3(1, 1, 1);
        _ParticleObject.name = "ParticleObject";
        _ParticleSystem = _ParticleObject.GetComponent<ParticleSystem>();

        _ParticleFlashObject = Instantiate(_ParticleSystemFlashPrefab, new Vector3(0, 0, 0), Quaternion.identity);
        _ParticleFlashObject.transform.parent = _SpawnArea.transform;
        _ParticleFlashObject.transform.localPosition = Vector3.zero;
        _ParticleFlashObject.transform.localScale = new Vector3(1, 1, 1);
        _ParticleFlashObject.name = "ParticleFlashObject";
        _ParticleFlashSystem = _ParticleFlashObject.GetComponent<ParticleSystem>();

        _Emit = new ParticleSystem.EmitParams();

        EmitParticles(_ParticleSystem, _SpawnArea, _NumberOfParticles);
    }

    private void AddParticle()
    {
        if (_NumberOfParticles > _ParticleSystem.particleCount)
        {
            Vector3 s = _SpawnArea.transform.localScale / 5.1f;

            Vector3 velocity = Vector3.zero;
            Vector3 position;

            // Generate random particle position
            position = new Vector3(Random.Range(-s.x, s.x),
                                        Random.Range(-s.y, s.y),
                                        Random.Range(-s.z, s.z));

            // Set emit properties and emit
            _Emit.position = position;
            _Emit.velocity = velocity;
            _ParticleSystem.Emit(_Emit, 1);

            _ParticleDump = new ParticleSystem.Particle[_ParticleSystem.particleCount];

            // Output total number of particles via osc
            SendOSC("/totalParticles", _ParticleSystem.particleCount);
        }
    }


    // Function to create particles
    private void EmitParticles(ParticleSystem particleSystem, GameObject spawn, int numberOfParticles)
    {
        if (particleSystem != null)
        {
            Vector3 s = _SpawnArea.transform.localScale / 5.1f;

            Vector3 velocity = Vector3.zero;
            Vector3 position;

            // Iterate through particles
            for (int i = 0; i < numberOfParticles; i++)
            {
                // Generate random particle position
                position = new Vector3(Random.Range(-s.x, s.x),
                                            Random.Range(-s.y, s.y),
                                            Random.Range(-s.z, s.z));

                //position = Vector3.zero;

                // Set emit properties and emit
                _Emit.position = position;
                _Emit.velocity = velocity;
                _ParticleSystem.Emit(_Emit, 1);
            }

            _ParticleDump = new ParticleSystem.Particle[_ParticleSystem.particleCount];
            _ParticleFlashDump = new ParticleSystem.Particle[_ParticleSystem.particleCount];


            // Output total number of particles via osc
            SendOSC("/totalParticles", _ParticleSystem.particleCount);
        }
    }

    private void EmitSingleParticle(ParticleSystem particleSystem, Vector3 position, float duration)
    {
        ParticleSystem.MainModule mainParticleModule = particleSystem.main;
        mainParticleModule.startLifetime = duration / 1000;

        _Emit.position = position;
        _Emit.velocity = Vector3.zero;

        particleSystem.Emit(_Emit, 1);
    }

    private void GetParticlePositions()
    {
        _ParticleSystem.GetParticles(_ParticleDump);
    }

    private void LookForParticlesToTrigger()
    {
        _ParticlesToTrigger.Clear();

        ParticleSystem.Particle singleParticle;

        BoxCollider theColliderBox = _SpawnArea.GetComponent<BoxCollider>();
        Vector3 s = _SpawnArea.transform.localScale / 4.1f;
        Vector3 position;


        Debug.Log(_SpawnArea.transform.localScale);

        if (_TriggerOnSpeed)
        {
            int i = 0;
            foreach (ParticleSystem.Particle particle in _ParticleDump)
            {
                //if (bounds.Contains(_ParticleSystem.transform.TransformPoint(particle.position)))
                //if (Utilities.PointInOABB(_ParticleObject.transform.TransformPoint(particle.position), theColliderBox))
                if (Utilities.IsWithinLocalBounds(particle.position, _SpawnArea.transform.localScale))
                {
                    //Debug.Log("WITHIN BOUNDS: " + particle.position + " / " + _ParticleSystem.transform.TransformPoint(particle.position));
                    if ((particle.velocity.sqrMagnitude) > _ParticleSpeedTriggerThreshold)
                        _ParticlesToTrigger.Add(i);
                }
                else
                {
                    singleParticle = particle;

                    // Generate random particle position
                    position = new Vector3(Random.Range(-s.x, s.x),
                                                Random.Range(-s.y, s.y),
                                                Random.Range(-s.z, s.z));

                    singleParticle.velocity = Vector3.zero;
                    singleParticle.position = position;
                    _ParticleDump[i] = singleParticle;

                    //singleParticle[0].velocity = Vector3.zero;
                    //singleParticle[0].position = position;
                    //_ParticleSystem.SetParticles(singleParticle, 1, i);
                    Debug.Log("PARTICLE POPPED OUT. LIFETIME NOW = " + singleParticle.remainingLifetime);
                }

            i++;
            }
            _ParticleSystem.SetParticles(_ParticleDump);
        }
        else
        {
            Vector3 playheadPosition;
            float playheadSize;

            foreach (GameObject playhead in _Playheads)
            {
                // Get playhead position relative to the current particle spawn area
                playheadPosition = _SpawnArea.transform.InverseTransformPoint(playhead.transform.position);
                playheadPosition = Vector3.Scale(playheadPosition, _SpawnArea.transform.localScale);

                playheadSize = playhead.transform.localScale.x / 2;
                playheadSize *= playheadSize;

                int i = 0;
                foreach (ParticleSystem.Particle particle in _ParticleDump)
                {
                    if ((particle.position - playheadPosition).sqrMagnitude < playheadSize)
                        _ParticlesToTrigger.Add(i);
                    i++;
                }
            }
        }
    }

    private void CalculateDeviation()
    {
        //_ParticleFlashDump = new ParticleSystem.Particle[_ParticleFlashSystem.particleCount];
        _ParticleFlashPositions.Clear();
        _ParticleFlashSystem.GetParticles(_ParticleFlashDump);
        float standardDev = 0;
        int particleCount = _ParticleFlashDump.Length;

        //Debug.Log(particleCount);

        if (particleCount > 0)
        {
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < particleCount; j++)
                {
                    _ParticleFlashPositions.Add(_ParticleFlashDump[j].position[i]);
                }

                standardDev += Utilities.StandardDeviation(_ParticleFlashPositions);
            }

            standardDev = standardDev / 3;
            SendOSC("/deviation", standardDev);
        }
    }

    private void CalculateParticleRotation()
    {
        if (_ParticleSystemOn && _ParticleSystem.particleCount > 0)
        {
            if (_TriggerOnSpeed)
            {
                float x = 0f;
                float y = 0f;
                float z = 0f;

                _ParticleAngularVelocityCluster = Vector3.zero;
                int i = 0;
                foreach (ParticleSystem.Particle particle in _ParticleDump)
                {
                    x += particle.angularVelocity3D.x;
                    y += particle.angularVelocity3D.y;
                    z += particle.angularVelocity3D.z;
                    i++;
                }

                _ParticleAngularVelocityCluster = new Vector3(x / i, y / i, z / i);
            }
        }
    }


    private Vector3 GetInverseSpawnScale(Transform spawnArea)
    {
        Vector3 scale = new Vector3(1 / spawnArea.localScale.x, 1 / spawnArea.localScale.y, 1 / spawnArea.localScale.z);
        return scale;
    }

    public void ParticleTriggerReceived(OscMessage message)
    {
        if (_ParticleSystemOn && _ParticleSystem != null && _ParticlesToTrigger.Count > 0)
        {
            float duration = message.GetFloat(0);
            int insideIndex = Random.Range(0, _ParticlesToTrigger.Count);
            int particleIndex = _ParticlesToTrigger[insideIndex];
            Vector3 position = _ParticleDump[particleIndex].position;

            OutputParticleData(particleIndex, Vector3.Scale(position, _InverseSpawnScale), _ParticleDump[particleIndex].velocity.sqrMagnitude);
            EmitSingleParticle(_ParticleFlashSystem, position, duration);
        }
    }

    private void OutputParticleData(int id, Vector3 position, float speed)
    {
        position = (position + _ParticlePositionOutputOffset) * 0.96f;
        float[] theOutput = {id, position.x, position.y, position.z, speed };

        // output particle id and position
        SendOSC("/particle/trigger", theOutput);
    }


    public void SendOSC(string name, float[] output)
    {
        OscMessage message = new OscMessage();
        message.address = name;

        foreach (float value in output)
            message.values.Add(value);
        _OscManager.Send(message);
    }

    public void SendOSC(string name, float output)
    {
        OscMessage message = new OscMessage();
        message.address = name;
        message.values.Add(output);
        _OscManager.Send(message);
    }
}
