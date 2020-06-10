using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

public class Granulator : MonoBehaviour
{
    public bool _IsPlaying = true;       // the on/off button

    public ParticleManager _ParticleManager;
    private ParticleSystem.Particle _TempParticle;
    private ParticleSystem.Particle[] _Particles;

    public GameObject _GrainObjectHolder;
    public GameObject _GrainPrefab;


    public int _MaxGrains = 100;


    [Range(0.1f, 5f)]
    public float _GrainPitch = 1;
    [Range(0.0f, 1f)]
    public float _GrainPitchRandom = 0;
    [Range(0.0f, 2.0f)]
    public float _GrainVolume = 1;          // from 0 > 1
    [Range(0.0f, 1.0f)]
    public float _GrainVolumeRandom = 0;      // from 0 > 1


    public AudioClip _EmitterClip;
    [Range(1.0f, 1000f)]
    public int _TimeBetweenGrains = 20;          // ms
    [Range(0.0f, 1000f)]
    public int _TimeBetweenGrainsRandom = 0;       // ms
    [Range(0.0f, 1.0f)]
    public float _GrainPosition = 0;          // from 0 > 1
    [Range(0.0f, 1.0f)]
    public float _GrainPositionRandom = 0;      // from 0 > 1
    [Range(2.0f, 1000f)]
    public int _GrainDuration = 300;       // ms
    [Range(0.0f, 1000f)]
    public int _GrainDurationRandom = 0;     // ms

    public AudioClip _CollisionClip;
    public int _CollisionGrainBurst = 5;
    [Range(1.0f, 1000f)]
    public int _CollisionDensity = 40;                  // ms
    [Range(0.0f, 1.0f)]
    public float _CollisionGrainPosition = 0;          // from 0 > 1
    [Range(0.0f, 1.0f)]
    public float _CollisionPositionRandom = 0;      // from 0 > 1
    [Range(2.0f, 1000f)]
    public int _CollisionDuration = 300;       // ms
    [Range(0.0f, 1000f)]
    public int _CollisionDurationRandom = 0;     // ms

    public enum ParticleMode { Spawning, Static };
    public ParticleMode _ParticleMode = ParticleMode.Spawning;

    
    // Temp vars
    private float _NewGrainPosition = 0;
    private float _NewCollisionGrainPosition = 0;
    private float _NewGrainPitch = 0;
    private float _NewGrainPitchRandom = 0;
    private float _NewGrainDuration = 0;
    private int _NewCollisionDuration;
    private int _NewGrainDensity = 0;
    private float _NewGrainVolume = 0;

    private int _SamplesSinceLastGrain;
    private int _EmitterGrainsLastUpdate = 0;
    private int _SamplesLastUpdate = 0;

    private Rigidbody _RigidBody;
    private Vector3 _ParticleSynthVelocity;

    public bool _MoveGrains = true;
    public bool _Collisions = false;
    [Range(0.0f, 10f)]
    public float _Mass = 0;
    [Range(0.0f, 30.0f)]
    public float _GrainSpeedOnBirth = 5.0f;

    private bool _CollisionsPrevious;
    private float _MassPrevious;

    public float _KeyboardForce = 1;

    private List<GrainData> _GrainQueue;
    private List<GrainData> _CollisionQueue;

    private List<Grain> _GrainsPlaying;
    private List<Grain> _GrainsFinished;


    private const int _SampleRate = 44100;
    private float[] _Window;


    //---------------------------------------------------------------------
    private void Start()
    {
        _ParticleManager.Initialise(this);

        CreateWindowingLookupTable();

        this.gameObject.AddComponent<AudioSource>();
        _RigidBody = this.GetComponent<Rigidbody>();

        _GrainsPlaying = new List<Grain>();
        _GrainsFinished = new List<Grain>();
        _GrainQueue = new List<GrainData>();
        _CollisionQueue = new List<GrainData>();

        
        for (int i = 0; i < _MaxGrains; i++)
        {
            GameObject go = Instantiate(_GrainPrefab);
            go.SetActive(true);
            Grain grain = go.GetComponent<Grain>();
            grain._Granulator = this;
            grain.transform.parent = _GrainObjectHolder.transform;
            _GrainsFinished.Add(grain);
        }

        _SamplesSinceLastGrain = 0;

        // Initialise some grain values
        GenerateGrainValues();
    }

