using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using Random = UnityEngine.Random;
using System.Linq;

/*
public class Granulator : MonoBehaviour
{
    public AudioClipLibrary _AudioClipLibrary;

    // ------------------------------------ PARTICLE VARS
    public ParticleManager _ParticleManager;
    private ParticleSystem.Particle _TempParticle;
    private ParticleSystem.Particle[] _Particles;

    // ------------------------------------ GRAIN POOLING
    public GameObject _GrainParentTform;
    public GameObject _GrainPrefab;
    public int _MaxGrains = 100;

    [Range(1.0f, 1000f)]
    public int _TimeBetweenGrains = 20;          // ms
    [Range(0.0f, 1000f)]
    public int _TimeBetweenGrainsRandom = 0;       // ms

    [Header("Emission properties")]
    public GrainEmissionProps _EmitGrainProps;
    public GrainEmissionProps _CollisionGrainProps;

    [Space]
    public int _CollisionGrainBurst = 5;
    [Range(1.0f, 1000f)]
    public int _CollisionDensity = 40;                  // ms

    [Space]
    private int _SamplesSinceLastGrain;
    private int _EmitterGrainsLastUpdate = 0;

    private Rigidbody _RigidBody;
    public float _InheritVelocityScalar = .5f;

    public bool _EnableParticleCollisions = false;
    [Range(0.0f, 10f)]
    public float _Mass = 0;       

    private List<GrainData> _GrainQueue;
    private List<GrainData> _CollisionQueue;

    private List<Grain> _GrainsActive;
    private List<Grain> _GrainsInactive;


    private const int _SampleRate = 44100;
    private float[] _Window;

    //DrawMeshInstanced _DrawMeshInstanced;


    //---------------------------------------------------------------------
    private void Start()
    {
        _AudioClipLibrary.Initialize();

        _ParticleManager.Initialise(this);

        CreateWindowingLookupTable();

        this.gameObject.AddComponent<AudioSource>();
        _RigidBody = this.GetComponent<Rigidbody>();

        _GrainsActive = new List<Grain>();
        _GrainsInactive = new List<Grain>();
        _GrainQueue = new List<GrainData>();
        _CollisionQueue = new List<GrainData>();

        
        for (int i = 0; i < _MaxGrains; i++)
        {
            GameObject go = Instantiate(_GrainPrefab);
            go.SetActive(true);
            Grain grain = go.GetComponent<Grain>();
            grain.transform.parent = _GrainParentTform.transform;
            _GrainsInactive.Add(grain);
        }

        _SamplesSinceLastGrain = 0;
    }

    void Update()
    {
        //---------------------------------------------------------------------
        // UPDATE MAINTAINANCE   TODO - Move all clamping to properties
        //---------------------------------------------------------------------
        int samplesLastUpdate = (int)(Time.deltaTime * _SampleRate);


        // Update particle manager
        _ParticleManager.SetMass(_Mass);
        _ParticleManager.EnableCollisions(_EnableParticleCollisions);


        // Remove finished grains from Playing List and add them to Finished list
        for (int i = _GrainsActive.Count - 1; i >= 0; i--)
        {
            Grain playingGrain = _GrainsActive[i];

            if (!playingGrain._IsPlaying)
            {
                _GrainsActive.RemoveAt(i);
                _GrainsInactive.Add(playingGrain);
            }
        }


        //---------------------------------------------------------------------
        // EMITTER GRAIN TIMING GENERATION
        //---------------------------------------------------------------------
        // Emitter grains are those which play back constantly throughout each
        // update, as opposed to being trigged from single events.
        // This function creates the timing of emitter grains to be played
        // over the next update.

        int emitterGrainsToPlay = 0;
        int firstGrainOffset = 0;
        int densityInSamples = (_TimeBetweenGrains + Random.Range(0, _TimeBetweenGrainsRandom)) * (_SampleRate / 1000);

        // If no sample was played last update, adding the previous update's samples count,
        // AFTER the update is complete, should correctly accumulate the samples since the
        // last grain playback. Otherwise, if a sample WAS played last update, the sample
        // offset of that grain is subtracted from the total samples of the previous update.
        // This provides the correct number of samples since the most recent grain was started.
        if (_EmitterGrainsLastUpdate == 0)
            _SamplesSinceLastGrain += samplesLastUpdate;
        else
            _SamplesSinceLastGrain = samplesLastUpdate - _SamplesSinceLastGrain;

        // If the density of grains minus samples since last grain fits within the
        // estimated time for the this update, calculate number of grains to play this update
        if (densityInSamples - _SamplesSinceLastGrain < samplesLastUpdate)
        {
            // Should always equal one or more
            // Not sure if the + 1 is correct here. Potentially introducing rounding errors?
            // Need to check
            emitterGrainsToPlay = samplesLastUpdate / densityInSamples + 1;
            
            // Create initial grain offset for this update
            firstGrainOffset = densityInSamples - _SamplesSinceLastGrain;
            
            // Hacky check to avoid offsets lower than 0 (if this occurs, something
            // isn't handled correctly. This is a precaution. Haven't properly checked this yet.
            if (firstGrainOffset < 0)
                firstGrainOffset = 0;
        }

        _EmitterGrainsLastUpdate = emitterGrainsToPlay;


        //---------------------------------------------------------------------
        // CREATE EMITTER GRAINS
        //---------------------------------------------------------------------
        // Populate grain queue with emitter grains
                
        for (int i = 0; i < emitterGrainsToPlay; i++)
        {              
            // Store duration locally because it's used twice
            float duration = _EmitGrainProps.Duration;

            // Generate new particle in trigger particle system and return it here
            ParticleSystem.Particle tempParticle = _ParticleManager.SpawnEmitterParticle(_RigidBody.velocity, duration / 1000f);

            // Calculate timing offset for grain
            int offset = firstGrainOffset + i * densityInSamples;

            // Create temporary grain data object and add it to the playback queue
            GrainData tempGrainData = new GrainData(_TempParticle.position, _GrainParentTform.transform, tempParticle.velocity + _RigidBody.velocity * _InheritVelocityScalar, _Mass,
                _EmitGrainProps._ClipIndex, duration, offset, _EmitGrainProps.Position, _EmitGrainProps.Pitch, _EmitGrainProps.Volume);

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
        
        
      

        // Profiler.BeginSample("Update 4");
        //---------------------------------------------------------------------
        // ASSIGN GRAIN QUEUE TO FREE GRAIN OBJECTS
        //---------------------------------------------------------------------      

        // print("Queue/Finished: " + _GrainQueue.Count + "   " + _GrainsFinished.Count);


        foreach (GrainData grainData in _GrainQueue)
        {
            EmitGrain(grainData);
            //if (_GrainsInactive.Count > 0)
            //{
            //    // Get first grain
            //    Grain grain = _GrainsInactive[0];

            //    // Init grain with data
            //    grain.Initialise(grainData);

            //    // Add and remove from lists
            //    _GrainsActive.Add(grain);
            //    _GrainsInactive.Remove(grain);
            //}
        }
        

       // Profiler.EndSample();

       // Profiler.BeginSample("Update 5");

        // Clears the grain queue for next update. Perhaps this might change if for some reason it's
        // better to maintain unfinished grains for the next udpate
        _GrainQueue.Clear();

      

       // Profiler.EndSample();
    }

    public void EmitGrain(GrainData grainData)
    {
        if (_GrainsInactive.Count == 0)
            print("No inactive grains, trying to spawn too quickly. Potentially boost max grains");

        // Get grain from inactive list and remove from list
        Grain grain = _GrainsInactive[0];
        _GrainsInactive.Remove(grain);

        

        // Init grain with data
        grain.Initialise(grainData, _AudioClipLibrary._ClipsDataArray[grainData._ClipIndex], _AudioClipLibrary._Clips[grainData._ClipIndex].channels, _AudioClipLibrary._Clips[grainData._ClipIndex].frequency);

        // Add grain to active list
        _GrainsActive.Add(grain);
    }

    public bool _DEBUG_NewListManager = false;

    //---------------------------------------------------------------------
    // Creates a burst of new grains on collision events
    //---------------------------------------------------------------------
    public void TriggerCollision(List<ParticleCollisionEvent> collisions, GameObject other)
    {       
        for (int i = 0; i < collisions.Count; i++)
        {
            for (int j = 0; j < _CollisionGrainBurst; j++)
            {
                // Calculate timing offset for grain
                int offset = j * _CollisionDensity * (_SampleRate / 1000);

                Vector3 pos = _GrainParentTform.transform.InverseTransformPoint(collisions[i].intersection);

                // Create temporary grain data object and add it to the playback queue
                GrainData collisionGrainData = new GrainData(pos, _GrainParentTform.transform, Vector3.zero, _Mass,
                    _CollisionGrainProps._ClipIndex, _CollisionGrainProps.Duration, offset, _CollisionGrainProps.Position, _CollisionGrainProps.Pitch, _CollisionGrainProps.Volume);

                _CollisionQueue.Add(collisionGrainData);
            }
        }
    }  

    public void GrainNotPlaying(Grain grain)
    {
        //grain.SetActive(false);
        _GrainsInactive.Add(grain);
        _GrainsActive.Remove(grain);
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

    public class GrainData
    {
        public Vector3 objectPosition;
        public Transform objectParent;
        public Vector3 objectVelocity;
        public float objectMass;

        public int offset;
        public float grainDuration;
        public float grainPos;
        public float grainPitch;
        public float grainVolume;

        public int _ClipIndex;

        public GrainData() { }
        public GrainData(Vector3 position, Transform parent, Vector3 velocity, float mass, int grainAudioClipIndex,
            float durationInMS, int grainOffsetInSamples, float playheadPosition, float pitch, float volume)
        {
            objectPosition = position;
            objectParent = parent;
            objectVelocity = velocity;
            objectMass = mass;
            _ClipIndex = grainAudioClipIndex;
            offset = grainOffsetInSamples;
            grainDuration = durationInMS;
            grainPos = playheadPosition;
            grainPitch = pitch;
            grainVolume = volume;
        }
    }
}
*/