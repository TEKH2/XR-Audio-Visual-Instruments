using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaterialSetColor : MonoBehaviour
{
    Renderer _Rend;
    public Color[] _Cols;
    public string _ColString = "_BaseColor";

    bool _Blending = false;

    Color _CurrentCol;
    Color _TargetCol;

    // Start is called before the first frame update
    void Start()
    {
        _Rend = GetComponent<Renderer>();
    }

    // Update is called once per frame
    public void SetCol(int index)
    {
        _Rend.material.SetColor(_ColString, _Cols[index]);
    }
}