    //---------------------------------------------------------------------
    void Awake()
    {
    }


    void Update()
    {
        Profiler.BeginSample("Update 0");

        //---------------------------------------------------------------------
        // UPDATE MAINTAINANCE
        //---------------------------------------------------------------------

        _SamplesLastUpdate = (int)(Time.deltaTime * _SampleRate);

        // Clamp values to reasonable ranges
        _GrainPosition = Clamp(_GrainPosition, 0, 1);
        _GrainPositionRandom = Clamp(_GrainPositionRandom, 0, 1);
        _TimeBetweenGrains = (int)Clamp(_TimeBetweenGrains, 1, 1000);
        _TimeBetweenGrainsRandom = (int)Clamp(_TimeBetweenGrainsRandom, 0, 1000);
        _GrainDurationRandom = (int)Clamp(_GrainDurationRandom, 0, 1000);

        _GrainPitch = Clamp(_GrainPitch, 0, 1000);
        _GrainPitchRandom = Clamp(_GrainPitchRandom, 0, 1000);
        _GrainVolume = Clamp(_GrainVolume, 0, 2);
        _GrainVolumeRandom = Clamp(_GrainVolumeRandom, 0, 1);

        _NewGrainDensity = _TimeBetweenGrains + Random.Range(0, _TimeBetweenGrainsRandom);
        _ParticleSynthVelocity = _RigidBody.velocity * 0.5f;

        // Check for updates from UI
        if (_Mass != _MassPrevious)
        {
            _MassPrevious = _Mass;
            _ParticleManager.SetMass(_Mass);
        }

        if (_Collisions != _CollisionsPrevious)
        {
            _CollisionsPrevious = _Collisions;
            _ParticleManager.SetCollisions(_Collisions);
        }

        //Move finished grains to inactive pool
        for (int i = _GrainsPlaying.Count - 1; i >= 0; i--)
        {
            Grain tempGrain = _GrainsPlaying[i];

            if (!tempGrain._IsPlaying)
            {
                //tempGrain.gameObject.SetActive(false);
                _GrainsFinished.Add(_GrainsPlaying[i]);
                _GrainsPlaying.RemoveAt(i);
            }
        }
        Profiler.EndSample();


        Profiler.BeginSample("Update 1");
        //---------------------------------------------------------------------
        // EMITTER GRAIN TIMING GENERATION
        //---------------------------------------------------------------------
        // Emitter grains are those which play back constantly throughout each
        // update, as opposed to being trigged from single events.
        // This function creates the timing of emitter grains to be played
        // over the next update.

        int emitterGrainsToPlay = 0;
        int firstGrainOffset = 0;
        int densityInSamples = _NewGrainDensity * (_SampleRate / 1000);

        // If no sample was played last update, adding the previous update's samples count,
        // AFTER the update is complete, should correctly accumulate the samples since the
        // last grain playback. Otherwise, if a sample WAS played last update, the sample
        // offset of that grain is subtracted from the total samples of the previous update.
        // This provides the correct number of samples since the most recent grain was started.
        if (_EmitterGrainsLastUpdate == 0)
            _SamplesSinceLastGrain += _SamplesLastUpdate;
        else
            _SamplesSinceLastGrain = _SamplesLastUpdate - _SamplesSinceLastGrain;

        // If the density of grains minus samples since last grain fits within the
        // estimated time for the this update, calculate number of grains to play this update
        if (densityInSamples - _SamplesSinceLastGrain < _SamplesLastUpdate)
        {
            // Should always equal one or more
            // Not sure if the + 1 is correct here. Potentially introducing rounding errors?
            // Need to check
            emitterGrainsToPlay = (int)(_SamplesLastUpdate / densityInSamples) + 1;
            
            // Create initial grain offset for this update
            firstGrainOffset = densityInSamples - _SamplesSinceLastGrain;
            
            // Hacky check to avoid offsets lower than 0 (if this occurs, something
            // isn't handled correctly. This is a precaution. Haven't properly checked this yet.
            if (firstGrainOffset < 0)
                firstGrainOffset = 0;
        }

        _EmitterGrainsLastUpdate = emitterGrainsToPlay;

        Profiler.EndSample();

        Profiler.BeginSample("Update 2");

        //---------------------------------------------------------------------
        // CREATE EMITTER GRAINS
        //---------------------------------------------------------------------
        // Populate grain queue with emitter grains
        //
        for (int i = 0; i < emitterGrainsToPlay; i++)
        {
            //_GrainTriggerList.Add(firstGrainOffset + i * densityInSamples);

            // Create new random values for each grain
            GenerateGrainValues();

            // Generate new particle in trigger particle system and return it here
            ParticleSystem.Particle tempParticle = _ParticleManager.SpawnEmitterParticle(_RigidBody.velocity, _GrainSpeedOnBirth, (float)_GrainDuration / 1000);

            // Calculate timing offset for grain
            int offset = firstGrainOffset + i * densityInSamples;

            // Create temporary grain data object and add it to the playback queue
            GrainData tempGrainData = new GrainData(_TempParticle.position, _GrainObjectHolder.transform, tempParticle.velocity + _ParticleSynthVelocity, _Mass,
                _EmitterClip, _NewGrainDuration, offset, _NewGrainPosition, _NewGrainPitch, _NewGrainVolume);

            _GrainQueue.Add(tempGrainData);
        }


        // If a grain is going to be played this update, set the samples since last grain
        // counter to the sample offset value of the final grain
        if (_GrainQueue.Count > 0)
            _SamplesSinceLastGrain = _GrainQueue[_GrainQueue.Count - 1].offset;

        // NOTE: If no grains are played, the time that the current update takes
        // (determined next update) will be added to this "samples since last grain" counter instead.
        // This provides the correct distribution of grains per x samples. Go to top of "emitter grain generation"
        // for more information

        Profiler.EndSample();



        Profiler.BeginSample("Update 3");

        //---------------------------------------------------------------------
        // ADD COLLISION GRAINS TO THE END OF THE GRAIN QUEUE
        //---------------------------------------------------------------------
        // Because collisions are added during fixed update, they may overpopulate the grain queue
        // before emitter grains have been added, causing incorrect timing to emitter grains.
        // Adding the collisions after emitters have been added prevents this.

        foreach (GrainData grain in _CollisionQueue)
        {
            _GrainQueue.Add(grain);
        }

        _CollisionQueue.Clear();
        Profiler.EndSample();

        Profiler.BeginSample("Update 4");
        //---------------------------------------------------------------------
        // ASSIGN GRAIN QUEUE TO FREE GRAIN OBJECTS
        //---------------------------------------------------------------------
        foreach (GrainData grainData in _GrainQueue)
        {
            if (_GrainsFinished.Count > 0)
            {
                // Get first grain
                Grain grain = _GrainsFinished[0];

                // Init grain with data
                grain.Initialise(grainData);

                // Add and remove from lists
                _GrainsPlaying.Add(grain);
                _GrainsFinished.Remove(grain);
            }
        }
     
        Profiler.EndSample();

        Profiler.BeginSample("Update 5");

        // Clears the grain queue for next update. Perhaps this might change if for some reason it's
        // better to maintain unfinished grains for the next udpate
        _GrainQueue.Clear();

        //---------------------------------------------------------------------
        // INTERACTION KEYS
        //---------------------------------------------------------------------
        if (Input.GetKey(KeyCode.W))
            _RigidBody.AddForce(0, 0, _KeyboardForce);
        if (Input.GetKey(KeyCode.A))
            _RigidBody.AddForce(-_KeyboardForce, 0, 0);
        if (Input.GetKey(KeyCode.S))
            _RigidBody.AddForce(0, 0, -_KeyboardForce);
        if (Input.GetKey(KeyCode.D))
            _RigidBody.AddForce(_KeyboardForce, 0, 0);

        Profiler.EndSample();
    }


