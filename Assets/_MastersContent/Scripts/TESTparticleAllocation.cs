using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TESTparticleAllocation : MonoBehaviour
{

    public GameObject _ParticlePrefab;
    private GameObject _ParticleObject;
    public ParticleSystem _ParticleSystem;
    public int _NumberOfParticles;

    private ParticleSystem.Particle[] _TempParticles;

    [SerializeField]
    private Vector3[] _StartPositions;

    [SerializeField]
    private Vector3[] _ParticlePositions;

    // Start is called before the first frame update
    void Start()
    {
        _ParticleObject = Instantiate(_ParticlePrefab, new Vector3(0, 0, 0), Quaternion.identity);
        _ParticleSystem = _ParticleObject.GetComponent<ParticleSystem>();
        ParticleSystem.NoiseModule noise = _ParticleSystem.noise;
        noise.enabled = false;

        ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams();

        _StartPositions = new Vector3[_NumberOfParticles];

        for (int i = 0; i < _StartPositions.Length; i++)
        {
            _StartPositions[i] = new Vector3(i, 0, 0);
            emitParams.position = _StartPositions[i];
            _ParticleSystem.Emit(emitParams, 1);
        }
    }

    // Update is called once per frame
    void Update()
    {
        _TempParticles = new ParticleSystem.Particle[_ParticleSystem.particleCount];
        _ParticleSystem.GetParticles(_TempParticles);

        if (Input.GetKeyDown(","))
        {
            int randomParticle = Random.Range(0, _TempParticles.Length);
            Debug.Log("DELETING PARTICLE NUMBER: " + randomParticle);
            _TempParticles[randomParticle].remainingLifetime = -1;
            _TempParticles[randomParticle].position = new Vector3(-2, 0, 0);
            _ParticleSystem.SetParticles(_TempParticles);
        }

        if (Input.GetKeyDown("."))
        {
            if (_ParticleSystem.particleCount < _NumberOfParticles)
            {
                Debug.Log("ADDING PARTICLE");
            }
        }

        _ParticlePositions = new Vector3[_ParticleSystem.particleCount];

        _ParticleSystem.GetParticles(_TempParticles);

        for (int i = 0; i < _ParticleSystem.particleCount; i++)
        {
            _ParticlePositions[i] = _TempParticles[i].position;
        }
    }
}
