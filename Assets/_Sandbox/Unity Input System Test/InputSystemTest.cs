using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using EXP.XR;

public class InputSystemTest : MonoBehaviour
{
    public Vector2 _LeftThumb;
    public Vector2 _RightThumb;

    public bool _LeftA;
    public bool _RightA;

    public bool _Fire;

    PlayerControls _PlayerControls;

    // Start is called before the first frame update
    void Start()
    {
        _PlayerControls = new PlayerControls();
        _PlayerControls.devices = InputSystem.devices;
        _PlayerControls.Enable();

        _PlayerControls.Player.Fire.started += ctx => print("FIRE started event");
        _PlayerControls.Player.Fire.performed += ctx => print("FIRE performed event");
        _PlayerControls.Player.Fire.canceled += ctx => print("FIRE canceled event");

        _PlayerControls.Player.Move.started += ctx => print("Move started event");
        _PlayerControls.Player.Move.performed += ctx => print("Move performed event");
        _PlayerControls.Player.Move.canceled += ctx => print("Move canceled event");

        _PlayerControls.Player.RightThumbstick.started += ctx => _RightThumb = ctx.ReadValue<Vector2>(); print("Started... " + _RightThumb);
        _PlayerControls.Player.RightThumbstick.performed += ctx => _RightThumb = ctx.ReadValue<Vector2>(); print("performed... " + _RightThumb);
        _PlayerControls.Player.RightThumbstick.canceled += ctx => _RightThumb = ctx.ReadValue<Vector2>(); print("cacnelled... " + _RightThumb);


    }

    void Update()
    {
      
    }

    public void OnFire(InputAction.CallbackContext context)
    {
        print("Fire " + context.phase);
        // 'Use' code here.
    }

    public void OnRightTumb(InputValue value)
    {
        _RightThumb = value.Get<Vector2>();
    }

    public void OnLeftTumb(InputValue value)
    {
        _LeftThumb = value.Get<Vector2>();
    }

    public void OnRightA()
    {
        _RightA = true;
    }

    public void OnLeftA()
    {
        _LeftA = true;
    }
}