    void EmitterGrainTiming()
    {

    }

    void EmitterGrainPopulating()
    {

    }



    //---------------------------------------------------------------------
    // Creates a burst of new grains on collision events
    //---------------------------------------------------------------------
    public void TriggerCollision(List<ParticleCollisionEvent> collisions, GameObject other)
    {
        for (int i = 0; i < collisions.Count; i++)
        {
            for (int j = 0; j < _CollisionGrainBurst; j++)
            {
                GenerateGrainValues();

                // Calculate timing offset for grain
                int offset = j * _CollisionDensity * (_SampleRate / 1000);

                Vector3 pos = _GrainObjectHolder.transform.InverseTransformPoint(collisions[i].intersection);

                // Create temporary grain data object and add it to the playback queue
                GrainData tempGrainData = new GrainData(pos, _GrainObjectHolder.transform, Vector3.zero, _Mass,
                    _CollisionClip, _NewCollisionDuration, offset, _NewCollisionGrainPosition, _NewGrainPitch, _NewGrainVolume);

                _CollisionQueue.Add(tempGrainData);
            }
        }
    }


    //---------------------------------------------------------------------
    // Updates random grain values for new grains
    //---------------------------------------------------------------------
    void GenerateGrainValues()
    {
        _NewGrainPosition = _GrainPosition + Random.Range(0, _GrainPositionRandom);
        _NewCollisionGrainPosition = _CollisionGrainPosition + Random.Range(0, _CollisionPositionRandom);

        _NewGrainPitch = _GrainPitch + Random.Range(-_GrainPitchRandom, _GrainPitchRandom);

        _NewGrainDuration = _GrainDuration + Random.Range(0, _GrainDurationRandom);
        _NewCollisionDuration = _CollisionDuration + Random.Range(0, _CollisionDurationRandom);

        _NewGrainVolume = Clamp(_GrainVolume + Random.Range(-_GrainVolumeRandom, _GrainVolumeRandom), 0, 3);
    }


