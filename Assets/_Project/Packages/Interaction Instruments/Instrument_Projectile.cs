using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using EXP.XR;

public class Instrument_Projectile : MonoBehaviour
{
    public enum FiringMode
    {
        Automatic,
        PowerUpShot,
    }

    public FiringMode _FiringMode = FiringMode.PowerUpShot;

    public GameObject[] _ProjectilePrefabs;
    public int _SelectedPrefabIndex = 0;
    public Transform _SpawnTransform;
    public float _SpawnRatePerSecond = 3;
    float SpawnInterval { get { return 1f / _SpawnRatePerSecond; } }
    float _SpawnTimer = 0;
    int _SpawnCounter = 0;
    bool _Firing = false;
    public float _Speed = 3;

    [SerializeField]
    InputActionProperty _TriggerAction;

    [SerializeField]
    InputActionProperty _ThumbStickAction;

    bool ThumbStickUsed { get { return _ThumbStickAction.action.phase == InputActionPhase.Started || _ThumbStickAction.action.phase == InputActionPhase.Performed; } }


    Rigidbody _SpawnedPowerUpProjectileRB;
    public Vector2 _ScaleRange = new Vector2(.1f, .5f);
    public float _PowerupDuration = 3;

    // Start is called before the first frame update
    void Start()
    {
        _TriggerAction.action.started += ctx => TriggerDown();
        _TriggerAction.action.canceled += ctx => TriggerUp();

        //XRControllers.Instance._RightControllerFeatures._XRFloatDict[XRFloats.Trigger].OnValueOne.AddListener(() => TriggerDown());
        //XRControllers.Instance._RightControllerFeatures._XRFloatDict[XRFloats.Trigger].OnValueZero.AddListener(() => TriggerUp());
    }


    // Update is called once per frame
    void Update()
    {
        //if (Input.GetMouseButtonDown(0))
        //    TriggerDown();
        //else if (Input.GetMouseButtonUp(0))
        //    TriggerUp();


        if (_Firing && ThumbStickUsed)
            TriggerUp();

        if (_FiringMode == FiringMode.Automatic)
        {
            if (_Firing)
            {
                if (_SpawnTimer >= SpawnInterval)
                {
                    GameObject newGO = InstantiateProjectile(_SpawnTransform.localScale);

                    Rigidbody rb = newGO.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.AddForce(_SpawnTransform.forward * _Speed, ForceMode.VelocityChange);
                    }

                    _SpawnTimer -= SpawnInterval;
                }

                _SpawnTimer += Time.deltaTime;
            }
        }
        else if (_FiringMode == FiringMode.PowerUpShot)
        {
            if (_Firing && _SpawnedPowerUpProjectileRB != null)
            {
                _SpawnTimer += Time.deltaTime;

                float scale = Mathf.Lerp(_ScaleRange.x, _ScaleRange.y, Mathf.Clamp01(_SpawnTimer / _PowerupDuration));
                _SpawnedPowerUpProjectileRB.transform.localPosition = new Vector3(0, 0, scale * .5f); 
                _SpawnedPowerUpProjectileRB.transform.localScale = Vector3.one * scale;
            }
        }
    }

    public void TriggerDown()
    {
        print("Trigger down");

        if (ThumbStickUsed)
            return;


        switch (_FiringMode)
        {
            case FiringMode.Automatic:
                _SpawnTimer = SpawnInterval;
                break;
            case FiringMode.PowerUpShot:
                print("Starting powerup shot");
                GameObject projectile = InstantiateProjectile(Vector3.one * _ScaleRange.x);
                _SpawnedPowerUpProjectileRB = projectile.GetComponent<Rigidbody>();
                print("_SpawnedPowerUpProjectileRB spawned");
                _SpawnedPowerUpProjectileRB.isKinematic = true;
                _SpawnedPowerUpProjectileRB.transform.SetParent(_SpawnTransform);
                _SpawnedPowerUpProjectileRB.transform.localPosition = new Vector3(0, 0, _ScaleRange.x * .5f);
                _SpawnTimer = 0;
                break;
        }

        _Firing = true;
    }

    public void TriggerUp()
    {
        if (_SpawnedPowerUpProjectileRB != null)
        {
            if (_FiringMode == FiringMode.Automatic)
            {
                _SpawnTimer = 0;
            }
            else if (_FiringMode == FiringMode.PowerUpShot)
            {
                _SpawnedPowerUpProjectileRB.transform.SetParent(null);
                _SpawnedPowerUpProjectileRB.isKinematic = false;
                _SpawnedPowerUpProjectileRB.AddForce(_SpawnTransform.forward * _Speed, ForceMode.VelocityChange);
            }
        }

        _Firing = false;
    }

    GameObject InstantiateProjectile(Vector3 scale)
    {
        // Spawn GO and set transform
        GameObject newGO = Instantiate(_ProjectilePrefabs[_SelectedPrefabIndex]);
        newGO.transform.position = _SpawnTransform.position;
        newGO.transform.localScale = scale;
        newGO.transform.rotation = _SpawnTransform.rotation;
        newGO.name = "Spawned Projectile " + _SpawnCounter;

        return newGO;
    }
}
