using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EXP.XR;

public class Instrument_Projectile : MonoBehaviour
{
    public GameObject[] _ProjectilePrefabs;
    public int _SelectedPrefabIndex = 0;

    public Transform _SpawnTransform;

    public float _SpawnRatePerSecond = 3;
    float SpawnInterval { get { return 1f / _SpawnRatePerSecond; } }

    float _SpawnTimer = 0;

    int _SpawnCounter = 0;

    bool _Firing = false;

    public float _Speed = 3;


    // Start is called before the first frame update
    void Start()
    {
        XRControllers.Instance._RightControllerFeatures._XRFloatDict[XRFloats.Trigger].OnValueUpdate.AddListener((float f) => _Firing = f>.8f);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
            TriggerDown();
        else if (Input.GetMouseButtonUp(0))
            TriggerUp();
           


        if (_Firing)
        {
            if (_SpawnTimer >= SpawnInterval)
            {
                // Spawn GO and set transform
                GameObject newGO = Instantiate(_ProjectilePrefabs[_SelectedPrefabIndex]);
                newGO.transform.position = _SpawnTransform.position;
                newGO.transform.localScale = _SpawnTransform.localScale;
                newGO.transform.rotation = _SpawnTransform.rotation;
                newGO.name = "Spawned Projectile " + _SpawnCounter;

                Rigidbody rb = newGO.GetComponent<Rigidbody>();
                if(rb != null)
                {
                    rb.AddForce(_SpawnTransform.forward * _Speed, ForceMode.VelocityChange);
                }

                _SpawnTimer -= SpawnInterval;
            }

            _SpawnTimer += Time.deltaTime;
        }
    }

    public void TriggerDown()
    {
        _Firing = true;
        _SpawnTimer = SpawnInterval;
    }

    public void TriggerUp()
    {
        _Firing = false;
        _SpawnTimer = 0;
    }
}
