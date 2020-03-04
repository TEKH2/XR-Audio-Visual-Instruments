using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum LoadTiming
{
    ASync,
    Immediate,
}

[System.Serializable]
public class SceneLoadData
{
    public string _SceneName;
    public LoadSceneMode _LoadSceneMode = LoadSceneMode.Additive;
    public LoadTiming _LoadTiming = LoadTiming.ASync;
    public UnloadSceneOptions _UnloadOptions = UnloadSceneOptions.None;
    public bool _DoNotUnload = false;
    public bool _Loaded = false;
    public bool _MustLoad = false;
}


public class SceneLoadManager : MonoBehaviour
{
    public static SceneLoadManager Instance;

    public SceneLoadData[] _SceneLoadData;
    Dictionary<string, SceneLoadData> SceneLoadDict = new Dictionary<string, SceneLoadData>();
    Queue<string> _LoadedQueue = new Queue<string>();
    public int _LimitLoadedScenes = 0;
    public int _LoadSceneCount = 0;

    public bool _UseDebugInput = false;

    #region UNITY METHODS
    private void Awake()
    {
        // Destroy if one already exists
        if(SceneLoadManager.Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        // Set singleton
        Instance = this;

        // Set to to no destroy
        DontDestroyOnLoad(gameObject);

        // Hook up event call backs from scene manager
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;

        // Init dictionary
        for (int i = 0; i < _SceneLoadData.Length; i++)
        {
            SceneLoadDict.Add(_SceneLoadData[i]._SceneName, _SceneLoadData[i]);
        }

        for (int i = 0; i < _SceneLoadData.Length; i++)
        {
            if (_SceneLoadData[i]._MustLoad)
                LoadSceneData(_SceneLoadData[i]);
        }
    }

    void Update()
    {
        #region DEBUG INPUT
        if (_UseDebugInput)
        {
            if (Input.GetKey(KeyCode.LeftShift))
            {
                if (Input.GetKeyDown(KeyCode.Alpha1))
                {
                    UnloadSceneData(_SceneLoadData[1]);
                }
                else if (Input.GetKeyDown(KeyCode.Alpha2))
                {
                    UnloadSceneData(_SceneLoadData[2]);
                }
                else if (Input.GetKeyDown(KeyCode.Alpha3))
                {
                    UnloadSceneData(_SceneLoadData[3]);
                }
                else if (Input.GetKeyDown(KeyCode.Alpha0))
                {
                    UnloadSceneData(_SceneLoadData[0]);
                }
            }
            else
            {
                if (Input.GetKeyDown(KeyCode.Alpha1))
                {
                    LoadSceneData(_SceneLoadData[1]);
                }
                else if (Input.GetKeyDown(KeyCode.Alpha2))
                {
                    LoadSceneData(_SceneLoadData[2]);
                }
                else if (Input.GetKeyDown(KeyCode.Alpha3))
                {
                    LoadSceneData(_SceneLoadData[3]);
                }
                else if (Input.GetKeyDown(KeyCode.Alpha0))
                {
                    LoadSceneData(_SceneLoadData[0]);
                }
            }
        }
        #endregion
    }
    #endregion

    #region CALLBACKS
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log("OnSceneLoaded: " + scene.name);

        SceneLoadDict[scene.name]._Loaded = true;

        if (!SceneLoadDict[scene.name]._DoNotUnload)
            _LoadedQueue.Enqueue(scene.name);

        if (_LimitLoadedScenes != 0 && _LoadedQueue.Count > _LimitLoadedScenes)
        {
            string sceneToUnload = _LoadedQueue.Dequeue();
            print("Maximum number of scenes loaded. Unloading scene from queue: " + sceneToUnload);
            UnloadSceneData(SceneLoadDict[sceneToUnload]);
        }
    }

    void OnSceneUnloaded(Scene scene)
    {
        Debug.Log("OnSceneUnloaded: " + scene.name);

        SceneLoadDict[scene.name]._Loaded = false;
    }
    #endregion

    #region LOAD AND UNLOAD
    public void LoadSceneData(int index)
    {
        LoadSceneData(_SceneLoadData[index]);
    }

    public void LoadSceneData(SceneLoadData data)
    {
        // Don't load if scene already loaded
        if (SceneManager.GetSceneByName(data._SceneName).isLoaded)
        {
            print("Scene already loaded - " + data._SceneName);
            return;
        }

        _LoadSceneCount++;

        if (data._LoadTiming == LoadTiming.ASync)
        {
            StartCoroutine(LoadYourAsyncScene(data));
        }
        else
        {
            SceneManager.LoadScene(data._SceneName, data._LoadSceneMode);
        }
    }

    IEnumerator LoadYourAsyncScene(SceneLoadData data)
    {
        print("Loading - " +  data._SceneName);

        // The Application loads the Scene in the background as the current Scene runs.
        // This is particularly good for creating loading screens.
        // You could also load the Scene by using sceneBuildIndex. In this case Scene2 has
        // a sceneBuildIndex of 1 as shown in Build Settings.

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(data._SceneName, data._LoadSceneMode);

        // Wait until the asynchronous scene fully loads
        while (!asyncLoad.isDone)
        {
            print("Loading...");
            yield return null;
        }

        print("Loading complete.");
    }

    public void UnloadSceneData(int index)
    {
        UnloadSceneData(_SceneLoadData[index]);
    }

    void UnloadSceneData(SceneLoadData data)
    {
        // don't unload if its not loaded or is a 'do no unload'
        if (data._DoNotUnload || !data._Loaded)
        {
            print("Cannot unload scene.: " + data._SceneName);
            return;
        }

        StartCoroutine(UnloadYourAsyncScene(data));
    }

    IEnumerator UnloadYourAsyncScene(SceneLoadData data)
    {
        print("Unloading - " + data._SceneName);

        AsyncOperation asyncLoad = SceneManager.UnloadSceneAsync(data._SceneName, data._UnloadOptions);

        // Wait until the asynchronous scene fully loads
        while (!asyncLoad.isDone)
        {
            print("Unloading...");
            yield return null;
        }

        print("Unloading complete.");
    }
    #endregion
}