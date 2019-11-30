using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneLoaderManagerProxy : MonoBehaviour
{
    public void LoadScene(int index)
    {
        SceneLoadManager.Instance.LoadSceneData(index);
    }

    public void UnloadScene(int index)
    {
        SceneLoadManager.Instance.UnloadSceneData(index);
    }
}
