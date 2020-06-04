using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public class QuadrantSystem<T>
{
    public float _CellSize = .5f;
    int _YMulti = 1000;
    int _ZMulti = 100000;

    Dictionary<int, List<T>> _HashedCellDictionary = new Dictionary<int, List<T>>();

    public QuadrantSystem(float cellSize, int yMulti = 1000, int zMulti = 100000)
    {
        _CellSize = cellSize;
        _YMulti = yMulti;
        _ZMulti = zMulti;
    }

    public List<List<T>> GetAllAdjascentQuads(Vector3 pos)
    {
        List<List<T>> AdjascentQuads = new List<List<T>>();

        int hash = GetPositionHashMapKey(pos);

        if (_HashedCellDictionary.ContainsKey(hash)) AdjascentQuads.Add(_HashedCellDictionary[hash]);

        if (_HashedCellDictionary.ContainsKey(hash - 1)) AdjascentQuads.Add(_HashedCellDictionary[hash - 1]);
        if (_HashedCellDictionary.ContainsKey(hash + 1)) AdjascentQuads.Add(_HashedCellDictionary[hash + 1]);

        if (_HashedCellDictionary.ContainsKey(hash - _YMulti)) AdjascentQuads.Add(_HashedCellDictionary[hash - _YMulti]);
        if (_HashedCellDictionary.ContainsKey(hash + _YMulti)) AdjascentQuads.Add(_HashedCellDictionary[hash + _YMulti]);

        if (_HashedCellDictionary.ContainsKey(hash - 1 - _YMulti)) AdjascentQuads.Add(_HashedCellDictionary[hash - 1 - _YMulti]);
        if (_HashedCellDictionary.ContainsKey(hash + 1 - _YMulti)) AdjascentQuads.Add(_HashedCellDictionary[hash + 1 - _YMulti]);

        if (_HashedCellDictionary.ContainsKey(hash - 1 + _YMulti)) AdjascentQuads.Add(_HashedCellDictionary[hash - 1 + _YMulti]);
        if (_HashedCellDictionary.ContainsKey(hash + 1 + _YMulti)) AdjascentQuads.Add(_HashedCellDictionary[hash + 1 + _YMulti]);


        if (_HashedCellDictionary.ContainsKey(hash - _ZMulti)) AdjascentQuads.Add(_HashedCellDictionary[hash - _ZMulti]);

        if (_HashedCellDictionary.ContainsKey(hash - 1 - _ZMulti)) AdjascentQuads.Add(_HashedCellDictionary[hash - 1 - _ZMulti]);
        if (_HashedCellDictionary.ContainsKey(hash + 1 - _ZMulti)) AdjascentQuads.Add(_HashedCellDictionary[hash + 1 - _ZMulti]);

        if (_HashedCellDictionary.ContainsKey(hash - _YMulti - _ZMulti)) AdjascentQuads.Add(_HashedCellDictionary[hash - _YMulti - _ZMulti]);
        if (_HashedCellDictionary.ContainsKey(hash + _YMulti - _ZMulti)) AdjascentQuads.Add(_HashedCellDictionary[hash + _YMulti - _ZMulti]);

        if (_HashedCellDictionary.ContainsKey(hash - 1 - _YMulti - _ZMulti)) AdjascentQuads.Add(_HashedCellDictionary[hash - 1 - _YMulti - _ZMulti]);
        if (_HashedCellDictionary.ContainsKey(hash + 1 - _YMulti - _ZMulti)) AdjascentQuads.Add(_HashedCellDictionary[hash + 1 - _YMulti - _ZMulti]);

        if (_HashedCellDictionary.ContainsKey(hash - 1 + _YMulti - _ZMulti)) AdjascentQuads.Add(_HashedCellDictionary[hash - 1 + _YMulti - _ZMulti]);
        if (_HashedCellDictionary.ContainsKey(hash + 1 + _YMulti - _ZMulti)) AdjascentQuads.Add(_HashedCellDictionary[hash + 1 + _YMulti - _ZMulti]);



        if (_HashedCellDictionary.ContainsKey(hash + _ZMulti)) AdjascentQuads.Add(_HashedCellDictionary[hash + _ZMulti]);

        if (_HashedCellDictionary.ContainsKey(hash - 1 + _ZMulti)) AdjascentQuads.Add(_HashedCellDictionary[hash - 1 + _ZMulti]);
        if (_HashedCellDictionary.ContainsKey(hash + 1 + _ZMulti)) AdjascentQuads.Add(_HashedCellDictionary[hash + 1 + _ZMulti]);

        if (_HashedCellDictionary.ContainsKey(hash - _YMulti + _ZMulti)) AdjascentQuads.Add(_HashedCellDictionary[hash - _YMulti + _ZMulti]);
        if (_HashedCellDictionary.ContainsKey(hash + _YMulti + _ZMulti)) AdjascentQuads.Add(_HashedCellDictionary[hash + _YMulti + _ZMulti]);

        if (_HashedCellDictionary.ContainsKey(hash - 1 - _YMulti + _ZMulti)) AdjascentQuads.Add(_HashedCellDictionary[hash - 1 - _YMulti + _ZMulti]);
        if (_HashedCellDictionary.ContainsKey(hash + 1 - _YMulti + _ZMulti)) AdjascentQuads.Add(_HashedCellDictionary[hash + 1 - _YMulti + _ZMulti]);

        if (_HashedCellDictionary.ContainsKey(hash - 1 + _YMulti + _ZMulti)) AdjascentQuads.Add(_HashedCellDictionary[hash - 1 + _YMulti + _ZMulti]);
        if (_HashedCellDictionary.ContainsKey(hash + 1 + _YMulti + _ZMulti)) AdjascentQuads.Add(_HashedCellDictionary[hash + 1 + _YMulti + _ZMulti]);



        return AdjascentQuads;
    }

    public List<T> GetAllAdjascentObjects(Vector3 pos, bool excludeCurrentQuadrant = false)
    {
        List<List<T>> AdjascentQuads = new List<List<T>>();

        int hash = GetPositionHashMapKey(pos);

        if (!excludeCurrentQuadrant)
        {
            if (_HashedCellDictionary.ContainsKey(hash)) AdjascentQuads.Add(_HashedCellDictionary[hash]);
        }

        if (_HashedCellDictionary.ContainsKey(hash - 1)) AdjascentQuads.Add(_HashedCellDictionary[hash - 1]);
        if (_HashedCellDictionary.ContainsKey(hash + 1)) AdjascentQuads.Add(_HashedCellDictionary[hash + 1]);

        if (_HashedCellDictionary.ContainsKey(hash - _YMulti)) AdjascentQuads.Add(_HashedCellDictionary[hash - _YMulti]);
        if (_HashedCellDictionary.ContainsKey(hash + _YMulti)) AdjascentQuads.Add(_HashedCellDictionary[hash + _YMulti]);

        if (_HashedCellDictionary.ContainsKey(hash - 1 - _YMulti)) AdjascentQuads.Add(_HashedCellDictionary[hash - 1 - _YMulti]);
        if (_HashedCellDictionary.ContainsKey(hash + 1 - _YMulti)) AdjascentQuads.Add(_HashedCellDictionary[hash + 1 - _YMulti]);

        if (_HashedCellDictionary.ContainsKey(hash - 1 + _YMulti)) AdjascentQuads.Add(_HashedCellDictionary[hash - 1 + _YMulti]);
        if (_HashedCellDictionary.ContainsKey(hash + 1 + _YMulti)) AdjascentQuads.Add(_HashedCellDictionary[hash + 1 + _YMulti]);


        if (_HashedCellDictionary.ContainsKey(hash - _ZMulti)) AdjascentQuads.Add(_HashedCellDictionary[hash - _ZMulti]);

        if (_HashedCellDictionary.ContainsKey(hash - 1 - _ZMulti)) AdjascentQuads.Add(_HashedCellDictionary[hash - 1 - _ZMulti]);
        if (_HashedCellDictionary.ContainsKey(hash + 1 - _ZMulti)) AdjascentQuads.Add(_HashedCellDictionary[hash + 1 - _ZMulti]);

        if (_HashedCellDictionary.ContainsKey(hash - _YMulti - _ZMulti)) AdjascentQuads.Add(_HashedCellDictionary[hash - _YMulti - _ZMulti]);
        if (_HashedCellDictionary.ContainsKey(hash + _YMulti - _ZMulti)) AdjascentQuads.Add(_HashedCellDictionary[hash + _YMulti - _ZMulti]);

        if (_HashedCellDictionary.ContainsKey(hash - 1 - _YMulti - _ZMulti)) AdjascentQuads.Add(_HashedCellDictionary[hash - 1 - _YMulti - _ZMulti]);
        if (_HashedCellDictionary.ContainsKey(hash + 1 - _YMulti - _ZMulti)) AdjascentQuads.Add(_HashedCellDictionary[hash + 1 - _YMulti - _ZMulti]);

        if (_HashedCellDictionary.ContainsKey(hash - 1 + _YMulti - _ZMulti)) AdjascentQuads.Add(_HashedCellDictionary[hash - 1 + _YMulti - _ZMulti]);
        if (_HashedCellDictionary.ContainsKey(hash + 1 + _YMulti - _ZMulti)) AdjascentQuads.Add(_HashedCellDictionary[hash + 1 + _YMulti - _ZMulti]);



        if (_HashedCellDictionary.ContainsKey(hash + _ZMulti)) AdjascentQuads.Add(_HashedCellDictionary[hash + _ZMulti]);

        if (_HashedCellDictionary.ContainsKey(hash - 1 + _ZMulti)) AdjascentQuads.Add(_HashedCellDictionary[hash - 1 + _ZMulti]);
        if (_HashedCellDictionary.ContainsKey(hash + 1 + _ZMulti)) AdjascentQuads.Add(_HashedCellDictionary[hash + 1 + _ZMulti]);

        if (_HashedCellDictionary.ContainsKey(hash - _YMulti + _ZMulti)) AdjascentQuads.Add(_HashedCellDictionary[hash - _YMulti + _ZMulti]);
        if (_HashedCellDictionary.ContainsKey(hash + _YMulti + _ZMulti)) AdjascentQuads.Add(_HashedCellDictionary[hash + _YMulti + _ZMulti]);

        if (_HashedCellDictionary.ContainsKey(hash - 1 - _YMulti + _ZMulti)) AdjascentQuads.Add(_HashedCellDictionary[hash - 1 - _YMulti + _ZMulti]);
        if (_HashedCellDictionary.ContainsKey(hash + 1 - _YMulti + _ZMulti)) AdjascentQuads.Add(_HashedCellDictionary[hash + 1 - _YMulti + _ZMulti]);

        if (_HashedCellDictionary.ContainsKey(hash - 1 + _YMulti + _ZMulti)) AdjascentQuads.Add(_HashedCellDictionary[hash - 1 + _YMulti + _ZMulti]);
        if (_HashedCellDictionary.ContainsKey(hash + 1 + _YMulti + _ZMulti)) AdjascentQuads.Add(_HashedCellDictionary[hash + 1 + _YMulti + _ZMulti]);


        List<T> adjascentFixtures = new List<T>();
        foreach (List<T> list in AdjascentQuads)
        {
            foreach (T t in list)
            {
                adjascentFixtures.Add(t);
            }
        }

        return adjascentFixtures;
    }

    public List<T> _FoundList = new List<T>();

    void AddHashToFoundList(int hash)
    {
        if (_HashedCellDictionary.ContainsKey(hash))
        {
            foreach (T t in _HashedCellDictionary[hash])
            {
                _FoundList.Add(t);                
            }
        }
    }

    public List<T> GetAllAdjascentObjectsSingleList(Vector3 pos, bool excludeCurrentQuadrant = false)
    {
        _FoundList.Clear();
        int hash = GetPositionHashMapKey(pos);

        if (!excludeCurrentQuadrant)
        {
            AddHashToFoundList(hash);
        }

        AddHashToFoundList(hash - 1);
        AddHashToFoundList(hash + 1);

        AddHashToFoundList(hash - _YMulti);
        AddHashToFoundList(hash + _YMulti);

        AddHashToFoundList(hash - 1 - _YMulti);
        AddHashToFoundList(hash + 1 - _YMulti);

        AddHashToFoundList(hash - 1 + _YMulti);
        AddHashToFoundList(hash + 1 + _YMulti);


        AddHashToFoundList(hash - _ZMulti);

        AddHashToFoundList(hash - 1 - _ZMulti);
        AddHashToFoundList(hash + 1 - _ZMulti);

        AddHashToFoundList(hash - _YMulti - _ZMulti);
        AddHashToFoundList(hash + _YMulti - _ZMulti);

        AddHashToFoundList(hash - 1 - _YMulti - _ZMulti);
        AddHashToFoundList(hash + 1 - _YMulti - _ZMulti);

        AddHashToFoundList(hash - 1 + _YMulti - _ZMulti);
        AddHashToFoundList(hash + 1 + _YMulti - _ZMulti);



        AddHashToFoundList(hash + _ZMulti);

        AddHashToFoundList(hash - 1 + _ZMulti);
        AddHashToFoundList(hash + 1 + _ZMulti);
        AddHashToFoundList(hash - _YMulti + _ZMulti);
        AddHashToFoundList(hash + _YMulti + _ZMulti);

        AddHashToFoundList(hash - 1 - _YMulti + _ZMulti);
        AddHashToFoundList(hash + 1 - _YMulti + _ZMulti);

        AddHashToFoundList(hash - 1 + _YMulti + _ZMulti);
        AddHashToFoundList(hash + 1 + _YMulti + _ZMulti);

        return _FoundList;
    }

    public List<T> GetListInQuad(Vector3 pos)
    {
        return _HashedCellDictionary[GetPositionHashMapKey(pos)];
    }

    int GetPositionHashMapKey(Vector3 pos)
    {
        return (int)(Mathf.Floor(pos.x / _CellSize) +
            _YMulti * Mathf.Floor(pos.y / _CellSize) +
            _ZMulti * Mathf.Floor(pos.z / _CellSize));
    }

    Vector3 GetQuadrantCenterPos(Vector3 pos)
    {
        return new Vector3(Mathf.Floor(pos.x / _CellSize) + (_CellSize * .5f),
                _YMulti * Mathf.Floor(pos.y / _CellSize) + (_CellSize * .5f),
                _ZMulti * Mathf.Floor(pos.z / _CellSize) + (_CellSize * .5f));
    }

    public void Add(T obj, Vector3 pos)
    {
        int hash = GetPositionHashMapKey(pos);

        if(_HashedCellDictionary.ContainsKey(hash))
        {
            _HashedCellDictionary[hash].Add(obj);
        }
        else
        {
            List<T> list = new List<T>();
            list.Add(obj);
            _HashedCellDictionary.Add(hash, list);
        }
    }

    public void DrawGizmos(Vector3 pos)
    {
        Vector3 cubePos = GetQuadrantCenterPos(pos);
        Gizmos.DrawCube(cubePos, Vector3.one * _CellSize);       
    }
}