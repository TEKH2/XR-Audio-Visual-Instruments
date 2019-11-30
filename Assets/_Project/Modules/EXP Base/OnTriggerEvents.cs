using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class OnTriggerEvents : MonoBehaviour
{
    public string _TagToTriggerOn;

    public UnityEvent _OnTriggerEnterEvent;
    public UnityEvent _OnTriggerStayEvent;
    public UnityEvent _OnTriggerExitEvent;

    public bool _OnlyTriggerOnce = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(_TagToTriggerOn))
            return;

        if (_OnTriggerEnterEvent != null) _OnTriggerEnterEvent.Invoke();

        if (_OnlyTriggerOnce)
            Destroy(this);
    }

    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag(_TagToTriggerOn))
            return;

        if (_OnTriggerStayEvent != null) _OnTriggerStayEvent.Invoke();

        if (_OnlyTriggerOnce)
            Destroy(this);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(_TagToTriggerOn))
            return;

        if (_OnTriggerExitEvent != null) _OnTriggerExitEvent.Invoke();

        if (_OnlyTriggerOnce)
            Destroy(this);
    }
}
