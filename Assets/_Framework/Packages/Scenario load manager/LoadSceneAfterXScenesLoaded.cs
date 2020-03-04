using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadSceneAfterXScenesLoaded : MonoBehaviour
{
    SceneLoadManager _SceneManager;
    int _TriggerWhenXScenesAreLoaded = 3;

    public SceneLoadData _SceneData;
    

    // Start is called before the first frame update
    void Start()
    {
        _SceneManager = FindObjectOfType<SceneLoadManager>();
    }

    // Update is called once per frame
    void Update()
    {
        if(_SceneManager._LoadSceneCount > _TriggerWhenXScenesAreLoaded)
        {
            _SceneManager.LoadSceneData(_SceneData);
            _SceneManager._LoadSceneCount = 0;
            enabled = false;
        }
    }
}