    public void GrainNotPlaying(Grain grain)
    {
        //grain.SetActive(false);
        _GrainsFinished.Add(grain);
        _GrainsPlaying.Remove(grain);
    }


    public class GrainData
    {
        public Vector3 objectPosition;
        public Transform objectParent;
        public Vector3 objectVelocity;
        public float objectMass;
        public AudioClip audioClip;

        public int offset;
        public float grainDuration;
        public float grainPos;
        public float grainPitch;
        public float grainVolume;

        public GrainData() { }
        public GrainData(Vector3 position, Transform parent, Vector3 velocity, float mass, AudioClip grainAudioClip,
            float durationInMS, int grainOffsetInSamples, float playheadPosition, float pitch, float volume)
        {
            objectPosition = position;
            objectParent = parent;
            objectVelocity = velocity;
            objectMass = mass;
            audioClip = grainAudioClip;
            offset = grainOffsetInSamples;
            grainDuration = durationInMS;
            grainPos = playheadPosition;
            grainPitch = pitch;
            grainVolume = volume;
        }
    }



    //---------------------------------------------------------------------
    float Clamp(float val, float min, float max)
    {
        val = val > min ? val : min;
        val = val < max ? val : max;
        return val;
    }


    void CreateWindowingLookupTable()
    {
        _Window = new float[512];

        for (int i = 0; i < _Window.Length; i++)
        {
            _Window[i] = 0.5f * (1 - Mathf.Cos(2 * Mathf.PI * i / _Window.Length));
        }
    }
}